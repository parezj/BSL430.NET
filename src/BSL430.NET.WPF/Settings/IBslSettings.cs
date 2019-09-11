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
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Config.Net;
using BSL430_NET;
using BSL430_NET_WPF.Models;
using static BSL430_NET.FirmwareTools.FwTools;


namespace BSL430_NET_WPF.Settings
{
    public interface IBslSettings
    {
        // ===================================== ShellView

        [Option(DefaultValue = "true")]
        bool GeneralFirstRun { get; set; }

        [Option(DefaultValue = "false")]
        bool GeneralShellAssocInstallForce { get; set; }

        [Option(DefaultValue = "false")]
        bool GeneralShellExtInstallForce { get; set; }

        // ===================================== MainView

        [Option(DefaultValue = "0")]
        int GeneralTabSelectedIndex { get; set; }

        // ===================================== ControlProcessView

        [Option(DefaultValue = "")]
        string MainLastDevice { get; set; }

        [Option(DefaultValue = "MSP430_F5xx")]
        MCU MainMCU { get; set; }

        [Option(DefaultValue = "BAUD_9600")]
        BaudRate MainBaudRate { get; set; }

        [Option(DefaultValue = "SHARED_JTAG")]
        InvokeMechanism MainInvokeMechanism { get; set; }

        [Option(DefaultValue = "")]
        string MainPassword { get; set; }

        // ===================================== UploadView

        [Option(DefaultValue = "")]
        string UploadFwPath { get; set; }

        // ===================================== DownloadView

        [Option(DefaultValue = "")]
        string DownloadFwPath { get; set; }

        [Option(DefaultValue = "32")]
        int DownloadStartAddress { get; set; }

        [Option(DefaultValue = "32")]
        int DownloadByteSize { get; set; }

        [Option(DefaultValue = "false")]
        bool DownloadSizeInDecimal { get; set; }

        [Option(DefaultValue = "TI_TXT")]
        FwFormat DownloadOutputFormat { get; set; }

        [Option(DefaultValue = "true")]
        bool DownloadFirst { get; set; }

        // ===================================== FwToolsView

        [Option(DefaultValue = "")]
        string FwToolsFwPath { get; set; }

        [Option(DefaultValue = "INTEL_HEX")]
        FwFormat FwToolsConvertFormat { get; set; }

        [Option(DefaultValue = "TI_TXT")]
        FwFormat FwToolsCombineFormat { get; set; }

        [Option(DefaultValue = "")]
        string FwToolsCombineWith { get; set; }

        [Option(DefaultValue = "false")]
        bool FwToolsConvertFillFF { get; set; }

        [Option(DefaultValue = "false")]
        bool FwToolsCombineFillFF { get; set; }

        [Option(DefaultValue = "MSP430_F5xx")]
        MCU FwToolsPasswordMCU { get; set; }

        // ===================================== Tray Icon Settings

        [Option(DefaultValue = "0")]
        int FwWriteLineLength { get; set; }

        [Option(DefaultValue = "128")]
        int DownloadSizeRange { get; set; }

        [Option(DefaultValue = "false")]
        bool GeneralTrayBar { get; set; }

        [Option(DefaultValue = "false")]
        bool GeneralDarkMode { get; set; }

        [Option(DefaultValue = "false")]
        bool GeneralShellExtension { get; set; }

        [Option(DefaultValue = "false")]
        bool GeneralShellAssociation { get; set; }

        [Option(DefaultValue = "VALIDATE")]
        ShellModel.ShellDefaultAct GeneralShellDefaultAct { get; set; }       
    }
}
