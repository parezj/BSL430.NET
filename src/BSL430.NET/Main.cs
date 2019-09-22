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

//#define BREAK_ON_BAUD_CHANGE_ERROR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Globalization;

using BSL430_NET.Utility;
using BSL430_NET.Constants;
using BSL430_NET.FirmwareTools;


namespace BSL430_NET
{
    namespace Main
    {
        #region Interfaces
        internal interface IDevice : IDisposable
        {
            Status Scan<Tdev>(out List<Tdev> device_list, ScanOptions scan_opt = ScanOptions.None) where Tdev : Bsl430NetDevice;
            StatusEx Erase(Bsl430NetDevice device);
            StatusEx Upload(Bsl430NetDevice device, string firmware, byte[] password);
            StatusEx Download(Bsl430NetDevice device, byte[] password, int addr_start, int data_size, out List<byte> data);
            Status ChangeBaudRate(BaudRate baud);
            Status SetInvoke(InvokeMechanism _invoke);
            Status SetMCU(MCU mcu);
            Bsl430NetDevice DefaultDevice { set; get; }
            BaudRate GetBaudRate();
            InvokeMechanism GetInvoke();
            MCU GetMCU();
        }

        internal interface ICommable
        {
            void CommOpen(Bsl430NetDevice device);
            void CommSet(BaudRate baud_rate);
            void CommDtr(bool val, bool ignore_err = false);
            void CommRts(bool val, bool ignore_err = false);
            void CommClrBuff();
            void CommClose(bool ignore_err = false);
            Bsl430NetDevice CommGetDefaultDevice();
            Status CommXfer(out byte[] msg_rx, byte[] msg_tx, int rx_size);
        }
        #endregion

        internal abstract partial class Core : IDevice, ICommable
        {
            #region Public Method Declaration
            public abstract void CommOpen(Bsl430NetDevice device);
            public abstract void CommSet(BaudRate baud_rate);
            public abstract void CommDtr(bool val = true, bool ignore_err = false);
            public abstract void CommRts(bool val = true, bool ignore_err = false);
            public abstract void CommClrBuff();
            public abstract void CommClose(bool ignore_err = false);
            public abstract Bsl430NetDevice CommGetDefaultDevice();
            public abstract Status CommXfer(out byte[] msg_rx, byte[] msg_tx, int rx_size);
            public abstract Status Scan<Tdev>(out List<Tdev> device_list, 
                                              ScanOptions scan_opt = ScanOptions.None) where Tdev : Bsl430NetDevice;
            public abstract Bsl430NetDevice DefaultDevice { set; get; }
            public BaudRate GetBaudRate() { return baud_rate; }
            public InvokeMechanism GetInvoke() { return invoke; }
            public MCU GetMCU() { return mcu; }
            #endregion

            #region Private Enum Declaration
            private enum Protocol
            {
                UART_1_2_4 = 250,
                UART_5_6   = 260,
                USB_5_6    = 62,
            };
            internal enum Command
            {
                BaudRate,
                BSLVersion,
                MassErase,
                CRC,
                Password,
                Upload,
                Download,
                LoadPC
                //UnLockInfo TODO
            }
            #endregion

            #region Private Class Declaration
            private abstract class CmdStat
            {
                public Command command;
                public Status status;
            }
            private sealed class Message : CmdStat
            {
                public byte[] msg;
                public int extra;
            }
            private sealed class Response : CmdStat
            {
                public byte[] data;
                public byte[] msg_sent;
            }
            private sealed class Result<T> : CmdStat where T : IData
            {
                public byte[] msg_sent;
                public byte[] msg_received;

                public bool ok;
                public T data;
            }
            private interface IData { }
            private sealed class Data_Void : IData { }
            private sealed class Data_BSLVersion : IData { public byte[] version; }
            private sealed class Data_CRC : IData { public UInt16 crc; }
            private sealed class Data_Download : IData { public byte[] bytes; }

            private sealed class Rx_Block
            {
                public int addr;
                public byte[] data;
            }
            #endregion

