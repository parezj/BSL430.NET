﻿/*
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
using System.Text;

using BSL430_NET.Comm;


namespace BSL430_NET
{
    /// <summary>
    /// Public interface of BSL430.NET library.
    /// </summary>
    public interface IBsl430Net
    {
        /// <summary>
        /// Detailed Scan for all available devices in multimode (FTDI, libftdi, USB and Serial). Returns ScanAllResult with 4
        /// specific ScanResult classes, each with Status and List of Devices - Bsl430NetDevice childs.
        /// </summary>
        /// <exception cref="Bsl430NetException"></exception>
        ScanAllResult ScanAllEx(ScanOptions ScanOpt = ScanOptions.None);

        /// <summary>
        /// Scan for all available devices in multimode (FTDI, libftdi, USB and Serial). Returns generic ScanResult class
        /// with Status and List of Devices - Bsl430NetDevice.
        /// </summary>
        /// <exception cref="Bsl430NetException"></exception>
        ScanResult<Bsl430NetDevice> ScanAll(ScanOptions ScanOpt = ScanOptions.None);

        /// <summary>
        /// Detailed Scan for available devices in single mode (FTDI / libftdi / USB / Serial). Returns generic ScanResult class
        /// with Status and List of Devices - Bsl430NetDevice.
        /// </summary>
        /// <exception cref="Bsl430NetException"></exception>
        ScanResult<Tdev> Scan<Tdev>(ScanOptions ScanOpt = ScanOptions.None) where Tdev : Bsl430NetDevice;

        /// <summary>
        /// Mass erase deletes the entire flash memory area except Information Memory if protocol 5xx6xx is used. 
        /// Please see MCU datasheet for detailed information, or TI BSL doc (slau319t.pdf).
        /// </summary>
        /// <exception cref="Bsl430NetException"></exception>
        StatusEx Erase(Bsl430NetDevice Device = null);

        /// <summary>
        /// Mass erase deletes the entire flash memory area except Information Memory if protocol 5xx6xx is used. 
        /// Please see MCU datasheet for detailed information, or TI BSL doc (slau319t.pdf). device_name case dont matter.
        /// </summary>
        /// <exception cref="Bsl430NetException"></exception>
        StatusEx Erase(string DeviceName);

        /// <summary>
        /// Uploads data from firmware_path to target MCU. Supported file formats are TI-TXT, Intel-HEX and ELF.
        /// If none, null or invalid password is entered, mass erase is executed first.
        /// Password is last 16-byte (F543x-non-A only) or 32-byte (others) of IVT (FFE0-FFFF), if newer 5xx/6xx MCU is
        /// used. If MCU from older series is used (1xx/2xx/4xx), password is exactly 20-byte long. Mostly it is 32-byte.
        /// </summary>
        /// <exception cref="Bsl430NetException"></exception>
        StatusEx Upload(string FirmwarePath, Bsl430NetDevice Device = null, byte[] Password = null);

        /// <summary>
        /// Uploads data from firmware_path to target MCU. Supported file formats are TI-TXT, Intel-HEX and ELF.
        /// If none, null or invalid password is entered, mass erase is executed first. device_name case dont matter.
        /// Password is last 16-byte (F543x-non-A only) or 32-byte (others) of IVT (FFE0-FFFF), if newer 5xx/6xx MCU is
        /// used. If MCU from older series is used (1xx/2xx/4xx), password is exactly 20-byte long. Mostly it is 32-byte.
        /// </summary>
        /// <exception cref="Bsl430NetException"></exception>
        StatusEx Upload(string FirmwarePath, string DeviceName, byte[] Password = null);

        /// <summary>
        /// Downloads bytes from target MCU starting from address AddrStart to AddrStart + DataSize.
        /// If wrong password is entered, mass erase is auto executed as a safety measure, erasing entire flash.
        /// Password is last 16-byte (F543x-non-A only) or 32-byte (others) of IVT (FFE0-FFFF), if newer 5xx/6xx MCU is
        /// used. If MCU from older series is used (1xx/2xx/4xx), password is exactly 20-byte long. Mostly it is 32-byte.
        /// </summary>
        /// <exception cref="Bsl430NetException"></exception>
        StatusEx Download(byte[] Password, int AddrStart, int DataSize, out List<byte> Data, Bsl430NetDevice Device = null);

        /// <summary>
        /// Downloads bytes from target MCU starting from address AddrStart to AddrStart + DataSize.
        /// If wrong password is entered, mass erase is auto executed as a safety measure, erasing entire flash.
        /// device_name case dont matter.
        /// Password is last 16-byte (F543x-non-A only) or 32-byte (others) of IVT (FFE0-FFFF), if newer 5xx/6xx MCU is
        /// used. If MCU from older series is used (1xx/2xx/4xx), password is exactly 20-byte long. Mostly it is 32-byte.
        /// </summary>
        /// <exception cref="Bsl430NetException"></exception>
        StatusEx Download(byte[] Password, int AddrStart, int DataSize, out List<byte> Data, string DeviceName);
        
        /// <summary>
        /// Sets baud rate. Default is 9600 bps (9600).
        /// Baud rate is applicable only in UART mode and only in some BSL revs.
        /// </summary>
        Status SetBaudRate(BaudRate BaudRate);

        /// <summary>
        /// Sets invoke mechanism mode. Default is DTR_RST__RTS_TEST (0).
        /// Note that USB HID mode requires manual BSL invocation.
        /// </summary>
        Status SetInvokeMechanism(InvokeMechanism InvokeMechanism);

        /// <summary>
        /// Sets MCU family. Default is MSP430_F5xx (4).
        /// Please see TI BSL doc (slau319t.pdf) for supported MCUs and their modes.
        /// </summary>
        Status SetMCU(MCU Mcu);

        /// <summary>
        /// Returns currently set baud rate.
        /// </summary>
        BaudRate GetBaudRate();

        /// <summary>
        /// Returns currently set invoke mechanism.
        /// </summary>
        InvokeMechanism GetInvokeMechanism();

        /// <summary>
        /// Returns currently set MCU family.
        /// </summary>
        MCU GetMCU();

        /// <summary>
        /// Returns current BSL430.NET instance operating mode (UART_FTDXX, UART_libftdi, UART_Serial, USB_HID).
        /// </summary>
        Mode GetMode();
    }
}
