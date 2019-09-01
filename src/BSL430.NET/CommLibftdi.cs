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
using System.Text;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using BSL430_NET.Main;
using BSL430_NET.Utility;
using BSL430_NET.Constants;

using libftdinet;
using LibUsbDotNet;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;


namespace BSL430_NET
{
    namespace Comm
    {
        /// <summary>
        /// Libftdi Device node extends generic Bsl430NetDevice. Use requires libraries: libftdi, libusb, LibUsbDotNet
        /// </summary>
        [Serializable]
        public sealed class Libftdi_Device : Bsl430NetDevice
        {
            /// <summary>FTDI device VID eg. 0x0403.</summary>
            public int Vid { get; set; }
            /// <summary>FTDI device PID eg. 0x6001.</summary>
            public int Pid { get; set; }

            /// <summary>
            /// Libftdi_Device constructor.
            /// </summary>
            public Libftdi_Device(int Vid,
                                  int Pid,
                                  string Name,
                                  string Desc,
                                  string Fulldesc,
                                  Mode Mode) : base(Name, Desc, Fulldesc, Mode, CommLibftdi.DEVICE_PREFIX)
            {
                this.Vid = Vid;
                this.Pid = Pid;
            }

            /// <summary>
            /// Libftdi_Device parameterless constructor for serialization.
            /// </summary>
            public Libftdi_Device() { }

            /// <summary>
            /// Short string general information about this device.
            /// </summary>
            public override string ToString()
            {
                return $"V/PID: {Vid.ToString("X4")}-{Pid.ToString("X4")}".Truncate(Const.DEV_STR_MAX_LEN);
            }
        }

        internal sealed class CommLibftdi : Core
        {
            private const int DELAY_READ = 10;
            private const int BUFFER_SIZE = 512; 
            private const int FTDI_VID = 0x0403;
            public const string DEVICE_PREFIX = "libftdi";

            private readonly Dictionary<string, Libftdi_Device> devices = new Dictionary<string, Libftdi_Device>();
            public override Bsl430NetDevice DefaultDevice { set; get; } = null;

            private FTDIContext ftdi;