            #region Private Data     
            private BaudRate baud_rate = BaudRate.BAUD_115200;
            private MCU mcu = MCU.MSP430_F5xx;    
            private Protocol protocol = Protocol.UART_5_6;
            private InvokeMechanism invoke = InvokeMechanism.SHARED_JTAG;  
            private Report report;
            private readonly List<Report> reports = new List<Report>();
            private readonly BSL430NET root;
            private readonly Mode mode;
            private bool skipped;
            private bool block_pending;
            private double progress_pending;
            #endregion

            #region Constructor
            public Core(BSL430NET _root, Mode _mode)
            {
                mode = _mode;
                root = _root;
                if (_mode == Mode.USB_HID)
                    invoke = InvokeMechanism.MANUAL;
                
                report = new Report { Name = "START" };
            }
            #endregion

            #region Main Public Methods
            public StatusEx Erase(Bsl430NetDevice device)
            {
                return LoadEx(device, Command.MassErase, null, "", null, null, out _);
            }
            public StatusEx Download(Bsl430NetDevice device,
                                     byte[] password, 
                                     int addr_start, 
                                     int data_size, 
                                     out List<byte> data)
            {
                return LoadEx(device, Command.Download, password, "", addr_start, data_size, out data);
            }
            public StatusEx Upload(Bsl430NetDevice device, string firmware_path, byte[] password)
            {
                return LoadEx(device, Command.Upload, password, firmware_path, null, null, out _);
            }
            #endregion

