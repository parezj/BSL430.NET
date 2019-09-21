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
            #region Private Core Methods
            private static Firmware ParseAutoDetect(string firmware_path, bool fill_FF = false, StringWriter log = null)
            {
                if (!System.IO.File.Exists(firmware_path))
                    throw new FirmwareToolsException(473, ERR_473);

                byte[] data_binary;
                string data_text;

                try
                {
                    data_text = System.IO.File.ReadAllText(firmware_path);
                    data_binary = System.IO.File.ReadAllBytes(firmware_path);
                }
                catch (Exception ex)
                {
                    throw new FirmwareToolsException(474, $"{ERR_474} {ex.Message}", ex);
                }

                if (data_binary != null &&
                    data_binary.Length >= 4 &&
                    data_binary[0] == 0x7F &&
                    data_binary[1] == 0x45 &&
                    data_binary[2] == 0x4C &&
                    data_binary[3] == 0x46)  // ELF
                {
                    return ParseElf32("", fill_FF, data_binary, log);
                }
                else if (data_text.Contains("@"))  // TI-TXT
                {
                    return ParseTiTxt("", fill_FF, data_text);
                }
                else if (data_text.Contains("S")) // SREC
                {
                    return ParseSrec("", fill_FF, data_text);
                }
                else if (data_text.Contains(":")) // Intel-HEX
                {
                    return ParseIntelHex("", fill_FF, data_text);
                }
                else throw new FirmwareToolsException(475, ERR_475);
            }

            private static Firmware ParseIntelHex(string firmware_path, bool fill_FF = false, string _data = "")
            {
                List<FwNode> ret = new List<FwNode>();
                try
                {
                    string data = "";

                    if (_data == "")
                    {
                        if (!System.IO.File.Exists(firmware_path))
                            throw new FirmwareToolsException(473, ERR_473);

                        try
                        {
                            data = System.IO.File.ReadAllText(firmware_path);
                        }
                        catch (Exception ex)
                        {
                            throw new FirmwareToolsException(474, $"{ERR_474} {ex.Message}");
                        }
                    }
                    else data = _data;

                    uint addr_offset = 0;
                    uint addr_ext = 0;

                    foreach (string dat in data.Split(':'))
                    {
                        if (dat.Length > 10)
                        {
                            string line = dat.Trim();

                            byte seg_size = System.Convert.ToByte(line.Substring(0, 2), 16);
                            uint seg_addr = System.Convert.ToUInt32(line.Substring(2, 4), 16);
                            byte seg_rec = System.Convert.ToByte(line.Substring(6, 2), 16);
                            byte[] seg_data = line.Substring(8, (seg_size * 2)).ToByteArray();
                            byte seg_chck = System.Convert.ToByte(line.Substring(8 + (seg_size * 2), 2), 16);

                            byte chck_sum = 0;
                            foreach (byte sm in line.Substring(0, line.Length - 2).ToByteArray())
                                chck_sum += sm;

                            chck_sum = (byte)((byte)(~chck_sum) + 1);

                            if (line.Length != (seg_size * 2) + 10 ||
                                seg_rec > 5 ||
                                (seg_rec == 1 && (seg_data != null && seg_data.Length > 0)) ||
                                (seg_rec != 1 && (seg_data == null || seg_data.Length == 0)) ||
                                chck_sum != seg_chck)
                            {
                                throw new FirmwareToolsException(445, ERR_445);
                            }

                            if (seg_rec == 2 && seg_data.Length == 2)   // Extended Segment Address - 16-bit segment base
                            {
                                addr_offset = ((uint)seg_data[0] << 8) | seg_data[1];
                            }

                            if (seg_rec == 4 && seg_data.Length == 2)   // Extended Linear Address - 32 bit addressing
                            {
                                addr_ext = ((uint)seg_data[0] << 24) | (uint)seg_data[1] << 16;
                            }

                            if (seg_rec == 0)   // Data
                            {
                                foreach (byte seg_dat in seg_data)
                                {
                                    ret.Add(new FwNode { Data = seg_dat, Addr = seg_addr + addr_offset + addr_ext });
                                    seg_addr++;
                                }
                            }
                        }
                    }

                    if (ret.Count == 0)
                        throw new FirmwareToolsException(445, ERR_445);

                    if (fill_FF)
                    {
                        var (Data, FilledFFAddr) = FillGapsWithFF(ret);
                        return new Firmware(Data, FwFormat.INTEL_HEX, FilledFFAddr: FilledFFAddr);
                    }

                    return new Firmware(ret, FwFormat.INTEL_HEX);
                }
                catch (Exception ex)
                {
                    if (ex is FirmwareToolsException)
                        throw ex;
                    else
                        throw new FirmwareToolsException(444, $"{ERR_444} {ex.Message}", ex);
                }
            }

            private static Firmware ParseTiTxt(string firmware_path, bool fill_FF = false, string _data = "")
            {
                List<FwNode> ret = new List<FwNode>();
                List<long> ret_fillFF = new List<long>();
                try
                {
                    string data = "";

                    if (_data == "")
                    {
                        if (!System.IO.File.Exists(firmware_path))
                            throw new FirmwareToolsException(473, ERR_473);

                        try
                        {
                            data = System.IO.File.ReadAllText(firmware_path);
                        }
                        catch (Exception ex)
                        {
                            throw new FirmwareToolsException(474, $"{ERR_474} {ex.Message}", ex);
                        }
                    }
                    else data = _data;

                    data = data.Trim();
                    string regex_header = @"(?:[@])?[0-9a-fA-F]{4}";
                    string regex_data = @"[0-9a-fA-F]{2}";

                    string[] blocks = Regex.Split(data, regex_header).Where(x => !string.IsNullOrEmpty(x)).ToArray();

                    uint[] addresses = Regex.Matches(data, regex_header)
                                            .Cast<Match>()
                                            .Select(a => System.Convert.ToUInt32(a.ToString().Replace("@", ""), 16)).ToArray();

                    if (addresses == null || blocks == null || blocks.Length != addresses.Length)
                        throw new FirmwareToolsException(445, ERR_445);

                    uint address = addresses[0];

                    for (int i = 0; i < blocks.Length; i++)
                    {
                        if (fill_FF && i > 0)
                        {
                            uint diff = addresses[i] - address;
                            if (diff > 0)
                            {
                                for (uint k = 0; k < diff; k++)
                                {
                                    uint filled_addr = address + k;
                                    ret.Add(new FwNode { Data = 0xFF, Addr = filled_addr });
                                    ret_fillFF.Add(filled_addr);
                                }
                            }
                        }
                        string[] bytes = Regex.Matches(blocks[i], regex_data).Cast<Match>().Select(m => m.Value).ToArray();

                        if (bytes == null || bytes.Length == 0)
                            throw new FirmwareToolsException(445, ERR_445);

                        address = addresses[i];

                        foreach (string dat in bytes)
                        {
                            ret.Add(new FwNode { Data = System.Convert.ToByte(dat, 16), Addr = address });
                            address++;
                        }
                    }

                    if (ret.Count == 0)
                        throw new FirmwareToolsException(445, ERR_445);

                    return new Firmware(ret, FwFormat.TI_TXT, FilledFFAddr: (ret_fillFF.Count > 0) ? ret_fillFF : null);
                }
                catch (Exception ex)
                {
                    if (ex is FirmwareToolsException)
                        throw ex;
                    else
                        throw new FirmwareToolsException(444, $"{ERR_444} {ex.Message}", ex);
                }
            }

            private static Firmware ParseSrec(string firmware_path, bool fill_FF = false, string _data = "")
            {
                List<FwNode> ret = new List<FwNode>();
                try
                {
                    string data = "";

                    if (_data == "")
                    {
                        if (!System.IO.File.Exists(firmware_path))
                            throw new FirmwareToolsException(473, ERR_473);

                        try
                        {
                            data = System.IO.File.ReadAllText(firmware_path);
                        }
                        catch (Exception ex)
                        {
                            throw new FirmwareToolsException(474, $"{ERR_474} {ex.Message}", ex);
                        }
                    }
                    else data = _data;

                    byte rec_type_first = 0;

                    foreach (string dat in data.Split('S'))
                    {
                        if (dat.Length >= 9)
                        {
                            string line = dat.Trim();
                            int addr_sclr = 0;

                            byte rec_type = System.Convert.ToByte(line.Substring(0, 1), 16);

                            if (rec_type == 1)
                                addr_sclr = 0;
                            else if (rec_type == 2)
                                addr_sclr = 2;
                            else if (rec_type == 3)
                                addr_sclr = 4;
                            else if ((rec_type == 7 && rec_type_first != 3) ||
                                     (rec_type == 8 && rec_type_first != 2) ||
                                     (rec_type == 9 && rec_type_first != 1))
                                throw new FirmwareToolsException(445, ERR_445);
                            else
                                continue;

                            byte rec_count = System.Convert.ToByte(line.Substring(1, 2), 16);
                            uint rec_addr = System.Convert.ToUInt32(line.Substring(3, 4 + addr_sclr), 16);
                            byte[] rec_data = line.Substring(7 + addr_sclr, (rec_count * 2) - 6 - addr_sclr).ToByteArray();
                            byte rec_chck = System.Convert.ToByte(line.Substring(line.Length - 2, 2), 16);

                            int _chck_sum = 0;
                            foreach (byte sm in line.Substring(1, line.Length - 3).ToByteArray())
                                _chck_sum += sm;

                            byte chck_sum = (byte)(~(_chck_sum & 0x000000FF));
                            if (chck_sum != rec_chck)
                                throw new FirmwareToolsException(445, ERR_445);

                            if (rec_type_first == 0)
                                rec_type_first = rec_type;
                            else if (rec_type_first != 0 && rec_type != rec_type_first)
                                throw new FirmwareToolsException(445, ERR_445);

                            foreach (byte rec_dat in rec_data)
                            {
                                ret.Add(new FwNode { Data = rec_dat, Addr = rec_addr });
                                rec_addr++;
                            }
                        }
                    }

                    if (ret.Count == 0)
                        throw new FirmwareToolsException(445, ERR_445);

                    if (fill_FF)
                    {
                        var (Data, FilledFFAddr) = FillGapsWithFF(ret);
                        return new Firmware(Data, FwFormat.SREC, FilledFFAddr: FilledFFAddr);
                    }

                    return new Firmware(ret, FwFormat.SREC);
                }
                catch (Exception ex)
                {
                    if (ex is FirmwareToolsException)
                        throw ex;
                    else
                        throw new FirmwareToolsException(444, $"{ERR_444} {ex.Message}", ex);
                }
            }

            private static Firmware ParseElf32(string firmware_path,
                                               bool fill_FF = false,
                                               byte[] _data = null,
                                               StringWriter log = null)
            {
                List<FwNode> ret = new List<FwNode>();
                try
                {
                    byte[] data;

                    if (_data == null)
                    {
                        if (!System.IO.File.Exists(firmware_path))
                            throw new FirmwareToolsException(473, ERR_473);

                        try
                        {
                            data = System.IO.File.ReadAllBytes(firmware_path);
                        }
                        catch (Exception ex)
                        {
                            throw new FirmwareToolsException(474, $"{ERR_474} {ex.Message}", ex);
                        }
                    }
                    else data = _data;

                    if (data[0] != 0x7F || data[1] != 0x45 || data[2] != 0x4C || data[3] != 0x46)
                        throw new FirmwareToolsException(480, ERR_480);    // magic number wrong -> bad file

                    if (data[5] != 0x01)
                        throw new FirmwareToolsException(476, ERR_476);    // only 32-bit ELF

                    if (data[6] != 0x01)
                        throw new FirmwareToolsException(477, ERR_477);    // only little endian

                    if ((ushort)(data[17] << 8 | data[16]) != 0x02)
                        throw new FirmwareToolsException(478, ERR_478);    // only executable file type

                    ushort machine = (ushort)(data[19] << 8 | data[18]);
                    string machineStr = $"machine: unknown (0x{machine.ToString("X4")})";

                    if (machine == 0x69)
                        machineStr = "machine: Texas Instrument embedded microcontroller MSP430 (0x69)";
                    else if (machine == 0x28)
                        machineStr = "machine: ARM 32-bit architecture (AARCH32) (0x28)";
                    else if (machine == 0x53)
                        machineStr = "machine: Atmel AVR 8-bit microcontroller (0x53)";
                    else if (machine == 0xB9)
                        machineStr = "machine: Atmel Corporation 32-bit microprocessor family (0xB9)";
                    else if (machine == 0x03)
                        machineStr = "machine: Intel 80386 (0x03)";
                    else if (machine == 0x07)
                        machineStr = "machine: Intel 80860 (0x07)";
                    else if (machine == 0x04)
                        machineStr = "machine: Motorola 68000 (0x04)";
                    //else throw new FirmwareToolsException(479, ERR_479);  // only TI MSP430 or ARM (MSP432) machine

                    log?.WriteLine("\nELF info:  32-bit/little-endian/executable (0x1/0x1/0x2)");
                    log?.WriteLine(machineStr);
                    log?.WriteLine("=======================================");


                    uint entry = (uint)(data[27] << 24 | data[26] << 16 | data[25] << 8 | data[24]);
                    uint phoff = (uint)(data[31] << 24 | data[30] << 16 | data[29] << 8 | data[28]);
                    uint shoff = (uint)(data[35] << 24 | data[34] << 16 | data[33] << 8 | data[32]);
                    ushort phentsize = (ushort)(data[43] << 8 | data[42]);
                    ushort phnum = (ushort)(data[45] << 8 | data[44]);
                    ushort shentsize = (ushort)(data[47] << 8 | data[46]);
                    ushort shnum = (ushort)(data[49] << 8 | data[48]);
                    ushort shstrndx = (ushort)(data[51] << 8 | data[50]);

                    uint shstrab_head = shoff + ((uint)shstrndx * shentsize);
                    uint shstrtab_off = (uint)(data[shstrab_head + 19] << 24 | data[shstrab_head + 18] << 16 |
                                                data[shstrab_head + 17] << 8 | data[shstrab_head + 16]);

                    log?.WriteLine("header:\n");
                    log?.WriteLine($"  entry:      0x{entry.ToString("X8")}");
                    log?.WriteLine($"  phoff:      0x{phoff.ToString("X8")}");
                    log?.WriteLine($"  shoff:      0x{shoff.ToString("X8")}");
                    log?.WriteLine($"  phentsize:  0x{phentsize.ToString("X4")}");
                    log?.WriteLine($"  phnum:      0x{phnum.ToString("X4")}");
                    log?.WriteLine($"  shentsize:  0x{shentsize.ToString("X4")}");
                    log?.WriteLine($"  shnum:      0x{shnum.ToString("X4")}");
                    log?.WriteLine($"  shstrndx:   0x{shstrndx.ToString("X4")}");

                    log?.WriteLine("=======================================");
                    log?.WriteLine("sections:\n");
                    log?.WriteLine("  num name                address         offset          size");

                    List<(string sh_name, uint sh_addr, uint sh_off, uint sh_size)> sections =
                        new List<(string sh_name, uint sh_addr, uint sh_off, uint sh_size)>();

                    for (uint s = 0, addr = shoff; s < shnum; s++, addr += shentsize)
                    {
                        uint sh_name = (uint)(data[addr + 3] << 24 | data[addr + 2] << 16 |
                                              data[addr + 1] << 8 | data[addr]);
                        uint sh_type = (uint)(data[addr + 7] << 24 | data[addr + 6] << 16 |
                                              data[addr + 5] << 8 | data[addr + 4]);
                        uint sh_addr = (uint)(data[addr + 15] << 24 | data[addr + 14] << 16 |
                                              data[addr + 13] << 8 | data[addr + 12]);
                        uint sh_off = (uint)(data[addr + 19] << 24 | data[addr + 18] << 16 |
                                              data[addr + 17] << 8 | data[addr + 16]);
                        uint sh_size = (uint)(data[addr + 23] << 24 | data[addr + 22] << 16 |
                                              data[addr + 21] << 8 | data[addr + 20]);

                        string sh_name_str = "";
                        if (data[shstrtab_off + sh_name] != (char)0)
                        {
                            List<byte> strBytes = new List<byte>();
                            while (data[shstrtab_off + sh_name] != 0x00)
                            {
                                strBytes.Add(data[shstrtab_off + sh_name]);
                                sh_name++;
                            }
                            sh_name_str = Encoding.ASCII.GetString(strBytes.ToArray());
                        }

                        if (sh_type == 0x01 && sh_size > 0 && !sh_name_str.ToLower().Contains("debug"))  // PROGBITS only
                        {
                            sections.Add((sh_name_str, sh_addr, sh_off, sh_size));
                            int str_pad = (sh_name_str.Length > 20) ? 0 : 20 - sh_name_str.Length;
                            log?.WriteLine($"  {s}{new String(' ', 4 - s.ToString().Length)}" +
                                           $"{sh_name_str}{new String(' ', str_pad)}" +
                                           $"0x{sh_addr.ToString("X8")}" +
                                           $"{new String(' ', 14 - sh_addr.ToString("X8").Length)}" +
                                           $"0x{sh_off.ToString("X8")}" +
                                           $"{new String(' ', 14 - sh_off.ToString("X8").Length)}" +
                                           $"0x{sh_size.ToString("X8")}");
                        }
                    }

                    log?.WriteLine("=======================================");
                    log?.WriteLine("program:\n");
                    log?.WriteLine("  offset          physaddr        memsize         flags align");


                    List<(uint p_offset, uint p_paddr, uint p_filesz, uint p_align)> program =
                        new List<(uint p_offset, uint p_paddr, uint p_filesz, uint p_align)>();

                    for (uint p = 0, addr = phoff; p < phnum; p++, addr += phentsize)
                    {
                        uint p_type = (uint)(data[addr + 3] << 24 | data[addr + 2] << 16 |
                                               data[addr + 1] << 8 | data[addr]);
                        uint p_offset = (uint)(data[addr + 7] << 24 | data[addr + 6] << 16 |
                                               data[addr + 5] << 8 | data[addr + 4]);
                        uint p_paddr = (uint)(data[addr + 15] << 24 | data[addr + 14] << 16 |
                                               data[addr + 13] << 8 | data[addr + 12]);
                        uint p_filesz = (uint)(data[addr + 19] << 24 | data[addr + 18] << 16 |
                                               data[addr + 17] << 8 | data[addr + 16]);
                        uint p_flags = (uint)(data[addr + 27] << 24 | data[addr + 26] << 16 |
                                               data[addr + 25] << 8 | data[addr + 24]);
                        uint p_align = (uint)(data[addr + 31] << 24 | data[addr + 30] << 16 |
                                               data[addr + 29] << 8 | data[addr + 28]);

                        string flags = "";
                        if ((p_flags & 0x01) == 0x01)
                            flags += "X ";
                        if ((p_flags & 0x02) == 0x02)
                            flags += "W ";
                        if ((p_flags & 0x04) == 0x04)
                            flags += "R ";
                        flags.TrimEnd();

                        if (p_type == 0x01 && p_filesz > 0)  // LOAD type only
                        {
                            program.Add((p_offset, p_paddr, p_filesz, p_align));
                            log?.WriteLine($"  0x{p_offset.ToString("X8")}" +
                                           $"{new String(' ', 14 - p_offset.ToString("X8").Length)}" +
                                           $"0x{p_paddr.ToString("X8")}" +
                                           $"{new String(' ', 14 - p_paddr.ToString("X8").Length)}" +
                                           $"0x{p_filesz.ToString("X8")}" +
                                           $"{new String(' ', 14 - p_filesz.ToString("X8").Length)}" +
                                           $"{flags}" +
                                           $"{new String(' ', 6 - flags.Length)}" +
                                           $"0x{p_align.ToString("X1")}");
                        }
                    }

                    log?.WriteLine("=======================================");

                    foreach (var (p_offset, p_paddr, p_filesz, p_align) in program)
                    {
                        for (uint i = p_paddr, j = 0, k = p_offset; j < p_filesz; i++, j++, k++)
                        {
                            ret.Add(new FwNode { Data = data[k], Addr = i });
                        }
                    }

                    if (ret.Count == 0)
                        throw new FirmwareToolsException(445, ERR_445);

                    if (fill_FF)
                    {
                        var (Data, FilledFFAddr) = FillGapsWithFF(ret);
                        return new Firmware(Data, FwFormat.ELF, FilledFFAddr: FilledFFAddr);
                    }

                    return new Firmware(ret, FwFormat.ELF);
                }
                catch (Exception ex)
                {
                    if (ex is FirmwareToolsException)
                        throw ex;
                    else
                        throw new FirmwareToolsException(444, $"{ERR_444} {ex.Message}", ex);
                }
            }
            private static (List<FwNode> Data, List<long> FilledFFAddr) FillGapsWithFF(List<FwNode> code)
            {
                long last_addr = code[0].Addr;
                List<FwNode> full_code = new List<FwNode>();
                List<long> filled_FF_addr = new List<long>();

                foreach (FwNode nod in code)
                {
                    long diff = nod.Addr - last_addr;
                    if (diff > 1)
                    {
                        for (int i = 0; i < diff - 1; i++)
                        {
                            long addr = last_addr + 1 + i;
                            full_code.Add(new FwNode { Data = 0xFF, Addr = addr });
                            filled_FF_addr.Add(addr);
                        }
                    }
                    full_code.Add(new FwNode { Data = nod.Data, Addr = nod.Addr });
                    last_addr = nod.Addr;
                }

                return (Data: full_code, FilledFFAddr: filled_FF_addr);
            }

            private static string CreateIntelHex(ICollection<FwNode> data, int _lineLength = 0)
            {
                const byte LINE_SIZE = 32;
                const string EOF = "00000001FF";
                const string EXTADDR = "02000004";

                byte lineLength = LINE_SIZE;
                if (_lineLength > 0)
                    lineLength = (byte)_lineLength;

                try
                {
                    if (data == null || data.Count < 1)
                        throw new FirmwareToolsException(471, ERR_471);

                    byte line_size = lineLength;
                    long last_ext_addr = -1;
                    StringBuilder ret = new StringBuilder();
                    List<FwNode> line_buff = new List<FwNode>();

                    data = data.OrderBy(n => n.Addr).ToList();

                    for (int i = 0; i < data.Count; i++)
                    {
                        line_buff.Add(data.ElementAt(i));

                        if (data.ElementAt(i).Addr > 0xFFFF && last_ext_addr != data.ElementAt(i).Addr)
                        {
                            last_ext_addr = line_buff.ElementAt(0).Addr;
                            ushort _extaddr = (ushort)(line_buff.ElementAt(0).Addr >> 16);
                            byte _extaddr_h = (byte)(_extaddr >> 8);
                            byte _extaddr_l = (byte)(_extaddr & 0xff);

                            ret.Append($":{EXTADDR}{_extaddr.ToString("X4")}" +
                                       $"{((byte)(~(6 + _extaddr_h + _extaddr_l) + 1)).ToString("X2")}{Environment.NewLine}");
                        }

                        if ((i > 0 && data.Count > i + 1 && data.ElementAt(i + 1).Addr - data.ElementAt(i).Addr > 1 && line_buff.Count > 1) ||
                            line_size <= 1 || i == data.Count - 1)
                        {
                            byte checksum = (byte)(lineLength - line_size + 1);
                            checksum += (byte)(line_buff[0].Addr);
                            checksum += (byte)(line_buff[0].Addr >> 8);

                            foreach (FwNode nod in line_buff)
                                checksum += nod.Data;

                            ret.Append($":{line_buff.Count.ToString("X2")}" +
                                       $"{line_buff.ElementAt(0).Addr.ToString("X4")}00" +
                                       $"{(BitConverter.ToString(line_buff.Select(x => x.Data).ToArray()).Replace("-", ""))}" +
                                       $"{((byte)(~checksum + 1)).ToString("X2")}{Environment.NewLine}");

                            line_buff.Clear();
                            line_size = lineLength;
                        }
                        else line_size--;
                    }
                    ret.Append($":{EOF}{Environment.NewLine}");

                    return ret.ToString();
                }
                catch (Exception ex)
                {
                    if (ex is FirmwareToolsException)
                        throw ex;
                    else
                        throw new FirmwareToolsException(470, $"{ERR_470} {ex.Message}", ex);
                }
            }
            private static string CreateSrec(ICollection<FwNode> data, int _lineLength = 0)
            {
                const byte LINE_SIZE = 32;
                const string S0 = "S00B0000ACEC0FFEEADDC0CAFE";
                const string S7 = "S70500000000FA";
                const string S8 = "S804000000FB";
                const string S9 = "S9030000FC";

                byte lineLength = LINE_SIZE;
                if (_lineLength > 0)
                    lineLength = (byte)_lineLength;

                try
                {
                    if (data == null || data.Count < 1)
                        throw new FirmwareToolsException(471, ERR_471);

                    byte line_size = lineLength;
                    StringBuilder ret = new StringBuilder();
                    List<FwNode> line_buff = new List<FwNode>();

                    data = data.OrderBy(n => n.Addr).ToList();

                    ret.Append($"{S0}{Environment.NewLine}");

                    long max_addr = data.ElementAt(data.Count - 1).Addr;
                    string rec_prefix = "S1";
                    string addr_formater = "X4";
                    byte rec_type = 1;
                    int addr_len = 2;
                    if (max_addr > 0xFFFFFF)
                    {
                        rec_prefix = "S3";
                        addr_formater = "X8";
                        rec_type = 3;
                        addr_len = 4;
                    }
                    else if (max_addr > 0xFFFF)
                    {
                        rec_prefix = "S2";
                        addr_formater = "X6";
                        rec_type = 2;
                        addr_len = 3;
                    }

                    for (int i = 0; i < data.Count; i++)
                    {
                        line_buff.Add(data.ElementAt(i));

                        if ((i > 0 && data.Count > i + 1 && data.ElementAt(i + 1).Addr - data.ElementAt(i).Addr > 1 && line_buff.Count > 1) ||
                            line_size <= 1 || i == data.Count - 1)
                        {
                            int data_count = lineLength - line_size + 1;
                            byte rec_count = (byte)(data_count + addr_len + 1);

                            int checksum = BitConverter.GetBytes(line_buff.ElementAt(0).Addr).Aggregate(0, (a, b) => a + b);
                            checksum += rec_count;

                            foreach (FwNode nod in line_buff)
                                checksum += nod.Data;

                            ret.Append($"{rec_prefix}{rec_count.ToString("X2")}{line_buff.ElementAt(0).Addr.ToString(addr_formater)}" +
                                       $"{(BitConverter.ToString(line_buff.Select(x => x.Data).ToArray()).Replace("-", ""))}" +
                                       $"{(((byte)~(checksum & 0x000000FF))).ToString("X2")}{Environment.NewLine}");

                            line_buff.Clear();
                            line_size = lineLength;
                        }
                        else line_size--;
                    }

                    switch (rec_type)
                    {
                        default:
                        case 1: ret.Append($"{S9}{Environment.NewLine}"); break;
                        case 2: ret.Append($"{S8}{Environment.NewLine}"); break;
                        case 3: ret.Append($"{S7}{Environment.NewLine}"); break;
                    }

                    return ret.ToString();
                }
                catch (Exception ex)
                {
                    if (ex is FirmwareToolsException)
                        throw ex;
                    else
                        throw new FirmwareToolsException(470, $"{ERR_470} {ex.Message}", ex);
                }
            }

            private static string CreateTiTxt(ICollection<FwNode> data, int _lineLength = 0)
            {
                const byte LINE_SIZE = 16;

                byte lineLength = LINE_SIZE;
                if (_lineLength > 0)
                    lineLength = (byte)_lineLength;

                try
                {
                    if (data == null || data.Count < 1)
                        throw new FirmwareToolsException(471, ERR_471);

                    byte horizontal_size = lineLength;
                    StringBuilder ret = new StringBuilder();

                    data = data.OrderBy(n => n.Addr).ToList();

                    for (int i = 0; i < data.Count; i++)
                    {
                        if (i == 0 || (i > 0 && data.ElementAt(i).Addr - data.ElementAt(i - 1).Addr > 1))
                        {
                            string _addr = data.ElementAt(i).Addr.ToString("X4").ToLower();
                            if (data.ElementAt(i).Addr > 0xFFFFFF)
                                _addr = data.ElementAt(i).Addr.ToString("X8").ToLower();
                            else if (data.ElementAt(i).Addr > 0xFFFF)
                                _addr = data.ElementAt(i).Addr.ToString("X6").ToLower();

                            ret.Append($"{((i == 0) ? "" : Environment.NewLine)}@{_addr}{Environment.NewLine}");
                            horizontal_size = lineLength;
                        }

                        ret.Append($"{data.ElementAt(i).Data.ToString("X2")} ");

                        if (horizontal_size <= 1)
                        {
                            ret.Append(Environment.NewLine);
                            horizontal_size = lineLength;
                        }
                        else horizontal_size--;
                    }
                    ret.Append($"q{Environment.NewLine}");

                    return ret.ToString();
                }
                catch (Exception ex)
                {
                    if (ex is FirmwareToolsException)
                        throw ex;
                    else
                        throw new FirmwareToolsException(470, $"{ERR_470} {ex.Message}", ex);
                }
            }
            private static ICollection<FwNode> CombineFw(Firmware fw1, Firmware fw2, bool fillFF = false)
            {
                if (fw1 == null || fw2 == null)
                    throw new FirmwareToolsException(471, ERR_471);

                var matches = fw1.Nodes.Intersect(fw2.Nodes, new FwNodeAddrComparer());
                if (matches == null || matches.Count() < 1)
                {
                    fw1.Nodes.AddRange(fw2.Nodes);
                    var ret = fw1.Nodes.OrderBy(o => o.Addr).ToList();

                    if (fillFF)
                        return FillGapsWithFF(ret).Data;
                    return ret;
                }
                else
                {
                    throw new FirmwareToolsException(481, ERR_481);
                }
            }
            private static BslPasswords GetBslPassword(string FirmwarePath)
            {
                List<byte> ret = new List<byte>();
                uint start_addr = 0xFFE0;

                Firmware fw = ParseAutoDetect(FirmwarePath, true, null);

                if (fw == null || fw.Nodes == null || fw.Nodes.Count < 32)
                    return null;

                foreach (FwNode nod in fw.Nodes)
                {
                    if (nod.Addr == start_addr)
                    {
                        ret.Add(nod.Data);
                        start_addr++;

                        if (ret.Count == 32)
                            break;
                    }
                }
                if (ret.Count != 32)
                    return null;

                return new BslPasswords()
                {
                    Password32Byte = ret.ToArray(),
                    Password20Byte = ret.Take(20).ToArray(),
                    Password16Byte = ret.Skip(16).Take(16).ToArray()
                };
            }
            #endregion
        }
    }
}
