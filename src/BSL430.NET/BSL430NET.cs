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

//      ____  ____  _     _  _   _____  ___    _   _ _____ _____ 
//     | __ )/ ___|| |   | || | |___ / / _ \  | \ | | ____|_   _|
//     |  _ \\___ \| |   | || |_  |_ \| | | | |  \| |  _|   | |  
//     | |_) |___) | |___|__   _|___) | |_| |_| |\  | |___  | |  
//     |____/|____/|_____|  |_| |____/ \___/(_)_| \_|_____| |_|  

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using BSL430_NET.Main;
using BSL430_NET.Utility;
using BSL430_NET.Comm;
using BSL430_NET.Constants;


namespace BSL430_NET
{
    /// <summary>
    /// Main mode which each library instance can operate in, explicitly stated in constructor or derived from device.
    /// <para/>First word UART/USB declares the target MCU data interface. Please see MCU datasheet for more info.
    /// <para/>Second word declares the library, which handles communication. This library needs to be present.
    /// </summary>
    public enum Mode : byte
    {
        /// <summary>UART mode via proprietary FTD2XX library requires ftdibus.sys driver</summary>
        UART_FTD2XX,
        /// <summary>UART mode via opensource libftdi library requires libusb</summary>
        UART_libftdi,
        /// <summary>UART mode via SerialPortStream library</summary>
        UART_Serial,
        /// <summary>USB mode via HidSharp and LibUsbDotNet library</summary>
        USB_HID
    }

    /// <summary>
    /// Baud rate value. Default is 9600 bps (9600).
    /// Setting other than default is supported only by some BSL versions.
    /// </summary>
    [DefaultValue(BAUD_9600)]
    public enum BaudRate : int
    {
        /// <summary>9600 bps</summary>
        BAUD_9600 = 9600,
        /// <summary>19200 bps</summary>
        BAUD_19200 = 19200,
        /// <summary>38400 bps</summary>
        BAUD_38400 = 38400,
        /// <summary>57600 bps</summary>
        BAUD_57600 = 57600,
        /// <summary>115200 bps</summary>
        BAUD_115200 = 115200
    }

    /// <summary>
    /// Entry sequence on MCU pins which forces the target to start program execution at the BSL RESET vector.
    /// Default is SHARED_JTAG (0) - DTR to RST and RTS to TEST pin.
    /// </summary>
    [DefaultValue(SHARED_JTAG)]
    public enum InvokeMechanism : byte
    {
        /// <summary>Only UART mode and MCU with shared JTAG pins. Tie DTR to RST and RTS to TEST pin.</summary>
        SHARED_JTAG = 0,
        /// <summary>Only UART mode and MCU with dedicated JTAG pins. Tie DTR to RST and RTS to TCK pin.</summary>
        DEDICATED_JTAG = 1,
        /// <summary>Only USB mode, where BSL is invoked either of the following conditions are met:
        /// <para/>The MCU is powered up by USB and the reset vector is blank.
        /// <para/>The MCU powers up with PUR pin tied to VUSB.</summary>
        MANUAL = 2
    }

    /// <summary>
    /// MSP430/432 MCU device family. Default is MSP430_F5xx (4).
    /// </summary>
    [DefaultValue(MSP430_F5xx)]
    public enum MCU : byte
    {
        /// <summary>MSP430 F1xx</summary>
        MSP430_F1xx = 0,
        /// <summary>MSP430 F2xx</summary>
        MSP430_F2xx = 1,
        /// <summary>MSP430 F4xx</summary>
        MSP430_F4xx = 2,
        /// <summary>MSP430 G2xx3</summary>
        MSP430_G2xx3 = 3,
        /// <summary>MSP430 F5xx</summary>
        MSP430_F5xx = 4,
        /// <summary>MSP430 F543x</summary>
        MSP430_F543x = 5,
        /// <summary>MSP430 F6xx</summary>
        MSP430_F6xx = 6,
        /// <summary>MSP430 FR5xx</summary>
        MSP430_FR5xx = 7,
        /// <summary>MSP430 FR6xx</summary>
        MSP430_FR6xx = 8,
        /// <summary>MSP430 FR2x33</summary>
        MSP430_FR2x33 = 9,
        /// <summary>MSP430 FR231x</summary>
        MSP430_FR231x = 10,
        /// <summary>MSP430 FR235x</summary>
        MSP430_FR235x = 11,
        /// <summary>MSP430 FR215x</summary>
        MSP430_FR215x = 12,
        /// <summary>MSP430 FR413x</summary>
        MSP430_FR413x = 13,
        /// <summary>MSP430 FR211x</summary>
        MSP430_FR211x = 14,
        /// <summary>MSP432 P401R</summary>
        MSP432_P401R = 15
    }