            #region LoadEx Code Core Method
            private StatusEx LoadEx(Bsl430NetDevice device, 
                                    Command cmd, byte[] password,
                                    string firmware_path, 
                                    int? addr_start, 
                                    int? data_size, 
                                    out List<byte> output)
            {
                output = new List<byte>();
                byte[] bsl_version = new byte[0];
                Debug.Assert(cmd == Command.Upload || cmd == Command.Download || cmd == Command.MassErase);
                List<Rx_Block> rx_blocks = new List<Rx_Block>();
                FwTools.FwInfo fw_info = new FwTools.FwInfo();
                bool baud_rate_fail = false;
                block_pending = false;
                progress_pending = 0.0;
                byte[] pw;

                try
                {
                    string dev_name = (device != null) ? device.Name : CommGetDefaultDevice().Name;
                    pw = ValidatePassword(password, out bool pw_overide);

                    UpdateProgress(5, $"INIT '{mode}'");

                    if (cmd == Command.Upload)
                    {
                        BlockStart("FW READ", 10);

                        Status parsed_fw = ParseFirmware(firmware_path, out rx_blocks, out fw_info);
                        if (!parsed_fw.OK)
                        {
                            BlockEnd(ReportResult.FAILED);
                            Task.Delay(Const.DELAY_ERR_RET).Wait();
                            return Utils.StatusCreateEx(180, parsed_fw, reports, report, bsl_version,
                                                        (fw_info == null) ? 0 : fw_info.SizeFull);
                        }
                        BlockEnd($"FW {fw_info.Format.ToString().Replace('_', '-')} x{fw_info.AddrFirst.ToString("X4")} " +
                                 $"{(fw_info.SizeFull / 1024.0).ToString("F1", CultureInfo.InvariantCulture)}K");
                    }
                    else if (cmd == Command.Download)
                    {
                        BlockStart("PREPARE FW DATA", 10);

                        fw_info = new FwTools.FwInfo(addr_start ?? 0,
                                                     (addr_start ?? 0) + (data_size ?? 0),
                                                     data_size ?? 0,
                                                     GetBufferSize(protocol));
                        BlockEnd();
                    }

                    if (fw_info != null && fw_info.AddrLast > int.MaxValue)
                        return Utils.StatusCreateEx(100, reports, report, bsl_version, fw_info.SizeFull);

                    // ---

                    BlockStart($"OPEN '{dev_name}'", 15);

                    CommOpen(device);
                    CommSet(Utility.Utility.GetEnumDefaultValue<BaudRate>());

                    BlockEnd();

                    // ---

                    BlockStart($"INVOKE '{invoke}'", 20, true);

                    if (mode != Mode.USB_HID && invoke != InvokeMechanism.MANUAL)
                    {
                        skipped = false;
                        InvokeBSL(invoke);
                        Task.Delay(100).Wait();
                    }

                    BlockEnd();

                    // --

                    CommClrBuff();

                    BlockStart($"#BAUD RATE '{(int)baud_rate}'", 25, true);

                    if (mode != Mode.USB_HID && baud_rate != BaudRate.BAUD_9600)
                    {
                        skipped = false;

                        Result<Data_Void> result_baudrate = ParseResp<Data_Void>(ProcessMsg(BuildMsg(Command.BaudRate)));

                        if (!result_baudrate.ok)
                            baud_rate_fail = true;

                        Task.Delay(5).Wait();

                        if (baud_rate_fail)
                        {
                            BlockEnd(ReportResult.FAILED);
                            if (result_baudrate.status.ToString().Contains("timeout"))
                            {
                                CommClose(true);
                                Task.Delay(Const.DELAY_ERR_RET).Wait();
                                return Utils.StatusCreateEx(100, result_baudrate.status, reports, report, bsl_version, fw_info.SizeFull);
                            }
#if BREAK_ON_BAUD_CHANGE_ERROR
                                CommClose(true);
                                Task.Delay(Const.DELAY_ERR_RET).Wait();
                                return Utils.StatusCreateEx(100, result_baudrate.status, reports, report, bsl_version, fw_info.SizeFull);
#endif
                        }
                        else
                        {
                            BlockEnd(ReportResult.SUCCESS);
                            CommSet(baud_rate);
                        }
                        Task.Delay(10).Wait();
                    }
                    else BlockEnd();

                    // --

                    if ((pw_overide || protocol == Protocol.USB_5_6 || cmd == Command.Download) && cmd != Command.MassErase)
                    {
                        BlockStart("#PASSWORD (CUSTOM)", 35);

                        if (pw_overide &&
                            ((protocol == Protocol.UART_1_2_4 && pw.Length == 20) ||
                             ((mcu == MCU.MSP430_F543x_NON_A && pw.Length == 16) || pw.Length == 32)))
                        {
                            Result<Data_Void> result_password = ParseResp<Data_Void>(ProcessMsg(BuildMsg(Command.Password, pw)));

                            if (!result_password.ok)
                            {
                                BlockEnd(ReportResult.FAILED);
                                CommClose(true);
                                Task.Delay(Const.DELAY_ERR_RET).Wait();
                                return Utils.StatusCreateEx(120, result_password.status, reports, report, bsl_version, fw_info.SizeFull);
                            }
                            else
                            {
                                pw_overide = true;
                            }
                        }
                        else
                        {
                            return Utils.StatusCreateEx(470, reports, report, bsl_version, fw_info.SizeFull);
                        }

                        Task.Delay(Const.BSL430_DELAY_BETWEEN_CMDS).Wait();

                        BlockEnd();
                    }
                    else // if (!pw_overide && protocol != Protocol.USB_5_6)
                    {
                        BlockStart("#MASS ERASE", 30);

                        Result<Data_Void> result_masserase = ParseResp<Data_Void>(ProcessMsg(BuildMsg(Command.MassErase)));

                        if (!result_masserase.ok)
                        {
                            BlockEnd(ReportResult.FAILED);
                            CommClose(true);
                            Task.Delay(Const.DELAY_ERR_RET).Wait();
                            return Utils.StatusCreateEx(110, result_masserase.status, reports, report, bsl_version, fw_info.SizeFull);
                        }

                        BlockEnd();

                        Task.Delay(100).Wait();

                        // --

                        BlockStart("#PASSWORD (DEFAULT)", 35);

                        Result<Data_Void> result_password = ParseResp<Data_Void>(ProcessMsg(BuildMsg(Command.Password, pw)));

                        if (!result_password.ok)
                        {
                            BlockEnd(ReportResult.FAILED);
                            CommClose(true);
                            Task.Delay(Const.DELAY_ERR_RET).Wait();
                            return Utils.StatusCreateEx(120, result_password.status, reports, report, bsl_version, fw_info.SizeFull);
                        }
                        Task.Delay(Const.BSL430_DELAY_BETWEEN_CMDS).Wait();

                        BlockEnd();
                    }

                    // --

                    BlockStart("#BSL VERSION", 40, true);

                    if (mode != Mode.USB_HID)
                    {
                        skipped = false;

                        Result<Data_BSLVersion> result_ver = ParseResp<Data_BSLVersion>(ProcessMsg(BuildMsg(Command.BSLVersion)));

                        if (!result_ver.ok)
                            BlockEnd(ReportResult.FAILED);
                        else
                        {
                            BlockEnd(ReportResult.SUCCESS,
                                     $"#BSL VERSION {BitConverter.ToString(result_ver.data.version).Replace("-", ".")}");
                            bsl_version = result_ver.data.version.ToArray();
                        }
                        Task.Delay(Const.BSL430_DELAY_BETWEEN_CMDS).Wait();
                    }
                    else BlockEnd();

                    // --
                    
                    if (mode == Mode.USB_HID)
                    {
                        // TODO Upload RAM BSL
                        // TODO Load PC
                    }
                    
                    // --

                    if (cmd == Command.Upload)
                    {
                        double quantum = 50.0 / (double)rx_blocks.Count;
                        double _prg = 40;

                        Result<Data_Void> result_rx;
                        for (int x = 0; x < rx_blocks.Count; x++)
                        {
                            BlockStart($"#UPLOADING ({x + 1}/{rx_blocks.Count})", _prg);

                            result_rx = ParseResp<Data_Void>(ProcessMsg(BuildMsg(Command.Upload,
                                                                                 rx_blocks[x].addr,
                                                                                 rx_blocks[x].data)));
                            if (!result_rx.ok)
                            {
                                BlockEnd(ReportResult.FAILED);
                                CommClose(true);
                                Task.Delay(Const.DELAY_ERR_RET).Wait();
                                return Utils.StatusCreateEx(140,
                                                            result_rx.status,
                                                            (x + 1) + "/" + rx_blocks.Count,
                                                            reports,
                                                            report,
                                                            bsl_version,
                                                            fw_info.SizeFull);
                            }
                            _prg += quantum;

                            Task.Delay(5).Wait();

                            if (x == rx_blocks.Count - 1)
                                BlockEnd($"#UPLOADED ({x + 1}/{rx_blocks.Count})");
                            else
                                BlockEnd();
                        }
                        Task.Delay(5).Wait();
                    }
                    else if (cmd == Command.Download)
                    {
                        int _data_size = (data_size ?? 0);
                        int total = (_data_size + fw_info.SizeBuffer - 1) / fw_info.SizeBuffer;
                        double quantum = 50.0 / (double)total;
                        double _prg = 40;

                        Result<Data_Download> result_tx;
                        for (int x = 0; x < total; x++)
                        {
                            BlockStart($"#DOWNLOADING ({x + 1}/{total})", _prg);

                            int len = data_size ?? 0;
                            if (len > fw_info.SizeBuffer)
                                len = fw_info.SizeBuffer;

                            result_tx = ParseResp<Data_Download>(ProcessMsg(BuildMsg(Command.Download,
                                                                                     addr_start,
                                                                                     new byte[2] { (byte)(len & 0x00FF),
                                                                                                   (byte)(len >> 8) })));
                            if (!result_tx.ok)
                            {
                                BlockEnd(ReportResult.FAILED);
                                CommClose(true);
                                Task.Delay(Const.DELAY_ERR_RET).Wait();
                                return Utils.StatusCreateEx(150,
                                                            result_tx.status,
                                                            x + "/" + rx_blocks.Count,
                                                            reports,
                                                            report,
                                                            bsl_version,
                                                            fw_info.SizeFull);
                            }

                            output.AddRange(result_tx.data.bytes.ToList());
                            _prg += quantum;
                            addr_start += fw_info.SizeBuffer;
                            data_size -= fw_info.SizeBuffer;
                            Task.Delay(5).Wait();

                            if (x == total - 1)
                                BlockEnd($"#DOWNLOADED ({x + 1}/{total})");
                            else
                                BlockEnd();
                        }
                        fw_info.Crc16 = output.Crc16Ccitt();
                        Task.Delay(5).Wait();
                    }

                    // --

                    if (cmd != Command.MassErase)
                    {
                        BlockStart($"#CRC CHECK ({fw_info.Crc16.ToString("X4")})", 95, true);

                        if (protocol != Protocol.UART_1_2_4 && fw_info != null && !(pw_overide && cmd == Command.Upload))
                        {
                            skipped = false;

                            Result<Data_CRC> result_crc = ParseResp<Data_CRC>(ProcessMsg(BuildMsg(Command.CRC,
                                                                                                  (int)fw_info.AddrFirst,
                                                                                                  new byte[2] { (byte)(fw_info.SizeFull & 0x00FF),
                                                                                                                (byte)(fw_info.SizeFull >> 8) })));
                            if (!result_crc.ok)
                            {
                                BlockEnd(ReportResult.FAILED);
                                CommClose(true);
                                Task.Delay(Const.DELAY_ERR_RET).Wait();
                                return Utils.StatusCreateEx(160, result_crc.status, reports, report, bsl_version, fw_info.SizeFull);
                            }
                            if (result_crc.data.crc != fw_info.Crc16)
                            {
                                BlockEnd(ReportResult.FAILED, $"#CRC CHECK ({fw_info.Crc16.ToString("X4")}!={result_crc.data.crc.ToString("X4")})");
                                CommClose(true);
                                Task.Delay(Const.DELAY_ERR_RET).Wait();
                                return Utils.StatusCreateEx(161, reports, report, bsl_version, fw_info.SizeFull);
                            }
                            Task.Delay(Const.BSL430_DELAY_BETWEEN_CMDS).Wait();

                            BlockEnd($"#CRC CHECK ({fw_info.Crc16.ToString("X4")}=={result_crc.data.crc.ToString("X4")})");
                        }
                        else BlockEnd();
                    }

                    // --

                    if (cmd != Command.MassErase)
                    {
                        BlockStart("#LOAD USER PROGRAM", 98, true);

                        if (fw_info.ResetVector != null)
                        {
                            skipped = false;

                            Result<Data_Void> result_load = ParseResp<Data_Void>(ProcessMsg(BuildMsg(Command.LoadPC, (int)fw_info.ResetVector)));

                            if (result_load.ok)
                                BlockEnd(ReportResult.SUCCESS);
                            else
                            {
                                BlockEnd(ReportResult.FAILED);
                                //Task.Delay(Const.DELAY_ERR_RET).Wait();
                                //return Utils.StatusCreateEx(170);
                            }
                        }
                        else BlockEnd();
                    }

                    // --

                    BlockStart($"RESET '{mcu}'", 99, true);

                    if (mode != Mode.USB_HID)
                    {
                        skipped = false;
                        ResetMCU(invoke);
                    }

                    BlockEnd();

                    // --

                    BlockStart($"FINISH", 100);

                    CommClose();

                    BlockEnd();
                    Task.Delay(Const.DELAY_OK_RET).Wait();

                    return Utils.StatusCreateEx(0, reports, null, bsl_version, fw_info.SizeFull);
                }
                catch (Exception ex)
                {
                    CommClose(true);

                    if (block_pending)
                    {
                        BlockEnd(ReportResult.FAILED);
                        Task.Delay(Const.DELAY_ERR_RET).Wait();
                    }

                    if (ex is Bsl430NetException)
                    {
                        return Utils.StatusCreateEx((ex as Bsl430NetException).Status.Error,
                            (ex as Bsl430NetException).Status,
                            reports,
                            report,
                            bsl_version,
                            fw_info.SizeFull);
                    }
                    else
                    {
                        return Utils.StatusCreateEx(
                           Utils.StatusCreate(467, ex.Message + ((Const.IS_DEBUG) ? ex.StackTrace : "")),
                           reports,
                           report,
                           bsl_version,
                           fw_info.SizeFull);
                    }
                }
            }
            #endregion

