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
        /// Scan for all available devices in multimode (FTDI, libftdi, USB and Serial).
        /// </summary>
        /// <exception cref="Bsl430NetException"></exception>
        (Status ftdi, Status libftdi, Status usb, Status serial) ScanAll(out List<FTDI_Device> ftdi_devices,
                                                                         out List<Libftdi_Device> libftdi_devices,
                                                                         out List<USB_HID_Device> usb_devices,
                                                                         out List<Serial_Device> serial_devices,
                                                                         ScanOptions scan_opt = ScanOptions.None);

        /// <summary>
        /// Scan for devices in single mode (FTDI, libftdi, USB or Serial).
        /// </summary>
        /// <exception cref="Bsl430NetException"></exception>
        Status Scan<Tdev>(out List<Tdev> device_list, ScanOptions scan_opt = ScanOptions.None) where Tdev : Bsl430NetDevice;

        /// <summary>
        /// Mass erase deletes the entire flash memory area except Information Memory if protocol 5xx6xx is used. 
        /// Please see MCU datasheet for detailed information, or TI BSL doc (slau319t.pdf).
        /// </summary>
        /// <exception cref="Bsl430NetException"></exception>
        StatusEx Erase(Bsl430NetDevice device = null);

        /// <summary>
        /// Mass erase deletes the entire flash memory area except Information Memory if protocol 5xx6xx is used. 
        /// Please see MCU datasheet for detailed information, or TI BSL doc (slau319t.pdf). device_name case dont matter.
        /// </summary>
        /// <exception cref="Bsl430NetException"></exception>
        StatusEx Erase(string device_name);

        /// <summary>
        /// Uploads data from firmware_path to target MCU. Supported file formats are TI-TXT, Intel-HEX and ELF.
        /// If none, null or invalid password is entered, mass erase is executed first.
        /// </summary>
        /// <exception cref="Bsl430NetException"></exception>
        StatusEx Upload(string firmware_path, Bsl430NetDevice device = null, byte[] password = null);

        /// <summary>
        /// Uploads data from firmware_path to target MCU. Supported file formats are TI-TXT, Intel-HEX and ELF.
        /// If none, null or invalid password is entered, mass erase is executed first. device_name case dont matter.
        /// </summary>
        /// <exception cref="Bsl430NetException"></exception>
        StatusEx Upload(string firmware, string device_name, byte[] password = null);

        /// <summary>
        /// Downloads bytes from target MCU starting from address 'addr_start' to 'addr_start' + 'data_size'.
        /// If wrong password is entered, mass erase is auto executed as a safety measure, erasing entire flash.
        /// </summary>
        /// <exception cref="Bsl430NetException"></exception>
        StatusEx Download(byte[] password, int addr_start, int data_size, out List<byte> data, Bsl430NetDevice device = null);

        /// <summary>
        /// Downloads bytes from target MCU starting from address 'addr_start' to 'addr_start' + 'data_size'.
        /// If wrong password is entered, mass erase is auto executed as a safety measure, erasing entire flash.
        /// device_name case dont matter.
        /// </summary>
        /// <exception cref="Bsl430NetException"></exception>
        StatusEx Download(byte[] password, int addr_start, int data_size, out List<byte> data, string device_name);

        /// <summary>
        /// Sets baud rate. Default is 9600 bps (9600).
        /// Baud rate is applicable only in UART mode and only in some BSL revs.
        /// </summary>
        Status SetBaudRate(BaudRate BaudRate);

        /// <summary>
        /// Sets invoke mechanism mode. Default is DTR_RST__RTS_TEST (0).
        /// Note that USB HID mode requires manual BSL invocation.
        /// </summary>
        Status SetInvokeMechanism(InvokeMechanism invoke_mechanism);

        /// <summary>
        /// Sets MCU family. Default is MSP430_F5xx (4).
        /// Please see TI BSL doc (slau319t.pdf) for supported MCUs and their modes.
        /// </summary>
        Status SetMCU(MCU mcu);

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
