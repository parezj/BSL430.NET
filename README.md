# BSL430.NET
Texas Instrument MSP430 USB/UART multiplatform tools (library, console, WPF) to download/upload memory<br>

<p align="center"><a href="https://1iq.cz/img/C9a2k/j9vCu.png"><img src="https://1iq.cz/img/C9a2k/j9vCu.png"></img></a></p>

## Projects
<pre>
<span style="font-size:large">
BSL430.NET lib     - .NET Standard 2.0      - Windows, Linux, macOS
BSL430.NET Console - .NET Core 2.0          - Windows, Linux, macOS
BSL430.NET WPF     - .NET Framework 4.6.1   - Windows
</span>
</pre>

## Modes:
UART FT2XX<br>
UART libftdi<br>
UART Serial<br>
USB  HID<br>

## Supported MCU series:
MSP430 F1xx<br>
MSP430 F2xx<br>
MSP430 F4xx<br>
MSP430 G2xx3<br>
MSP430 F5xx<br>
MSP430 F543x<br>
MSP430 F6xx<br>
MSP430 FR5xx<br>
MSP430 FR6xx<br>
MSP430 FR2x33<br>
MSP430 FR231x<br>
MSP430 FR235x<br>
MSP430 FR215x<br>
MSP430 FR413x<br>
MSP430 FR211x<br>
MSP432 P401R<br>

## ILibrary
```C#
Status Scan<Tdev>(out List<Tdev> device_list) where Tdev : Bsl430NetDevice;
StatusEx Upload(Bsl430NetDevice device, string firmware_path, out byte[] version);
StatusEx Upload(Bsl430NetDevice device, byte[] password, string firmware_path, out byte[] version);
StatusEx Download(Bsl430NetDevice device, byte[] password, uint addr_start, int data_size, out List<byte> data, out byte[] version);

Status SetBaudRate(BaudRate baud_rate);
Status SetInvokeMechanism(InvokeMechanism invoke_mechanism);
Status SetMCU(MCU mcu);
```
