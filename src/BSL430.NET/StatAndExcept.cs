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

using BSL430_NET.Constants;
using BSL430_NET.Utility;

namespace BSL430_NET
{
    /// <summary>
    /// BLS UART Message. If there are any errors with the data transmission, an error message is sent back.
    /// This message is sent only when 5xx or 6xx protocol version is used.
    /// </summary>
    public enum BslUartMessage : byte
    {
        /// <summary>ACK - Success.</summary>
        [Description("Success (ACK).")]
        ACK = 0x00,
        /// <summary>Header incorrect. The packet did not begin with the required 0x80 value.</summary>
        [Description("Header incorrect. The packet did not begin with the required 0x80 value.")]
        HeaderIncorrect = 0x51,
        /// <summary>Checksum incorrect. The packet did not have the correct checksum value.</summary>
        [Description("Checksum incorrect. The packet did not have the correct checksum value.")]
        ChecksumIncorrect = 0x52,
        /// <summary>Packet size zero. The size for the BSL core command was given as 0.</summary>
        [Description("Packet size zero. The size for the BSL core command was given as 0.")]
        PacketSizeZero = 0x53,
        /// <summary>Packet size exceeds buffer. The packet size given is too big for the RX buffer.</summary>
        [Description("Packet size exceeds buffer. The packet size given is too big for the RX buffer.")]
        PacketSizeOverflow = 0x54,
        /// <summary>Unknown UART error.</summary>
        [Description("Unknown UART error.")]
        UnknownError = 0x55,
        /// <summary>Unknown baud rate. The supplied data for baud rate change is not a known value.</summary>
        [Description("Unknown baud rate. The supplied data for baud rate change is not a known value.")]
        UnknownBaudRate = 0x56,
        /// <summary>BSL UART Message is not available.</summary>
        [Description("BSL UART Message is not available.")]
        NotAvailable = 0xFF
    }

    /// <summary>
    /// BSL Core Message is a response to Erase or Download action. (Mass Erase, TX Data Block)
    /// This message is sent only when 5xx or 6xx protocol version is used.
    /// </summary>
    public enum BslCoreMessage : byte
    {
        /// <summary>Operation Successful.</summary>
        [Description("Success (Operation Successful).")] 
        Success = 0x00,
        /// <summary>Flash Write Check Failed. After programming, a CRC is run on the programmed data.
        /// If the CRC does not match the expected result, this error is returned.
        /// </summary>
        [Description("Flash Write Check Failed. Internal CRC does not match the expected result.")]
        FlashWriteCheckFail = 0x01,
        /// <summary>Flash Fail Bit Set. An operation set the FAIL bit in the flash controller
        /// (see the MSP430x5xx and MSP430x6xx Family User's Guide for more details on the flash fail bit).</summary>
        [Description("Flash Fail Bit Set. An operation set the FAIL bit in the flash controller.")]
        FlashFailBitSet = 0x02,
        /// <summary>Voltage Change During Program. The VPE was set during the requested write operation
        /// (see the MSP430x5xx and MSP430x6xx Family User's Guide for more details on the VPE bit).</summary>
        [Description("Voltage Changed During Program. The VPE was set during the requested write operation.")]
        VoltageChanged = 0x03,
        /// <summary>BSL Locked. The correct password has not yet been supplied to unlock the BSL.</summary>
        [Description("BSL Locked. The correct password has not yet been supplied to unlock the BSL.")]
        BSLLocked = 0x04,
        /// <summary>BSL Password Error. An incorrect password was supplied to the BSL when attempting an unlock.</summary>
        [Description("BSL Password Error. An incorrect password was supplied to the BSL when attempting an unlock.")]
        BSLPasswordError = 0x05,
        /// <summary>Byte Write Forbidden. This error is returned when a byte write is attempted in a flash area.</summary>
        [Description("Byte Write Forbidden. This error is returned when a byte write is attempted in a flash area.")]
        ByteWriteForbidden = 0x06,
        /// <summary>Unknown Command. The command given to the BSL was not recognized.</summary>
        [Description("Unknown Command. The command given to the BSL was not recognized.")]
        UnknownCommand = 0x07,
        /// <summary>Packet Length Exceeds Buffer Size.
        /// The supplied packet length value is too large to be held in the BSL receive buffer.</summary>
        [Description("Packet Length Exceedes Buffer Size. The supplied packet length value is too large for rx buffer.")]
        PacketLengthOverflow = 0x08,
        /// <summary>BSL Core Message is not available.</summary>
        [Description("BSL Core Message is not available.")]
        NotAvailable = 0xFF
    }

    /// <summary>
    /// Status class is an Error Cluster with int, bool and string status with Core and UART messages.
    /// </summary>
    [Serializable]
    public class Status
    {
        /// <summary>Numeric representation of status, any value other then 0 indicates an error.</summary>
        public int Error { get; set; } = 0;
        /// <summary>Boolean representation of status, true means OK, false means ERROR.</summary>
        public bool OK { get; set; } = false;
        /// <summary>String representation of status.</summary>
        public string Msg { get; set; } = "unknown";
        /// <summary>Additional info, that can be used for detailed problem analysis.</summary>
        public string Extra { get; set; } = "";
        /// <summary>BslCoreMessage status.</summary>
        public BslCoreMessage CoreStatus { get; set; } = BslCoreMessage.NotAvailable;
        /// <summary>BslUartMessage status.</summary>
        public BslUartMessage UartStatus { get; set; } = BslUartMessage.NotAvailable;
        /// <summary>Inner Status like inner exception can contain the previous Status, usually with error.</summary>
        public Status InnerStatus { get; set; } = null;

