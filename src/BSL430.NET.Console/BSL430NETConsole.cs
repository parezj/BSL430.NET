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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Serialization;

using BSL430_NET;
using BSL430_NET.Comm;
using BSL430_NET.FirmwareTools;
using BSL430_NET.FirmwareTools.Helpers;
using BSL430_NET.Utility;

using CommandLine;
using CommandLine.Text;
using ConsoleTables;


namespace BSL430_NET_Console
{
    class BSL430NETConsole
    {
        #region Public Consts
        public const string ALIAS_FTDI = "ftdi";
        public const string ALIAS_LIBFTDI = "libftdi";
        public const string ALIAS_USBHID = "usb";
        public const string ALIAS_SERIAL = "serial";
        public const string ALIAS_ALL = "all";
        public const int SPINNER_PERIOD = 70;
        #endregion

        #region Private Data
        private object lock1 = new object();
        private Options options;
        private int spinner = 0;
        private string vertical = "│";
        private string horizontal = "─";
        private char progress = '█'; // ■
        #endregion

        #region Args Options
        class Options
        {
            // -- Main Commands
            
            [Option('s', "scan", Required = false, HelpText = "== Main Command == Device scan. Choose from: '" + ALIAS_FTDI + "', '"
                                                                                                                + ALIAS_LIBFTDI + "', '"
                                                                                                                + ALIAS_USBHID + "', '"
                                                                                                                + ALIAS_SERIAL + "', '"
                                                                                                                + ALIAS_ALL + "'.")]
            public string ScanMode { get; set; } = "";

            [Option('u', "upload", Required = false, HelpText = "== Main Command == Upload firmware to specified device eg. FTDI1 from TI-TXT, Intel-HEX, SREC or ELF file.")]
            public string Upload { get; set; } = "";

            [Option('d', "download", Required = false, HelpText = "== Main Command == Download firmware from specified device eg. USB2. and write it to TI-TXT, Intel-HEX or SREC file (ELF is not supported yet).")]
            public string Download { get; set; } = "";

            [Option('e', "erase", Required = false, HelpText = "== Main Command == Erase specified device eg. COM3.")]
            public string Erase { get; set; } = "";

            [Option('c', "convert", Required = false, HelpText = "== Main Command == Convert specified firmware file and write it to -f path. Set -o output fw format: TI-TXT, Intel-HEX or SREC (TI-TXT is default. ELF is not supported yet).")]
            public string Convert { get; set; } = "";

            [Option('n', "combine", Required = false, HelpText = "== Main Command == Combine specified firmware file with another from -j path and write it to -f path. Set -o output fw format: TI-TXT, Intel-HEX or SREC (TI-TXT is default. ELF is not supported yet).")]
            public string Combine { get; set; } = "";

            [Option('v', "validate", Required = false, HelpText = "== Main Command == Read and parse firmware file (format auto-detected; TI-TXT, Intel-HEX, SREC and ELF supported) and print information (first addr, last addr, size, crc16..) and status if firmware is valid or not.")]
            public string Validate { get; set; } = "";

            [Option('r', "get-password", Required = false, HelpText = "== Main Command == Read and parse firmware file (format auto-detected; TI-TXT, Intel-HEX, SREC and ELF supported) and print BSL password (last 16 bytes of interrupt vector table 0xFFE0 - 0xFFFF).")]
            public string GetPassword { get; set; } = "";

            [Option('k', "compare", Required = false, HelpText = "== Main Command == Compare two firmware files (format auto-detected; TI-TXT, Intel-HEX, SREC and ELF supported) and print result (Equal - True/False, Match - percentage [0.0 ; 100.0] %, BytesDiff - count of different byte nodes).")]
            public string Compare { get; set; } = "";

            // -- Parameters

            [Option('f', "file", Required = false, HelpText = "Full or relative path to firmware file. (TI-TXT, Intel-HEX, SREC or ELF).")]
            public string File { get; set; } = "";

            [Option('j', "second-file", Required = false, HelpText = "Full or relative path to another firmware file, usually for Combine or Compare. (TI-TXT, Intel-HEX, SREC or ELF).")]
            public string SecondFile { get; set; } = "";

            [Option('p', "password", Required = false, HelpText = "BSL password is 16 bytes long. Enter 32 chars long hex string. For upload and erase this is optional.")]
            public string Password { get; set; } = "";

            [Option('a', "addrstart", Required = false, HelpText = "(Download only) start download from 16bit address specified. eg. '0F00'")]
            public string AddrStart { get; set; } = "";

            [Option('z', "datasize", Required = false, HelpText = "(Download only) download specified amount of bytes in hex. eg. 'FFFF'")]
            public string DataSize { get; set; } = "";

            [Option('b', "baudrate", Required = false, HelpText = "Sets baud rate. Default is 9600 bps (9600).")]
            public string BaudRate { get; set; } = "";

            [Option('i', "invoke", Required = false, HelpText = "Sets invoke mechanism mode. Default is SHARED_JTAG (0).")]
            public string InvokeMech { get; set; } = "";

            [Option('m', "mcu", Required = false, HelpText = "Sets MCU family. Default is MSP430_F5xx (4).")]
            public string MCU { get; set; } = "";
            
            [Option('l', "fill-ff", Required = false, HelpText = "Optional. Force output fw monolithic structure as missing addr nodes are filled with 0xFF.")]
            public bool FillFF { get; set; } = false;

            [Option('x', "xml", Required = false, HelpText = "Optional. XML Log output file. Can be only filename or absolute path.")]
            public string Xml { get; set; } = "";

