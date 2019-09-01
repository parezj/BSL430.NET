/*
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

using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Xunit;
using Moq;

using BSL430_NET.Comm;
using BSL430_NET.Main;
using BSL430_NET.FirmwareTools;


namespace BSL430_NET.Test
{
    public static class TestData
    {
        [Flags]
        public enum Fw
        {
            TI_TXT    = 1,
            INTEL_HEX = 2,
            ELF       = 4,
            TI_TXT_2  = 8,
            INFO_A    = 16
        }

        private static readonly Dictionary<Fw, string> FW_PATHS;

        public static readonly StatusEx OK = new StatusEx() { OK = true };

        public static readonly byte[] RES_BAUDRATE_OK = new byte[] { 0x00 };
        public static readonly byte[] RES_PASSWORD_OK = new byte[] { 0x00, 0x80, 0x02, 0x00, 0x3B, 0x00, 0x60, 0xC4 };
        public static readonly byte[] RES_MASSERASE_OK = new byte[] { 0x00, 0x80, 0x02, 0x00, 0x3B, 0x00, 0x60, 0xC4 };
        public static readonly byte[] RES_BSLVERSION_OK = new byte[] { 0x00, 0x80, 0x05, 0x00, 0x3A, 0x00, 0x01, 0x01, 0x01, 0x6C, 0x4F };
        public static readonly byte[] RES_UPLOAD_OK = new byte[] { 0x00, 0x80, 0x02, 0x00, 0x3B, 0x00, 0x60, 0xC4 };
        public static readonly byte[] RES_DOWNLOAD_OK = new byte[] { 0x00, 0x80, 0x02, 0x00, 0x3A, 0xFF, 0x73, 0xF3, 0x83 };
        public static readonly byte[] RES_CRC_OK = new byte[] { 0x00, 0x80, 0x01, 0x00, 0x3A, 0x04, 0x50, 0xC9, 0x58 };
        public static readonly byte[] RES_LOADPC_OK = new byte[] { };

        public static readonly List<byte[]> ERASE_DATA_OK = new List<byte[]>() { RES_BAUDRATE_OK,
                                                                                 RES_MASSERASE_OK,
                                                                                 RES_PASSWORD_OK,
                                                                                 RES_BSLVERSION_OK};

        public static readonly List<byte[]> UPLOAD_DATA_OK = new List<byte[]>() { RES_BAUDRATE_OK,
                                                                                  RES_MASSERASE_OK,
                                                                                  RES_PASSWORD_OK,
                                                                                  RES_BSLVERSION_OK,
                                                                                  RES_UPLOAD_OK,
                                                                                  RES_CRC_OK,
                                                                                  RES_LOADPC_OK};

        public static readonly List<byte[]> DOWNLOAD_DATA_OK = new List<byte[]>() { RES_BAUDRATE_OK,
                                                                                    RES_PASSWORD_OK,
                                                                                    RES_BSLVERSION_OK,
                                                                                    RES_DOWNLOAD_OK,
                                                                                    RES_CRC_OK,
                                                                                    RES_LOADPC_OK};

        private const string TEST_DATA_FOLDER = "TestData";
        private const string FW_TI_TXT = "fw.txt";
        private const string FW_TI_TXT_2 = "fw2.txt";
        private const string FW_INTEL_HEX = "fw.hex";
        private const string FW_ELF = "fw.out";
        private const string FW_TI_TXT_INFO_A = "info_a.txt";

        static TestData()
        {
            string test_dir = Path.GetDirectoryName(typeof(FirmwareTools).GetTypeInfo().Assembly.Location);
            FW_PATHS = new Dictionary<Fw, string>
            {
                { Fw.TI_TXT, Path.Combine(test_dir, TEST_DATA_FOLDER, FW_TI_TXT) },
                { Fw.INTEL_HEX, Path.Combine(test_dir, TEST_DATA_FOLDER, FW_INTEL_HEX) },
                { Fw.ELF, Path.Combine(test_dir, TEST_DATA_FOLDER, FW_ELF) },
                { Fw.TI_TXT_2, Path.Combine(test_dir, TEST_DATA_FOLDER, FW_TI_TXT_2) },
                { Fw.INFO_A, Path.Combine(test_dir, TEST_DATA_FOLDER, FW_TI_TXT_INFO_A) }
            };
        }
        
        public static string GetFwPath(Fw fw)
        {
            return FW_PATHS[fw];
        }
    }
    internal static class TestExtensions
    {
        public static IEnumerable<TestData.Fw> GetFlags(this Enum input)   // where T : enum -> (C# 7.3)
        {
            foreach (TestData.Fw value in Enum.GetValues(input.GetType()))
                if (input.HasFlag(value))
                    yield return value;
        }
        public class XferOutQueue
        {
            private readonly Queue<byte[]> q = new Queue<byte[]>();
            public XferOutQueue(IEnumerable<byte[]> data)
            {
                foreach (byte[] nod in data)
                    q.Enqueue(nod);
            }
            public byte[] DataOut
            {
                get { return q.Dequeue(); }
                set { q.Enqueue(value); }
            }
        }
    }
    public class Core
    {
        private static readonly Mock<CommFTD2XX> mock_ftdi;
        private static readonly Mock<CommLibftdi> mock_libftdi;
        private static readonly Mock<CommUSB> mock_usb;
        private static readonly Mock<CommSerial> mock_serial;

        static Core()
        {
            mock_ftdi = new Mock<CommFTD2XX>(MockBehavior.Loose);
            mock_libftdi = new Mock<CommLibftdi>(MockBehavior.Loose);
            mock_usb = new Mock<CommUSB>(MockBehavior.Loose);
            mock_serial = new Mock<CommSerial>(MockBehavior.Loose);

            MockSetUp(mock_ftdi);
            MockSetUp(mock_libftdi);
            MockSetUp(mock_usb);
            MockSetUp(mock_serial);
        }
        static private void MockSetUp<T>(Mock<T> mock) where T : Main.Core
        {
            mock.Setup(dev => dev.CommOpen(It.IsAny<Bsl430NetDevice>()));
            mock.Setup(dev => dev.CommSet(It.IsAny<BaudRate>()));
            mock.Setup(dev => dev.CommDtr(It.IsAny<bool>(), It.IsAny<bool>()));
            mock.Setup(dev => dev.CommRts(It.IsAny<bool>(), It.IsAny<bool>()));
            mock.Setup(dev => dev.CommClrBuff());
            mock.Setup(dev => dev.CommClose(It.IsAny<bool>()));
        }
        private void SetupXfer(Mock<Main.Core> mock, TestExtensions.XferOutQueue buff)
        {
            byte[] data = buff.DataOut;
            mock.Setup(m => m.CommXfer(out data, null, 0)).Returns(new Status { OK = true })
                                                          .Callback(() => SetupXfer(mock, buff));
        }

        public static IEnumerable<object[]> EraseData()
        {
            yield return new object[] { mock_ftdi, TestData.ERASE_DATA_OK, TestData.OK };
            yield return new object[] { mock_libftdi, TestData.ERASE_DATA_OK, TestData.OK };
            yield return new object[] { mock_usb, TestData.ERASE_DATA_OK, TestData.OK };
            yield return new object[] { mock_serial, TestData.ERASE_DATA_OK, TestData.OK };
        }
        [Theory]
        [MemberData(nameof(EraseData))]
        public void Erase(Mock mock, List<byte[]> dataOut, StatusEx res)
        {
            var buff = new TestExtensions.XferOutQueue(dataOut);
            SetupXfer(mock as Mock<Main.Core>, buff);

            using (var dev = new BSL430NET(mock.Object))
            {
                dev.SetBaudRate(BaudRate.BAUD_115200);
                dev.SetMCU(MCU.MSP430_F5xx);
                StatusEx stat = dev.Erase();

                Assert.NotNull(stat);
                Assert.True(res.OK);
            }
        }

        public static IEnumerable<object[]> UploadData()
        {
            yield return new object[] { mock_ftdi, TestData.UPLOAD_DATA_OK, TestData.Fw.TI_TXT_2, TestData.OK };
            yield return new object[] { mock_libftdi, TestData.UPLOAD_DATA_OK, TestData.Fw.TI_TXT_2, TestData.OK };
            yield return new object[] { mock_usb, TestData.UPLOAD_DATA_OK, TestData.Fw.TI_TXT_2, TestData.OK };
            yield return new object[] { mock_serial, TestData.UPLOAD_DATA_OK, TestData.Fw.TI_TXT_2, TestData.OK };
        }
        [Theory]
        [MemberData(nameof(UploadData))]
        public void Upload(Mock mock, List<byte[]> dataOut, TestData.Fw fw, StatusEx res)
        {
            var buff = new TestExtensions.XferOutQueue(dataOut);
            SetupXfer(mock as Mock<Main.Core>, buff);

            using (var dev = new BSL430NET(mock.Object))
            {
                dev.SetBaudRate(BaudRate.BAUD_115200);
                dev.SetMCU(MCU.MSP430_F5xx);
                StatusEx stat = dev.Upload(TestData.GetFwPath(fw));

                Assert.NotNull(stat);
                Assert.True(res.OK);
            }
        }

        public static IEnumerable<object[]> DownloadData()
        {
            yield return new object[] { mock_ftdi, TestData.DOWNLOAD_DATA_OK, null, 0x8000, 2, TestData.OK };
            yield return new object[] { mock_libftdi, TestData.DOWNLOAD_DATA_OK, null, 0x8000, 2, TestData.OK };
            yield return new object[] { mock_usb, TestData.DOWNLOAD_DATA_OK, null, 0x8000, 2, TestData.OK };
            yield return new object[] { mock_serial, TestData.DOWNLOAD_DATA_OK, null, 0x8000, 2, TestData.OK };
        }
        [Theory]
        [MemberData(nameof(DownloadData))]
        public void Download(Mock mock, List<byte[]> dataOut, byte[] pw, int addr_start, int size, StatusEx res)
        {
            var buff = new TestExtensions.XferOutQueue(dataOut);
            SetupXfer(mock as Mock<Main.Core>, buff);

            using (var dev = new BSL430NET(mock.Object))
            {
                dev.SetBaudRate(BaudRate.BAUD_115200);
                dev.SetMCU(MCU.MSP430_F5xx);
                StatusEx stat = dev.Download(pw ?? Enumerable.Repeat((byte)0xFF, 16).ToArray(), 
                                             addr_start, 
                                             size, 
                                             out List<byte> data_ret);

                Assert.NotNull(stat);
                Assert.NotNull(data_ret);
                Assert.True(res.OK);
                Assert.True(data_ret.Count == 2);
                Assert.True(data_ret[0] == dataOut[3][5]);
                Assert.True(data_ret[1] == dataOut[3][6]);
            }
        }

        [Fact]
        public void ScanAll()
        {
            using (var dev = new BSL430NET())
            {
                var stat = dev.ScanAll(out List<FTDI_Device> ftdi,
                                       out List<Libftdi_Device> libftdi,
                                       out List<USB_HID_Device> usb,
                                       out List<Serial_Device> serial,
                                       ScanOptions.None);

                Assert.NotNull(stat.Ftdi);
                Assert.NotNull(stat.Libftdi);
                Assert.NotNull(stat.Usb);
                Assert.NotNull(stat.Serial);
                Assert.True(stat.Ftdi.OK);
                Assert.True(stat.Libftdi.OK);
                Assert.True(stat.Usb.OK);
                Assert.True(stat.Serial.OK);
            }
        }

        [Theory]
        [InlineData(Mode.UART_FTD2XX)]
        [InlineData(Mode.UART_libftdi)]
        [InlineData(Mode.USB_HID)]
        [InlineData(Mode.UART_Serial)]
        public void Scan(Mode mode)
        {
            using (var dev = new BSL430NET(mode))
            {
                Status stat = dev.Scan(out List<Bsl430NetDevice> devices, ScanOptions.None);

                Assert.NotNull(stat);
                Assert.True(stat.OK);
            }
        }
    }

    public class FirmwareTools
    {
        [Theory]
        [InlineData(FwTools.FwFormat.AUTO, TestData.Fw.TI_TXT | TestData.Fw.INTEL_HEX | TestData.Fw.ELF, false)]
        [InlineData(FwTools.FwFormat.AUTO, TestData.Fw.TI_TXT | TestData.Fw.INTEL_HEX | TestData.Fw.ELF, true)]
        [InlineData(FwTools.FwFormat.TI_TXT, TestData.Fw.TI_TXT, false)]
        [InlineData(FwTools.FwFormat.INTEL_HEX, TestData.Fw.INTEL_HEX, false)]
        [InlineData(FwTools.FwFormat.ELF, TestData.Fw.ELF, false)]
        public void FirmwareParse(FwTools.FwFormat format, TestData.Fw fw_paths, bool fill_FF)
        {
            foreach (TestData.Fw fw in fw_paths.GetFlags())
            {
                FwTools.Firmware ret = FwTools.Parse(TestData.GetFwPath(fw), format, fill_FF, true);

                Assert.NotNull(ret);
                Assert.NotNull(ret.Nodes);
                Assert.NotNull(ret.Info);
                Assert.True(ret.Nodes.Count > 0);
                Assert.True(ret.Info.SizeCode > 0);
            }
        }

        [Theory]
        [InlineData(FwTools.FwFormat.TI_TXT, TestData.Fw.TI_TXT | TestData.Fw.INTEL_HEX | TestData.Fw.ELF)]
        [InlineData(FwTools.FwFormat.INTEL_HEX, TestData.Fw.TI_TXT | TestData.Fw.INTEL_HEX | TestData.Fw.ELF)]
        public void FirmwareCreate(FwTools.FwFormat format, TestData.Fw fw_paths)
        {
            foreach (TestData.Fw fw in fw_paths.GetFlags())
            {
                FwTools.Firmware parsed = FwTools.Parse(TestData.GetFwPath(fw), FwTools.FwFormat.AUTO);
                string ret = FwTools.Create(parsed, format);

                Assert.NotNull(parsed);
                Assert.NotNull(parsed.Nodes);
                Assert.NotNull(parsed.Info);
                Assert.True(parsed.Nodes.Count > 0);
                Assert.True(parsed.Info.SizeCode > 0);
                Assert.True(ret.Length > 0);
            }
        }

        [Theory]
        [InlineData(FwTools.FwFormat.TI_TXT, TestData.Fw.TI_TXT | TestData.Fw.INTEL_HEX | TestData.Fw.ELF)]
        [InlineData(FwTools.FwFormat.INTEL_HEX, TestData.Fw.TI_TXT | TestData.Fw.INTEL_HEX | TestData.Fw.ELF)]
        public void FirmwareConvertTo(FwTools.FwFormat format, TestData.Fw fw_paths)
        {
            foreach (TestData.Fw fw in fw_paths.GetFlags())
            {
                var (Fw, Format) = FwTools.ConvertTo(TestData.GetFwPath(fw), format);

                Assert.True(Fw.Length > 0);
            }
        }

        [Theory]
        [InlineData(FwTools.FwFormat.TI_TXT, TestData.Fw.TI_TXT | TestData.Fw.INTEL_HEX | TestData.Fw.ELF, TestData.Fw.INFO_A)]
        [InlineData(FwTools.FwFormat.INTEL_HEX, TestData.Fw.TI_TXT | TestData.Fw.INTEL_HEX | TestData.Fw.ELF, TestData.Fw.INFO_A)]
        public void FirmwareCombine(FwTools.FwFormat format, TestData.Fw fw_path1, TestData.Fw fw_path2)
        {
            foreach (TestData.Fw fw in fw_path1.GetFlags())
            {
                var (Fw, Format1, Format2) = FwTools.Combine(TestData.GetFwPath(fw), TestData.GetFwPath(fw_path2), format);

                Assert.True(Fw != "");
            }
        }

        [Theory]
        [InlineData(TestData.Fw.TI_TXT | TestData.Fw.INTEL_HEX | TestData.Fw.ELF)]
        public void FirmwareGetPassword(TestData.Fw fw_paths)
        {
            foreach (TestData.Fw fw in fw_paths.GetFlags())
            {
                byte[] ret = FwTools.GetPassword(TestData.GetFwPath(fw));

                Assert.NotNull(ret);
                Assert.True(ret.Length == 16);
            }
        }

        [Theory]
        [InlineData(TestData.Fw.TI_TXT | TestData.Fw.INTEL_HEX | TestData.Fw.ELF)]
        public void FirmwareValidate(TestData.Fw fw_paths)
        {
            foreach (TestData.Fw fw in fw_paths.GetFlags())
            {
                FwTools.FwInfo ret = FwTools.Validate(TestData.GetFwPath(fw));

                Assert.NotNull(ret);
                Assert.True(ret.SizeCode > 0);
            }
        }
    }
}