        /// <summary>Formatted status from single object.</summary>
        public string FormattedString(Status Stat, bool TopLevel = false)
        {
            string ret = "";
            if (!Stat.OK)
            {
                if (TopLevel)
                    ret += $"======== Top Level Error {Stat.Error} =======\n";
                else
                    ret += $"=========== Error {Stat.Error} ===========\n";
            }          
            ret += Stat.Msg;
            if (Stat.Extra != "")
                ret += $"\n\n{Stat.Extra.Replace(';', '\n')}";
            if (Stat.CoreStatus != BslCoreMessage.NotAvailable)
                ret += $"\n\nCoreStatus: {Stat.CoreStatus.GetEnumDescription()}";
            if (Stat.UartStatus != BslUartMessage.NotAvailable)
                ret += $"\nUartStatus: {Stat.UartStatus.GetEnumDescription()}";
            return ret;
        }

        /// <summary>
        /// Formatted Status chain, that is built from Msg and InnerStatus Msg properties, with Extra and optional Core/Uart Status.
        /// </summary>
        public override string ToString()
        {
            string ret = "";
            Status stat = this;
            Status prev = null;
            int cnt = 0;
            do
            {
                if (prev == null || (stat != null && prev.Msg != stat.Msg))
                {
                    ret += FormattedString(stat, (cnt == 0 && stat.InnerStatus != null));
                    if (stat.InnerStatus != null)
                        ret += "\n\n";
                }
                prev = stat;
                stat = stat.InnerStatus;
                cnt++;
            }
            while (stat != null);
                
            return ret;
        }
    }

    /// <summary>
    /// StatusEx is Status extended with Report List, returned as a result of main public methods.
    /// </summary>
    [Serializable]
    public class StatusEx : Status
    {
        /// <summary>Number of bytes that were processed (uploaded/downloaded to/from target MCU).</summary>
        public int BytesProcessed { get; set; } = -1;
        /// <summary>Null, 4-byte or 10-byte array, meaning differs, please see TI BSL doc (slau319t.pdf).</summary>
        public byte[] BSLVersion { get; set; } = null;
        /// <summary>Report List.</summary>
        public List<Report> Reports { get; set; } = null;

        /// <summary>
        /// Formatted StatusEx chain, that is built from Msg and InnerStatus Msg properties, with Extra and optional Core/Uart Status,
        /// and with BSL detail info like BytesProcessed and BSLVersion.
        /// </summary>
        public override string ToString()
        {
            string ret = "";
            Status stat = this;
            Status prev = null;
            int cnt = 0;
            do
            {
                if (prev == null || (stat != null && prev.Msg != stat.Msg))
                {
                    ret += FormattedString(stat, (cnt == 0 && stat.InnerStatus != null && stat.InnerStatus.Msg != stat.Msg));
                    if (cnt == 0 && this.BytesProcessed != -1)
                        ret += $"\nProcessed Bytes: {this.BytesProcessed}";
                    if (cnt == 0 && this.BSLVersion != null && this.BSLVersion.Length > 0)
                        ret += $"\nBSL Version: {this.BSLVersion.ToHexString()}";
                    if (stat.InnerStatus != null)
                        ret += "\n\n";
                }
                prev = stat;
                stat = stat.InnerStatus;
                cnt++;
            }
            while (stat != null);

            return ret;
        }
    }

    /// <summary>
    /// Report is result of an action block with Name, Result and Timestamp.
    /// </summary>
    [Serializable]
    public class Report
    {
        /// <summary>Report Name is headline of action.</summary>
        public string Name { get; set; } = "unknown";
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
    /// BSL430.NET Exception is generic Exception extended with Status object.
    /// </summary>
    [Serializable]
    public class Bsl430NetException : Exception
    {
        /// <summary>BSL430.NET Status</summary>
        public Status Status { get; set; }
        /// <summary>
        /// Bsl430NetException constructor
        /// </summary>
        public Bsl430NetException(string Message) : base(Message) { }
        /// <summary>
        /// Bsl430NetException constructor
        /// </summary>
        public Bsl430NetException(Status Stat) : base(Stat.ToString()) { Status = Stat; }
        /// <summary>
        /// Bsl430NetException constructor
        /// </summary>
        public Bsl430NetException(int ErrCode) : base(Utils.StatusCreate(ErrCode).ToString())
        {
            Status = Utils.StatusCreate(ErrCode);
        }
        /// <summary>
        /// Bsl430NetException constructor
        /// </summary>
        public Bsl430NetException(int ErrCode, Exception Ex) :
            base($"{Utils.StatusCreate(ErrCode).ToString()}{((Const.IS_DEBUG) ? $"\n{Ex.StackTrace}" : "")}")
        {
            Status = Utils.StatusCreate(ErrCode, $"{Ex.Message}{((Const.IS_DEBUG) ? $"\n{ Ex.StackTrace}" : "")}");
        }
        /// <summary>
        /// Bsl430NetException constructor
        /// </summary>
        public Bsl430NetException(int ErrCode, string Extra) :
            base($"{Utils.StatusCreate(ErrCode, Extra).ToString()}")
        {
            Status = Utils.StatusCreate(ErrCode, Extra);
        }
    }
}