            [Option('t', "usb-ignore-ti-vid", Required = false, HelpText = "Optional. When scanning USB HID, ignore TI VID(0x2047) and get all USB HID devices present in system.")]
            public bool UsbHid_IgnoreTexasVid { get; set; } = false;

            [Option('w', "ftdi-ignore-unknown", Required = false, HelpText = "Optional. When scanning FTDI, ignore UNKNOWN type devices, that are most likely already connected.")]
            public bool Ftdi_IgnoreUnknownDev { get; set; } = false;

            [Option('g', "force-ascii", Required = false, HelpText = "Optional. Default output text encoding is UTF-8, however if you need ASCII only, use this option.")]
            public bool ForceAscii { get; set; } = false;

            [Option('o', "output-format", Required = false, HelpText = "Optional. Download or convert firmware format output mode: TI-TXT, Intel-HEX or SREC (TI-TXT is default. ELF is not supported yet as output).")]
            public string OutputFormat { get; set; } = "";

            [Option('y', "fw-line-length", Required = false, HelpText = "Optional. Defines amount of data bytes present at final firmware text file at single row.  When = 0, default values are set (TI-TXT = 16, Intel-HEX = 32, SREC = 32).")]
            public int FwLineLength { get; set; } = 0;

            [Usage(ApplicationAlias = "BSL430.NET.Console")]
            public static IEnumerable<Example> Examples => new List<Example>
            {
                new Example($"-\nScan for all devices", new Options { ScanMode = "all" }),
                new Example($"-\nScan for all devices and save XML output file.", new Options { ScanMode = "all", Xml = "scan.xml" }),
                new Example($"-\nScan for single mode devices", new Options { ScanMode = "ftdi" }),
                new Example($"-\nUpload firmware to device", new Options { Upload = "libftdi1", File = "fw.hex", MCU = "MSP430_F5xx", BaudRate = "115200", Xml = "log.xml" }),
                new Example($"-\nDownload firmware from device", new Options { Download = "usb2", File = "fw.hex", AddrStart = "0x8000", DataSize = "0x7FFF", Password = "16xFF", MCU = "MSP430_F5xx", BaudRate = "115200", OutputFormat = "TI_TXT", FwLineLength = 16 }),
                new Example($"-\nErase device", new Options { Erase = "serial3", MCU = "MSP430_F5xx" }),
                new Example($"-\nConvert firmware to Intel-HEX", new Options { Convert = "fw_ti.txt", File = "fw_intel.hex", OutputFormat = "INTEL_HEX", FillFF = false, FwLineLength = 64 }),
                new Example($"-\nConvert firmware to monolithic TI-TXT", new Options { Convert = "fw_elf.out", File = "fw_ti.txt",  OutputFormat = "TI_TXT", FillFF = true, FwLineLength = 32 }),
                new Example($"-\nCombine TI-TXT firmware with INTEL-HEX firmware", new Options { Combine = "fw_ti.txt", SecondFile = "fw_intel.hex", File = "fw_combined_ti.txt",  OutputFormat = "TI_TXT" }),
                new Example($"-\nCompare ELF firmware with TI-TXT firmware", new Options { Compare = "fw_elf.out", SecondFile = "fw_ti.txt" }),
                new Example($"-\nGet BSL password from firmware", new Options { GetPassword = "fw_elf.out" }),
                new Example($"-\nValidate firmware file", new Options { Validate = "fw_elf.out" })
            };
            public static void PrintEnumsAndInfo()
            {
                Console.WriteLine($"\nSupported baud rates:\n{Str('-', 21)}\n{GetEnumInfo<BaudRate, int>('\n', ": ", 13)}");
                Console.WriteLine($"\nSupported invoke mechanisms:\n{Str('-', 28)}\n{GetEnumInfo<InvokeMechanism, byte>('\n', ": ", 16)}");
                Console.WriteLine($"\nSupported MCU families:\n{Str('-', 23)}\n{GetEnumInfo<MCU, byte>('\n', ": ", 15)}");
                Console.WriteLine($"\nSupported firmware formats:\n{Str('-', 27)}\n{GetEnumInfo<FwTools.FwFormat, byte>('\n', ": ", 12)}");

                Console.WriteLine();
                Console.WriteLine($"          ____  _____ __    __ __  _____ ____     _   ______________");
                Console.WriteLine($"         / __ )/ ___// /   / // / |__  // __ \\   / | / / ____/_  __/");
                Console.WriteLine($"        / __  |\\__ \\/ /   / // /_  /_ </ / / /  /  |/ / __/   / /   ");
                Console.WriteLine($"       / /_/ /___/ / /___/__  __/___/ / /_/ /_ / /|  / /___  / /    ");
                Console.WriteLine($"      /_____//____/_____/  /_/  /____/\\____/(_)_/ |_/_____/ /_/     ");
                
                Console.WriteLine($"\nCopyright (c) 2018 Jakub Parez. All rights reserved.");
                Console.WriteLine("This work is licensed under the terms of the MIT license.");
                Console.WriteLine("For a copy, see <https://opensource.org/licenses/MIT>.");
                Console.WriteLine("==========================================================================");
                Console.Write("For more info about BSL430.NET visit ");
                ConsoleColor _color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"https://github.com/parezj/BSL430.NET/");
                Console.ForegroundColor = _color;
                Console.WriteLine();
            }
        }
        #endregion

        #region Entry Point - Main
        static void Main(string[] args)
        {
            System.AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.CursorVisible = false;
            BSL430NETConsole main = new BSL430NETConsole();
            if (System.Environment.OSVersion.Platform == PlatformID.Win32NT)
                WinSetConsoleFont.SetConsoleFont();
            main.BSL430NET(args);
        }
        #endregion

