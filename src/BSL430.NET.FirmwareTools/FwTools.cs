/*
    BSL430.NET - MSP430 bootloader (BSL) .NET toolchain
    Original source by: Jakub Parez - https://github.com/parezj/
	  
    The MIT License (MIT)
    
    Copyright (c) 2019 Jakub Parez

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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using BSL430_NET.FirmwareTools.Helpers;


namespace BSL430_NET
{
    namespace FirmwareTools
    {
        /// <summary>
        /// FwTools class provides basic manipulation (Parse, Create, Convert, Combine, Validate, Compare) with Intel-HEX, 
        /// TI-TXT, SREC and ELF firmware formatted files.
        /// </summary>
        public static partial class FwTools
        {
            #region Private Data
            private const string ERR_444 = "Parsing firmware failed! Invalid or corrupted firmware file. Intel-HEX, TI-TXT, SREC " +
                                           "and ELF formats are supported.";
            private const string ERR_445 = "Invalid or corrupted firmware file. Intel-HEX, TI-TXT, SREC and ELF formats are supported.";
            private const string ERR_470 = "Failed to create firmware.";
            private const string ERR_471 = "Failed to create firmware. Input data is empty.";
            private const string ERR_472 = "ELF firmware format is not supported yet as output.";
            private const string ERR_473 = "File does not exists.";
            private const string ERR_474 = "Exception aborted execution while trying to read file.";
            private const string ERR_475 = "Auto-detect firmware format failed. Most likely wrong or corrupted file. Intel-HEX, " +
                                           "TI-TXT, SREC and ELF formats are supported.";
            private const string ERR_476 = "Only 32-bit ELF format is supported.";
            private const string ERR_477 = "Only little endian ELF format is supported.";
            private const string ERR_478 = "Only executable file type ELF format is supported.";
            //private const string ERR_479 = "Only TI-MSP430 or ARM32 (MSP432) machine ELF format is supported.";
            private const string ERR_480 = "Wrong or corrupted file. ELF magic number (7F-45-4C-46) mismatch.";
            private const string ERR_481 = "Firmware address conflict. There is an overlap between both firmware files, combining " +
                                           "them would result in corrupted firmware.";
            #endregion

            #region Firmware Format
            /// <summary>
            /// Supported firmware formats. AUTO is default value. When chosen AUTO, Parse will auto detect fw format,
            /// and Create will use TI-TXT. ELF format is currently supported only by Parse.
            /// </summary>
            [DefaultValue(AUTO)]
            public enum FwFormat : byte
            {
                /// <summary>When chosen AUTO, Parse will auto detect fw format, and Create will use TI-TXT.</summary>
                [Description("Auto")]
                AUTO = 0,
                /// <summary>TI-TXT format described at: <see href="http://srecord.sourceforge.net/man/man5/srec_ti_txt.html">
                /// HERE</see></summary>
                [Description("TI-TXT")]
                TI_TXT = 1,
                /// <summary>Intel-HEX format described at: <see href="https://en.wikipedia.org/wiki/Intel_HEX">HERE</see></summary>
                [Description("Intel-HEX")]
                INTEL_HEX = 2,
                /// <summary>Motorola-SREC S-Record format described at: <see href="https://en.wikipedia.org/wiki/SREC_(file_format)">
                /// HERE</see></summary>
                [Description("SREC")]
                SREC = 3,
                /// <summary>ELF format described at: <see href="http://man7.org/linux/man-pages/man5/elf.5.html">HERE</see></summary>
                [Description("ELF")]
                ELF = 4
            }
            #endregion

            #region Public Classes

            /// <summary>
            /// Firmware object representation. Nodes is collection of FwNode (Addr + Data) and Info is FwInfo class.
            /// ToString, Equal and Euqality Operators (==, !=) overides are available.
            /// </summary>
            public class Firmware
            {
                /// <summary>
                /// FwInfo provides info about firmware, like first and last addresses, CRC, code size and 
                /// reset vector.
                /// </summary>
                public FwInfo Info { get; }
                /// <summary>
                /// List of FwNode, what firmware consits of, representated by single byte with max 32-bit, 
                /// usually 16-bit address.
                /// </summary>
                public List<FwNode> Nodes { get; }
                /// <summary>Memory Stream is sequence of raw bytes.</summary>
                public MemoryStream MemoryStream
                {
                    get
                    {
                       try { return new MemoryStream(this.Nodes.Select(n => n.Data).ToArray()); }
                       catch (Exception) { return null; }
                    }
                }
                /// <summary>Stream is sequence of raw bytes.</summary>
                public Stream Stream
                {
                    get { return this.MemoryStream; }
                }

                /// <summary>
                /// Init Firmware with simple data Nodes and new FwInfo class based on valid input data.
                /// </summary>
                public Firmware(List<FwNode> Data, 
                                FwFormat Format, 
                                int SizeBuffer = 0,
                                long ResetVectorAddr = -1, 
                                ICollection<long> FilledFFAddr = null)
                {
                    Nodes = Data.OrderBy(o => o.Addr).ToList();
                    Info = new FwInfo(Data, Format, SizeBuffer, ResetVectorAddr, FilledFFAddr);
                }

                /// <summary>
                /// Init Firmware with raw memory stream bytes and new FwInfo class based on valid input data.
                /// </summary>
                public Firmware(Stream Data, 
                                FwFormat Format,
                                long FirstAddress, 
                                int SizeBuffer = 0,
                                long ResetVectorAddr = -1, 
                                ICollection<long> FilledFFAddr = null)
                {
                    byte[] nodes;
                    Data.Position = 0;
                    using (MemoryStream ms = new MemoryStream())
                    {
                        Data.CopyTo(ms);
                        nodes = ms.ToArray();
                    }
                    Nodes = nodes.Select(n => new FwNode() { Data = n, Addr = FirstAddress++ }).ToList();
                    Info = new FwInfo(Nodes, Format, SizeBuffer, ResetVectorAddr, FilledFFAddr);
                }

                /// <summary>
                /// Sets Reset Vector property to valid data, calculated from Nodes. ResetVectorAddr is usually 0xFFFE.
                /// </summary>
                public long? SetResetVector(long ResetVectorAddr)
                {
                    if (Info != null && Nodes != null)
                        return Info.SetResetVector(Nodes, ResetVectorAddr);
                    else return null;
                }

                /// <summary>
                /// Formatted important properites describing current Firmware, taken from FwInfo class.
                /// </summary>
                public override string ToString()
                {
                    if (this.Info != null)
                        return this.Info.ToString();
                    else
                        return $"Unknown.";
                }

                /// <summary>
                /// True if two Firmwares are the same, meaning ale nodes (Address and Data) are the same, else false.
                /// Returns false if any or even if both Firmwares are null.
                /// </summary>
                public override bool Equals(object value)
                {
                    return Compare(value as Firmware, this).Equal;
                }

                /// <summary>
                /// Hash Code created by XORing Address and Data from all Nodes of Firmware.
                /// </summary>
                public override int GetHashCode()
                {
                    return this.Nodes.Aggregate(0, (a, n) => a ^ (int)n.Addr ^ n.Data);
                }


                /// <summary>
                /// Equality operator for comparing two Firmwares via overiden Equals and so public Compare.
                /// Unlikely in Equals and also Compare, here two null Firmwares return True.
                /// </summary>
                public static bool operator ==(Firmware Fw1, Firmware Fw2)
                {
                    if (Fw1 is null && Fw2 is null)
                        return true;
                    if (Fw1 is null || Fw2 is null)
                        return false;

                    return Fw1.Equals(Fw2);
                }

                /// <summary>
                /// Not Equality operator for comparing two Firmwares via overiden Equals and so public Compare. 
                /// Unlikely in Equals, here two null Firmwares return True.
                /// </summary>
                public static bool operator !=(Firmware Fw1, Firmware Fw2)
                {
                    if (Fw1 is null && Fw2 is null)
                        return false;
                    if (Fw1 is null || Fw2 is null)
                        return true;

                    return !Fw1.Equals(Fw2);
                }
            }

            /// <summary>
            /// Atomic unit which every firmware consits of representated by single byte with max 32-bit long address.
            /// </summary>
            public class FwNode
            {
                /// <summary>Data is single data byte with associated 16-bit address Addr.</summary>
                public byte Data { get; set; } = 0xFF;
                /// <summary>Addr is address of Data byte, max 32-bit length is supported.</summary>
                public long Addr { get; set; } = 0;
            }

            /// <summary>
            /// FwInfo provides info about firmware, like format, first and last addresses, CRC, code size and reset vector.
            /// </summary>
            public class FwInfo
            {
                /// <summary>If firmware is invalid, Valid = false. Otherwise Valid = True.</summary>
                public bool Valid { get; } = false;
                /// <summary>Firmware format. TI-TXT, Intel-HEX or ELF is supported.</summary>
                public FwFormat Format { get; set; } = FwFormat.AUTO;
                /// <summary>First address in firmware, max 32-bit, usually 16-bit.</summary>
                public long AddrFirst { get; set; } = 0;
                /// <summary>Last address in firmware, max 32-bit, usually 16-bit.</summary>
                public long AddrLast { get; set; } = 0;
                /// <summary>Total length of firmware, count of all bytes from first address to last address.</summary>
                public int SizeFull { get; set; } = 0;
                /// <summary>Real count of all bytes in firmware parsed from file.</summary>
                public int SizeCode { get; set; } = 0;
                /// <summary>CRC-16-CCITT is 16-bit crc value of all data bytes in firmware.</summary>
                public int Crc16 { get; set; } = 0;
                /// <summary>[MSP430 specific] Reset vector is address (value) located usually at 16-bit address 0xFFFE.</summary>
                public long? ResetVector { get; set; } = 0;
                /// <summary>[MSP430 specific] Help property for later firmware manipulation, like slicing buffer blocks.</summary>
                public int SizeBuffer { get; set; } = 0;
                /// <summary>
                /// When parsing FW, FillFF can be set, to output code in single piece. Addresses, that dont belong to 
                /// original FW, are in this list.
                /// </summary>
                public List<long> FilledFFAddr { get; set; }

                /// <summary>
                /// Init all property to defaults.
                /// </summary>
                public FwInfo() { }

                /// <summary>
                /// Init fake info with values specified as parameters, the rest is init to zero.
                /// </summary>
                public FwInfo(long AddrFirst,
                              long AddrLast, 
                              int SizeFull, 
                              int SizeBuffer = 0, 
                              ICollection<long> FilledFFAddr = null)
                {
                    this.AddrFirst = AddrFirst;
                    this.AddrLast = AddrLast;
                    this.SizeFull = SizeFull;
                    this.SizeBuffer = SizeBuffer;
                    if (FilledFFAddr != null)
                        this.FilledFFAddr = FilledFFAddr.ToList();
                }

                /// <summary>
                /// Init info class to values calculated from valid firmware data. size_buffer is optional flag and
                /// ResetVectorAddr is usually 0xFFFE, address where reset vector is, and its also optional.
                /// </summary>
                public FwInfo(ICollection<FwNode> Nodes, 
                              FwFormat Format, 
                              int SizeBuffer = 0,
                              long ResetVectorAddr = -1, 
                              ICollection<long> FilledFFAddr = null)
                {
                    this.Valid = true;
                    this.Format = Format;
                    this.Crc16 = Nodes.Select(nod => nod.Data).ToList().Crc16Ccitt();
                    this.AddrFirst = Nodes.ElementAt(0).Addr;
                    this.AddrLast = Nodes.ElementAt(Nodes.Count - 1).Addr;
                    this.SizeBuffer = SizeBuffer;
                    this.SizeFull = (int)(AddrLast - AddrFirst) + 1;
                    this.SizeCode = Nodes.Count;
                    if (FilledFFAddr != null)
                        this.FilledFFAddr = FilledFFAddr.ToList();
                    if (ResetVectorAddr >= 0)
                        SetResetVector(Nodes, ResetVectorAddr);
                }

                /// <summary>
                /// Sets Reset Vector property to valid data, calculated from nodes. ResetVectorAddr is usually 0xFFFE.
                /// </summary>
                public long? SetResetVector(ICollection<FwNode> Nodes, long ResetVectorAddr)
                {
                    for (int j = Nodes.Count - 1; j >= 0; j--)
                    {
                        if (Nodes.ElementAt(j).Addr == ResetVectorAddr + 1 &&
                            Nodes.ElementAt(j - 1).Addr == ResetVectorAddr)
                        {
                            ResetVector = ((Nodes.ElementAt(j - 1).Data << 8) | Nodes.ElementAt(j).Data);
                            return ResetVector;
                        }
                    }
                    return null;
                }

                /// <summary>
                /// Formatted important properites describing current firmware.
                /// </summary>
                public override string ToString()
                {
                    if (this.Valid)
                    {
                        return $"Format: {this.Format}, Addresses: 0x{this.AddrFirst:X}-0x{this.AddrLast:X}, " +
                               $"Sizes: {this.SizeCode}/{this.SizeFull}, CRC16: 0x{this.Crc16:X4},";
                    }
                    else
                    {
                        return $"Invalid firmware.";
                    }                     
                }
            }

            /// <summary>
            /// [MSP430 specific] BSL Password is required for almost any BSL operation except Mass Erase.
            /// Password is last 16-byte (F543x-non-A only) or 32-byte (others) of IVT (FFE0-FFFF), if newer 5xx/6xx MCU is
            /// used. If MCU from older series is used (1xx/2xx/4xx), password is exactly 20-byte long. Mostly it is 32-byte.
            /// </summary>
            public class BslPasswords
            {
                /// <summary>This is the password mostly used in todays MSP430 MCUs, 5xx/6xx series except F543x (non A).</summary>
                public byte[] Password32Byte { get; set; }
                /// <summary>16-byte long Password used only in the very first series of 5xx, the F543x (non A).</summary>
                public byte[] Password16Byte { get; set; }
                /// <summary>20-byte long Password used in old 1xx/2xx/4xx series.</summary>
                public byte[] Password20Byte { get; set; }
            }
            #endregion

            #region Public Methods

            /// <summary>
            /// Parse firmware file from FirmwarePath in TI-TXT, Intel-HEX or ELF format and returns List of FwNode 
            /// (Data+Addr) and Info. Auto Mode reads data and based on some particular characters determine
            /// what firmare format it should be.
            /// <para/>FillFF is optional parameter forcing to fill missing addr nodes with 0xFF 
            /// and return monolithic piece of code, which is usefull for crc calc or overwriting whole memory in mcu.
            /// <para/>Log writes text (new line formatted) output only when parsing ELF firmware file.
            /// </summary>
            /// <exception cref="FirmwareToolsException"></exception>
            public static Firmware Parse(string FirmwarePath, 
                                         FwFormat Format = FwFormat.AUTO,
                                         bool FillFF = false,
                                         StringWriter Log = null)
            {
                switch(Format)
                {
                    default:
                    case FwFormat.AUTO:      return ParseAutoDetect(FirmwarePath, FillFF, log: Log);
                    case FwFormat.TI_TXT:    return ParseTiTxt(FirmwarePath, FillFF);
                    case FwFormat.INTEL_HEX: return ParseIntelHex(FirmwarePath, FillFF);
                    case FwFormat.SREC:      return ParseSrec(FirmwarePath, FillFF);
                    case FwFormat.ELF:       return ParseElf32(FirmwarePath, FillFF, log: Log);
                }
            }

            /// <summary>
            /// Create firmware multi-line string in TI-TXT or Intel-HEX format. ELF is not supported yet. 
            /// AUTO format will force TI-TXT format.
            /// <para/>LineLength defines amount of data bytes per one text row. When = 0, default values are set
            /// (TI-TXT = 16, Intel-HEX = 32, SREC = 32).
            /// </summary>
            /// <exception cref="FirmwareToolsException"></exception>
            public static string Create(Firmware Firmware, FwFormat Format = FwFormat.AUTO, int LineLength = 0)
            {
                return Create(Firmware.Nodes, Format, LineLength);
            }

            /// <summary>
            /// Create firmware multi-line string in TI-TXT or Intel-HEX format. ELF is not supported yet. 
            /// AUTO format will force TI-TXT format. AddrStart is address of first byte in data collection.
            /// <para/>LineLength defines amount of data bytes per one text row. When = 0, default values are set
            /// (TI-TXT = 16, Intel-HEX = 32, SREC = 32).
            /// </summary>
            /// <exception cref="FirmwareToolsException"></exception>
            public static string Create(IEnumerable<byte> Data, 
                                        int AddrStart, 
                                        FwFormat Format = FwFormat.AUTO, 
                                        int LineLength = 0)
            {
                return Create(Data.Select(x => new FwNode { Data = x, Addr = AddrStart++ }).ToList(), 
                              Format, 
                              LineLength);
            }

            /// <summary>
            /// Create firmware multi-line string in TI-TXT or Intel-HEX format. ELF is not supported yet. 
            /// AUTO format will force TI-TXT format.
            /// <para/>LineLength defines amount of data bytes per one text row. When = 0, default values are set
            /// (TI-TXT = 16, Intel-HEX = 32, SREC = 32).
            /// </summary>
            /// <exception cref="FirmwareToolsException"></exception>
            public static string Create(ICollection<FwNode> Data, FwFormat Format = FwFormat.AUTO, int LineLength = 0)
            {
                switch (Format)
                {
                    default:
                    case FwFormat.AUTO:
                    case FwFormat.TI_TXT:    return CreateTiTxt(Data, LineLength);
                    case FwFormat.INTEL_HEX: return CreateIntelHex(Data, LineLength);
                    case FwFormat.SREC:      return CreateSrec(Data, LineLength);
                    case FwFormat.ELF:       throw new FirmwareToolsException(472, ERR_472);
                }
            }

            /// <summary>
            /// Convert firmware TI-TXT, Intel-HEX or ELF format (auto detected) to firmware in TI-TX or Intel-HEX format.
            /// Returned Fw is firmware and Format is useful for auto-detect feedback, indicates input format.
            /// <para/>FillFF is optional parameter forcing to fill missing addr nodes with 0xFF 
            /// and return monolithic piece of code, which is usefull for crc calc or overwriting whole memory in mcu.
            /// <para/>LineLength defines amount of data bytes per one text row. When = 0, default values are set
            /// (TI-TXT = 16, Intel-HEX = 32, SREC = 32).
            /// </summary>
            /// <exception cref="FirmwareToolsException"></exception>
            public static (string Fw, FwFormat Format) Convert(string FirmwarePath, 
                                                               FwFormat Format, 
                                                               bool FillFF = false, 
                                                               int LineLength = 0)
            { 
                switch (Format)
                {
                    default:
                    case FwFormat.AUTO:
                    case FwFormat.TI_TXT:
                        {
                            Firmware ret = ParseAutoDetect(FirmwarePath, FillFF);
                            return (Create(ret, FwFormat.TI_TXT, LineLength), ret.Info.Format);
                        }
                    case FwFormat.INTEL_HEX:
                        {
                            Firmware ret = ParseAutoDetect(FirmwarePath, FillFF);
                            return (Create(ret, FwFormat.INTEL_HEX, LineLength), ret.Info.Format);
                        }
                    case FwFormat.SREC:
                        {
                            Firmware ret = ParseAutoDetect(FirmwarePath, FillFF);
                            return (Create(ret, FwFormat.SREC, LineLength), ret.Info.Format);
                        }
                    case FwFormat.ELF: throw new FirmwareToolsException(472, ERR_472);
                }
            }

            /// <summary>
            /// [MSP430 specific] Read and parse firmware file (format auto-detected) and return BSL password.
            /// Password is last 16-byte (F543x-non-A only) or 32-byte (others) of IVT (FFE0-FFFF), if newer 5xx/6xx MCU is
            /// used. If MCU from older series is used (1xx/2xx/4xx), password is exactly 20-byte long. Mostly it is 32-byte.
            /// </summary>
            public static BslPasswords GetPassword(string FirmwarePath)
            {
                return GetBslPassword(FirmwarePath);
            }

            /// <summary>
            /// Validate firmware file and return FwInfo class, with specific firmware information.
            /// ResetVector and SizeBuffer are MSP430 specifics, other properties are universal.
            /// <para/>Log writes text (new line formatted) output only when parsing ELF firmware file.
            /// </summary>
            /// <exception cref="FirmwareToolsException"></exception>
            public static FwInfo Validate(string FirmwarePath, StringWriter Log = null)
            {
                Firmware ret = Parse(FirmwarePath, Log: Log);
                ret.SetResetVector(0xFFFE);
                return ret.Info;
            }

            /// <summary>
            /// Combines two firmware files into single one with format specified. Usually, main firmware
            /// and EEPROM file is done this way, or main firmware and Info A flash content is merged.
            /// Returned Fw is firmware and Format1 with Format2 are useful for auto-detect feedback, indicates input formats.
            /// <para/>FillFF is optional parameter forcing to fill missing addr nodes with 0xFF 
            /// and return monolithic piece of code, which is usefull for crc calc or overwriting whole memory in mcu.
            /// <para/>LineLength defines amount of data bytes per one text row. When = 0, default values are set
            /// (TI-TXT = 16, Intel-HEX = 32, SREC = 32).
            /// </summary>
            /// <exception cref="FirmwareToolsException"></exception>
            public static (string Fw, FwFormat Format1, FwFormat Format2) Combine(string FirmwarePath1, 
                                                                                  string FirmwarePath2, 
                                                                                  FwFormat Format, 
                                                                                  bool FillFF = false, 
                                                                                  int LineLength = 0)
            {
                Firmware fw1 = ParseAutoDetect(FirmwarePath1, FillFF);
                Firmware fw2 = ParseAutoDetect(FirmwarePath2, FillFF);
                return (Create(CombineFw(fw1, fw2, FillFF), Format, LineLength), fw1.Info.Format, fw2.Info.Format);
            }

            /// <summary>
            /// Compare two Firmware files. First, auto-detects format, then parse Nodes (Address + Data) and finally run compare.
            /// <para/>Equal = True if both files contains exactly same set of Firmware Nodes, in other case result is false.
            /// <para/>Equal = False if any Firmware is null or if any Firmware Node count is zero.
            /// <para/>Match [0.0 ; 1.0] is match when 1.0 is full match and Equal = True, and 0.0 is different fw.
            /// <para/>BytesDiff is count of bytes (Data nodes) which both firmwares differ at. 0 means full match.
            /// </summary>
            /// <exception cref="FirmwareToolsException"></exception>
            public static (bool Equal, double Match, int BytesDiff) Compare(string FirmwarePath1, string FirmwarePath2)
            {
                return Compare(ParseAutoDetect(FirmwarePath1, true), ParseAutoDetect(FirmwarePath2, true));
            }

            /// <summary>
            /// Compare two already parsed Firmware objects (Address + Data).
            /// <para/>Equal = True if both files contains exactly same set of Firmware Nodes, in other case result is false.
            /// <para/>Equal = False if any Firmware is null or if any Firmware Node count is zero.
            /// <para/>Match [0.0 ; 1.0] is match when 1.0 is full match and Equal = True, and 0.0 is  different fw.
            /// <para/>BytesDiff is count of bytes (Data nodes) which both firmwares differ at. 0 means full match.
            /// </summary>
            /// <exception cref="FirmwareToolsException"></exception>
            public static (bool Equal, double Match, int BytesDiff) Compare(Firmware Firmware1, Firmware Firmware2)
            {
                if (Firmware1 == null || Firmware2 == null ||
                    Firmware1.Nodes == null || Firmware2.Nodes == null ||
                    Firmware1.Nodes.Count == 0 || Firmware2.Nodes.Count == 0)
                {
                    return (false, 0.0, -1);
                }

                var matches = Firmware2.Nodes.Intersect(Firmware1.Nodes, new FwNodeComparer());
                int fwCnt = (Firmware1.Nodes.Count > Firmware2.Nodes.Count) ? Firmware1.Nodes.Count : Firmware2.Nodes.Count;
                int matchCnt = matches.Count();

                if (matches == null)
                    return (false, 0.0, fwCnt);
                if (matchCnt != Firmware1.Nodes.Count || matchCnt != Firmware2.Nodes.Count)
                    return (false, matchCnt / (double)fwCnt, fwCnt - matchCnt);

                return (true, 1.0, 0);
            }

            #endregion
        }
    }
}
