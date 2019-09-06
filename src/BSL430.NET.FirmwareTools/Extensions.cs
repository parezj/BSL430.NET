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
using System.Linq;
using System.Text;

namespace BSL430_NET
{
    namespace FirmwareTools
    {
        #region Extensions
        /// <summary>
        /// Extensions class wrapping some extensions methods like CRC calc, ToByteArray or ToExt.
        /// </summary>
        public static class Extensions
        {
            /// <summary>
            /// Covnerts hex string to byte array.
            /// </summary>
            public static byte[] ToByteArray(this string HexString)
            {
                int NumberChars = HexString.Length;
                byte[] bytes = new byte[NumberChars / 2];
                for (int i = 0; i < NumberChars; i += 2)
                {
                    bytes[i / 2] = Convert.ToByte(HexString.Substring(i, 2), 16);
                }
                return bytes;
            }

            /// <summary>
            /// Extension method to provide firmware format ext string with dot.
            /// </summary>
            public static string ToExt(this FwTools.FwFormat format)
            {
                switch (format)
                {
                    case FwTools.FwFormat.AUTO: return "";
                    case FwTools.FwFormat.ELF: return ".out";
                    case FwTools.FwFormat.INTEL_HEX: return ".hex";
                    case FwTools.FwFormat.SREC: return ".srec";
                    default:
                    case FwTools.FwFormat.TI_TXT: return ".txt";
                }
            }

            /// <summary>
            /// CRC-16-CCITT: Polynom = 0x1021, Initial = 0xFFFF
            /// </summary>
            public static int Crc16Ccitt(this IEnumerable<byte> bytes)
            {
                return Crc16(bytes, 0x1021, 0xffff);
            }

            /// <summary>
            /// CRC-16 generic implementation that needs a specific polynom and initial values.
            /// </summary>
            public static int Crc16(this IEnumerable<byte> bytes, int _polynom, int _init)
            {
                ushort polynom = (ushort)_polynom;
                ushort init = (ushort)_init;
                ushort[] table = new ushort[256];
                ushort temp, a;
                ushort crc = init;
                for (int i = 0; i < table.Length; ++i)
                {
                    temp = 0;
                    a = (ushort)(i << 8);
                    for (int j = 0; j < 8; ++j)
                    {
                        if (((temp ^ a) & 0x8000) != 0)
                            temp = (ushort)((temp << 1) ^ polynom);
                        else
                            temp <<= 1;
                        a <<= 1;
                    }
                    table[i] = temp;
                }
                for (int i = 0; i < bytes.Count(); ++i)
                {
                    crc = (ushort)((crc << 8) ^ table[((crc >> 8) ^ (0xff & bytes.ElementAt(i)))]);
                }
                return crc;
            }
        }
        #endregion
    }
}