        #region Main Core Method
        private void BSL430NET(string[] args)
        {
            bool blank = false;
            if (args == null || args.Length == 0)
            {
                args = new string[] { "--help" };
                blank = true;
            }
                
            var results = CommandLine.Parser.Default.ParseArguments<Options>(args)
                                                    .WithParsed<Options>(opts => RunOptionsAndReturnExitCode(opts))
                                                    .WithNotParsed<Options>((errs) => HandleParseError(errs, blank));

            if (options == null)
                return;

            if (options.Xml != "" && !System.IO.Path.IsPathRooted(options.Xml))
                options.Xml = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(options.Xml));

            if (options.File != "" && !System.IO.Path.IsPathRooted(options.File))
                options.File = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(options.File));

            if (options.Convert != "" && !System.IO.Path.IsPathRooted(options.Convert))
                options.Convert = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(options.Convert));

            if (options.ForceAscii)
            {
                vertical = "|";
                horizontal = "-";
                progress = '=';
            }  

            if (options.ScanMode != "")
            {
                Timer timer = null;
                Console.WriteLine();
                try
                {
                    timer = new Timer(PrintSpinner, 5, 0, SPINNER_PERIOD);

                    switch (options.ScanMode.ToLower())
                    {
                        case ALIAS_ALL:
                            {
                                using (var dev = new BSL430NET())
                                {
                                    var ret = dev.ScanAllEx((((options.Ftdi_IgnoreUnknownDev) ? ScanOptions.Ftdi_IgnoreUnknownDev :
                                                                                                 ScanOptions.None) |
                                                              ((options.UsbHid_IgnoreTexasVid) ? ScanOptions.UsbHid_IgnoreTexasVid :
                                                                                                 ScanOptions.None)));

                                    timer.Dispose();
                                    PrintDevices(ALIAS_FTDI, ret.FtdiDevices.Status, ret.FtdiDevices.Devices);
                                    PrintDevices(ALIAS_LIBFTDI, ret.LibftdiDevices.Status, ret.LibftdiDevices.Devices);
                                    PrintDevices(ALIAS_USBHID, ret.UsbDevices.Status, ret.UsbDevices.Devices);
                                    PrintDevices(ALIAS_SERIAL, ret.SerialDevices.Status, ret.SerialDevices.Devices);
                                    WriteXML(ret, "All", options.Xml);
                                }
                            }
                            break;

                        case ALIAS_FTDI:
                            {
                                using (var dev = new BSL430NET(Mode.UART_FTD2XX))
                                {
                                    var ret = dev.Scan<FTDI_Device>((options.Ftdi_IgnoreUnknownDev) ? 
                                                                    ScanOptions.Ftdi_IgnoreUnknownDev : ScanOptions.None);

                                    timer.Dispose();
                                    PrintDevices(ALIAS_FTDI, ret.Status, ret.Devices);
                                    WriteXML(ret, "FTDI", options.Xml);
                                }
                            }
                            break;

                        case ALIAS_LIBFTDI:
                            {
                                using (var dev = new BSL430NET(Mode.UART_libftdi))
                                {
                                    var ret = dev.Scan<Libftdi_Device>();

                                    timer.Dispose();
                                    PrintDevices(ALIAS_LIBFTDI, ret.Status, ret.Devices);
                                    WriteXML(ret, "Libftdi", options.Xml);
                                }
                            }
                            break;

                        case ALIAS_USBHID:
                            {
                                using (var dev = new BSL430NET(Mode.USB_HID))
                                {
                                    var ret = dev.Scan<USB_HID_Device>((options.UsbHid_IgnoreTexasVid) ? 
                                                                       ScanOptions.UsbHid_IgnoreTexasVid : ScanOptions.None);
                                    
                                    timer.Dispose();
                                    PrintDevices(ALIAS_USBHID, ret.Status, ret.Devices);
                                    WriteXML(ret, "USB", options.Xml);
                                }
                            }
                            break;

                        case ALIAS_SERIAL:
                            {
                                using (var dev = new BSL430NET(Mode.UART_Serial))
                                {
                                    var ret = dev.Scan<Serial_Device>();

                                    timer.Dispose();
                                    PrintDevices(ALIAS_SERIAL, ret.Status, ret.Devices);
                                    WriteXML(ret, "Serial", options.Xml);
                                }
                            }
                            break;

                        default:
                            {
                                timer.Dispose();
                                Console.WriteLine("ERROR. Bad device type to scan.\nChoose from: '" + ALIAS_FTDI + "', '"
                                                                                                    + ALIAS_LIBFTDI + "', '"
                                                                                                    + ALIAS_USBHID + "', '"
                                                                                                    + ALIAS_SERIAL + "', '"
                                                                                                    + ALIAS_ALL + "'.");
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    timer.Dispose();
                    ConsoleColor _color = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(String.Format($" {options.ScanMode}:{Str(' ', 10 - options.ScanMode.Length)}ERROR:\n\n" +
                        $"{((ex is Bsl430NetException) ? ((Bsl430NetException)ex).Status.ToString() : ex.Message)}\n"));
                    Console.ForegroundColor = _color; 
                }
            }
            else if (options.Upload != "")
            {
                Timer timer = null;
                Console.WriteLine();
                Stopwatch sw = new Stopwatch();

                if (options.File == "")
                {
                    Console.WriteLine("ERROR. Input firmware file missing. eg. '-f file.txt'");
                    return;
                }

                try
                {
                    using (var dev = new BSL430NET(options.Upload))
                    {
                        dev.ProgressChanged += new Bsl430NetEventHandler(ProgressChanged);

                        var baud = ValidateBaudrate(options.BaudRate);
                        var mcu = ValidateMCU(options.MCU);
                        var invoke = ValidateInvokeMech(options.InvokeMech);

                        Status status_baud = dev.SetBaudRate(baud.baud);
                        Status status_mcu = dev.SetMCU(mcu.mcu);
                        Status status_invoke = dev.SetInvokeMechanism(invoke.invoke);

                        ValidateSetters(status_baud, status_mcu, status_invoke, baud.stat, mcu.stat, invoke.stat);
                        byte[] pw = ValidatePassword(options.Password, false);

                        if ((status_baud.OK || options.BaudRate == "") && 
                            (status_mcu.OK || options.MCU == "") && 
                            (status_invoke.OK || options.InvokeMech == ""))
                        {
                            PrintReportHeader("'UPLOAD'");
                            timer = new Timer(PrintSpinner, 5, 0, SPINNER_PERIOD);
                            sw.Start();
                            StatusEx result = dev.Upload(options.File, Password: pw);
                            timer.Dispose();
                            Thread.Sleep(100);
                            PrintReportFooter(result, result.BytesProcessed, sw.Elapsed.TotalMilliseconds / 1000.0, dev.GetMCU());
                            WriteXML(result, "Upload", options.Xml);
                            sw.Stop();
                        }
                    }
                }
                catch (Exception ex)
                {
                    timer?.Dispose();
                    PrintRedError((ex is Bsl430NetException) ? ((Bsl430NetException)ex).Status.ToString() : ex.Message);
                    return;
                }
            }
            else if (options.Download != "")
            {
                Timer timer = null;
                Console.WriteLine();
                Stopwatch sw = new Stopwatch();
                int addr_start = -1;
                int data_size = -1;

                byte[] password = ValidatePassword(options.Password, true);
                Int32.TryParse(options.AddrStart, NumberStyles.HexNumber, null, out addr_start);
                Int32.TryParse(options.DataSize, NumberStyles.HexNumber, null, out data_size);

                if (password == null)
                    return;
                if (options.File == "")
                {
                    Console.WriteLine("ERROR. Output firmware file missing. eg. '-f file.txt'");
                    return;
                }
                if (addr_start < 0)
                {
                    Console.WriteLine("ERROR. Bad start address. eg. '-a 0800'");
                    return;
                }
                if (data_size < 1)
                {
                    Console.WriteLine("ERROR. Bad data size. eg. '-z FFFF'");
                    return;
                }

                try
                {
                    using (var dev = new BSL430NET(options.Download))
                    {
                        dev.ProgressChanged += new Bsl430NetEventHandler(ProgressChanged);

                        var baud = ValidateBaudrate(options.BaudRate);
                        var mcu = ValidateMCU(options.MCU);
                        var invoke = ValidateInvokeMech(options.InvokeMech);
                        var fw_format = ValidateFwFormats(options.OutputFormat);

                        if (fw_format.stat != "")
                            Console.WriteLine(fw_format.stat);

                        Status status_baud = dev.SetBaudRate(baud.baud);
                        Status status_mcu = dev.SetMCU(mcu.mcu);
                        Status status_invoke = dev.SetInvokeMechanism(invoke.invoke);

                        ValidateSetters(status_baud, status_mcu, status_invoke, baud.stat, mcu.stat, invoke.stat);

                        if ((status_baud.OK || options.BaudRate == "") &&
                            (status_mcu.OK || options.MCU == "") &&
                            (status_invoke.OK || options.InvokeMech == ""))
                        {
                            PrintReportHeader("'DOWNLOAD'");
                            timer = new Timer(PrintSpinner, 5, 0, SPINNER_PERIOD);
                            sw.Start();
                            StatusEx result = dev.Download(password, addr_start, data_size, out List<byte> data);
                            timer.Dispose();
                            PrintReportFooter(result, result.BytesProcessed, sw.Elapsed.TotalMilliseconds / 1000.0, dev.GetMCU());
                            if (result.OK)
                                WriteFirmware(data, addr_start, options.File, fw_format.fw_format, options.FwLineLength);
                            WriteXML(result, "Download", options.Xml);
                            sw.Stop();
                        }
                    }
                }
                catch (Exception ex)
                {
                    timer?.Dispose();
                    PrintRedError((ex is Bsl430NetException) ? ((Bsl430NetException)ex).Status.ToString() : ex.Message);
                    return;
                }
            }   
            else if (options.Erase != "")
            {
                Timer timer = null;
                Console.WriteLine();
                Stopwatch sw = new Stopwatch();
                try
                {
                    using (var dev = new BSL430NET(options.Erase))
                    {
                        dev.ProgressChanged += new Bsl430NetEventHandler(ProgressChanged);

                        var baud = ValidateBaudrate(options.BaudRate);
                        var mcu = ValidateMCU(options.MCU);
                        var invoke = ValidateInvokeMech(options.InvokeMech);

                        Status status_baud = dev.SetBaudRate(baud.baud);
                        Status status_mcu = dev.SetMCU(mcu.mcu);
                        Status status_invoke = dev.SetInvokeMechanism(invoke.invoke);

                        ValidateSetters(status_baud, status_mcu, status_invoke, baud.stat, mcu.stat, invoke.stat);

                        if ((status_baud.OK || options.BaudRate == "") &&
                            (status_mcu.OK || options.MCU == "") &&
                            (status_invoke.OK || options.InvokeMech == ""))
                        {
                            PrintReportHeader("'ERASE'");
                            timer = new Timer(PrintSpinner, 5, 0, SPINNER_PERIOD);
                            sw.Start();
                            StatusEx result = dev.Erase();
                            timer.Dispose();
                            PrintReportFooter(result, result.BytesProcessed, sw.Elapsed.TotalMilliseconds / 1000.0, dev.GetMCU());
                            WriteXML(result, "Erase", options.Xml);
                            sw.Stop();
                        }
                    }
                }
                catch (Exception ex)
                {
                    timer?.Dispose();
                    PrintRedError((ex is Bsl430NetException) ? ((Bsl430NetException)ex).Status.ToString() : ex.Message);
                    return;
                }
            }
            else if (options.Convert != "")
            {
                Console.WriteLine();
                try
                {
                    if (options.File == "")
                        throw new Exception("Output file parameter missing. Set -f output file path. (-f out.hex)\n");

                    var fw_format = ValidateFwFormats(options.OutputFormat);
                    if (fw_format.stat != "")
                        Console.WriteLine(fw_format.stat);

                    var output = FwTools.Convert(options.Convert, fw_format.fw_format, options.FillFF);

                    if (output.Fw == "")
                        throw new Exception("Unknown error occured while converting firmware.\n");

                    using (StreamWriter wr = new StreamWriter(options.File))
                    {
                        wr.Write(output.Fw);
                    }

                    string inf = fw_format.fw_format.ToString();
                    ConsoleColor _color = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Successfully converted firmware from [{output.Format}] to [{inf}]:\n{options.File}");
                    Console.ForegroundColor = _color;
                }
                catch (Exception ex)
                {
                    PrintRedError(GetExceptionMsg(ex));
                    return;
                }
            }
            else if (options.Combine != "")
            {
                Console.WriteLine();
                try
                {
                    if (options.SecondFile == "")
                        throw new Exception("Second firmware path parameter missing. Set -j second firmware path. (-j second_fw.hex)\n");

                    if (options.File == "")
                        throw new Exception("Output file parameter missing. Set -f output file path. (-f out.hex)\n");

                    var fw_format = ValidateFwFormats(options.OutputFormat);
                    if (fw_format.stat != "")
                        Console.WriteLine(fw_format.stat);

                    var output = FwTools.Combine(options.Combine, options.SecondFile, fw_format.fw_format, options.FillFF);

                    if (output.Fw == "")
                        throw new Exception("Unknown error occured while converting firmware.\n");

                    using (StreamWriter wr = new StreamWriter(options.File))
                    {
                        wr.Write(output);
                    }

                    string inf = fw_format.fw_format.ToString();
                    ConsoleColor _color = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Successfully combined firmware files into \"{options.File}\" as \"{fw_format.fw_format}\"");
                    Console.ForegroundColor = _color;
                }
                catch (Exception ex)
                {
                    PrintRedError(GetExceptionMsg(ex));
                    return;
                }
            }
            else if (options.GetPassword != "")
            {
                Console.WriteLine();
                try
                {
                    byte[] pw = FwTools.GetPassword(options.GetPassword);

                    if (pw == null || pw.Length != 16)
                        throw new Exception("Unknown error occured while reading BSL password.\n");

                    string password = BitConverter.ToString(pw).Replace("-", " ");

                    Console.WriteLine($"BSL password:");
                    ConsoleColor _color = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(password);
                    Console.ForegroundColor = _color;
                }
                catch (Exception ex)
                {
                    PrintRedError(GetExceptionMsg(ex));
                    return;
                }
            }
            else if (options.Validate != "")
            {
                Console.WriteLine();
                try
                {
                    FwTools.FwInfo fw_info = FwTools.Validate(options.Validate);

                    if (fw_info == null)
                        throw new Exception("Unknown error occured while validating firmware file.\n");

                    string reset_vector = (fw_info.ResetVector == null) ? "?" : "0x" + fw_info.ResetVector?.ToString("X8");
                    string full_kb = (fw_info.SizeFull / 1024.0).ToString("F1", CultureInfo.InvariantCulture) + " KiB";
                    string code_kb = (fw_info.SizeCode / 1024.0).ToString("F1", CultureInfo.InvariantCulture) + " KiB";

                    Console.WriteLine($"Format:        {fw_info.Format.ToString().Replace('_', '-')}");
                    Console.WriteLine($"First addres:  0x{fw_info.AddrFirst.ToString("X8")}");
                    Console.WriteLine($"Last address:  0x{fw_info.AddrLast.ToString("X8")}");
                    Console.WriteLine($"Size (full):   {fw_info.SizeFull.ToString("# ##0")} B ({full_kb})");
                    Console.WriteLine($"Size (code):   {fw_info.SizeCode.ToString("# ##0")} B ({code_kb})");
                    Console.WriteLine($"CRC16:         0x{fw_info.Crc16.ToString("X4")}");
                    Console.WriteLine($"Reset vector:  {reset_vector}");

                    ConsoleColor _color = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Firmware file '{System.IO.Path.GetFileName(options.Validate)}' is OK.");
                    Console.ForegroundColor = _color;
                }
                catch (Exception ex)
                {
                    PrintRedError(GetExceptionMsg(ex));
                    return;
                }
            }
            else if (options.Compare != "")
            {
                Console.WriteLine();
                try
                {
                    if (options.SecondFile == "")
                        throw new Exception("Second firmware path parameter missing. Set -j second firmware path. (-j second_fw.hex)\n");

                    var output = FwTools.Compare(options.Compare, options.SecondFile);

                    ConsoleColor _color = Console.ForegroundColor;

                    if (output.Equal)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Firmwares are equal!");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Firmwares are NOT equal! Match: {(output.Match * 100.0):F1} %, BytesDiff: {output.BytesDiff}");
                    }

                    Console.ForegroundColor = _color;
                }
                catch (Exception ex)
                {
                    PrintRedError(GetExceptionMsg(ex));
                    return;
                }
            }
        }
        #endregion

        #region Private Methods
        private void PrintDevices<T>(string mode, Status stat, List<T> dev) where T: Bsl430NetDevice
        {
            ConsoleColor _color = Console.ForegroundColor;
            if (!stat.OK)
                Console.WriteLine($" {mode}:{Str(' ', 10 - mode.Length)}ERROR:\n" + stat.ToString() + "\n");
            else if (dev == null || dev.Count == 0)
                Console.WriteLine($" {mode}:{Str(' ', 10 - mode.Length)}0 devices\n");
            else
            {
                string suff = (dev.Count > 1) ? "s" : "";
                Console.WriteLine($" {mode}:{Str(' ', 10 - mode.Length)}{dev.Count} device{suff}");
                ConsoleTable table;
                if (typeof(T).Equals(typeof(FTDI_Device)))
                {
                    table = new ConsoleTable("Name", "Description", "Type", "SN", "VID", "PID");
                    foreach (var nod in dev)
                        table.AddRow((nod as FTDI_Device).Name,
                                     HandleEmptyStr((nod as FTDI_Device).Description).Truncate(15),
                                     HandleEmptyStr((nod as FTDI_Device).Type).Truncate(17),
                                     HandleEmptyStr((nod as FTDI_Device).Serial),
                                     HandleEmptyStr((nod as FTDI_Device).Vid.ToString("X4")),
                                     HandleEmptyStr((nod as FTDI_Device).Pid.ToString("X4")));
                }
                else if (typeof(T).Equals(typeof(Libftdi_Device)))
                {
                    table = new ConsoleTable("Name", "Description", "VID", "PID");
                    foreach (var nod in dev)
                        table.AddRow((nod as Libftdi_Device).Name,
                                     HandleEmptyStr((nod as Libftdi_Device).Description.Truncate(30)),
                                     HandleEmptyStr((nod as Libftdi_Device).Vid.ToString("X4")),
                                     HandleEmptyStr((nod as Libftdi_Device).Pid.ToString("X4")));
                }
                else if (typeof(T).Equals(typeof(USB_HID_Device)))
                {
                    table = new ConsoleTable("Name", "Product Name", "Manufacturer", "Serial", "VID", "PID");
                    foreach (var nod in dev)
                        table.AddRow((nod as USB_HID_Device).Name,
                                     HandleEmptyStr((nod as USB_HID_Device).ProductName.Truncate(20)),
                                     HandleEmptyStr((nod as USB_HID_Device).Manufacturer.Truncate(12)),
                                     HandleEmptyStr((nod as USB_HID_Device).Serial),
                                     HandleEmptyStr((nod as USB_HID_Device).Vid.ToString("X4")),
                                     HandleEmptyStr((nod as USB_HID_Device).Pid.ToString("X4")));
                }
                else if (typeof(T).Equals(typeof(Serial_Device)))
                {
                    table = new ConsoleTable("Name", "Description");
                    foreach (var nod in dev)
                        table.AddRow((nod as Serial_Device).Name,
                                     HandleEmptyStr((nod as Serial_Device).Description.Truncate(50)));
                }
                else
                {
                    table = new ConsoleTable("Name", "Description");
                    foreach (var nod in dev)
                        table.AddRow(nod.Name, nod.Description);
                }
                
                string[] tokens1 = Regex.Split(table.Get(horizontal, Format.Alternative), @"(\n\|\s+\w+\s+)");
                for (int i = 0; i < tokens1.Length; i++)
                {
                    if (i % 2 == 1 && i > 1)
                    {
                        string[] tokens2 = Regex.Split(tokens1[i], @"(\w+)");
                        Console.Write(tokens2[0]);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(tokens2[1]);
                        Console.ForegroundColor = _color;
                        Console.Write(tokens2[2]);
                    }
                    else Console.Write(tokens1[i]);
                }
                Console.WriteLine();
            }
        }
        private void PrintReportHeader(string action)
        {
            string timestamp____ = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
            string mcu_pad = Str(' ', 20 - action.Length);
            if(!options.ForceAscii)
                Console.WriteLine("  ┌─────────────────────────────────────────────────────────────────────┐");
            else
                Console.WriteLine("  +---------------------------------------------------------------------+");
            Console.Write($"  {vertical}  0% [          ]  <{timestamp____}>  START {action}{mcu_pad}");
            ConsoleColor _color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("SUCCESS");
            Console.ForegroundColor = _color;
            Console.Write($" {vertical}\n");
        }
        private void PrintReportFooter(StatusEx status, int bytes, double sec, MCU mcu)
        {
            ConsoleColor color = ConsoleColor.Green;
            bool ok = (status == null) ? false : status.OK;
            //string mark = (ok) ? ReportResult.SUCCESS.ToString() : ReportResult.FAILED.ToString();
            string result = "";
            string seconds = sec.ToString("F2", CultureInfo.InvariantCulture);
            //NumberFormatInfo num_format = new NumberFormatInfo { NumberGroupSeparator = " " };
            if (!ok)
                color = ConsoleColor.Red;
            if (options.Upload != "")
            {
                if (ok)
                    result = $"Uploaded {bytes.ToString("# ##0")} B to {mcu} in {seconds}s";
                else
                    result = $"Failed to upload {bytes.ToString("# ##0")} B to {mcu} in {seconds}s";
            }
            else if (options.Download != "")
            {
                if (ok)
                    result = $"Downloaded {bytes.ToString("# ##0")} B from {mcu} in {seconds}s";
                else
                    result = $"Failed to download {bytes.ToString("# ##0")} B from {mcu} in {seconds}s";
            }
            else if (options.Erase != "")
            {
                if (ok)
                    result = $"Erased {mcu} in {seconds}s";
                else
                    result = $"Failed to erase {mcu} in {seconds}s";
            }
            if (!options.ForceAscii)
                Console.WriteLine("  ╞═════════════════════════════════════════════════════════════════════╡");
            else
                Console.WriteLine("  +---------------------------------------------------------------------+");
            Console.Write(   $"  {vertical} #RESULT: ");
            ConsoleColor _color = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(result.Truncate(59));
            Console.ForegroundColor = _color;
            Console.Write($"{ Str(' ', 59 - result.Length)}{vertical}\n");
            if (!options.ForceAscii)
                Console.WriteLine("  └─────────────────────────────────────────────────────────────────────┘");
            else
                Console.WriteLine("  +---------------------------------------------------------------------+");
            if (!status.OK)
            {
                ConsoleColor __color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERROR {status.Error}\n{status.ToString()}");
                Console.ForegroundColor = __color;
            }
        }
        private void ProgressChanged(object source, Bsl430NetEventArgs args)
        {
            lock (lock1)
            {
                ConsoleColor color = ConsoleColor.Green;
                string pad_perc = "";
                string lf = $"  \n";
                if (args.Progress < 10)
                    pad_perc = "  ";
                else if (args.Progress < 100)
                    pad_perc = " ";

                string dat = args.Report.Timestamp.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
                string mark = ReportResult.SUCCESS.ToString();

                if (args.Report.Result == ReportResult.FAILED)
                {
                    mark = ReportResult.FAILED.ToString();
                    color = ConsoleColor.Red;
                }
                else if (args.Report.Result == ReportResult.SKIPPED)
                {
                    mark = ReportResult.SKIPPED.ToString();
                    color = ConsoleColor.Yellow;
                }
                else if (args.Report.Result == ReportResult.PENDING)
                {
                    mark = $"{ReportResult.PENDING.ToString()}..";
                    color = ConsoleColor.DarkGray;
                }

                if (args.Report.Result == ReportResult.PENDING || 
                    (args.Report.Name.Contains("DOWNLOADING") && args.Report.Result == ReportResult.SUCCESS) ||
                    (args.Report.Name.Contains("UPLOADING") && args.Report.Result == ReportResult.SUCCESS))
                {
                    lf = " ";
                }
           
                string pad_name = Str(' ', 33 - args.Report.Name.Length - mark.Length);

                string _progress = "";
                if (args.Progress >= 10)
                    _progress = Str(progress, (int)args.Progress / 10);
                if (_progress.Length < 10)
                    _progress += Str(' ', 10 - _progress.Length);

                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write($"  {vertical}{pad_perc}{(int)args.Progress}% [{_progress}]  <{dat}>  {args.Report.Name}{pad_name}");
                ConsoleColor _color = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.Write(mark);
                Console.ForegroundColor = _color;
                Console.Write($" {vertical}{lf}");
            }
        }
        private void PrintSpinner(Object state)
        {
            lock (lock1)
            {
                try
                {
                    spinner++;
                    switch (spinner % 4)
                    {
                        case 0: Console.Write("/"); spinner = 0; break;
                        case 1: Console.Write("-"); break;
                        case 2: Console.Write("\\"); break;
                        case 3: Console.Write("|"); break;
                    }
                    Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                }
                catch (Exception) { }
            }
        }
        private void WriteFirmware(List<byte> data, int addr_start, string path, FwTools.FwFormat format, int lineLength)
        {
            if (path != "")
            {
                try
                {
                    using (StreamWriter wr = new StreamWriter(path))
                    {
                        wr.Write(FwTools.Create(data, addr_start, format, lineLength));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: Failed to write firmware file {path}\n{ex.Message}");
                }
            }
        }
        private void WriteXML(object o, string root, string path)
        {
            if (path != "")
            {
                try
                {
                    XmlSerializer xs = new XmlSerializer(o.GetType(), new XmlRootAttribute(root));
                    using (StreamWriter wr = new StreamWriter(path))
                    {
                        xs.Serialize(wr, o);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: Failed to write XML report file {path}\n{ex.Message}");
                }
            }
        }
        private (BaudRate baud, string stat) ValidateBaudrate(string baudrate)
        {
            if (baudrate != "")
            {
                if (!Enum.TryParse(baudrate, out BaudRate ret))
                {
                    return (baud: Utility.GetEnumDefaultValue<BaudRate>(),
                            stat: "Wrong baudrate. Overiding with default value 'BAUD_9600' (9600).");
                }
                else
                {
                    return (baud: ret, stat: "");
                }
            }
            else
            {
                return (baud : Utility.GetEnumDefaultValue<BaudRate>(), 
                        stat : "Using default baud rate 'BAUD_9600' (9600).");
            }
        }
        private (MCU mcu, string stat) ValidateMCU(string mcu)
        {
            if (mcu != "")
            {
                if (!Enum.TryParse(mcu, out MCU ret))
                {
                    return (mcu: Utility.GetEnumDefaultValue<MCU>(), 
                            stat: "Wrong mcu family. Overiding with default value 'MSP430_F5xx' (4).");
                }
                else
                {
                    return (mcu: ret, stat: "");
                }
            }
            else
            {
                return (mcu: Utility.GetEnumDefaultValue<MCU>(), 
                        stat: "Using default MCU family 'MSP430_F5xx' (4).");
            }
        }
        private (InvokeMechanism invoke, string stat) ValidateInvokeMech(string invoke)
        {
            if (invoke != "")
            {
                if (!Enum.TryParse(invoke, out InvokeMechanism ret))
                {
                    return (invoke: Utility.GetEnumDefaultValue<InvokeMechanism>(), 
                            stat: "Wrong invoke mechanims. Overiding with default value 'SHARED_JTAG' (0).");
                }
                else
                {
                    return (invoke: ret, stat: "");
                }
            }
            else
            {
                return (invoke: Utility.GetEnumDefaultValue<InvokeMechanism>(), 
                        stat: "Using default invoke mechanism 'SHARED_JTAG' (0).");
            }
        }
        private (FwTools.FwFormat fw_format, string stat) ValidateFwFormats(string fw_format)
        {
            if (fw_format != "")
            {
                if (!Enum.TryParse(fw_format, out FwTools.FwFormat ret))
                {
                    return (fw_format: Utility.GetEnumDefaultValue<FwTools.FwFormat>(),
                            stat: "Wrong output firmware format. Overiding with default value 'AUTO' (TI_TXT) (0).");
                }
                else
                {
                    return (fw_format: ret, stat: "");
                }
            }
            else
            {
                return (fw_format: Utility.GetEnumDefaultValue<FwTools.FwFormat>(),
                        stat: "Using default output firmware format 'AUTO' (TI_TXT) (0).");
            }
        }
        private void ValidateSetters(Status status_baud, 
                                     Status status_mcu, 
                                     Status status_invoke, 
                                     string stat_baud,
                                     string stat_mcu,
                                     string stat_invoke)
        {
            if (options.BaudRate != "")
            {
                if (!status_baud.OK)
                    Console.WriteLine($"ERROR {status_baud.Error}\n{status_baud.ToString()}");
            }
            if (stat_baud != "" && !(options.BaudRate == "" && !status_baud.OK))
                Console.WriteLine(stat_baud);

            if (options.MCU != "")
            {
                if (!status_mcu.OK)
                    Console.WriteLine($"ERROR {status_mcu.Error}\n{status_mcu.ToString()}");
            }
            if (stat_mcu != "" && !(options.MCU == "" && !status_mcu.OK))
                Console.WriteLine(stat_mcu);

            if (options.InvokeMech != "")
            {
                if (!status_invoke.OK)
                    Console.WriteLine($"ERROR {status_invoke.Error}\n{status_invoke.ToString()}");
            }
            if (stat_invoke != "" && !(options.InvokeMech == "" && !status_invoke.OK))
                Console.WriteLine(stat_invoke);
        }
        private byte[] ValidatePassword(string password, bool force_input)
        {
            if (options.Password.Length == 32)
            {
                try
                {
                    byte[] bytes = new byte[password.Length / 2];
                    for (int i = 0; i < password.Length; i += 2)
                        bytes[i / 2] = Convert.ToByte(password.Substring(i, 2), 16);
                    return bytes;
                }
                catch(Exception)
                {
                    if (force_input)
                        Console.WriteLine("ERROR. Invalid 16B BSL password.");
                    else
                        Console.WriteLine("Invalid 16B BSL password. Overiding with default (erase -> blank pw).");
                    return null;
                }
            }
            else if (options.Password != "")
            {
                if (force_input)
                    Console.WriteLine("ERROR. Invalid 16B BSL password.");
                else
                    Console.WriteLine("Invalid 16B BSL password. Overiding with default (erase -> blank pw).");
                return null;
            }
            else
            {
                if (force_input)
                    Console.WriteLine("ERROR. Missing BSL password (-p F1EA2..). Password is necessary for download.");
                else
                    Console.WriteLine("Using default BSL password (erase -> blank pw).");
                return null;
            }
        }
        private void PrintRedError(string error)
        {
            ConsoleColor _color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: {error}");
            Console.ForegroundColor = _color;
        }
        private void RunOptionsAndReturnExitCode(Options opt)
        {
            options = opt;
        }
        private void HandleParseError(IEnumerable<Error> errs, bool blank)
        {
            Options.PrintEnumsAndInfo();
            options = null;
            if (!blank)
                return;
            //Console.Write("Wrong input arguments.");
            Environment.Exit(-1);
        }
        private static string GetEnumInfo<T1,T2>(char vertical_separator, string horizontal_delim, int pad)
        {
            return String.Join(vertical_separator, Enum.GetValues(typeof(T1)).Cast<T2>()
                   .Select(x => $"{Enum.GetName(typeof(T1), x)}" +
                                $"{Str(' ', pad - Enum.GetName(typeof(T1), x).Length)}" +
                                $"{horizontal_delim}{x}").ToList());
        }
        private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ExceptionObject.ToString());
            Console.WriteLine("Press Enter to continue");
            Console.ReadLine();
            Environment.Exit(1);
        }
        private string GetExceptionMsg(Exception ex)
        {
            string msg = ex.Message;
            if (ex is Bsl430NetException)
            {
                msg = ((Bsl430NetException)ex).Status.ToString();
            }
            else if (ex is FirmwareToolsException)
            {
                msg = new Status()
                {
                    Error = ((FirmwareToolsException)ex).Error,
                    Msg = ((FirmwareToolsException)ex).Msg,
                    OK = false
                }.ToString();
            }
            return msg;
        }
        #endregion

        #region Public Helpers
        public string HandleEmptyStr(string str)
        {
            return (str == String.Empty) ? "-" : str;
        }
        public static string Str(char ch, int count)
        {
            return new String(ch, count);
        }
        public static IEnumerable<T> ToEnumerable<T>(params T[] items)
        {
            return items;
        }
        #endregion
    }

    #region Extensions
    public static class Extensions
    {
        public static string Truncate(this string value, int maxChars)
        {
            return value.Length <= maxChars ? value : value.Substring(0, maxChars) + "...";
        }
    }
    #endregion
}
