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
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using BSL430_NET.Main;
using BSL430_NET.Utility;
using BSL430_NET.Constants;

using HidSharp;
using HidSharp.Reports;
using HidSharp.Reports.Input;

namespace BSL430_NET
{
    namespace Comm
    {
        /// <summary>
        /// USB HID Device node extends generic Bsl430NetDevice. Use requires libraries: HidSharp.
        /// </summary>
        [Serializable]
        public sealed class USB_HID_Device : Bsl430NetDevice
        {
            /// <summary>USB HID device VID.</summary>
            public int Vid { get; set; }
            /// <summary>USB HID device PID.</summary>
            public int Pid { get; set; }
            /// <summary>USB HID device serial number.</summary>
            public string Serial { get; set; }
            /// <summary>USB HID device manufacturer.</summary>
            public string Manufacturer { get; set; }
            /// <summary>USB HID device product name.</summary>
            public string ProductName { get; set; }

            /// <summary>
            /// USB_HID_Device constructor.
            /// </summary>
            public USB_HID_Device(int Vid,
                                  int Pid,
                                  string Serial,
                                  string Manufacturer,
                                  string ProductName,
                                  string Name,
                                  string Desc,
                                  string FullDesc,
                                  Mode Mode) : base(Name, Desc, FullDesc, Mode, CommUSB.DEVICE_PREFIX)
            {
                this.Vid = Vid;
                this.Pid = Pid;
                this.Serial = Serial;
                this.Manufacturer = Manufacturer;
                this.ProductName = ProductName;
            }

            /// <summary>
            /// USB_HID_Device parameterless constructor for serialization.
            /// </summary>
            public USB_HID_Device() { }

            /// <summary>
            /// Short string general information about this device.
            /// </summary>
            public override string ToString()
            {
                return $"V/PID: {Vid.ToString("X4")}-{Pid.ToString("X4")} [{Manufacturer.Truncate(25)}] SN: {Serial}"
                        .Truncate(Const.DEV_STR_MAX_LEN);
            }
        }

        internal sealed class CommUSB : Core
        {
            private const int TI_VID = 0x2047;
            private const int TI_BSL_PID = 0x0200;
            public const string DEVICE_PREFIX = "USB";

            private readonly Dictionary<string, USB_HID_Device> devices = new Dictionary<string, USB_HID_Device>();
            public override Bsl430NetDevice DefaultDevice { set; get; } = null;

            private HidStream usb;
            private HidDeviceInputReceiver inputReceiver;
            private ReportDescriptor reportDescriptor;

            private int max_output_len = 0;
            private int max_input_len = 0;

