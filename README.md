<div align="center" margin="0" padding="0">
<img src="https://raw.githubusercontent.com/parezj/BSL430.NET/master/img/logo/logo2.png" alt="BSL430.NET" width="200" height="200">
</div>

# BSL430.NET - The cheapest way to flash MSP430
[![Github tag (BSL430.NET)](https://img.shields.io/github/v/release/parezj/BSL430.NET?include_prereleases&color=orange)](https://github.com/parezj/BSL430.NET/releases/latest)
[![NuGet version (BSL430.NET)](https://img.shields.io/nuget/v/BSL430.NET.svg)](https://www.nuget.org/packages/BSL430.NET/)
[![fuget version (BSL430.NET)](https://www.fuget.org/packages/BSL430.NET/badge.svg)](https://www.fuget.org/packages/BSL430.NET)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT)

> **[DOWNLOAD HERE](https://github.com/parezj/BSL430.NET/releases)** - TI MSP430 Bootloader (BSL) .NET Cross-Platform Toolchain 

| Package                  | OS             | Architecture  | Framework                            | Last Stable |
|--------------------------|:--------------:|:-------------:|--------------------------------------|-------------|
| BSL430.NET               | WinNT, Linux   | x86, amd64    | net461, netstandard2.0               | [v1.3.4](https://github.com/parezj/BSL430.NET/releases) |
| BSL430.NET.FirmwareTools | any            | any           | net40, net45, net461, netstandard2.0 | [v1.3.4](https://github.com/parezj/BSL430.NET/releases) |
| BSL430.NET.Console       | WinNT, Linux   | x86, amd64    | netcoreapp3.0                        | [v1.3.2](https://github.com/parezj/BSL430.NET/releases) |
| BSL430.NET.WPF (GUI)     | WinNT          | x86, amd64    | net461, (netcoreapp3.0)              | [v1.3.2](https://github.com/parezj/BSL430.NET/releases),  [Store](https://www.microsoft.com/en-us/p/bsl430net/9n0sgvj0mbmn) |
<br>

0. [Wiki](https://github.com/parezj/BSL430.NET/wiki/)
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
Note:    Old *1xx/2xx/4xx* bootloader protocol not tested yet!
Warning: Old *1xx/2xx/4xx* bootloader protocol handle **Erase** or incorrectly entered password 
         as complete memory wipe including Info A (with **calibration** data), if *LOCK A* bit is not set!
```
BSL430.NET project started back in 2016, when I worked on my *Wireless Weather Station* project based on **CC430** MCU (F5xxX), 
connected on PCB to FT232. And I wanted to implement automatic firmware upgrade feature, while PC control app was already
written in C# (WPF). So I started to study TI docs and coding, but soon I realized, that there is nothing like this (except
C++ **TI BS430** library or **Python MSP430 Tools**), that is both versatile and multifunctional. So today, after weather station
project already have finished, I changed my direction to **BSL430.NET**, and want to encourage other developers and enthusiats
to use this app/library, which I had completely *open-sourced*, and moreover I made some nice Win GUI App for simple use.
Library can now be integrated into any MSP430 based project, even commercial, to enable **automatic firmware upgrades**.
More at [Wiki Homepage](https://github.com/parezj/BSL430.NET/wiki/), [Wiki Library](https://github.com/parezj/BSL430.NET/wiki/BSL430.NET.Library),
[Wiki GUI App](https://github.com/parezj/BSL430.NET/wiki/BSL430.NET.GUI-App), [Wiki Console App](https://github.com/parezj/BSL430.NET/wiki/BSL430.NET.Console-App) or 
[Wiki Firmware Tools](https://github.com/parezj/BSL430.NET/wiki/BSL430.NET.FirmwareTools.Library)
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

- **Tray Icon Settings (Dark Mode)**:  
![Tray](https://raw.githubusercontent.com/parezj/BSL430.NET/master/img/screenshots/wpf_gui_tray_dark.png)
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

BSL430.NET.FirmwareTools is *Cross-Platform* library created as a part of BSL430.NET and then, after growing a little bit, 
made standalone sub package. First motivation to create Firmware Tools was when I needed to unify parsing methods which 
precede Erase, Download and Upload blocks in BSL430.NET. Intel-HEX and TI-TXT was supported from the begining, SREC and 
ELF was added a while after. Today this library offers **basic firmare manipulation** with few, but powerful, static methods.

<br>  
  
## 8. GUI  & Console App
> **[Wiki docs](https://github.com/parezj/BSL430.NET/wiki/BSL430.NET.GUI-App)** - BSL430.NET GUI App docs and tutorials
  
```
Note: BSL430.NET and Firmware Tools are integrated into single GUI and Console Apps!
```
  
- **Firmware Tools (Dark Mode)**:  
![Firmware Tools](https://raw.githubusercontent.com/parezj/BSL430.NET/master/img/screenshots/wpf_gui_fw_tools_dark.png)

- **Validate**:  
![Validate](https://raw.githubusercontent.com/parezj/BSL430.NET/master/img/screenshots/wpf_gui_fw_tools_validate.png)

- **Hex-Edit**:  
![Hex-Edit](https://raw.githubusercontent.com/parezj/BSL430.NET/master/img/screenshots/wpf_gui_fw_tools_hex_edit.png)
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

```
Note: Author is not responsible for any kind of damage, that could arise 
      from wrong use or misuse of this library or apps!
```