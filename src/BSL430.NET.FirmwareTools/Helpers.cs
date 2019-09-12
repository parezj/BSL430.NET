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
using System.Text;

namespace BSL430_NET
{
    namespace FirmwareTools
    {
        namespace Helpers
        {
            #region Equality Comparers
            /// <summary>
            /// Compares whole Firmware Node, address and data. Useful when comparing two firmware files.
            /// </summary>
            public class FwNodeComparer : IEqualityComparer<FwTools.FwNode>
            {
                /// <summary>
                /// True if addresses and data match.
                /// </summary>
                public bool Equals(FwTools.FwNode fw1, FwTools.FwNode fw2)
                {
                    if (fw1.Addr == fw2.Addr && fw1.Data == fw2.Data)
                    {
                        return true;
                    }
                    return false;
                }
                /// <summary>
                /// Hash Code created by XORing Address and Data.
                /// </summary>
                public int GetHashCode(FwTools.FwNode obj)
                {
                    return obj.Addr.GetHashCode() ^ obj.Data.GetHashCode();
                }
            }

            /// <summary>
            /// Compares Firmware Node addresses only. Useful when searching for fw addr overlap between two files.
            /// </summary>
            public class FwNodeAddrComparer : IEqualityComparer<FwTools.FwNode>
            {
                /// <summary>
                /// True if addresses match.
                /// </summary>
                public bool Equals(FwTools.FwNode fw1, FwTools.FwNode fw2)
                {
                    if (fw1.Addr == fw2.Addr)
                    {
                        return true;
                    }
                    return false;
                }
                /// <summary>
                /// Hash Code created from Address.
                /// </summary>
                public int GetHashCode(FwTools.FwNode obj)
                {
                    return obj.Addr.GetHashCode();
                }
            }
            #endregion

            #region Firmware Tools Exception
            /// <summary>
            /// Firmware Tools Exception is generic Exception extended with Error and Msg (status) objects.
            /// </summary>
            [Serializable]
            public class FirmwareToolsException : Exception
            {
                /// <summary>Numeric representation of exception.</summary>
                public int Error { get; set; } = 0;
                /// <summary>String representation of status.</summary>
                public string Msg { get; set; } = "unknown";

                /// <summary>
                /// FirmwareToolsException constructor
                /// </summary>
                public FirmwareToolsException(int Error, string Msg, Exception innerEx = null) : base(Msg, innerEx)
                {
                    this.Msg = Msg;
                    this.Error = Error;
                }
            }
            #endregion
            internal class Helpers { }
        }
    }
}
