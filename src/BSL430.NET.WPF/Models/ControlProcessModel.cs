/*
    BSL430.NET - MSP430 bootloader (BSL) .NET toolchain
    Original source by: Jakub Parez - https://github.com/parezj/
	  
    The MIT License (MIT)
    
    Copyright (c) 2019 Jakub Parez

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
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml;
using System.Xml.Serialization;

using Caliburn.Micro;

using BSL430_NET;
using BSL430_NET.Comm;
using BSL430_NET.FirmwareTools;
using BSL430_NET.Utility;
using BSL430_NET_WPF.Helpers;
using BSL430_NET_WPF.Models;
using BSL430_NET_WPF.Settings;
using BSL430_NET_WPF.ViewModels;
using BSL430_NET_WPF.Views;


namespace BSL430_NET_WPF.Models
{
    public class ControlProcessModel
    {
        #region Public Data
        public bool logError = false;
        public readonly string logPath = "";
        public string logOveride = "";
        #endregion

        #region Private Data
        private readonly ControlProcessViewModel viewModel;
        private StatusEx retStat;

        private readonly string xmlDecl = $"<?xml version=\"1.0\" encoding=\"utf-8\" ?>";
        private readonly string xmlHead = "";
        private readonly string xmlFoot = "</BSL430.NET>";
        private readonly string xmlRoot = "<BSL430.NET>";
        private readonly XmlSerializerNamespaces xmlNs = new XmlSerializerNamespaces();
        private readonly XmlWriterSettings xmlSettings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "    ",
            OmitXmlDeclaration = true
        };
        #endregion

        #region Constructor
        public ControlProcessModel(ControlProcessViewModel viewModel)
        {
            this.viewModel = viewModel;
            this.devices = new List<Bsl430NetDevice>();
            this.logPath = GetLogPath();
            this.xmlNs.Add("", "");
            this.xmlHead = $"{xmlDecl}\n\n{xmlRoot}\n\n";
        }
        #endregion

        #region Devices Container
        private List<Bsl430NetDevice> devices;
        public List<Bsl430NetDevice> Devices
        {
            get => devices;
            set => devices = value;
        }
        #endregion

        #region Main Tasks
        public void ScanTask()
        {
            int lastDev = 0;
            try
            {
                using (var dev = new BSL430NET())
                {
                    var ret = dev.ScanAllEx();

                    this.Devices.AddRange(ret.FtdiDevices.Devices);
                    this.Devices.AddRange(ret.LibftdiDevices.Devices);
                    this.Devices.AddRange(ret.UsbDevices.Devices);
                    this.Devices.AddRange(ret.SerialDevices.Devices);

                    lastDev = this.Devices.FindIndex(d => d.Name == BslSettings.Instance.MainLastDevice);
                    lastDev = (lastDev >= 0) ? this.viewModel.SelectedIndex = lastDev : 0;

                    var _xml = new ScanAllResult()
                    {
                        FTDI = new ScanAllResult.FTDIResult()
                        {
                            ErrorNum = ret.FtdiDevices.Status.Error,
                            Status = ret.FtdiDevices.Status.ToString(),
                            Count = ret.FtdiDevices.Devices.Count,
                            Devices = ret.FtdiDevices.Devices
                        },
                        Libftdi = new ScanAllResult.LibftdiResult()
                        {
                            ErrorNum = ret.LibftdiDevices.Status.Error,
                            Status = ret.LibftdiDevices.Status.ToString(),
                            Count = ret.LibftdiDevices.Devices.Count,
                            Devices = ret.LibftdiDevices.Devices
                        },
                        USB = new ScanAllResult.USBResult()
                        {
                            ErrorNum = ret.UsbDevices.Status.Error,
                            Status = ret.UsbDevices.Status.ToString(),
                            Count = ret.UsbDevices.Devices.Count,
                            Devices = ret.UsbDevices.Devices
                        },
                        Serial = new ScanAllResult.SerialResult()
                        {
                            ErrorNum = ret.SerialDevices.Status.Error,
                            Status = ret.SerialDevices.Status.ToString(),
                            Count = ret.SerialDevices.Devices.Count,
                            Devices = ret.SerialDevices.Devices
                        }
                    };

                    WriteXML(GetLogRoot(_xml, "ScanAllEx"));
                }
            }
            catch (Exception ex)
            {
                WriteXML(GetLogRoot((ex is Bsl430NetException) ? (Bsl430NetException)ex : ex, "Failed.Exception"));
                System.Windows.Application.Current.Dispatcher.Invoke((System.Action)(() =>
                {
                    //MessageBox.Show($"Operation Failed!\nError: {retStat.Error}\n\n{retStat.Msg.Replace("\n[", "\n\n[")}", "BSL430.NET",
                    //MessageBoxButton.OK, MessageBoxImage.Error);
                    MessageBox.Show($"{ex.GetExceptionMsg()}", "BSL430.NET", MessageBoxButton.OK, MessageBoxImage.Error);
                }));
            }

            System.Windows.Application.Current.Dispatcher.Invoke((System.Action)(() =>
            {
                this.viewModel.Devices.Refresh();
                viewModel.NotifyOfPropertyChange(() => Devices);
                this.viewModel.SelectedIndex = lastDev;
                this.viewModel.Scanning = false;
            }));
        }
        public void ProcessContainerTask(string processName)
        {
            retStat = null;
            //retEx = null;

            Task task;
            switch (processName)
            {
                default: return;
                case "Upload": task = new Task(delegate { ProcessUploadTask(); }); break;
                case "Download": task = new Task(delegate { ProcessDownloadTask(); }); break;
                case "Erase": task = new Task(delegate { ProcessEraseTask(); }); break;
            }

            task?.Start();
            task?.Wait();

            WriteXML(GetLogRoot(retStat, processName));

            System.Windows.Application.Current.Dispatcher.Invoke((System.Action)(() =>
            {
                this.viewModel.InProgress = false;

                if (retStat != null && retStat.OK)
                {
                    this.viewModel.State = ProcessState.SUCCESS;
                    MessageBox.Show("Success!", "BSL430.NET", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    this.viewModel.State = ProcessState.FAILED;
                    //MessageBox.Show($"Operation Failed!\nError: {retStat.Error}\n\n{retStat.Msg.Replace("\n[", "\n\n[")}", "BSL430.NET",
                    //MessageBoxButton.OK, MessageBoxImage.Error);
                    MessageBox.Show($"{retStat.ToString()}", "BSL430.NET", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }));
        }
        public void ProcessUploadTask()
        {
            try
            {
                using (var dev = new BSL430NET(this.Devices[this.viewModel.SelectedIndex]))
                {
                    dev.SetMCU(this.viewModel.MCU);
                    dev.SetBaudRate(this.viewModel.BaudRate);
                    dev.SetInvokeMechanism(this.viewModel.InvokeMechanism);
                    dev.ProgressChanged += new Bsl430NetEventHandler(ProgressChanged);
                    retStat = dev.Upload(this.viewModel.FwPathUpload, Password: this.viewModel.Password.ToByteArray());
                }
            }
            catch (Exception ex)
            {
                WriteXML(GetLogRoot((ex is Bsl430NetException) ? (Bsl430NetException)ex : ex, "Failed.Exception"));
            }
        }
        public void ProcessDownloadTask()
        {
            try
            {
                using (var dev = new BSL430NET(this.Devices[this.viewModel.SelectedIndex]))
                {
                    dev.SetMCU(this.viewModel.MCU);
                    dev.SetBaudRate(this.viewModel.BaudRate);
                    dev.SetInvokeMechanism(this.viewModel.InvokeMechanism);
                    dev.ProgressChanged += new Bsl430NetEventHandler(ProgressChanged);
                    retStat = dev.Download(this.viewModel.Password.ToByteArray(), 
                                           this.viewModel.StartAddress, 
                                           this.viewModel.ByteSize, 
                                           out List<byte> data);

                    using (StreamWriter wr = new StreamWriter(this.viewModel.FwPathDownload, false))
                    {
                        wr.Write(FwTools.Create(data, 
                                                this.viewModel.StartAddress, 
                                                this.viewModel.OutputFormat, 
                                                BslSettings.Instance.FwWriteLineLength));
                    }
                }
            }
            catch (Exception ex)
            {
                WriteXML(GetLogRoot((ex is Bsl430NetException) ? (Bsl430NetException)ex : ex, "Failed.Exception"));
            }
        }
        private void ProcessEraseTask()
        {
            try
            {
                using (var dev = new BSL430NET(this.Devices[this.viewModel.SelectedIndex]))
                {
                    dev.SetMCU(this.viewModel.MCU);
                    dev.SetBaudRate(this.viewModel.BaudRate);
                    dev.SetInvokeMechanism(this.viewModel.InvokeMechanism);
                    dev.ProgressChanged += new Bsl430NetEventHandler(ProgressChanged);
                    retStat = dev.Erase();
                }
            }
            catch (Exception ex)
            {
                WriteXML(GetLogRoot((ex is Bsl430NetException) ? (Bsl430NetException)ex : ex, "Failed.Exception"));
            }
        }
        #endregion

        #region XML Log
        private string GetLogPath()
        {
            var ver = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
            string logName = $"{ver.ProductName.Replace(' ', '_')}.log.xml";

            string file2 = Path.Combine(Path.GetTempPath(), logName);
            string file1 = Path.Combine(BslSettings.SettingsDir, logName);

            if (File.Exists(file1))
                return file1;

            try
            {
                using (FileStream fs = File.Create(file1)) { }
                if (File.Exists(file1))
                    return file1;
            }
            catch (Exception) { }

            try
            {
                if (File.Exists(file2))
                    return file2;
                using (FileStream fs = File.Create(file2)) { }
                if (File.Exists(file2))
                    return file2;
            }
            catch (Exception) { }

            logError = true;
            return logOveride = "";
        }
        private void ProgressChanged(object source, Bsl430NetEventArgs args)
        {
            System.Windows.Application.Current.Dispatcher.Invoke((System.Action)(() =>
            {
                string dat = args.Report.Timestamp.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
                this.viewModel.Status = $"{args.Report.Result.ToString()} <{dat}> {args.Report.Name}";
                this.viewModel.Progress = args.Progress;
            }));
        }
        private LogRoot<T> GetLogRoot<T>(T o, string name)
        {
            return new LogRoot<T>() { Time = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ"), Name = name, Data = o };
        }
        private void WriteXML(object o)
        {
            try
            {
                XmlSerializer xs = new XmlSerializer(o.GetType(), "");
                if (!this.logError)
                {
                    if (!File.Exists(this.logPath))
                        using (FileStream fs = File.Create(this.logPath)) { }

                    var txt = File.ReadAllText(this.logPath);
                    txt = txt.Replace(this.xmlDecl, "").Replace(this.xmlRoot, "").Replace(this.xmlFoot, "").Trim('\n');
                    bool isBlank = (txt == "");

                    using (StreamWriter wrF = new StreamWriter(this.logPath, false))
                    using (StringWriter wrS = new StringWriter())
                    using (XmlWriter xmlwr = XmlWriter.Create(wrS, this.xmlSettings))
                    {
                        wrF.Write($"{this.xmlHead}{txt}");
                        if (!isBlank)
                            wrF.Write("\n\n");
                        xs.Serialize(xmlwr, o, this.xmlNs);
                        wrF.Write($"    {wrS.ToString().Replace("\n", "\n    ")}");
                        wrF.Write($"\n\n{this.xmlFoot}\n");
                    }
                }
                else
                {
                    this.logOveride = this.logOveride.Replace(this.xmlDecl, "").Replace(this.xmlRoot, "").Replace(this.xmlFoot, "").Trim('\n');
                    bool isBlank = (this.logOveride == "");

                    using (StringWriter wrF = new StringWriter())
                    using (StringWriter wrS = new StringWriter())
                    using (XmlWriter xmlwr = XmlWriter.Create(wrS, this.xmlSettings))
                    {
                        wrF.Write($"{this.xmlHead}{this.logOveride}");
                        if (!isBlank)
                            wrF.Write("\n\n");
                        xs.Serialize(xmlwr, o, this.xmlNs);
                        wrF.Write($"    {wrS.ToString().Replace("\n", "\n    ")}");
                        wrF.Write($"\n\n{this.xmlFoot}\n");
                        this.logOveride = wrF.ToString();
                    }
                }
            }
            catch (Exception) { }
        }
        #endregion

        #region XML Serialization Classes
        [XmlRoot("BSL430.NET.Log", Namespace = ""), Serializable()]
        public class LogRoot<T>
        {
            public string Name { set; get; }
            public string Time { set; get; }
            public T Data { set; get; }
        }

        [Serializable]
        public class ScanAllResultGeneric
        {
            public int ErrorNum { get; set; }
            public string Status { get; set; }
            public int Count { get; set; }
        }

        [Serializable]
        public class ScanAllResult
        {
            [Serializable]
            public class FTDIResult : ScanAllResultGeneric
            {
                public List<FTDI_Device> Devices { get; set; }
            }

            [Serializable]
            public class LibftdiResult : ScanAllResultGeneric
            {
                public List<Libftdi_Device> Devices { get; set; }
            }

            [Serializable]
            public class USBResult : ScanAllResultGeneric
            {
                public List<USB_HID_Device> Devices { get; set; }
            }

            [Serializable]
            public class SerialResult : ScanAllResultGeneric
            {
                public List<Serial_Device> Devices { get; set; }
            }

            public FTDIResult FTDI { get; set; }
            public LibftdiResult Libftdi { get; set; }
            public USBResult USB { get; set; }
            public SerialResult Serial { get; set; }
        }
        #endregion
    }
}
