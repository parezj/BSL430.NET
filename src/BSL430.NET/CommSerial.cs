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
using System.Text.RegularExpressions;

using BSL430_NET.Main;
using BSL430_NET.Utility;
using BSL430_NET.Constants;

using RJCP.IO.Ports;


namespace BSL430_NET
{
    namespace Comm
    {
        /// <summary>
        /// Serial Device node extends generic Bsl430NetDevice. Use requires library: SerialPortStream.
        /// NOTE: If target app use .NET Framework 4.6.2 or lower, Nuget package: Microsoft.Win32.Registry is required!
        /// </summary>
        [Serializable]
        public sealed class Serial_Device : Bsl430NetDevice
        {
            /// <summary>Serial device port name.</summary>
            public string Port { get; set; }

            /// <summary>
            /// Serial_Device constructor.
            /// </summary>
            public Serial_Device(string Port,
                                 string Name,
                                 string Desc,
                                 string Fulldesc,
                                 Mode Mode) : base(Name, Desc, Fulldesc, Mode, CommSerial.DEVICE_PREFIX)
            {
                this.Port = Port;
            }

            /// <summary>
            /// Serial_Device parameterless constructor for serialization.
            /// </summary>
            public Serial_Device() { }

            /// <summary>
            /// Short string general information about this device.
            /// </summary>
            public override string ToString()
            {
                return $"Port: {Port}".Truncate(Const.DEV_STR_MAX_LEN);
            }
        }

        internal sealed class CommSerial : Core
        {
            public const int TIMEOUT_READ = 5000;
            public const int TIMEOUT_WRITE = 5000;
            private const int DELAY_READ = 10;
            public const string DEVICE_PREFIX = "COM";

            private readonly Dictionary<string, Serial_Device> devices = new Dictionary<string, Serial_Device>();
            public override Bsl430NetDevice DefaultDevice { set; get; } = null;

            private SerialPortStream serial;

            public CommSerial(BSL430NET root = null, Bsl430NetDevice device = null) : base(root, Mode.UART_Serial)
            {
                int check = SerialCheckLibrary();
                if (check == -1)
                    throw new Bsl430NetException(723);  // mising or wrong dll
                if (check == -2)
                    throw new Bsl430NetException(733);  // dll load exception
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
                        Status stat = Scan<Serial_Device>(out _);
                        if (!stat.OK)
                            throw new Bsl430NetException(stat.Error);
                    }

                    if (!devices.TryGetValue(_device.Name.ToLower(), out Serial_Device dev))
                        throw new Bsl430NetException(462);

                    serial = new SerialPortStream(dev.Port, 
                                                  (int)BaudRate.BAUD_9600, 
                                                  8, 
                                                  Parity.Even, 
                                                  StopBits.One);
                    serial.Open(); // OpenDirect

                    if (serial == null || !serial.IsOpen)
                        throw new Bsl430NetException(710);

