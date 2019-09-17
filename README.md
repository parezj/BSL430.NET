<div align="center" margin="0" padding="0">
<img src="https://raw.githubusercontent.com/parezj/BSL430.NET/master/img/logo/logo2.png" alt="BSL430.NET" width="200" height="200">
</div>

# BSL430.NET - The cheapest way to flash MSP430
[![Github tag (BSL430.NET)](https://img.shields.io/github/v/release/parezj/BSL430.NET?include_prereleases&color=orange)](https://github.com/parezj/BSL430.NET/releases/latest)
[![NuGet version (BSL430.NET)](https://img.shields.io/nuget/v/BSL430.NET.svg)](https://www.nuget.org/packages/BSL430.NET/)
[![fuget version (BSL430.NET)](https://www.fuget.org/packages/BSL430.NET/badge.svg)](https://www.fuget.org/packages/BSL430.NET)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT)

> **[DOWNLOAD HERE](https://github.com/parezj/BSL430.NET/releases)** - TI MSP430 Bootloader (BSL) .NET Cross-Platform Toolchain 
<br>

1. [Main Features](#1-Main-Features)
2. [GUI App (Windows)](#2-GUI-App-Windows)
3. [Console App (Windows, Linux)](#3-Console-App-Windows-Linux)
4. [Wiring diagram](#4-Wiring-Diagram)
5. [Library](#5-BSL430NET-Library)
6. [References](#6-References)
7. [Firmware Tools](#Firmware-Tools)

## 1. Main Features
* Replace expensive original *MSP FET* programmer with **cheap** FTDI **FT232** or Serial **COM** port
* **Upload**, **Download** or **Erase** MSP430 MCU memory with minimal effort via generic UART converter
* Fexible way how to upgrade device firmware with .NET library, integrable into any app (also production)
* Most common firmware format support: **TI-TXT**, **Intel-HEX**, **SREC**, **ELF**
* *Original* C# code implementing TI bootloader protocols *5xx/6xx* and *1xx/2xx/4xx* [(TI doc)](https://raw.githubusercontent.com/parezj/BSL430.NET/master/docs/slau319z.pdf)
```
Note: Old 1xx/2xx/4xx bootloader protocol not tested yet!
```
BSL430.NET project started back in 2016, when I worked on my *Wireless Weather Station* project based on **CC430** MCU (F5xxX), 
connected on PCB to FT232. And I wanted to implement automatic firmware upgrade feature, while PC control app was already
written in C# (WPF). So I started to study TI docs and coding, but soon I realized, that there is nothing like this (except
C++ **TI BS430** library or **Python MSP430 Tools**), that is both versatile and multifunctional. So today, after weather station
project already have finished, I changed my direction to **BSL430.NET**, and want to encourage other developers and enthusiats
to use this app/library, which I had completely *open-sourced*, and moreover I made some nice Win GUI App for simple use.

Library can be integrated into any MSP430 based project, even commercial, to enable **automatic firmware upgrades**.
Communication with MCU is handled by 4 different ways. **FTDI** (FT232) is Windows only approach and requires *FT2XX drivers*
installed on target PC. Another approch is called **Libftdi** and this works on Windows and also Linux, because rather on 
FT2XX original FTDI drivers it depends on open-sourced alternative libftdfi. Both ways use unmanaged libraries for low-level
communication, those libraris are provided in folder *lib* or just integrated into Win GUI App. Another simple approch is 
to use standard **Serial** port (COM) with RS232/UART converter, or the last one, **USB** with F5xx/F6xx USB enabled MCUs.
These 2 ways use managed libraries so you dont need to worry about anything, plus they are also multiplatform. Also *dont forget* 
to connect RST/TEST pins to DTR/RTS according to **Wiring Diagram** and specific MCU family.
<br>
  
## 2. GUI App (Windows)
> **[Wiki docs](https://github.com/parezj/BSL430.NET/wiki/BSL430.NET.GUI-App)** - First start quick go-through for ease of use

- **Download code from MCU**:  
![Download](https://raw.githubusercontent.com/parezj/BSL430.NET/master/img/screenshots/wpf_gui_download2.png)

- **Upload firmware to MCU (Dark Mode)**:  
![Upload](https://raw.githubusercontent.com/parezj/BSL430.NET/master/img/screenshots/wpf_gui_upload_dark.png)

- **Erase whole MCU**:  
![Erase](https://raw.githubusercontent.com/parezj/BSL430.NET/master/img/screenshots/wpf_gui_erase.png)

- **Shell Extension & Association**:  
![Shell](https://raw.githubusercontent.com/parezj/BSL430.NET/master/img/screenshots/wpf_gui_shell_extensions.png)
<br>  
  
## 3. Console App (Windows, Linux)

> **[Wiki docs](https://github.com/parezj/BSL430.NET/wiki/BSL430.NET.Console-App)** - Tutorials how to use console app
  
- **Upload firmware to MCU**:  
![Upload](https://raw.githubusercontent.com/parezj/BSL430.NET/master/img/screenshots/console_upload.png)

- **Scan for available devices**:  
![Scan](https://raw.githubusercontent.com/parezj/BSL430.NET/master/img/screenshots/console_scan.png)
<br>  

## 4. Wiring Diagram
![Wiring](https://raw.githubusercontent.com/parezj/BSL430.NET/master/img/wiring_diagram.png)
<br>

## 5. BSL430.NET Library
> **[Wiki docs](https://github.com/parezj/BSL430.NET/wiki/BSL430.NET.Library)** - Documentation and code samples for easy start and copy & paste
  
```csharp
public interface IBsl430Net
{
    ScanAllResult ScanAllEx(ScanOptions ScanOpt);
    ScanResult<Bsl430NetDevice> ScanAll(ScanOptions ScanOpt);
    ScanResult<Tdev> Scan<Tdev>(ScanOptions ScanOpt) where Tdev : Bsl430NetDevice;
    
    StatusEx Erase(Bsl430NetDevice Device);
    StatusEx Erase(string DeviceName);

    StatusEx Upload(string FirmwarePath, Bsl430NetDevice Device, byte[] Password);
    StatusEx Upload(string FirmwarePath, string DeviceName, byte[] Password);

    StatusEx Download(byte[] Password, int AddrStart, int DataSize, out List<byte> Data, Bsl430NetDevice Device);
    StatusEx Download(byte[] Password, int AddrStart, int DataSize, out List<byte> Data, string DeviceName);
    
    Status SetBaudRate(BaudRate BaudRate);
    Status SetInvokeMechanism(InvokeMechanism InvokeMechanism);
    Status SetMCU(MCU Mcu);
    
    BaudRate GetBaudRate();
    InvokeMechanism GetInvokeMechanism();
    MCU GetMCU();
    Mode GetMode();
}
```
<br>

## 6. References
I would like to say *thank you* to each of these developers (or companies), because this project use all of them in some kind of way and
without them, there will be no BSL430.NET:

- [Texas Instruments](http://www.ti.com/)
- [MahApps.Metro](https://mahapps.com)
- [MahApps.IconPack](https://mahapps.com)
- [Caliburn.Micro](https://caliburnmicro.com)
- [Config.Net](https://github.com/aloneguid/config)
- [Newtonsoft.Json](https://www.newtonsoft.com/json)
- [Fody.Costura](https://www.github.com/Fody/Costura)
- [Hardcodet.NotifyIcon](https://hardcodet.net/wpf-notifyicon)
- [AvalonEdit](http://avalonedit.net/)
- [WpfHexEditor](https://github.com/abbaye/WpfHexEditorControl)
- [HidSharp](https://www.zer7.com/software/hidsharp)
- [LibUsbDotNet](https://www.github.com/LibUsbDotNet)
- [SerialPortStream](www.github.com/jcurl/SerialPortStream/SerialPortStream)
- [FTD2XX](https://www.ftdichip.com/Drivers/D2XX.htm)
- [libftdi](https://www.intra2net.com/en/developer/libftdi/)
- [libusb](https://libusb.info/)
<br>
<br>
  
<h1 id="Firmware-Tools">
    <div align="center" margin="0" padding="0">
        <img src="https://raw.githubusercontent.com/parezj/BSL430.NET/master/img/logo/logo_fw.png" alt="BSL430.NET.FirmwareTools" width="200" height="200">
    </div>
  BSL430.NET.FirmwareTools - FW manipulation made easy
</h1>

[![Github tag (BSL430.NET)](https://img.shields.io/github/v/release/parezj/BSL430.NET?include_prereleases&color=orange)](https://github.com/parezj/BSL430.NET/releases/latest)
[![NuGet version (BSL430.NET.FirmwareTools)](https://img.shields.io/nuget/v/BSL430.NET.FirmwareTools.svg)](https://www.nuget.org/packages/BSL430.NET.FirmwareTools/)
[![fuget version (BSL430.NET.FirmwareTools)](https://www.fuget.org/packages/BSL430.NET.FirmwareTools/badge.svg)](https://www.fuget.org/packages/BSL430.NET.FirmwareTools)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT)

> **[DOWNLOAD HERE](https://github.com/parezj/BSL430.NET/releases)** - Firmware Tools as BSL430.NET sub package
<br>  
  
7. [Main Features](#7-Main-Features)
8. [GUI  & Console App](#8-GUI---Console-App)
9. [Library](#9-FirmwareTools-Library)
  
## 7. Main Features
* Multiple firmware format support: **TI-TXT**, **Intel-HEX**, **SREC**, **ELF** 
* **Parse** (read from file) and **Create** (write to file or string) 
* **Convert**, **Combine** and **Compare** between any of these formats
* **Validate** firmware file and get information like addresses, CRC, sizes..
* *[MSP430 only]* **Get BSL password** used to correctly download fw by BSL430.NET

<br>  
  
## 8. GUI  & Console App
> **[Wiki docs](https://github.com/parezj/BSL430.NET/wiki/BSL430.NET.GUI-App)** - BSL430.NET GUI App docs and tutorials
  
```
Note: BSL430.NET and Firmware Tools are integrated into single GUI and Console Apps!
```
  
- **Firmware Tools (Dark Mode)**:  
![Firmware Tools](https://raw.githubusercontent.com/parezj/BSL430.NET/master/img/screenshots/wpf_gui_fw_tools_dark.png)

- **Firmware Hex-Edit**:  
![Firmware Hex Edit](https://raw.githubusercontent.com/parezj/BSL430.NET/master/img/screenshots/wpf_gui_fw_tools_hex_edit.png)
<br>  
  
## 9. FirmwareTools Library
> **[Wiki docs](https://github.com/parezj/BSL430.NET/wiki/BSL430.NET.FirmwareTools.Library)** - Ready to use code samples and tutorials 
  
```csharp
public static Firmware Parse(string FirmwarePath, FwFormat Format, bool FillFF,StringWriter Log);

public static string Create(Firmware Firmware, FwFormat Format, int LineLength);
public static string Create(IEnumerable<byte> Data, int AddrStart, FwFormat Format, int LineLength);
public static string Create(ICollection<FwNode> Data, FwFormat Format, int LineLength);

public static (string Fw, FwFormat Format) Convert(string FirmwarePath, FwFormat Format, bool FillFF, int LineLength);
public static (string Fw, FwFormat Format1, FwFormat Format2) Combine(string FirmwarePath1, string FirmwarePath2, FwFormat Format, bool FillFF, int LineLength);

public static BslPasswords GetPassword(string FirmwarePath);
public static FwInfo Validate(string FirmwarePath, StringWriter Log);

public static (bool Equal, double Match, int BytesDiff) Compare(string FirmwarePath1, string FirmwarePath2);
public static (bool Equal, double Match, int BytesDiff) Compare(Firmware Firmware1, Firmware Firmware2);
```