    /// <summary>
    /// [Flags] Scan Options adjust scanner behavior, use it like this: (UsbHid_IgnoreTexasVid | Ftdi_IgnoreUnknownDev)
    /// <para/>'UsbHid_IgnoreTexasVid' forces scanner to ignore TI VID(0x2047) and get all USB HID devices present in system.
    /// <para/>'Ftdi_IgnoreUnknownDev' forces scanner to ignore "unknown" type devices, that are most likely already connected.
    /// </summary>
    [Flags]
    [DefaultValue(None)]
    public enum ScanOptions : byte
    {
        /// <summary>[Flag] Default value None has zero effect on scanner.</summary>
        None = 0x0,
        /// <summary>[Flag] Forces scanner to ignore TI VID(0x2047) and get all USB HID devices present in system.</summary>
        UsbHid_IgnoreTexasVid = 0x1,
        /// <summary>[Flag] Forces scanner to ignore "unknown" type devices, that are most likely already connected.</summary>
        Ftdi_IgnoreUnknownDev = 0x2,
    }

    /// <summary>
    /// BSL430.NET Device node serve as a generic device indentificator with field 'Name' as a key.
    /// </summary>
    [Serializable]
    public class Bsl430NetDevice
    {
        /// <summary>Name is unique string that serves as main key to handle devices in this library
        /// eg. COM1, FTDI2, libtfi3, USB4 ..</summary>
        public string Name { get; set; } = "";
        /// <summary>Device description.</summary>
        public string Description { get; set; } = "";
        /// <summary>Device kind. [FTDI, libftdi, COM (Serial), USB]</summary>
        public string Kind { get; set; } = "";
        /// <summary>Device general formated description eg. 'V/PID: 1234-5678 [Dev1] SN: 0123456'</summary>
        public string FormattedDescription { get; set; } = "";
        /// <summary>Device mode.</summary>
        public Mode Mode { get; set; } = Mode.UART_Serial;

        /// <summary>
        /// Bsl430NetDevice constructor
        /// </summary>
        public Bsl430NetDevice() { }

        /// <summary>
        /// Bsl430NetDevice copy constructor
        /// </summary>
        public Bsl430NetDevice(Bsl430NetDevice Dev)
        {
            Name = Dev.Name;
            Description = Dev.Description;
            Kind = Dev.Kind;
            FormattedDescription = Dev.FormattedDescription;
            Mode = Dev.Mode;
        }

        /// <summary>
        /// Bsl430NetDevice constructor
        /// </summary>
        public Bsl430NetDevice(string DeviceName)
        {
            Name = DeviceName;
        }
        /// <summary>
        /// Bsl430NetDevice constructor
        /// </summary>
        public Bsl430NetDevice(string DeviceName, string Description, string FullDesc, Mode Mode, string Kind)
        {
            this.Name = DeviceName;
            this.Description = Description;
            this.FormattedDescription = FullDesc;
            this.Mode = Mode;
            this.Kind = Kind;
        }
        /// <summary>
        /// Returns FormattedDescription of current device.
        /// </summary>
        public override string ToString()
        {
            return $"{FormattedDescription}";
        }
    }

    /// <summary>
    /// BSL430.NET Event handler.
    /// </summary>
    public delegate void Bsl430NetEventHandler(object source, Bsl430NetEventArgs e);

    /// <summary>
    /// BSL430.NET Event Args. Progress is double percentage and Report is current action.
    /// </summary>
    [Serializable]
    public class Bsl430NetEventArgs : EventArgs
    {
        /// <summary>0-100% value indicating current progress of main action (Upload, Download, Erase)</summary>
        public double Progress { get; set; }
        /// <summary>Current report which is in progress at the moment.</summary>
        public Report Report { get; set; }
    }

    /// <summary>
    /// BSL430.NET is cross-platform toolkit to manage memory of MSP430 MCUs via UART (FTDI, libftdi), USB or Serial (COM) port.
    /// It is a cheap replacement for stock TI MSP-FET programmer without debug capability. It can Upload, Download, Erase and Scan.
    /// </summary>
    public class BSL430NET : IBsl430Net, IDisposable
    {
        /// <summary>
        /// Event fired when progress of main action changes eg. Open -> Set -> Uploading 1 batch -> next batch ...
        /// </summary>
        public event Bsl430NetEventHandler ProgressChanged = null;