            #region Setters/Helpers
            private void BlockStart(string name, double val, bool _skipped = false)
            {
                block_pending = true;
                progress_pending = val;
                skipped = _skipped;
                report = new Report
                {
                    Name = name,
                    Result = ReportResult.PENDING,
                    Timestamp = DateTime.Now
                };
                UpdateProgress(val, report, false);
            }
            private void BlockEnd(ReportResult result, string name_override = "")
            {
                block_pending = false;
                UpdateProgress(progress_pending, new Report
                {
                    Name = ((name_override == "") ? report.Name : name_override),
                    Result = result,
                    Timestamp = DateTime.Now
                }, true);
            }
            private void BlockEnd(string name_override = "")
            {
                BlockEnd((skipped == true) ? ReportResult.SKIPPED : ReportResult.SUCCESS, name_override);
            }
            private void UpdateProgress(double val, string str)
            {
                UpdateProgress(val, new Report
                {
                    Name = str,
                    Result = (skipped == true) ? ReportResult.SKIPPED : ReportResult.SUCCESS,
                    Timestamp = DateTime.Now
                }, true);
            }
            //private void UpdateProgress(double val, string str, ReportResult res)
            //{
            //    UpdateProgress(val, new Report { Name = str, Result = res, Timestamp = DateTime.Now }, true);
            //}

