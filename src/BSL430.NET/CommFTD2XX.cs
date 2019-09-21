/*
    BSL430.NET - MSP430 bootloader (BSL) .NET toolchain
    Original source by: Jakub Parez - https://github.com/parezj/
	  
    The MIT License (MIT)
    
    Copyright (c) 2018 Jakub Parez

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Diagnostics;

using BSL430_NET.Main;
using BSL430_NET.Utility;
using BSL430_NET.Constants;

using FTD2XX_NET;


namespace BSL430_NET
{
    namespace Comm
    {
        /// <summary>
        /// FTDI Device node extends generic Bsl430NetDevice. Use requires D2XX library 
        /// (FTD2XX.dll or libftd2xx.so) and driver installed (ftdibus.sys).
        /// </summary>
        [Serializable]
        public sealed class FTDI_Device : Bsl430NetDevice
        {            
            /// <summary>FTDI device VID eg. VID: 0x0403.</summary>
            public int Vid { get; set; }
            /// <summary>FTDI device PID eg. 0x6001.</summary>
            public int Pid { get; set; }
            /// <summary>FTDI device type eg. FT232R.</summary>
            public string Type { get; set; }
            /// <summary>FTDI device serial number.</summary>
            public string Serial { get; set; }

            /// <summary>
            /// FTDI_Device constructor.
            /// </summary>
            public FTDI_Device(int Vid,
                               int Pid,
                               string Type, 
                               string Serial, 
                               string Name, 
                               string Desc,
                               string Fulldesc,
                               Mode Mode) : base(Name, Desc, Fulldesc, Mode, CommFTD2XX.DEVICE_PREFIX)
            {
                this.Type = Type;
                this.Vid = Vid;
                this.Pid = Pid;
                this.Serial = Serial;
            }

            /// <summary>
            /// FTDI_Device parameterless constructor for serialization.
            /// </summary>
            public FTDI_Device() { }

            /// <summary>
            /// Short string general information about this device.
            /// </summary>
            public override string ToString()
            {
                string __desc = Description.Truncate(25);
                if (Description == "")
                    __desc = Type.Truncate(25);
                return $"V/PID: {Vid.ToString("X4")}-{Pid.ToString("X4")} [{__desc}] SN: {Serial}"
                        .Truncate(Const.DEV_STR_MAX_LEN);
            }
        }

        internal sealed class CommFTD2XX : Core
        {
            private const int OPEN_REPEATS = 10;
            private const int DELAY_OPEN = 50;
            private const int DELAY_READ = 10;
            public const string DEVICE_PREFIX = "FTDI";

            private readonly Dictionary<string, FTDI_Device> devices = new Dictionary<string, FTDI_Device>();
            public override Bsl430NetDevice DefaultDevice { set; get; } = null;

            FTDI.FT_STATUS ftStatus;
            FTDI ftdi;

            public CommFTD2XX(BSL430NET root = null, Bsl430NetDevice device = null) : base(root, Mode.UART_FTD2XX)
            {
                int check = FTDICheckDriver();
                if (check == -1)
                    throw new Bsl430NetException(323);  // mising or wrong dll
                //if (check == 0)
                    //throw new Bsl430NetException(333);  // missing or wrong driver
                DefaultDevice = device;
            }
            public override void CommOpen(Bsl430NetDevice device)
            {
                try
                {
                    if (ftdi == null)
                        ftdi = new FTDI();

                    ftStatus = FTDI.FT_STATUS.FT_OK;

                    Bsl430NetDevice _device = device;

                    if (device == null)
                        _device = DefaultDevice;

                    if (_device == null || _device.Name == "")
                        throw new Bsl430NetException(461);
                    
                    if (!devices.ContainsKey(_device.Name.ToLower()))
                    {
                        Status stat = Scan<FTDI_Device>(out _);
                        if (!stat.OK)
                            throw new Bsl430NetException(stat.Error);
                    }

                    if (!devices.TryGetValue(_device.Name.ToLower(), out FTDI_Device dev))
                        throw new Bsl430NetException(462);

                    ftStatus = FTDI.FT_STATUS.FT_OK;
                    int repeats = OPEN_REPEATS;
                    do
                    {
                        Task.Delay(DELAY_OPEN).Wait();
                        ftStatus = ftdi.OpenBySerialNumber(dev.Serial);
                        repeats -= 1;
                        if (repeats <= 0)
                            throw new Bsl430NetException(310);
                        if (BSL430NET.Interrupted)
                            throw new Bsl430NetException(666);
                    }
                    while (ftStatus != FTDI.FT_STATUS.FT_OK);
                }
                catch (Bsl430NetException ex)
                {
                    if (ex.Status.Error == 666)
                        throw ex;
                    else
                        throw new Bsl430NetException(311, ex);
                }
                catch (Exception ex)
                {
                    throw new Bsl430NetException(311, ex);
                }
            }
            public override void CommSet(BaudRate baud_rate)
            {
                try
                {
                    ftStatus = ftdi.SetBaudRate((uint)baud_rate);
                    if (ftStatus != FTDI.FT_STATUS.FT_OK)
                        throw new Bsl430NetException(340, ftStatus.ToString());

                    ftStatus = ftdi.SetDataCharacteristics(FTDI.FT_DATA_BITS.FT_BITS_8, FTDI.FT_STOP_BITS.FT_STOP_BITS_1, FTDI.FT_PARITY.FT_PARITY_EVEN);
                    if (ftStatus != FTDI.FT_STATUS.FT_OK)
                        throw new Bsl430NetException(340, ftStatus.ToString());

                    ftStatus = ftdi.SetFlowControl(FTDI.FT_FLOW_CONTROL.FT_FLOW_NONE, 0x00, 0x00);
                    if (ftStatus != FTDI.FT_STATUS.FT_OK)
                        throw new Bsl430NetException(340, ftStatus.ToString());

                    ftStatus = ftdi.SetTimeouts(5000, 0);
                    if (ftStatus != FTDI.FT_STATUS.FT_OK)
                        throw new Bsl430NetException(340, ftStatus.ToString());
                }
                catch (Exception ex) { throw new Bsl430NetException(341, ex); }
            }
            public override void CommDtr(bool val, bool ignore_err = false)
            {
                try
                {
                    ftStatus = ftdi.SetDTR(val);
                    if (ftStatus != FTDI.FT_STATUS.FT_OK)
                        throw new Bsl430NetException(350, ftStatus.ToString());
                }
                catch (Exception ex) { if (!ignore_err) throw new Bsl430NetException(351, ex); }
            }
            public override void CommRts(bool val, bool ignore_err = false)
            {
                try
                {
                    ftStatus = ftdi.SetRTS(val);
                    if (ftStatus != FTDI.FT_STATUS.FT_OK)
                        throw new Bsl430NetException(360, ftStatus.ToString());
                }
                catch (Exception ex) { if (!ignore_err) throw new Bsl430NetException(361, ex); }
            }
            public override void CommClrBuff()
            {
                try
                {
                    ftStatus = ftdi.Purge(FTDI.FT_PURGE.FT_PURGE_RX | FTDI.FT_PURGE.FT_PURGE_TX);
                    if (ftStatus != FTDI.FT_STATUS.FT_OK)
                        throw new Bsl430NetException(370, ftStatus.ToString());
                }
                catch (Exception ex) { throw new Bsl430NetException(371, ex); }
            }
            public override void CommClose(bool ignore_err)
            {
                try
                {
                    CommRts(false, true);
                    CommDtr(false, true);
                    ftdi.Purge(FTDI.FT_PURGE.FT_PURGE_RX | FTDI.FT_PURGE.FT_PURGE_TX);
                    ftdi.ResetDevice();
                    ftStatus = ftdi.Close();
                    if (ftStatus != FTDI.FT_STATUS.FT_OK && !ignore_err)
                        throw new Bsl430NetException(380, ftStatus.ToString());
                }
                catch (Exception ex) { if (!ignore_err) throw new Bsl430NetException(381, ex); }
            }

            public override Status CommXfer(out byte[] msg_rx, byte[] msg_tx, int rx_size)
            {
                Status status = new Status();
                ftStatus = FTDI.FT_STATUS.FT_OK;
                msg_rx = new byte[0];
                if (ftdi == null)
                    return Utils.StatusCreate(333);
                try
                {
                    if (msg_tx.Length > 0)
                    {
                        uint numBytesWritten = 0;
                        ftStatus = ftdi.Write(msg_tx, msg_tx.Length, ref numBytesWritten);

                        if (ftStatus != FTDI.FT_STATUS.FT_OK)
                            return Utils.StatusCreate(390, ftStatus.ToString());
                        if (numBytesWritten != msg_tx.Length)
                            return Utils.StatusCreate(391);

                        status = Utils.StatusCreate(0);
                    }

                    if (rx_size > 0)
                    {
                        byte[] buffer = Enumerable.Repeat((byte)0xFF, rx_size).ToArray();
                        uint numBytesAvailable = 0;
                        uint numBytesRead = 0;
                        int timeout = Const.TIMEOUT_READ;
                        bool timeout_flip = false;
                        Stopwatch sw = new Stopwatch();
                        sw.Start();

                        do
                        { 
                            ftStatus = ftdi.GetRxBytesAvailable(ref numBytesAvailable);

                            if (ftStatus != FTDI.FT_STATUS.FT_OK)
                                return Utils.StatusCreate(392, ftStatus.ToString());
                            else if (numBytesAvailable >= rx_size || (timeout_flip && numBytesAvailable > 0))
                            {
                                if (numBytesAvailable > rx_size)
                                    numBytesAvailable = (uint)rx_size;

                                ftStatus = ftdi.Read(buffer, numBytesAvailable, ref numBytesRead);

                                if (ftStatus != FTDI.FT_STATUS.FT_OK)
                                    return Utils.StatusCreate(393, ftStatus.ToString());
                                if (numBytesRead != numBytesAvailable)
                                    return Utils.StatusCreate(394);
                                if (numBytesAvailable > rx_size)
                                    ftdi.Purge(FTDI.FT_PURGE.FT_PURGE_RX | FTDI.FT_PURGE.FT_PURGE_TX);

                                msg_rx = buffer.Take(rx_size).ToArray();
                                status = Utils.StatusCreate(0);
                                break;
                            }

                            if (BSL430NET.Interrupted)
                                throw new Bsl430NetException(666);

                            if (timeout_flip)
                                return Utils.StatusCreate(395);

                            if (timeout <= 0)
                                timeout_flip = true;
                            else
                                timeout -= (int)sw.ElapsedMilliseconds;

                            Task.Delay(DELAY_READ).Wait();

                        } while (numBytesAvailable < rx_size);
                    }
                    return status;
                }
                catch (Bsl430NetException ex)
                {
                    throw ex;
                }
                catch (Exception ex)
                {
                    return Utils.StatusCreate(446, ex.Message);
                }
            }
            public override Status Scan<FTDI_Device>(out List<FTDI_Device> device_list,
                                                     ScanOptions scan_opt = ScanOptions.None)
            {
                device_list = new List<FTDI_Device>();
                devices.Clear();

                try
                {
                    uint ftdiDeviceCount = 0;
                    ftStatus = FTDI.FT_STATUS.FT_OK;
                    if (ftdi == null)
                        ftdi = new FTDI();
                    ftStatus = ftdi.GetNumberOfDevices(ref ftdiDeviceCount);

                    if (ftStatus != FTDI.FT_STATUS.FT_OK)
                        return Utils.StatusCreate(300, ftStatus.ToString());

                    FTDI.FT_DEVICE_INFO_NODE[] ftdiDeviceList = new FTDI.FT_DEVICE_INFO_NODE[ftdiDeviceCount];

                    int timeout = Const.TIMEOUT_SCAN;
                    do
                    {
                        Task.Delay(5).Wait();
                        ftStatus = ftdi.GetDeviceList(ftdiDeviceList);
                        timeout -= 5;
                        if (timeout <= 0)
                            return Utils.StatusCreate(302);
                        if (BSL430NET.Interrupted)
                            throw new Bsl430NetException(666);
                    }
                    while (ftStatus != FTDI.FT_STATUS.FT_OK);

                    int i = 1;
                    foreach (var dev in ftdiDeviceList)
                    {
                        string device_name = DEVICE_PREFIX + i;
                        string type = dev.Type.ToString();
                        string description = dev.Description;
                        string serial = dev.SerialNumber.ToString();
                        string vidpid = String.Format("{0:x}", dev.ID);
                        int vid = -1;
                        int pid = -1;
                        Int32.TryParse(vidpid.Substring(vidpid.Length - 4, 4), NumberStyles.HexNumber, null, out pid);
                        Int32.TryParse(vidpid.Substring(0, vidpid.Length - 4), NumberStyles.HexNumber, null, out vid);

                        if (!(scan_opt.HasFlag(ScanOptions.Ftdi_IgnoreUnknownDev) && type.ToLower().Contains("unknown")))
                        {
                            Comm.FTDI_Device nod = new Comm.FTDI_Device
                            (
                                vid,
                                pid,
                                type,
                                serial,
                                device_name,
                                description,
                                "",
                                Mode.UART_FTD2XX
                            );
                            nod.FormattedDescription = nod.ToString();
                            devices.Add(device_name.ToLower(), nod);
                            i++;
                        }
                    }

                    device_list = devices.Select(pair => pair.Value).Cast<FTDI_Device>().ToList();
                    return Utils.StatusCreate(0);
                }
                catch (Bsl430NetException ex)
                {
                    throw ex;
                }
                catch (Exception ex)
                {
                    return Utils.StatusCreate(301, ex.Message);
                }
            }
            public override Bsl430NetDevice CommGetDefaultDevice()
            {
                return DefaultDevice;
            }
            private int FTDICheckDriver()
            {
                //uint driver = 0;
                try
                {
                    if (ftdi == null)
                        ftdi = new FTDI();
                    if (ftdi == null)
                        return -1;
                    return 1;
                    /*
                    if (serial != "")
                        ftdi.OpenBySerialNumber(serial);
                    ftStatus = ftdi.GetDriverVersion(ref driver);
                    ftdi.Close();
                    if (ftStatus != FTDI.FT_STATUS.FT_OK)
                        return 0;
                    return (int)driver;
                    */
                }
                catch (Exception) { return -1; }
            }
        }
    }
}