        /// <summary>
        /// Use this constructor to exec ScanAll method only. Any other methods will return error.
        /// </summary>
        public BSL430NET()
        {
            dev = null;
        }

        /// <summary>
        /// Use this constructor for standard, lifelong library operations, and also when you dont know device already.
        /// </summary>
        public BSL430NET(Mode Mode)
        {
            Bsl430NetInit(Mode);
        }

        /// <summary>
        /// Use this constructor when target device is already known and Bsl430NetDevice object is present from previous scan.
        /// </summary>
        public BSL430NET(Bsl430NetDevice DefaultDevice)
        {
            if (DefaultDevice == null)
                throw new Bsl430NetException(462);
            Bsl430NetInit(DefaultDevice.Mode, (DefaultDevice.Name.Trim() == "") ? null : DefaultDevice);
        }

        /// <summary>
        /// Use this constructor when target device name is already known eg. COM1, FTDI2, libftdi3, usb4. Case doesnt matter.
        /// </summary>
        public BSL430NET(string DefaultDeviceName)
        {
            if (DefaultDeviceName.ToLower().Contains(CommFTD2XX.DEVICE_PREFIX.ToLower()))
                Bsl430NetInit(Mode.UART_FTD2XX, new Bsl430NetDevice(DefaultDeviceName, "", "", Mode.UART_FTD2XX, CommFTD2XX.DEVICE_PREFIX));
            else if (DefaultDeviceName.ToLower().Contains(CommLibftdi.DEVICE_PREFIX.ToLower()))
                Bsl430NetInit(Mode.UART_libftdi, new Bsl430NetDevice(DefaultDeviceName, "", "", Mode.UART_libftdi, CommLibftdi.DEVICE_PREFIX));
            else if (DefaultDeviceName.ToLower().Contains(CommUSB.DEVICE_PREFIX.ToLower()))
                Bsl430NetInit(Mode.USB_HID, new Bsl430NetDevice(DefaultDeviceName, "", "", Mode.USB_HID, CommUSB.DEVICE_PREFIX));
            else if (DefaultDeviceName.ToLower().Contains(CommSerial.DEVICE_PREFIX.ToLower()))
                Bsl430NetInit(Mode.UART_Serial, new Bsl430NetDevice(DefaultDeviceName, "", "", Mode.UART_Serial, CommSerial.DEVICE_PREFIX));
            else
                throw new Bsl430NetException(464);
        }

        /// <summary>
        /// Dependecny injection constructor useful for unit testing.
        /// </summary>
        internal BSL430NET(Core CoreInjection)
        {
            dev = CoreInjection;
        }

        /// <summary>
        /// Dependecny injection constructor useful for unit testing.
        /// </summary>
        internal BSL430NET(object CoreInjection)
        {
            dev = (Core)CoreInjection; // TODO - opravdu to tak funguje?? neni treba pretypovat na potomka? list
        }

        private Bsl430NetDevice defaultDevice = new Bsl430NetDevice();
        /// <summary>
        /// DefaultDevice is useful when call public methods without explicitly declaring target device.
        /// </summary>
        public Bsl430NetDevice DefaultDevice
        {
            get { return this.defaultDevice; }
            set
            {
                this.defaultDevice = value;
                if (dev != null)
                    dev.DefaultDevice = value;
            }
        }

        private IDevice dev;
        private Mode mode;

