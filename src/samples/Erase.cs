using System;
using System.Threading.Tasks;
using BSL430_NET;

namespace BSL430_NETSamples
{
    class SampleErase
    {
        public StatusEx EraseSimple(Bsl430NetDevice Dev)
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

                StatusEx ret = dev.Erase();

                Console.WriteLine($"{ret}");
                return ret;
            }
        }
        
        public  async void EraseDetailed()
        {
            // Devices
            string HardcodedDevice = "FTDI1";                  // hardcoded device name
            var DeviceFromScan = new Bsl430NetDevice("USB2");  // device from Scan methods
            Mode GenericDevice = Mode.UART_Serial;             // not know device yet

            // For Erase there is no need for input data

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

                // dev1 settings: F6xx MCU, dedicated JTAG pins (TCK pin)
                Status stat1Mcu = dev1.SetMCU(MCU.MSP430_F6xx);
                Status stat1Invoke = dev1.SetInvokeMechanism(InvokeMechanism.DEDICATED_JTAG);

                // dev2 settings: F5xx MCU, shared JTAG pins (TEST pin)
                Status stat2Mcu = dev2.SetMCU(MCU.MSP430_F5xx);
                Status stat2Invoke = dev2.SetInvokeMechanism(InvokeMechanism.SHARED_JTAG);

                // dev3 settings: old G2xx3 MCU - careful to not wipe Info A! set LOCK A bit
                Status stat3Mcu = dev3.SetMCU(MCU.MSP430_G2xx3);
                Status stat3Invoke = dev3.SetInvokeMechanism(InvokeMechanism.SHARED_JTAG);

                // Run Mass Erase of 3 MCUs - whole memory is wiped. Beware when 1xx/2xx/4xx 
                // old MCU is used and LOCK A is NOT set, Info A with calib data is wiped!
                var result1 = Task.FromResult<StatusEx>(dev1.Erase());
                var result2 = Task.FromResult<StatusEx>(dev2.Erase());
                var result3 = await Task.FromResult<StatusEx>(dev3.Erase("COM1"));
                
                // use overiden ToString method to output all important result data
                Console.WriteLine($"Dev1: {result1}\n\n");
                Console.WriteLine($"Dev2: {result2}\n\n");
                Console.WriteLine($"Dev3: {result3}");
            }
        }
    }
}