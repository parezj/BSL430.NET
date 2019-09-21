using System;
using BSL430_NET;
using BSL430_NET.Comm;

namespace BSL430_NETSamples
{
    class SampleScan
    {
        public ScanResult<FTDI_Device> ScanFTDI()
        {
            using (var dev = new BSL430NET(Mode.UART_FTD2XX))
            {
                var scan = dev.Scan<FTDI_Device>();

                Console.WriteLine(scan.Status);
                Console.WriteLine(scan.Devices);

                return scan;
            }
        }

        public ScanResult<Libftdi_Device> ScanLibftdi()
        {
            using (var dev = new BSL430NET(Mode.UART_libftdi))
            {
                var scan = dev.Scan<Libftdi_Device>();

                Console.WriteLine(scan.Status);
                Console.WriteLine(scan.Devices);

                return scan;
            }
        }

        public ScanResult<USB_HID_Device> ScanUSB()
        {
            using (var dev = new BSL430NET(Mode.USB_HID))
            {
                var scan = dev.Scan<USB_HID_Device>();

                Console.WriteLine(scan.Status);
                Console.WriteLine(scan.Devices);

                return scan;
            }
        }

        public ScanResult<Serial_Device> ScanSerial()
        {
            using (var dev = new BSL430NET(Mode.UART_Serial))
            {
                var scan = dev.Scan<Serial_Device>();

                Console.WriteLine(scan.Status);
                Console.WriteLine(scan.Devices);

                return scan;
            }
        }

        public ScanAllResult ScanAll()
        {
            using (var dev = new BSL430NET())
            {
                var scan = dev.ScanAllEx();

                Console.WriteLine(scan.FtdiDevices.Status);
                Console.WriteLine(scan.LibftdiDevices.Status);
                Console.WriteLine(scan.UsbDevices.Status);
                Console.WriteLine(scan.SerialDevices.Status);

                Console.WriteLine(scan.FtdiDevices.Devices);
                Console.WriteLine(scan.LibftdiDevices.Devices);
                Console.WriteLine(scan.UsbDevices.Devices);
                Console.WriteLine(scan.SerialDevices.Devices);

                return scan;
            }
        }
    }
}