                    serial.WriteTimeout = TIMEOUT_WRITE;
                    serial.ReadTimeout = TIMEOUT_READ;
                }
                catch (Exception ex) { throw new Bsl430NetException(711, ex); }
            }
            public override void CommSet(BaudRate baud_rate)
            {
                if (serial != null)
                {
                    try
                    {
                        serial.BaudRate = (int)baud_rate;
                    }
                    catch (Exception ex) { throw new Bsl430NetException(741, ex); }
                }
            }
            public override void CommDtr(bool val, bool ignore_err = false)
            {
                if (serial != null)
                {
                    try
                    {
                        serial.DtrEnable = val;
                    }
                    catch (Exception ex) { if (!ignore_err) throw new Bsl430NetException(751, ex); }
                }
            }
            public override void CommRts(bool val, bool ignore_err = false)
            {
                if (serial != null)
                {
                    try
                    {
                        serial.RtsEnable = val;
                    }
                    catch (Exception ex) { if (!ignore_err) throw new Bsl430NetException(761, ex); }
                }
            }
            public override void CommClrBuff()
            {
                try
                {
                    serial?.DiscardInBuffer();
                    serial?.DiscardOutBuffer();
                }
                catch (Exception ex) { throw new Bsl430NetException(771, ex); }
            }
            public override void CommClose(bool ignore_err)
            {
                try
                {
                    CommRts(false, true);
                    CommDtr(false, true);
                    serial.Close();
                }
                catch (Exception ex) { if (!ignore_err) throw new Bsl430NetException(781, ex); }
                finally
                {
                    try
                    {
                        serial.Dispose();
                    }
                    catch (Exception) { }
                }
            }
            public override Status CommXfer(out byte[] msg_rx, byte[] msg_tx, int rx_size)
            {
                msg_rx = new byte[0];
                if (serial == null)
                    return Utils.StatusCreate(733);
                try
                {
                    if (msg_tx.Length > 0)
                    {
                        serial.Write(msg_tx, 0, msg_tx.Length);
                    }

                    if (rx_size > 0)
                    {
                        int buff_size = serial.ReadBufferSize; 
                        byte[] buffer = Enumerable.Repeat((byte)0xFF, buff_size).ToArray();
                        List<byte> data_list = new List<byte>();
                        int timeout = TIMEOUT_READ;

                        while (timeout > 0)
                        {
                            int stat = serial.Read(buffer, 0, rx_size);

                            if (stat < 0)
                                return Utils.StatusCreate(793);
                            else if (stat > 0)
                                data_list.AddRange(buffer.Take(stat));

                            if (data_list.Count >= rx_size)
                            {
                                msg_rx = data_list.Take(rx_size).ToArray();
                                return Utils.StatusCreate(0);
                            }

                            if (BSL430NET.Interrupted)
                                throw new Bsl430NetException(666);

                            timeout -= TIMEOUT_READ;
                            Task.Delay(DELAY_READ).Wait();
                        }
                        return Utils.StatusCreate(795);  // timeout
                    }
                    return Utils.StatusCreate(0);
                }
                catch (Bsl430NetException ex)
                {
                    throw ex;
                }
                catch (Exception ex)
                {
                    return Utils.StatusCreate(746, ex.Message);
                }
            }
            public override Status Scan<Serial_Device>(out List<Serial_Device> device_list,
                                                       ScanOptions scan_opt = ScanOptions.None)
            {
                device_list = new List<Serial_Device>();
                devices.Clear();

        
                    int i = 1;
                //var allcom = SerialPortStream.GetPortNames();
                var desc = SerialPortStream.GetPortDescriptions();
                    if (desc != null && desc.Length > 0)
                    {
                        foreach (PortDescription com in desc)
                        {
                            string device_name = com.Port.ToUpper().Replace(" ", String.Empty).Trim();
                            if (!device_name.Contains(DEVICE_PREFIX) || !device_name.Any(c => char.IsDigit(c)))
                                device_name = DEVICE_PREFIX + i;
                            while (devices.ContainsKey(device_name))
                                device_name += "0";

                            Comm.Serial_Device nod = new Comm.Serial_Device
                            (
                                com.Port,
                                device_name,
                                com.Description,
                                "",
                                Mode.UART_Serial
                            );
                            nod.FormattedDescription = nod.ToString();
                            devices.Add(device_name.ToLower(), nod);
                            i++;
                        }

                        try
                        {
                            device_list = devices.Select(pair => pair.Value)
                                                 .Cast<Serial_Device>()
                                                 .Select(s => new { key = Int32.Parse(Regex.Match(s.Name, @"\d+").Value), value = s })
                                                 .OrderBy(p => p.key)
                                                 .Select(p => p.value)
                                                 .ToList();
                        }
                        catch(Exception)
                        {
                            device_list = devices.Select(pair => pair.Value).Cast<Serial_Device>().ToList();
                        }
                        
                    }
                    return Utils.StatusCreate(0);
     
            }
            public override Bsl430NetDevice CommGetDefaultDevice()
            {
                return DefaultDevice;
            }
            private int SerialCheckLibrary()
            {
                try
                {
                    SerialPortStream _serial = new SerialPortStream();  // todo remove
                    _serial.Close();
                    return 0;
                }
                catch (FileNotFoundException) { return -1; }
                catch (TypeLoadException) { return -1; }
                catch (Exception) { return -2; }
            }
        }
    }
}
