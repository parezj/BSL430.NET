using System;
using System.Collections.Generic;
using System.IO;
using static BSL430_NET.FirmwareTools.FwTools;

namespace BSL430_NETSamples
{
    class SampleFirmwareTools
    {
        public void ParseFirmware(string FwPath)
        {
            Firmware fw1 = Parse(FwPath);                // Fw.Format = AUTO
            Firmware fw2 = Parse(FwPath, FillFF: true);  // gaps are filled with 0xFF
            Firmware fw3 = Parse(FwPath, FwFormat.SREC); // Fw.Format = SREC
            Firmware fw4 = Parse(FwPath, Log: new StringWriter()); // save parse log
        }
        
        public void CreateFromFirmware(Firmware Fw)
        {
            string fw1 = Create(Fw);                     // Fw.Format = AUTO, Len = default
            string fw2 = Create(Fw, FwFormat.INTEL_HEX); // Fw.Format = Intel-HEX
            string fw3 = Create(Fw, LineLength: 64);     // custom line length, compatibility
        }

        public void CreateFromRawData(IEnumerable<byte> RawData, int AddrStart)
        {
            string fw1 = Create(RawData, AddrStart);   // Fw.Format = AUTO, Line Len = default
            string fw2 = Create(RawData, AddrStart, FwFormat.TI_TXT); // Fw.Format = TI-TXT
            string fw3 = Create(RawData, AddrStart, LineLength: 128); // custom fw line length
        }

        public void CreateFromFwNodes(ICollection<FwNode> FwNodes)
        {
            string fw1 = Create(FwNodes);              // Fw.Format = AUTO, Line Len = default
            string fw2 = Create(FwNodes, FwFormat.SREC);              // Fw.Format = SREC
            string fw3 = Create(FwNodes, LineLength: 24);             // custom fw line length
        }
        
        public void ConvertFirmware(string FwPath)
        {
            // simple convert of firmware, format auto-detected, to TI-TXT
            var (Fw1, Format1) = Convert(FwPath, FwFormat.TI_TXT);
            // convert when gaps in firmware are filled with 0xFF
            var ret2 = Convert(FwPath, FwFormat.INTEL_HEX, true);    
            // custom output line length: 16
            var ret3 = Convert(FwPath, FwFormat.SREC, LineLength: 16);
        }
        
        public void CombineFirmwares(string FwPath1, string FwPath2)
        {
            // simple combine of two firmware files
            var (Fw1, Format1, Format2) = Combine(FwPath1, FwPath2, FwFormat.TI_TXT);
            // combine and fill gaps with 0xFF
            var ret2 = Combine(FwPath1, FwPath2, FwFormat.TI_TXT, true);
            // combine and set custom output line length
            var ret3 = Combine(FwPath1, FwPath2, FwFormat.TI_TXT, LineLength: 64);
        }
        
        public void CompareFirmwareFiles(string FwPath1, string FwPath2)
        {
            var (Equal, Match, BytesDiff) = Compare(FwPath1, FwPath2); // simple fw compare
        }

        public void CompareFirmwares(Firmware Firmware1, Firmware Firmware2)
        {
            var (Equal, Match, BytesDiff) = Compare(Firmware1, Firmware2); // fw class cmp
        }
        
        public void ValidateFirmware(string FwPath)
        {
            FwInfo fwInfo1 = Validate(FwPath);         // simple firmware validation
            FwInfo fwInfo2 = Validate(FwPath, new StringWriter()); // save parse log

            // If firmware is invalid, Valid = false. Otherwise Valid = True
            Console.WriteLine(fwInfo1.Valid);
            // Firmware format. TI-TXT, Intel-HEX, ELF or SREC
            Console.WriteLine(fwInfo1.Format);
            // First address in firmware, max 32-bit, usually 16-bit
            Console.WriteLine(fwInfo1.AddrFirst);
            // Last address in firmware, max 32-bit, usually 16-bit
            Console.WriteLine(fwInfo1.AddrLast);
            // Total length of firmware, count of all bytes from first address to last
            Console.WriteLine(fwInfo1.SizeFull);
            // Real count of all bytes in firmware parsed from file
            Console.WriteLine(fwInfo1.SizeCode);
            // CRC-16-CCITT is 16-bit crc value of all data bytes in firmware
            Console.WriteLine(fwInfo1.Crc16);
            // [MSP430 specific] Reset vector is address (value) located usually at 
            // 16-bit address 0xFFFE.
            Console.WriteLine(fwInfo1.ResetVector);
            // List<long>. When parsing FW, FillFF can be set, to output code in single 
            // piece. Addresses, that dont belong to original FW, are in this list.
            Console.WriteLine(fwInfo1.FilledFFAddr);
        }
        
        public void GetBSLPassword(string FwPath)
        {
            BslPasswords pw = GetPassword(FwPath); // read/parse fw file and get pw

            // 32-Byte mostly used in todays MSP430, 5xx/6xx series except F543x (non A).
            Console.WriteLine(BitConverter.ToString(pw.Password32Byte));
            // 16-Byte used only in the very first series of 5xx, the F543x (non A)
            Console.WriteLine(BitConverter.ToString(pw.Password16Byte));
            // 20-byte long Password used in old 1xx/2xx/4xx series
            Console.WriteLine(BitConverter.ToString(pw.Password20Byte));
        }
    }
}