            public CommLibftdi(BSL430NET root = null, Bsl430NetDevice device = null) : base(root, Mode.UART_libftdi)
            {
                int check = LibftdiCheckLibrary();
                if (check == -1)
                    throw new Bsl430NetException(523);  // mising or wrong dll
                if (check == -2)
                    throw new Bsl430NetException(533);  // dll load exception
                DefaultDevice = device;
            }
            public override void CommOpen(Bsl430NetDevice device)
            {
                try
                {
                    Bsl430NetDevice _device = null;

                    if (device == null)
                        _device = DefaultDevice;

                    if (_device == null || _device.Name == "")
                        throw new Bsl430NetException(461);

                    if (!devices.ContainsKey(_device.Name.ToLower()))
                    {
                        Status stat = Scan<Libftdi_Device>(out _);
                        if (!stat.OK)
                            throw new Bsl430NetException(stat.Error);
                    }

                    if (!devices.TryGetValue(_device.Name.ToLower(), out Libftdi_Device dev))
                        throw new Bsl430NetException(462);

                    ftdi = new FTDIContext(dev.Vid, dev.Pid);

                    if (ftdi == null)
                        throw new Bsl430NetException(510);
                }
                catch (Exception ex) { throw new Bsl430NetException(511, ex); }
            }
            public override void CommSet(BaudRate baud_rate)
            {
                if (ftdi != null)
                {
                    try
                    {
                        ftdi.Baudrate = (int)baud_rate;
                        ftdi.SetLineProperty(BitsType.BITS_8, StopBitsType.STOP_BIT_1, ParityType.EVEN);
                        ftdi.FlowControl = 0;
                    }
                    catch (Exception ex) { throw new Bsl430NetException(541, ex); }
                }
            }
            public override void CommDtr(bool val, bool ignore_err = false)
            {
                if (ftdi != null)
                {
                    try
                    {
                        ftdi.SetDTR = val ? 1 : 0;
                    }
                    catch (Exception ex) { if (!ignore_err) throw new Bsl430NetException(551, ex); }
                }
            }
            public override void CommRts(bool val, bool ignore_err = false)
            {
                if (ftdi != null)
                {
                    try
                    {
                        ftdi.SetRTS = val ? 1 : 0;
                    }
                    catch (Exception ex) { if (!ignore_err) throw new Bsl430NetException(561, ex); }
                }
            }
            public override void CommClrBuff()
            {
                try
                {
                    ftdi?.PurgeBuffers();
                }
                catch (Exception ex) { throw new Bsl430NetException(571, ex); }
            }
            public override void CommClose(bool ignore_err)
            {
                try
                {
                    CommRts(false, true);
                    CommDtr(false, true);
                    ftdi.Close();
                }
                catch (Exception ex) { if (!ignore_err) throw new Bsl430NetException(581, ex); }
            }
            public override Status CommXfer(out byte[] msg_rx, byte[] msg_tx, int rx_size)
            {
                msg_rx = new byte[0];
                if (ftdi == null)
                    return Utils.StatusCreate(533);
                try
                {
                    if (msg_tx.Length > 0)
                    {
                        int stat = ftdi.WriteData(msg_tx, msg_tx.Length);

                        if (stat != msg_tx.Length)
                            return Utils.StatusCreate(591);
                    }

                    if (rx_size > 0)
                    {
                        byte[] buffer = Enumerable.Repeat((byte)0xFF, BUFFER_SIZE).ToArray();
                        List<byte> data_list = new List<byte>();
                        int timeout = Const.TIMEOUT_READ;
                        
                        while (timeout > 0)
                        {
                            int stat = ftdi.ReadData(buffer, rx_size);

                            if (stat < 0)
                                return Utils.StatusCreate(593);
                            else if (stat > 0)
                                data_list.AddRange(buffer.Take(stat));
                            
                            if (data_list.Count >= rx_size)
                            {
                                msg_rx = data_list.Take(rx_size).ToArray();
                                return Utils.StatusCreate(0);
                            }

                            if (BSL430NET.Interrupted)
                                throw new Bsl430NetException(666);

                            timeout -= DELAY_READ;
                            Task.Delay(DELAY_READ).Wait();
                        }
                        return Utils.StatusCreate(595);  // timeout
                    }
                    return Utils.StatusCreate(0);
                }
                catch (Bsl430NetException ex)
                {
                    throw ex;
                }
                catch (Exception ex)
                {
                    return Utils.StatusCreate(546, ex.Message);
                }
            }
            public override Status Scan<Libftdi_Device>(out List<Libftdi_Device> device_list,
                                                        ScanOptions scan_opt = ScanOptions.None)
            {
                device_list = new List<Libftdi_Device>();
                devices.Clear();

                try
                {
                    int i = 1;
                    UsbRegDeviceList alldev = UsbDevice.AllDevices;
                    if (alldev != null && alldev.Count > 0)
                    {
                        foreach (UsbRegistry dev in alldev)
                        {
                            if (dev.Vid == FTDI_VID)
                            {
                                string device_name = DEVICE_PREFIX + i;

                                Comm.Libftdi_Device nod = new Comm.Libftdi_Device
                                (
                                    dev.Vid,
                                    dev.Pid,
                                    device_name,
                                    dev.FullName,
                                    "",
                                    Mode.UART_libftdi
                                );
                                nod.FormattedDescription = nod.ToString();
                                devices.Add(device_name.ToLower(), nod);
                                i++;
                            }
                        }

                        device_list = devices.Select(pair => pair.Value).Cast<Libftdi_Device>().ToList();
                    }
                    return Utils.StatusCreate(0);
                }
                catch (Exception ex)
                {
                    return Utils.StatusCreate(501, ex.Message);
                }
            }
            public override Bsl430NetDevice CommGetDefaultDevice()
            {
                return DefaultDevice;
            }
            private int LibftdiCheckLibrary()
            {
                try
                {
                    FTDIContext _ftdi = new FTDIContext(null);  // todo remove
                    _ftdi.Close();
                    return 0;
                }
                catch (FileNotFoundException) { return -1; }
                catch (TypeLoadException) { return -1; }
                catch (Exception) { return -2; }
            }
        }
    }  
}