            //private void UpdateProgress(double val, ReportResult res)
            //{
            //    UpdateProgress(val, new Report { Name = report.Name, Result = res, Timestamp = DateTime.Now }, true);
            //}
            private void UpdateProgress(double val, Report _report, bool add_report)
            {
                try
                {
                    root?.ProgressUpdate(val, _report);
                    if (add_report)
                        reports.Add(_report);
                }
                catch (Exception) { }
            }
            public Status ChangeBaudRate(BaudRate _baud)
            {
                try
                {
                    baud_rate = _baud;
                    if (mode == Mode.USB_HID)
                        return Utils.StatusCreate(215);
                    else
                        return Utils.StatusCreate(0);
                }
                catch (Exception) { return Utils.StatusCreate(999); }
            }
            public Status SetInvoke(InvokeMechanism _invoke)
            {
                if (mode == Mode.USB_HID && _invoke != InvokeMechanism.MANUAL)
                    return Utils.StatusCreate(217);
                invoke = _invoke;
                return Utils.StatusCreate(0);
            }
            public Status SetMCU(MCU _mcu)
            {
                if (mode == Mode.USB_HID &&
                    (mcu != MCU.MSP430_F5xx && 
                     mcu != MCU.MSP430_F543x_A &&
                     mcu != MCU.MSP430_F543x_NON_A &&
                     mcu != MCU.MSP430_F6xx))
                {
                    return Utils.StatusCreate(216);
                }

                mcu = _mcu;

                if (mode == Mode.USB_HID)
                {
                    protocol = Protocol.USB_5_6;
                }
                else
                {
                    if (_mcu == MCU.MSP430_F1xx || 
                        _mcu == MCU.MSP430_F2xx ||
                        _mcu == MCU.MSP430_F4xx || 
                        _mcu == MCU.MSP430_G2xx3)
                    {
                        protocol = Protocol.UART_1_2_4;
                    }
                    else
                    {
                        protocol = Protocol.UART_5_6;
                    }
                }
                return Utils.StatusCreate(0);
            }
            #endregion

            #region IDisposable Support
            private bool disposedValue = false;

            void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        //unmanaged
                    }

                    try
                    {
                        CommClose(true);
                    }
                    catch (Exception) { }

                    disposedValue = true;
                }
            }

            public void Dispose()
            {
                Dispose(true);
            }
            #endregion
        }
    }
}
