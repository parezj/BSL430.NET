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
    /// BLS UART Message. If there are any errors with the data transmission, an error message is sent back.
    /// This message is sent only when 5xx or 6xx protocol version is used.
    /// </summary>
    public enum BslUartMessage : byte
    {
        /// <summary>ACK</summary>
        ACK = 0x00,
        /// <summary>Header incorrect. The packet did not begin with the required 0x80 value.</summary>
        HeaderIncorrect = 0x51,
        /// <summary>Checksum incorrect. The packet did not have the correct checksum value.</summary>
        ChecksumIncorrect = 0x52,
        /// <summary>Packet size zero. The size for the BSL core command was given as 0.</summary>
        PacketSizeZero = 0x53,
        /// <summary>Packet size exceeds buffer. The packet size given is too big for the RX buffer.</summary>
        PacketSizeOverflow = 0x54,
        /// <summary>Unknown error.</summary>
        UnknownError = 0x55,
        /// <summary>Unknown baud rate. The supplied data for baud rate change is not a known value.</summary>
        UnknownBaudRate = 0x56,
        /// <summary>BSL UART Message is not available.</summary>
        NotAvailable = 0x99
    }

    /// <summary>
    /// BSL Core Message is a response to Erase or Download action. (Mass Erase, TX Data Block)
    /// This message is sent only when 5xx or 6xx protocol version is used.
    /// </summary>
    public enum BslCoreMessage : byte
    {
        /// <summary>Operation Successful.</summary>
        Success = 0x00,
        /// <summary>Flash Write Check Failed. After programming, a CRC is run on the programmed data.
        /// If the CRC does not match the expected result, this error is returned.
        /// </summary>
        FlashWriteCheckFail = 0x01,
        /// <summary>Flash Fail Bit Set. An operation set the FAIL bit in the flash controller
        /// (see the MSP430x5xx and MSP430x6xx Family User's Guide for more details on the flash fail bit).</summary>
        FlashFailBitSet = 0x02,
        /// <summary>Voltage Change During Program. The VPE was set during the requested write operation
        /// (see the MSP430x5xx and MSP430x6xx Family User's Guide for more details on the VPE bit).</summary>
        VoltageChanged = 0x03,
        /// <summary>BSL Locked. The correct password has not yet been supplied to unlock the BSL.</summary>
        BSLLocked = 0x04,
        /// <summary>BSL Password Error. An incorrect password was supplied to the BSL when attempting an unlock.</summary>
        BSLPasswordError = 0x05,
        /// <summary>Byte Write Forbidden. This error is returned when a byte write is attempted in a flash area.</summary>
        ByteWriteForbidden = 0x06,
        /// <summary>Unknown Command. The command given to the BSL was not recognized.</summary>
        UnknownCommand = 0x07,
        /// <summary>Packet Length Exceeds Buffer Size.
        /// The supplied packet length value is too large to be held in the BSL receive buffer.
        /// </summary>
        PacketLengthOverflow = 0x08,
        /// <summary>BSL Core Message is not available.</summary>
        NotAvailable = 0x99
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
    public class Bsl430NetDevice
    {
        /// <summary>Name is the key unique element by which this library handles devices
        /// eg. COM1, FTDI2, libtfi3, USB4 ..</summary>
        public readonly string Name = "";
        /// <summary>Device description.</summary>
        public readonly string Description = "";
        /// <summary>Device full available description eg. Name, Type, Serial ...</summary>
        public readonly string FullDescription = "";
        /// <summary>Device mode.</summary>
        public readonly Mode Mode = Mode.UART_Serial;

        /// <summary>
        /// Bsl430NetDevice constructor
        /// </summary>
        public Bsl430NetDevice()
        {
        }

        /// <summary>
        /// Bsl430NetDevice constructor
        /// </summary>
        public Bsl430NetDevice(string device_name)
        {
            Name = device_name;
        }
        /// <summary>
        /// Bsl430NetDevice constructor
        /// </summary>
        public Bsl430NetDevice(string device_name, string description, string fulldesc, Mode mode)
        {
            Name = device_name;
            Description = description;
            FullDescription = fulldesc;
            Mode = mode;
        }
        /// <summary>
        /// Returns FullDescription of current device.
        /// </summary>
        public override string ToString()
        {
            return $"{FullDescription}";
        }
    }

    /// <summary>
    /// Status class is an Error Cluster with int, bool and string status with Core and UART messages.
    /// </summary>
    public class Status
    {
        /// <summary>Numeric representation of status, any value other then 0 indicates an error.</summary>
        public int Error { get; set; } = 0;
        /// <summary>Boolean representation of status, true means OK, false means ERROR.</summary>
        public bool OK { get; set; } = false;
        /// <summary>String representation of status.</summary>
        public string Msg { get; set; } = "unknown error";
        /// <summary>BslCoreMessage status.</summary>
        public BslCoreMessage CoreStatus { get; set; } = BslCoreMessage.NotAvailable;
        /// <summary>BslUartMessage status.</summary>
        public BslUartMessage UartStatus { get; set; } = BslUartMessage.NotAvailable;
    }

    /// <summary>
    /// StatusEx is Status extended with Report List, returned as a result of main public methods.
    /// </summary>
    public class StatusEx : Status
    {
        /// <summary>Number of bytes that were processed (uploaded/downloaded to/from target MCU).</summary>
        public int BytesProcessed { get; set; } = 0;
        /// <summary>Null, 4-byte or 10-byte array, meaning differs, please see TI BSL doc (slau319t.pdf).</summary>
        public byte[] BSLVersion { get; set; }
        /// <summary>Report List.</summary>
        public List<Report> Reports { get; set; }
    }

    /// <summary>
    /// Report is result of an action block with Name, Result and Timestamp.
    /// </summary>
    public class Report
    {
        /// <summary>Report Name is headline of action.</summary>
        public string Name { get; set; } = "unknown report";
        /// <summary>Report Result indicates the result of action.</summary>
        public ReportResult Result { get; set; } = ReportResult.FAILED;
        /// <summary>Report Timestamp is timestamp of Report creation.</summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Report result.
    /// </summary>
    public enum ReportResult
    {
        /// <summary>Action headlined by Report Name completed successfully.</summary>
        SUCCESS,
        /// <summary>Action headlined by Report Name was skipped.</summary>
        SKIPPED,
        /// <summary>Action headlined by Report Name failed.</summary>
        FAILED,
        /// <summary>Action headlined by Report Name is currently in progress.</summary>
        PENDING
    }

    /// <summary>
    /// BSL430.NET Event handler.
    /// </summary>
    public delegate void Bsl430NetEventHandler(object source, Bsl430NetEventArgs e);


    /// <summary>
    /// BSL430.NET Event Args. Progress is double percentage and Report is current action.
    /// </summary>
    public class Bsl430NetEventArgs : EventArgs
    {
        /// <summary>0-100% value indicating current progress of main action (Upload, Download, Erase)</summary>
        public double Progress { get; set; }
        /// <summary>Current report which is in progress at the moment.</summary>
        public Report Report { get; set; }
    }

    /// <summary>
    /// BSL430.NET Exception is generic Exception extended with Status object.
    /// </summary>
    public class Bsl430NetException : Exception
    {
        /// <summary>BSL430.NET Status</summary>
        public Status Status { get; set; }
        /// <summary>
        /// Bsl430NetException constructor
        /// </summary>
        public Bsl430NetException(string message) : base(message) { }
        /// <summary>
        /// Bsl430NetException constructor
        /// </summary>
        public Bsl430NetException(Status _status) : base(_status.Msg) { Status = _status; }
        /// <summary>
        /// Bsl430NetException constructor
        /// </summary>
        public Bsl430NetException(int err_code) : base(Utils.StatusCreate(err_code).Msg)
        {
            Status = Utils.StatusCreate(err_code);
        }
        /// <summary>
        /// Bsl430NetException constructor
        /// </summary>
        public Bsl430NetException(int err_code, Exception ex) :
            base($"{Utils.StatusCreate(err_code).Msg}\n[MSG]:{ex.Message}{((Const.IS_DEBUG) ? $"\n{ex.StackTrace}" : "")}")
        {
            Status = Utils.StatusCreate(err_code);
            Status.Msg += $"\n[msg]:{ex.Message}{((Const.IS_DEBUG) ? $"\n{ex.StackTrace}" : "")}";
        }
        /// <summary>
        /// Bsl430NetException constructor
        /// </summary>
        public Bsl430NetException(int err_code, string extra) :
            base($"{Utils.StatusCreate(err_code).Msg}\n[MSG]:{extra}")
        {
            Status = Utils.StatusCreate(err_code);
            Status.Msg += $"\n[msg]:{extra}";
        }
    }

    /// <summary>
    /// BSL430.NET is cross-platform toolkit to manage memory of MSP430 MCUs via UART or USB.
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
        public BSL430NET(Mode mode)
        {
            Bsl430NetInit(mode);
        }

        /// <summary>
        /// Use this constructor when target device is already known and Bsl430NetDevice object is present from previous scan.
        /// </summary>
        public BSL430NET(Bsl430NetDevice default_device)
        {
            if (default_device == null)
                throw new Bsl430NetException(462);
            Bsl430NetInit(default_device.Mode, (default_device.Name.Trim() == "") ? null : default_device);
        }

        /// <summary>
        /// Use this constructor when target device name is already known eg. COM1, FTDI2, libftdi3, usb4. Case doesnt matter.
        /// </summary>
        public BSL430NET(string default_device_name)
        {
            if (default_device_name.ToLower().Contains(CommFTD2XX.DEVICE_PREFIX.ToLower()))
                Bsl430NetInit(Mode.UART_FTD2XX, new Bsl430NetDevice(default_device_name, "", "", Mode.UART_FTD2XX));
            else if (default_device_name.ToLower().Contains(CommLibftdi.DEVICE_PREFIX.ToLower()))
                Bsl430NetInit(Mode.UART_libftdi, new Bsl430NetDevice(default_device_name, "", "", Mode.UART_libftdi));
            else if (default_device_name.ToLower().Contains(CommUSB.DEVICE_PREFIX.ToLower()))
                Bsl430NetInit(Mode.USB_HID, new Bsl430NetDevice(default_device_name, "", "", Mode.USB_HID));
            else if (default_device_name.ToLower().Contains(CommSerial.DEVICE_PREFIX.ToLower()))
                Bsl430NetInit(Mode.UART_Serial, new Bsl430NetDevice(default_device_name, "", "", Mode.UART_Serial));
            else
                throw new Bsl430NetException(464);
        }

        /// <summary>
        /// Dependecny injection constructor useful for unit testing.
        /// </summary>
        internal BSL430NET(Core core_injection)
        {
            dev = core_injection;
        }

        /// <summary>
        /// Dependecny injection constructor useful for unit testing.
        /// </summary>
        internal BSL430NET(object core_injection)
        {
            dev = (Core)core_injection; // todo - opravdu to tak funguje?? neni treba pretypovat na potomka? list
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

        private void Bsl430NetInit(Mode mode, Bsl430NetDevice device = null)
        {
            try
            {
                switch (mode)
                {
                    case Mode.UART_FTD2XX: dev = new CommFTD2XX(this, device); break;
                    case Mode.UART_libftdi: dev = new CommLibftdi(this, device); break;
                    case Mode.UART_Serial: dev = new CommSerial(this, device); break;
                    case Mode.USB_HID: dev = new CommUSB(this, device); break;
                }
                this.mode = mode;
                this.DefaultDevice = device;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Scan for all available devices in multimode (FTDI, libftdi, USB and Serial).
        /// </summary>
        public (Status ftdi, Status libftdi, Status usb, Status serial) ScanAll(out List<FTDI_Device> ftdi_devices,
                                                                                out List<Libftdi_Device> libftdi_devices,
                                                                                out List<USB_HID_Device> usb_devices,
                                                                                out List<Serial_Device> serial_devices,
                                                                                ScanOptions scan_opt = ScanOptions.None)
        {
            Status status_ftdi;
            Status status_libftdi;
            Status status_usb;
            Status status_serial;

            try
            {
                using (IDevice dev_ftdi = new CommFTD2XX())
                {
                    status_ftdi = dev_ftdi.Scan(out ftdi_devices, scan_opt);
                }
            }
            catch (Exception ex)
            {
                ftdi_devices = new List<FTDI_Device>();
                status_ftdi = Utils.StatusCreate(465, ex.Message);
            }

            try
            {
                using (IDevice dev_libftdi = new CommLibftdi())
                {
                    status_libftdi = dev_libftdi.Scan(out libftdi_devices, scan_opt);
                }
            }
            catch (Exception ex)
            {
                libftdi_devices = new List<Libftdi_Device>();
                status_libftdi = Utils.StatusCreate(465, ex.Message);
            }

            try
            {
                using (IDevice dev_usb = new CommUSB())
                {
                    status_usb = dev_usb.Scan(out usb_devices, scan_opt);
                }
            }
            catch (Exception ex)
            {
                usb_devices = new List<USB_HID_Device>();
                status_usb = Utils.StatusCreate(465, ex.Message);
            }

            try
            {
                using (IDevice dev_uart = new CommSerial())
                {
                    status_serial = dev_uart.Scan(out serial_devices, scan_opt);
                }
            }
            catch (Exception ex)
            {
                serial_devices = new List<Serial_Device>();
                status_serial = Utils.StatusCreate(465, ex.Message);
            }

            return (ftdi: status_ftdi, libftdi: status_libftdi, usb: status_usb, serial: status_serial);
        }

        /// <summary>
        /// Scan for devices in single mode (FTDI, libftdi, USB or Serial).
        /// </summary>
        public Status Scan<Tdev>(out List<Tdev> device_list,
                                 ScanOptions scan_opt = ScanOptions.None) where Tdev : Bsl430NetDevice
        {
            return dev.Scan(out device_list, scan_opt) ?? Utils.StatusCreate(466);
        }

        /// <summary>
        /// Erase deletes the entire flash memory area except Information Memory if protocol 5xx6xx is used. 
        /// Please see MCU datasheet for detailed information, or TI BSL doc (slau319t.pdf).
        /// </summary>
        public StatusEx Erase(Bsl430NetDevice device = null)
        {
            return dev.Erase(device)?? Utils.StatusCreateEx(466);
        }

        /// <summary>
        /// Erase deletes the entire flash memory area except Information Memory if protocol 5xx6xx is used. 
        /// Please see MCU datasheet for detailed information, or TI BSL doc (slau319t.pdf). device_name case dont matter.
        /// </summary>
        public StatusEx Erase(string device_name)
        {
            return Erase(new Bsl430NetDevice(device_name)) ?? Utils.StatusCreateEx(466);
        }

        /// <summary>
        /// Uploads data from firmware_path to target MCU. Supported file formats are TI-TXT, Intel-HEX and ELF.
        /// If none, null or invalid password is entered, mass erase is executed first.
        /// </summary>
        public StatusEx Upload(string firmware_path, Bsl430NetDevice device = null, byte[] password = null)
        { 
            return dev.Upload(device, firmware_path, password) ?? Utils.StatusCreateEx(466);
        }

        /// <summary>
        /// Uploads data from firmware_path to target MCU. Supported file formats are TI-TXT, Intel-HEX and ELF.
        /// If none, null or invalid password is entered, mass erase is executed first. device_name case dont matter.
        /// </summary>
        public StatusEx Upload(string firmware_path, string device_name, byte[] password = null)
        {
            return Upload(firmware_path, new Bsl430NetDevice(device_name), password) ?? Utils.StatusCreateEx(466);
        }

        /// <summary>
        /// Downloads bytes from target MCU starting from address 'addr_start' to 'addr_start' + 'data_size'.
        /// If wrong password is entered, mass erase is auto executed as a safety measure, erasing entire flash.
        /// </summary>
        public StatusEx Download(byte[] password, int addr_start, int data_size, out List<byte> data, Bsl430NetDevice device = null)
        {
            return dev.Download(device, password, addr_start, data_size, out data) ?? Utils.StatusCreateEx(466);
        }

        /// <summary>
        /// Downloads bytes from target MCU starting from address 'addr_start' to 'addr_start' + 'data_size'.
        /// If wrong password is entered, mass erase is auto executed as a safety measure, erasing entire flash.
        /// device_name case dont matter.
        /// </summary>
        public StatusEx Download(byte[] password, int addr_start, int data_size, out List<byte> data, string device_name)
        {
            return Download(password, addr_start, data_size, out data, new Bsl430NetDevice(device_name)) ?? Utils.StatusCreateEx(466);
        }

        /// <summary>
        /// Sets baud rate. Default is 9600 bps.
        /// Baud rate is applicable only in UART mode and only in some BSL revs.
        /// </summary>
        public Status SetBaudRate(BaudRate baud_rate) 
        { 
            return dev.ChangeBaudRate(baud_rate) ?? Utils.StatusCreateEx(466);
        }

        /// <summary>
        /// Sets invoke mechanism mode. Default is DTR_RST__RTS_TEST.
        /// Note that USB HID mode requires manual BSL invocation.
        /// </summary>
        public Status SetInvokeMechanism(InvokeMechanism invoke_mechanism) 
        { 
            return dev.SetInvoke(invoke_mechanism) ?? Utils.StatusCreateEx(466);
        }

        /// <summary>
        /// Sets MCU family. Default is MSP430_F5xx.
        /// Please see TI BSL doc (slau319t.pdf) for supported MCUs and their modes.
        /// </summary>
        public Status SetMCU(MCU mcu) 
        { 
            return dev.SetMCU(mcu) ?? Utils.StatusCreateEx(466);
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

        internal void ProgressUpdate(double progress, Report report)
        {
            this.ProgressChanged?.Invoke(this, new Bsl430NetEventArgs { Progress = progress, Report = report });
        }
    }
}
