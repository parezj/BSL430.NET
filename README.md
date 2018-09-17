# BSL430.NET
Texas Instrument MSP430 USB/UART multiplatform tools (library, console, WPF) to download/upload memory<br>

<p align="center">[![BSL430.NET](https://1iq.cz/img/C9a2k/j9vCu.png)](https://1iq.cz/img/C9a2k/j9vCu.png)</p>

## Projects
BSL430.NET lib     - .NET Standard 2.0      - Windows, Linux, macOS
BSL430.NET Console - .NET Core 2.0          - Windows, Linux, macOS
BSL430.NET WPF     - .NET Framework 4.6.1   - Windows

## Modes:
UART FT2XX
UART libftdi
UART Serial
USB  HID

## Supported MCU series:
MSP430 F1xx
MSP430 F2xx
MSP430 F4xx
MSP430 G2xx3
MSP430 F5xx
MSP430 F543x
MSP430 F6xx
MSP430 FR5xx
MSP430 FR6xx
MSP430 FR2x33
MSP430 FR231x
MSP430 FR235x
MSP430 FR215x
MSP430 FR413x
MSP430 FR211x
MSP432 P401R

## ILibrary
Status Scan<Tdev>(out List<Tdev> device_list) where Tdev : Bsl430NetDevice;
StatusEx Upload(Bsl430NetDevice device, string firmware_path, out byte[] version);  
StatusEx Upload(Bsl430NetDevice device, byte[] password, string firmware_path, out byte[] version);
StatusEx Download(Bsl430NetDevice device, byte[] password, uint addr_start, int data_size, out List<byte> data, out byte[] version);
  
Status SetBaudRate(BaudRate baud_rate);
Status SetInvokeMechanism(InvokeMechanism invoke_mechanism);
Status SetMCU(MCU mcu);