            public CommUSB(BSL430NET root = null, Bsl430NetDevice device = null) : base(root, Mode.USB_HID)
            {
                int check = USBCheckLibrary();
                if (check == -1)
                    throw new Bsl430NetException(623);  // mising or wrong dll
                if (check == -2)
                    throw new Bsl430NetException(633);  // dll load exception

                DefaultDevice = device;
            }
            public override void CommOpen(Bsl430NetDevice device)
            {
                try
                {
                    Bsl430NetDevice _device = device;

                    if (device == null)
                        _device = DefaultDevice;

                    if (_device == null || _device.Name == "")
                        throw new Bsl430NetException(461);

                    if (!devices.ContainsKey(_device.Name.ToLower()))
                    {
                        Status stat = Scan<USB_HID_Device>(out _);
                        if (!stat.OK)
                            throw new Bsl430NetException(stat.Error);
                    }

                    if (!devices.TryGetValue(_device.Name.ToLower(), out USB_HID_Device dev))
                        throw new Bsl430NetException(462);

                    DeviceList.Local.GetHidDeviceOrNull(dev.Vid, 
                                                        dev.Pid)?.TryOpen(out usb);

                    if (usb == null)
                        throw new Bsl430NetException(610);
                    else
                    {
                        max_output_len = usb.Device.GetMaxOutputReportLength();
                        max_input_len = usb.Device.GetMaxInputReportLength();
                        usb.ReadTimeout = Timeout.Infinite;
                        reportDescriptor = usb.Device.GetReportDescriptor();
                        inputReceiver = reportDescriptor.CreateHidDeviceInputReceiver();
                        inputReceiver.Start(usb);
                    }
                }
                catch (Exception ex) { throw new Bsl430NetException(611, ex); }
            }
            public override void CommSet(BaudRate baud_rate)
            {
            }
            public override void CommDtr(bool val, bool ignore_err = false)
            {
            }
            public override void CommRts(bool val, bool ignore_err = false)
            {
            }
            public override void CommClrBuff()
            {
                //usb.Flush();
            }
            public override void CommClose(bool ignore_err)
            {
                try
                {
                    usb.Close();
                }
                catch (Exception ex) { if (!ignore_err) throw new Bsl430NetException(681, ex); }
                finally
                {
                    try
                    {
                        usb.Dispose();
                    }
                    catch (Exception) { }
                }
            }
            public override Status CommXfer(out byte[] msg_rx, byte[] msg_tx, int rx_size)
            {
                msg_rx = new byte[0];
                if (usb == null)
                    return Utils.StatusCreate(633);
                try
                {
                    if (msg_tx.Length > 0)
                    {
                        int blocks = (int)(Math.Ceiling((double)msg_tx.Length / (double)max_output_len));
                        int last_batch = msg_tx.Length % max_output_len;
                        if (last_batch == 0)
                            last_batch = max_output_len;
                        for (int i = 0; i < blocks; i++)
                        {
                            usb.Write(msg_tx.Skip(i * max_output_len)
                                            .Take((i < blocks - 1) ? max_output_len :
                                                                     last_batch).ToArray());
                        }
                    }

                    if (rx_size > 0)  // todo check if parallel processing is needed
                    {
                        List<byte> data_list = new List<byte>();
                        byte[] buffer = Enumerable.Repeat((byte)0xFF, max_input_len).ToArray();
                        var inputParser = reportDescriptor.DeviceItems[0].CreateDeviceItemInputParser();

                        int timeout = Const.TIMEOUT_READ;
                        Stopwatch sw = new Stopwatch();
                        sw.Start();

                        while (timeout > 0) // read all reports
                        {
                            while (timeout > 0) // read one report (64b)
                            {
                                int remaining = rx_size - data_list.Count;
                                int toread = (remaining >= max_input_len) ? max_input_len : remaining;

                                if (!inputReceiver.IsRunning)
                                    return Utils.StatusCreate(646);

                                // ANOTHER METHOD TO READ - TODO test
                                //int read = usb.Read(buffer, 0, toread);
                                //if (read >= toread)
                                //{
                                //    data_list.AddRange(buffer.Take(toread));
                                //    if (data_list.Count >= rx_size)
                                //    {
                                //        msg_rx = data_list.Take(rx_size).ToArray();
                                //        return Utils.StatusCreate(0);
                                //    }
                                //    break;
                                //}

                                while (inputReceiver.TryRead(buffer, 0, out HidSharp.Reports.Report input_report))
                                {
                                    if (inputParser.TryParseReport(buffer, 0, input_report))
                                    {
                                        if (inputParser.HasChanged)
                                        {
                                            int valueCount = inputParser.ValueCount;

                                            if (valueCount == 1)
                                            {
                                                DataValue dataValue = inputParser.GetValue(0);
                                                Usage usage = (Usage)dataValue.Usages.FirstOrDefault(); // todo process?


                                                data_list.AddRange(buffer.Take(toread));

                                                if (data_list.Count >= rx_size)
                                                {
                                                    msg_rx = data_list.Take(rx_size).ToArray();
                                                    return Utils.StatusCreate(0);
                                                }
                                            }
                                            //for (int valueIndex = 0; valueIndex < valueCount; valueIndex++)  - TODO test
                                            //{
                                            //    var dataValue = inputParser.GetValue(valueIndex);
                                            //      Console.Write(string.Format("  {0}: {1}",
                                            //      (Usage)dataValue.Usages.FirstOrDefault(), dataValue.GetPhysicalValue()));
                                            //
                                            //}
                                        }
                                    }
                                }

                                if (BSL430NET.Interrupted)
                                    throw new Bsl430NetException(666);

                                Task.Delay(5).Wait();
                                timeout -= (int)sw.ElapsedMilliseconds;
                            }
                        }
                        return Utils.StatusCreate(695);  // timeout
                    }
                    return Utils.StatusCreate(0);
                }
                catch (Bsl430NetException ex)
                {
                    throw ex;
                }
                catch (Exception ex)
                {
                    return Utils.StatusCreate(646, ex.Message);
                }
            }
            public override Status Scan<USB_HID_Device>(out List<USB_HID_Device> device_list, 
                                                        ScanOptions scan_opt = ScanOptions.None)
            {
                device_list = new List<USB_HID_Device>();
                devices.Clear();

                try
                {
                    int i = 1;
                    var alldev = DeviceList.Local.GetHidDevices();
                    if (alldev != null && alldev.Count() > 0)
                    {
                        foreach (HidDevice dev in alldev)
                        {
                            if (scan_opt.HasFlag(ScanOptions.UsbHid_IgnoreTexasVid) || 
                                (dev.VendorID == TI_VID && dev.ProductID == TI_BSL_PID))
                            {
                                //dev.GetReportDescriptor().DeviceItems[0].Usages;
                                string device_name = DEVICE_PREFIX + i;

                                Comm.USB_HID_Device nod = new Comm.USB_HID_Device
                                (
                                    dev.VendorID,
                                    dev.ProductID,
                                    dev.GetSerialNumber(),
                                    dev.GetManufacturer(),
                                    dev.GetProductName(), // dev.GetFriendlyName()
                                    device_name,
                                    dev.DevicePath,
                                    "",
                                    Mode.USB_HID
                                );
                                nod.FormattedDescription = nod.ToString();
                                devices.Add(device_name.ToLower(), nod);
                                i++;
                            }
                        }

                        device_list = devices.Select(pair => pair.Value).Cast<USB_HID_Device>().ToList();
                    }
                    return Utils.StatusCreate(0);
                }
                catch (Exception ex)
                {
                    return Utils.StatusCreate(601, ex.Message);
                }
            }
            public override Bsl430NetDevice CommGetDefaultDevice()
            {
                return DefaultDevice;
            }
            private int USBCheckLibrary()
            {
                try
                {
                    var _usb = new HidSharp.Reports.Report();   // todo remove
                    return 0;
                }
                catch (FileNotFoundException) { return -1; }
                catch (TypeLoadException) { return -1; }
                catch (Exception) { return -2; }
            }
        }
    }
}
