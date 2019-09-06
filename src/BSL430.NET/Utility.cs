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
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;


namespace BSL430_NET
{
    namespace Utility
    {
        /// <summary>
        /// Utility class providing some helpful methods.
        /// </summary>
        public static class Utility
        {
            /// <summary>
            /// Gets default value of enum/struct, which is declared as DefaultValue attribute.
            /// </summary>
            public static T GetEnumDefaultValue<T>() where T : struct
            {

                if (typeof(T).GetCustomAttributes(typeof(DefaultValueAttribute),false) 
                    is DefaultValueAttribute[] attributes && attributes.Length > 0)
                {
                    return (T)attributes[0].Value;
                }
                return default;
            }
        }

        internal sealed class Utils
        {
            public static Status StatusCreate(int value)
            {
                return StatusCreate(value, null, "", null, null);
            }
            public static Status StatusCreate(int value, Status stat)
            {
                return StatusCreate(value, stat, "", null, null);
            }
            public static Status StatusCreate(int value, string extra)
            {
                return StatusCreate(value, null, extra, null, null);
            }
            public static StatusEx StatusCreateEx(int value)
            {
                return StatusCreateEx(StatusCreate(value, null, "", null, null), null, null, null, 0);
            }
            public static StatusEx StatusCreateEx(int value, List<Report> reports, Report report, byte[] version, int bytes)
            {
                return StatusCreateEx(StatusCreate(value, null, "", null, null), reports, report, version, bytes);
            }
            public static StatusEx StatusCreateEx(int value, Status stat, List<Report> reports, Report report, byte[] version, int bytes)
            {
                return StatusCreateEx(StatusCreate(value, stat, "", null, null), reports, report, version, bytes);
            }
            public static StatusEx StatusCreateEx(int value, Status stat, string extra, List<Report> reports, Report report, byte[] version, int bytes)
            {
                return StatusCreateEx(StatusCreate(value, stat, extra, null, null), reports, report, version, bytes);
            }
            public static StatusEx StatusCreateEx(Status status, List<Report> reports, Report report, byte[] version, int bytes)
            {
                if (reports != null && report != null)
                {
                    //report.timestamp = DateTime.Now;
                    //report.result = (status.ok) ? ReportResult.SUCCESS : ReportResult.FAILED;
                    reports.Add(report);
                }
                return new StatusEx
                {
                    Error = status.Error,
                    Msg = status.Msg,
                    OK = status.OK,
                    CoreStatus = status.CoreStatus,
                    UartStatus = status.UartStatus,
                    Reports = reports.OrderBy(r => r.Timestamp).ToList(),
                    BSLVersion = version,
                    BytesProcessed = bytes,
                    InnerStatus = status.InnerStatus
                };
            }

