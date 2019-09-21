using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BSL430_NET;
using BSL430_NET.FirmwareTools;

namespace BSL430_NETSamples
{
    class SampleDownload
    {
        public StatusEx DownloadSimple(string OutputPath, FwTools.FwFormat Format, 
                                       Bsl430NetDevice Dev, byte[] Password,
                                       int AddrStart, int DataSize)
        { 
            using (var dev = new BSL430NET(Dev))
            {
                dev.ProgressChanged += new Bsl430NetEventHandler(delegate
                    (object s, Bsl430NetEventArgs args) {
                    Console.WriteLine($"{args.Progress} {args.Report}");
                });

                dev.SetBaudRate(BaudRate.BAUD_115200);
                dev.SetMCU(MCU.MSP430_F5xx);
                dev.SetInvokeMechanism(InvokeMechanism.SHARED_JTAG);

                StatusEx ret = dev.Download(Password, AddrStart, DataSize, out List<byte> Data);
                string fw = FwTools.Create(Data, AddrStart, Format);

                using (StreamWriter wr = new StreamWriter(OutputPath)) {
                    wr.Write(fw);
                }

                Console.WriteLine($"{ret}\n{fw}");
                return ret;
            }
        }
        
        public async void DownloadDetailed()
        {
            // Devices
            string HardcodedDevice = "FTDI1";                  // hardcoded device name
            var DeviceFromScan = new Bsl430NetDevice("USB2");  // device from Scan methods
            Mode GenericDevice = Mode.UART_Serial;             // not know device yet

            // Input data

            // Output firmware file paths
            string OutputPath1 = @"firmware1.hex";             // firmware output path 1
            string OutputPath2 = @"firmware2.txt";             // firmware output path 2
            string OutputPath3 = @"firmware3.s19";             // firmware output path 3

            // Output firmware file formats
            FwTools.FwFormat OutputFormat1 = FwTools.FwFormat.INTEL_HEX;  // Intel-HEX
            FwTools.FwFormat OutputFormat2 = FwTools.FwFormat.TI_TXT;     // TI-TXT 
            FwTools.FwFormat OutputFormat3 = FwTools.FwFormat.SREC;       // SREC

            // First address - from where to start
            int AddrStart1 = 0x8000;                           // start address 1 - 0x8000
            int AddrStart2 = 0x9999;                           // start address 2 - 0x9999
            int AddrStart3 = 0xAACC;                           // start address 3 - 0xAACC

            // Byte size - how many bytes to download
            int DataSize1 = 32768;                             // byte size 1 - 0x8000 hex
            int DataSize2 = 1000;                              // byte size 2 - 1000 dec
            int DataSize3 = 1;                                 // byte size 3 - single byte

            // BSL password, crucial parameter (read doc)
            byte[] Password1 = Enumerable.Repeat((byte)0xFF, 32).ToArray(); // standard 32 len
            byte[] Password2 = Enumerable.Repeat((byte)0xFF, 16).ToArray(); // F543x Non A only
            byte[] Password3 = Enumerable.Repeat((byte)0xFF, 20).ToArray(); // 20 byte old MCUs

            // Dev1 and dev2 use DefaultDevice - default device is entered once into
            // constructor, and then doesnt need to be filled again; the usual way.
            // Dev3 use generic approach, that can be useful when target multiple MCUs
            using (var dev1 = new BSL430NET(HardcodedDevice))
            using (var dev2 = new BSL430NET(DeviceFromScan))
            using (var dev3 = new BSL430NET(GenericDevice))
            {
                // create simple event handler, prints progress (0-100) and report
                var BslEventHandler = new Bsl430NetEventHandler(delegate
                    (object s, Bsl430NetEventArgs args) {
                    Console.WriteLine($"{args.Progress} {args.Report}");
                });

                // assign same event handler for all devices
                dev1.ProgressChanged += BslEventHandler;
                dev2.ProgressChanged += BslEventHandler;
                dev3.ProgressChanged += BslEventHandler;

                // dev1 settings: fastest speed, F6xx MCU, standard 32 byte password
                Status stat1Baud = dev1.SetBaudRate(BaudRate.BAUD_115200);
                Status stat1Mcu = dev1.SetMCU(MCU.MSP430_F6xx);
                Status stat1Invoke = dev1.SetInvokeMechanism(InvokeMechanism.DEDICATED_JTAG);

                // dev2 settings: slowest speed, F543x MCU Non A -> 16 byte password!
                Status stat2Baud = dev2.SetBaudRate(BaudRate.BAUD_9600);
                Status stat2Mcu = dev2.SetMCU(MCU.MSP430_F543x_NON_A);
                Status stat2Invoke = dev2.SetInvokeMechanism(InvokeMechanism.SHARED_JTAG);

                // dev3 settings: middle speed, old G2xx3 MCU -> 20 byte password, old protocol
                Status stat3Baud = dev3.SetBaudRate(BaudRate.BAUD_38400);
                Status stat3Mcu = dev3.SetMCU(MCU.MSP430_G2xx3);
                Status stat3Invoke = dev3.SetInvokeMechanism(InvokeMechanism.SHARED_JTAG);

                // Run Download of 3 firmwares to 3 MCUs, BSL password is required,
                // Beware when 1xx/2xx/4xx old MCU is used, incorrect password could 
                // wipe also Info A with calibration data. This is not case when 
                // LOCK A bit is set, preventing erase, or if modern 5xx/6xx MCUs used
                var result1 = Task.FromResult<StatusEx>(
                    dev1.Download(Password1, AddrStart1, DataSize1, out List<byte> Data1));
                var result2 = Task.FromResult<StatusEx>(
                    dev2.Download(Password2, AddrStart2, DataSize2, out List<byte> Data2));
                var result3 = await Task.FromResult<StatusEx>(
                    dev3.Download(Password3, AddrStart3, DataSize3, out List<byte> Data3, "COM1"));

                // After download create firmare string from raw data
                string fw1 = FwTools.Create(Data1, AddrStart1, OutputFormat1);
                string fw2 = FwTools.Create(Data2, AddrStart2, OutputFormat2);
                string fw3 = FwTools.Create(Data3, AddrStart3, OutputFormat3);

                // Finally write ready firmwares to disk
                using (StreamWriter wr1 = new StreamWriter(OutputPath1))
                using (StreamWriter wr2 = new StreamWriter(OutputPath2))
                using (StreamWriter wr3 = new StreamWriter(OutputPath3)) 
                {
                    wr1.Write(fw1);
                    wr2.Write(fw2);
                    wr3.Write(fw3);
                }
                
                // use overiden ToString method to output all important result data
                Console.WriteLine($"Dev1: {result1}\n{fw1}\n\n");
                Console.WriteLine($"Dev2: {result2}\n{fw2}\n\n");
                Console.WriteLine($"Dev3: {result3}\n{fw3}");
            }
        }
    }
}