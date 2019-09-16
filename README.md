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
4. [Library](#4-Library)

## 1. Main Features
* replace expensive original MSP FET programmer with cheap FTDI FT232 or Serial COM port
* upload, download or erase MSP430 memory with minimal effort with generic UART convertes
* fexible way how to upgrade device firmware with .NET library that can be integrated to any app
* convert, combine, validate, hex edit TI-TXT, Intel-HEX, SREC or ELF (WPF GUI / Console only)
* original pure C# code implementing TI bootloader protocols 5xx/6xx and 1xx/2xx/4xx [(TI doc)](https://raw.githubusercontent.com/parezj/BSL430.NET/master/docs/slau319z.pdf)
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
  
## 4. Library
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
<br>
  
<div align="center" margin="0" padding="0">
<img src="https://raw.githubusercontent.com/parezj/BSL430.NET/master/img/logo/logo_fw.png" alt="BSL430.NET.FirmwareTools" width="200" height="200">
</div>

<h1 id="Firmware-Tools">
  FirmwareTools - Firmware manipulation made easy
</h1>  
[![Github tag (BSL430.NET)](https://img.shields.io/github/v/release/parezj/BSL430.NET?include_prereleases&color=orange)](https://github.com/parezj/BSL430.NET/releases/latest)
[![NuGet version (BSL430.NET.FirmwareTools)](https://img.shields.io/nuget/v/BSL430.NET.FirmwareTools.svg)](https://www.nuget.org/packages/BSL430.NET.FirmwareTools/)
[![fuget version (BSL430.NET.FirmwareTools)](https://www.fuget.org/packages/BSL430.NET.FirmwareTools/badge.svg)](https://www.fuget.org/packages/BSL430.NET.FirmwareTools)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT)

> **[DOWNLOAD HERE](https://github.com/parezj/BSL430.NET/releases)** - Firmware Tools as BSL430.NET sub package
<br>  
  
5. [Main Features](#5-Main-Features)
6. [GUI  & Console App](#6-GUI---Console-App)
7. [Library](#7-Library)
  
## 5. Main Features
* TI-TXT, Intel-HEX, SREC and ELF format support
* parse (from file) and create (write to file) most common firwmare formats
* convert, combine and compare between any of these formats
* validate firmware file and get information like addresses, crc, sizes..
* [MSP430 specific] get BSL password used to correctly download by BSL430.NET

<br>  
  
## 6. GUI  & Console App
> **[Wiki docs](https://github.com/parezj/BSL430.NET/wiki/BSL430.NET.GUI-App)** - BSL430.NET GUI App docs and tutorials
  
```
BSL430.NET and Firmware Tools are integrated into single GUI and Console App!
```
  
- **Firmware Tools (Dark Mode)**:  
![Firmware Tools](https://raw.githubusercontent.com/parezj/BSL430.NET/master/img/screenshots/wpf_gui_fw_tools_dark.png)

- **Firmware Hex Edit**:  
![Firmware Hex Edit](https://raw.githubusercontent.com/parezj/BSL430.NET/master/img/screenshots/wpf_gui_fw_tools_hex_edit.png)
<br>  
  
## 7. Library
> **[Wiki docs](https://github.com/parezj/BSL430.NET/wiki/BSL430.NET.FirmwareTools.Library)** - Ready to use code samples and tutorials 
  
```csharp
public static Firmware Parse(string FirmwarePath, FwFormat Format, bool FillFF,StringWriter Log)

public static string Create(Firmware Firmware, FwFormat Format, int LineLength)
public static string Create(IEnumerable<byte> Data, int AddrStart, FwFormat Format, int LineLength)
public static string Create(ICollection<FwNode> Data, FwFormat Format, int LineLength)

public static (string Fw, FwFormat Format) Convert(string FirmwarePath, FwFormat Format, bool FillFF, int LineLength)
public static (string Fw, FwFormat Format1, FwFormat Format2) Combine(string FirmwarePath1, string FirmwarePath2, FwFormat Format, bool FillFF, int LineLength)

public static BslPasswords GetPassword(string FirmwarePath)
public static FwInfo Validate(string FirmwarePath, StringWriter Log)

public static (bool Equal, double Match, int BytesDiff) Compare(string FirmwarePath1, string FirmwarePath2)
public static (bool Equal, double Match, int BytesDiff) Compare(Firmware Firmware1, Firmware Firmware2)
```