            public static Status StatusCreate(int value, Status stat = null, string extra = "", byte[] data_tx = null, byte[] data_rx = null)
            {
                Status ret = new Status() { Error = value, InnerStatus = stat, Extra = extra, OK = false };

                string err_msg;
                switch (value)
                {
                    default: err_msg = "Unknown Error."; break;
                    case 0: err_msg = "Success."; break;

                    case 215: err_msg = "USB mode does not support Baud Rate setting."; break;
                    case 216: err_msg = "USB mode is supported only on these MCUs: F5xx, F6xx."; break;
                    case 217: err_msg = "USB mode support only manual invoking of BSL (PUR pin tied to VUSB)."; break;
                    case 218: err_msg = "USB mode support only reduced Command Set: RX Password, RX Data Fast and Load PC."; break;
                    case 219: err_msg = "1xx 2xx 4xx protocol Change Baud Rate support is rather experimental, so I decided to dont implement it. Sorry."; break;
                    case 220: err_msg = "1xx 2xx 4xx protocol does not support CRC check functionality."; break;

                    case 440: err_msg = "Exception aborted execution."; break;
                    case 441: err_msg = "Parsing Response failed! at: [parse_resp()]."; break;
                    case 442: err_msg = "Building Message failed! at: [build_msg()]."; break;
                    case 443: err_msg = "Processing Message failed! at: [process_msg()]."; break;
                    case 445: err_msg = "Invalid or corrupted firmware file. Intel-HEX, TI-TXT, SREC and ELF formats are supported."; break;
                    case 446: err_msg = "Main Communication Handler failed! at: [comm_xfer()]."; break;
                    case 447: err_msg = "Building Message failed! at: [build_msg()]."; break;
                    case 448: err_msg = "Parsing Response failed! at: [parse_resp()]."; break;
                    case 449: err_msg = "Parsing Response failed! at: [parse_resp() - File.ReadAllText]."; break;
                    case 450: err_msg = "Preparing Firmware for upload failed!"; break;
                    case 463: err_msg = "Operation failed because of an error."; break;
                    case 467: err_msg = "Main Process Sequence failed! at: [loadEx()]."; break;                 
                    case 460: err_msg = "[BUG] Type mismatch at Parsing Response. This is a develop time error."; break;

                    case 461: err_msg = "Bad device. Input <Bsl430NetDevice> object is null or property Name is empty."; break;
                    case 462: err_msg = "Device does not exist."; break;
                    case 464: err_msg = "Unknown or disconnected device."; break;
                    case 465: err_msg = ""; break;  // blank error
                    case 466: err_msg = "BSL430.NET instance was created with zero parameters, therefore can do nothing except ScanAll."; break;
                    case 469: err_msg = "BSL430.NET instance was created with different Mode than Scan was called with."; break;
                    case 468: err_msg = "Firmware address out of range - not supported address mode, try with lower address range."; break;

                    case 801: err_msg = "build_msg() failed. wrong input parameters."; break;
                    case 802: err_msg = "parse_resp() failed. UART error message is invalid (not equal 0x00 - ACK)."; break;
                    case 803: err_msg = "parse_resp() failed. UART header byte is invalid (not equal 0x80)."; break;
                    case 804: err_msg = "parse_resp() failed. USB header byte is invalid (not equal 0x3F)."; break;
                    case 805: err_msg = "parse_resp() failed. Received byte array has wrong length."; break;
                    case 806: err_msg = "parse_resp() failed. CMD byte in BSL Core packet is invalid."; break;
                    case 807: err_msg = "parse_resp() failed. MSG byte in BSL Core packet is invalid."; break;
                    case 808: err_msg = "Operation failed. MSG byte in BSL Core packet is invalid (not equal 0x00)."; break;
                    case 809: err_msg = "process_msg() failed. ACK byte is not equal 0x90 (DATA_ACK after SYNC)."; break;
                    case 810: err_msg = "parse_resp() failed. HDR byte in BSL data frame is invalid (not equal 0x80)."; break;
                    case 811: err_msg = "Operation failed. BSL returned NAK (0xA0). This could indicate any error, since this early BSL version supports only simple protocol."; break;
                    case 812: err_msg = "parse_resp() failed. Received byte array is invalid (has wrong length or bad data)."; break;

                    // Core messages
                    case 100: err_msg = "'CHANGE BAUDRATE' BSL command failed."; break;
                    case 110: err_msg = "'MASS ERASE' BSL command failed."; break;
                    case 120: err_msg = "'RX PASSWORD' BSL command failed."; break;
                    case 130: err_msg = "'BSL VERSION' BSL command failed."; break;
                    case 140: err_msg = "'RX DATA " + extra + "' BSL command failed."; break;
                    case 150: err_msg = "'TX DATA " + extra + "' BSL command failed."; break;
                    case 160: err_msg = "'CRC CHECK' BSL command failed."; break;
                    case 161: err_msg = "'CRC CHECK' CRC mismatch error. crc16 of hex file does not match crc16 of data uploaded to device."; break;
                    case 170: err_msg = "'LOAD PC' BSL command failed."; break;
                    case 180: err_msg = "Reading firmware file failed."; break;
                    case 666: err_msg = "Interrupted. Any action is invalidated, resources are released."; break;

                    // FTD2XX
                    case 300: err_msg = "[FTD2XX SCAN] FTDI device error occured while getting devices list."; break;
                    case 301: err_msg = "[FTD2XX SCAN] Exception occured while trying to get list of FTDI devices."; break;
                    case 302: err_msg = "[FTD2XX SCAN] Timeout occured while trying to get list of FTDI devices."; break;
                    case 323: err_msg = "[FTD2XX INIT] Missing or bad dynamic link library: FTD2XX.DLL. Download it and put it in same folder as BSL430.NET is."; break;
                    case 333: err_msg = "[FTD2XX INIT] FTDI driver (FTDIBUS.SYS) missing or bad. Reinstall the driver from http://www.ftdichip.com/FTDrivers.htm"; break;
                    case 310: err_msg = "[FTD2XX OPEN] Cannot open FTDI device. Already opened?"; break;
                    case 311: err_msg = "[FTD2XX OPEN] Exception occured while trying to open FTDI device."; break;
                    case 312: err_msg = "[FTD2XX OPEN] Cannot open device because there is zero devices present."; break;
                    case 313: err_msg = "[FTD2XX OPEN] Cannot open device. Invalid name."; break;
                    case 340: err_msg = "[FTD2XX SET] FTDI device error occured while setting connection parameters."; break;
                    case 341: err_msg = "[FTD2XX SET] Exception occured while trying to set FTDI device parameters."; break;
                    case 350: err_msg = "[FTD2XX DTR] FTDI device error occured while setting DTR."; break;
                    case 351: err_msg = "[FTD2XX DTR] Exception occured while trying to set FTDI device DTR."; break;
                    case 360: err_msg = "[FTD2XX RTS] FTDI device error occured while setting RTS."; break;
                    case 361: err_msg = "[FTD2XX RTS] Exception occured while trying to set FTDI device RTS."; break;
                    case 370: err_msg = "[FTD2XX PURGE] FTDI device error occured while purging RX TX buffers."; break;
                    case 371: err_msg = "[FTD2XX PURGE] Exception occured while trying to purge FTDI RX TX buffers"; break;
                    case 380: err_msg = "[FTD2XX CLOSE] FTDI device error occured while closing FTDI device."; break;
                    case 381: err_msg = "[FTD2XX CLOSE] Exception occured while trying to close FTDI device."; break;
                    case 390: err_msg = "[FTD2XX] ftdi.Write() returned error."; break;
                    case 391: err_msg = "[FTD2XX] ftdi.Write() failed to write required number of bytes."; break;
                    case 392: err_msg = "[FTD2XX] ftdi.GetRxBytesAvailable() returned error."; break;
                    case 393: err_msg = "[FTD2XX] ftdi.Read() returned error."; break;
                    case 394: err_msg = "[FTD2XX] ftdi.Read() failed to read required number of bytes."; break;
                    case 395: err_msg = "[FTD2XX] No answer from device (comm_xfer() timeout). Disconnected?"; break; // or wrong rx message length

                    // libftdi
                    case 501: err_msg = "[libftdi SCAN] Exception occured while trying to get list of FTDI devices."; break;
                    case 523: err_msg = "[libftdi INIT] Missing or bad dynamic library: libftdi. Download it and put it in same folder as BSL430.NET is."; break;
                    case 533: err_msg = "[libftdi INIT] Exception occured while loading libftdi."; break;
                    case 510: err_msg = "[libftdi OPEN] Cannot open FTDI device. Already opened?"; break;
                    case 511: err_msg = "[libftdi OPEN] Exception occured while trying to open FTDI device."; break;
                    case 541: err_msg = "[libftdi SET] Exception occured while trying to set FTDI device parameters."; break;
                    case 551: err_msg = "[libftdi DTR] Exception occured while trying to set FTDI device DTR."; break;
                    case 561: err_msg = "[libftdi RTS] Exception occured while trying to set FTDI device RTS."; break;
                    case 571: err_msg = "[libftdi PURGE] Exception occured while trying to purge FTDI RX TX buffers"; break;
                    case 581: err_msg = "[libftdi CLOSE] Exception occured while trying to close FTDI device."; break;
                    //case 590: err_msg = "[libftdi] libftdi.Write() returned error."; break;
                    case 591: err_msg = "[libftdi] libftdi.Write() failed to write required number of bytes."; break;
                    //case 592: err_msg = "[libftdi] libftdi.GetRxBytesAvailable() returned error."; break;
                    case 593: err_msg = "[libftdi] libftdi.Read() returned error."; break;
                    //case 594: err_msg = "libftdi.Read() failed to read required number of bytes."; break;
                    case 595: err_msg = "[libftdi] No answer from device (comm_xfer() timeout). Disconnected?"; break;

                    // HID USB
                    case 601: err_msg = "[USB HID SCAN] Exception occured while trying to get list of USB HID devices."; break;
                    case 623: err_msg = "[USB HID INIT] Missing or bad dynamic library: HidSharp. Download it and put it in same folder as BSL430.NET is."; break;
                    case 633: err_msg = "[USB HID INIT] Exception occured while loading HidSharp."; break;
                    case 610: err_msg = "[USB HID OPEN] Cannot open USB HID device. Already opened?"; break;
                    case 611: err_msg = "[USB HID OPEN] Exception occured while trying to open USB HID device."; break;
                    //case 641: err_msg = "[USB HID SET] Exception occured while trying to set USB HID device parameters."; break;
                    //case 651: err_msg = "[USB HID DTR] Exception occured while trying to set USB HID device DTR."; break;
                    //case 661: err_msg = "[USB HID RTS] Exception occured while trying to set USB HID device RTS."; break;
                    //case 671: err_msg = "[USB HID PURGE] Exception occured while trying to purge USB HID RX TX buffers"; break;
                    case 681: err_msg = "[USB HID CLOSE] Exception occured while trying to close USB HID device."; break;
                    //case 690: err_msg = "USB.Write() returned error."; break;
                    //case 691: err_msg = "USB.Write() failed to write required number of bytes."; break;
                    //case 692: err_msg = "USB.GetRxBytesAvailable() returned error."; break;
                    //case 693: err_msg = "USB.Read() returned error."; break;
                    //case 694: err_msg = "USB.Read() failed to read required number of bytes."; break;
                    case 695: err_msg = "[USB HID] No answer from device (comm_xfer() timeout). Disconnected?"; break;

                    // Serial
                    //case 701: err_msg = "[Serial SCAN] Exception occured while trying to get list of Serial devices."; break;
                    case 723: err_msg = "[Serial INIT] Missing or bad dynamic library: RJCP.SerialPortStream. Download it and put it in same folder as BSL430.NET is."; break;
                    case 733: err_msg = "[Serial INIT] Exception occured while loading RJCP.SerialPortStream."; break;
                    case 710: err_msg = "[Serial OPEN] Cannot open Serial device. Already opened?"; break;
                    case 711: err_msg = "[Serial OPEN] Exception occured while trying to open Serial device."; break;
                    case 741: err_msg = "[Serial SET] Exception occured while trying to set Serial device parameters."; break;
                    case 751: err_msg = "[Serial DTR] Exception occured while trying to set Serial device DTR."; break;
                    case 761: err_msg = "[Serial RTS] Exception occured while trying to set Serial device RTS."; break;
                    case 771: err_msg = "[Serial PURGE] Exception occured while trying to purge Serial RX TX buffers"; break;
                    case 781: err_msg = "[Serial CLOSE] Exception occured while trying to close Serial device."; break;
                    //case 790: err_msg = "Serial.Write() returned error."; break;
                    //case 791: err_msg = "Serial.Write() failed to write required number of bytes."; break;
                    //case 792: err_msg = "Serial.GetRxBytesAvailable() returned error."; break;
                    case 793: err_msg = "[Serial] Serial.Read() returned error."; break;
                    //case 794: err_msg = "Serial.Read() failed to read required number of bytes."; break;
                    case 795: err_msg = "[Serial] No answer from device (comm_xfer() timeout). Disconnected?"; break;
                }

                ret.Msg = err_msg;

                if (data_rx != null && data_rx.Length > 0)
                    ret.Extra += $";[BYTES RX]:{data_rx.ToHexString()}";

                if (data_tx != null && data_tx.Length > 0)
                    ret.Extra += $";[BYTES TX]:{data_tx.ToHexString()}";

                if (value == 0 || value == 1)
                    ret.OK = true;

                return ret;
            }
        }
    }
}