        private void Bsl430NetInit(Mode Mode, Bsl430NetDevice Device = null)
        {
            try
            {
                switch (Mode)
                {
                    case Mode.UART_FTD2XX: dev = new CommFTD2XX(this, Device); break;
                    case Mode.UART_libftdi: dev = new CommLibftdi(this, Device); break;
                    case Mode.UART_Serial: dev = new CommSerial(this, Device); break;
                    case Mode.USB_HID: dev = new CommUSB(this, Device); break;
                }
                this.mode = Mode;
                this.DefaultDevice = Device;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Scan for all available devices in multimode (FTDI, libftdi, USB and Serial) and return more specific data.
        /// </summary>
        public (Status Ftdi, Status Libftdi, Status Usb, Status Serial) ScanAll(out List<FTDI_Device> FtdiDevices,
                                                                                out List<Libftdi_Device> LibftdiDevices,
                                                                                out List<USB_HID_Device> UsbDevices,
                                                                                out List<Serial_Device> SerialDevices,
                                                                                ScanOptions ScanOpt = ScanOptions.None)
        {
            Status status_ftdi;
            Status status_libftdi;
            Status status_usb;
            Status status_serial;

            try
            {
                using (IDevice dev_ftdi = new CommFTD2XX())
                {
                    status_ftdi = dev_ftdi.Scan(out FtdiDevices, ScanOpt);
                }
            }
            catch (Exception ex)
            {
                FtdiDevices = new List<FTDI_Device>();
                status_ftdi = Utils.StatusCreate(465, ex.Message);
            }

            try
            {
                using (IDevice dev_libftdi = new CommLibftdi())
                {
                    status_libftdi = dev_libftdi.Scan(out LibftdiDevices, ScanOpt);
                }
            }
            catch (Exception ex)
            {
                LibftdiDevices = new List<Libftdi_Device>();
                status_libftdi = Utils.StatusCreate(465, ex.Message);
            }

            try
            {
                using (IDevice dev_usb = new CommUSB())
                {
                    status_usb = dev_usb.Scan(out UsbDevices, ScanOpt);
                }
            }
            catch (Exception ex)
            {
                UsbDevices = new List<USB_HID_Device>();
                status_usb = Utils.StatusCreate(465, ex.Message);
            }

            try
            {
                using (IDevice dev_uart = new CommSerial())
                {
                    status_serial = dev_uart.Scan(out SerialDevices, ScanOpt);
                }
            }
            catch (Exception ex)
            {
                SerialDevices = new List<Serial_Device>();
                status_serial = Utils.StatusCreate(465, ex.Message);
            }

            return (Ftdi: status_ftdi, Libftdi: status_libftdi, Usb: status_usb, Serial: status_serial);
        }

        /// <summary>
        /// Scan for all available devices in multimode (FTDI, libftdi, USB and Serial) and return uniform less specific data.
        /// </summary>
        public (Status Ftdi, Status Libftdi, Status Usb, Status Serial) ScanAll(out List<Bsl430NetDevice> Devices,
                                                                                ScanOptions ScanOpt = ScanOptions.None)
        {
            var ret = ScanAll(out List<FTDI_Device> _ftdi,
                              out List<Libftdi_Device> _libftdi,
                              out List<USB_HID_Device> _usb,
                              out List<Serial_Device> _serial, 
                              ScanOpt);

            Devices = new List<Bsl430NetDevice>();
            Devices.AddRange(_ftdi.Select(x => new Bsl430NetDevice(x)).ToList());
            Devices.AddRange(_libftdi.Select(x => new Bsl430NetDevice(x)).ToList());
            Devices.AddRange(_usb.Select(x => new Bsl430NetDevice(x)).ToList());
            Devices.AddRange(_serial.Select(x => new Bsl430NetDevice(x)).ToList());

            return ret;
        }

        /// <summary>
        /// Scan for devices in single mode (FTDI, libftdi, USB or Serial).
        /// </summary>
        public Status Scan<Tdev>(out List<Tdev> DeviceList,
                                 ScanOptions ScanOpt = ScanOptions.None) where Tdev : Bsl430NetDevice
        {
            return dev.Scan(out DeviceList, ScanOpt) ?? Utils.StatusCreate(466);
        }

        /// <summary>
        /// Erase deletes the entire flash memory area except Information Memory if protocol 5xx6xx is used. 
        /// Please see MCU datasheet for detailed information, or TI BSL doc (slau319t.pdf).
        /// </summary>
        public StatusEx Erase(Bsl430NetDevice Device = null)
        {
            return dev.Erase(Device) ?? Utils.StatusCreateEx(466);
        }

        /// <summary>
        /// Erase deletes the entire flash memory area except Information Memory if protocol 5xx6xx is used. 
        /// Please see MCU datasheet for detailed information, or TI BSL doc (slau319t.pdf). device_name case dont matter.
        /// </summary>
        public StatusEx Erase(string DeviceName)
        {
            return Erase(new Bsl430NetDevice(DeviceName)) ?? Utils.StatusCreateEx(466);
        }

        /// <summary>
        /// Uploads data from firmware_path to target MCU. Supported file formats are TI-TXT, Intel-HEX and ELF.
        /// If none, null or invalid password is entered, mass erase is executed first.
        /// </summary>
        public StatusEx Upload(string FirmwarePath, Bsl430NetDevice Device = null, byte[] Password = null)
        {
            return dev.Upload(Device, FirmwarePath, Password) ?? Utils.StatusCreateEx(466);
        }

        /// <summary>
        /// Uploads data from firmware_path to target MCU. Supported file formats are TI-TXT, Intel-HEX and ELF.
        /// If none, null or invalid password is entered, mass erase is executed first. device_name case dont matter.
        /// </summary>
        public StatusEx Upload(string FirmwarePath, string DeviceName, byte[] Password = null)
        {
            return Upload(FirmwarePath, new Bsl430NetDevice(DeviceName), Password) ?? Utils.StatusCreateEx(466);
        }

        /// <summary>
        /// Downloads bytes from target MCU starting from address 'addr_start' to 'addr_start' + 'data_size'.
        /// If wrong password is entered, mass erase is auto executed as a safety measure, erasing entire flash.
        /// </summary>
        public StatusEx Download(byte[] Password, int AddrStart, int DataSize, out List<byte> Data, Bsl430NetDevice Device = null)
        {
            return dev.Download(Device, Password, AddrStart, DataSize, out Data) ?? Utils.StatusCreateEx(466);
        }

        /// <summary>
        /// Downloads bytes from target MCU starting from address 'addr_start' to 'addr_start' + 'data_size'.
        /// If wrong password is entered, mass erase is auto executed as a safety measure, erasing entire flash.
        /// device_name case dont matter.
        /// </summary>
        public StatusEx Download(byte[] Password, int AddrStart, int DataSize, out List<byte> Data, string DeviceName)
        {
            return Download(Password, AddrStart, DataSize, out Data, new Bsl430NetDevice(DeviceName)) ?? Utils.StatusCreateEx(466);
        }

        /// <summary>
        /// Sets baud rate. Default is 9600 bps.
        /// Baud rate is applicable only in UART mode and only in some BSL revs.
        /// </summary>
        public Status SetBaudRate(BaudRate BaudRate)
        {
            return dev.ChangeBaudRate(BaudRate) ?? Utils.StatusCreateEx(466);
        }

        /// <summary>
        /// Sets invoke mechanism mode. Default is DTR_RST__RTS_TEST.
        /// Note that USB HID mode requires manual BSL invocation.
        /// </summary>
        public Status SetInvokeMechanism(InvokeMechanism InvokeMechanism)
        {
            return dev.SetInvoke(InvokeMechanism) ?? Utils.StatusCreateEx(466);
        }

        /// <summary>
        /// Sets MCU family. Default is MSP430_F5xx.
        /// Please see TI BSL doc (slau319t.pdf) for supported MCUs and their modes.
        /// </summary>
        public Status SetMCU(MCU Mcu)
        {
            return dev.SetMCU(Mcu) ?? Utils.StatusCreateEx(466);
        }

        /// <summary>
        /// Returns currently set baud rate.
        /// </summary>
        public BaudRate GetBaudRate()
        {
            return dev.GetBaudRate();
        }

        /// <summary>
        /// Returns currently set invoke mechanism.
        /// </summary>
        public InvokeMechanism GetInvokeMechanism()
        {
            return dev.GetInvoke();
        }

        /// <summary>
        /// Returns currently set MCU family.
        /// </summary>
        public MCU GetMCU()
        {
            return dev.GetMCU();
        }

        /// <summary>
        /// Returns current BSL430.NET instance operating mode (UART_FTDXX, UART_libftdi, UART_Serial, USB_HID).
        /// </summary>
        public Mode GetMode()
        {
            return this.mode;
        }

        /// <summary>
        /// Closes device, disposes all handles and suppresses any errors.
        /// </summary>
        public void Dispose()
        {
            dev?.Dispose();
        }

        /// <summary>
        /// Returns string of current instance mode and default device name.
        /// </summary>
        public override string ToString()
        {
            return dev != null ? $"Mode:{mode}, Default Device:{DefaultDevice.Name}" : "";
        }

        /// <summary>
        /// Interrupts all current processes in all intances by initiating a throw of an exception, effectively breaking 
        /// anything in progress. This method is just not recommended to call, however it is the only way how perform
        /// "valid" termination, when user requires immediate hard stop, without using stuff like thread terminate.
        /// It will invalidate current action, but resources will stay safe. Delay of effect is variable, but less than 1s.
        /// </summary>
        public static void Interrupt()
        {
            Interrupted = true;
        }
        private static bool _Interrupted = false;
        internal static bool Interrupted
        {
            get
            {
                if (_Interrupted)
                {
                    _Interrupted = false;
                    return true;
                }
                return false;
            }
            set { _Interrupted = value; }
        }
        internal void ProgressUpdate(double Progress, Report Report)
        {
            this.ProgressChanged?.Invoke(this, new Bsl430NetEventArgs { Progress = Progress, Report = Report });
        }
    }
}
