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

using BSL430_NET;
using BSL430_NET.Comm;
using BSL430_NET.FirmwareTools;
using BSL430_NET.Utility;
using BSL430_NET_WPF.Models;
using BSL430_NET_WPF.Settings;
using BSL430_NET_WPF.Views;
using Caliburn.Micro;

namespace BSL430_NET_WPF.ViewModels
{
    public class InProgressChangedArgs : EventArgs
    {
        public bool InProgress;
    }
    public interface IOnLogClose
    {
        void OnLogClose();
    }
    public enum ProcessState
    {
        SUCCESS = 0,
        FAILED = 1,
        UNKNOWN = 2
    }
    public class ControlProcessViewModel : Screen, IControlProcessViewModel, IOnLogClose
    {
        #region Private Data
        private readonly ControlProcessModel model;
        private readonly IThemeProvider themeProvider;
        private readonly LogViewModel logViewModel;
        private LogView logView;

        private string currentTab = "";
        #endregion

        #region Constructor
        public ControlProcessViewModel(LogViewModel _logViewModel, IThemeProvider _themeProvider)
        {
            this.model = new ControlProcessModel(this);
            this.themeProvider = _themeProvider;
            this.logViewModel = _logViewModel;
            this.Devices = new ListCollectionView(model.Devices);
            this.Devices.GroupDescriptions.Add(new PropertyGroupDescription("Kind"));
            this.themeProvider.ThemeChanged += OnThemeChanged;
        }
        #endregion

        #region Public Interface
        public void CurrentTabHeader(string header)
        {
            this.currentTab = header;
        }
        public void OnLogClose()
        {
            this.logViewModel.LogData = "";
            this.logView = null;
        }
        public void ResetState()
        {
            this.State = ProcessState.UNKNOWN;
            this.Progress = 0.0;
        }
        #endregion

        #region Interface Events
        public event EventHandler InProgressChanged;
        readonly object locq = new object();
        event EventHandler IControlProcessViewModel.InProgressChanged
        {
            add
            {
                lock (locq)
                {
                    InProgressChanged += value;
                }
            }
            remove
            {
                lock (locq)
                {
                    InProgressChanged -= value;
                }
            }
        }
        public void AppReadyHandler()
        {
            Scan();
        }
        #endregion

        #region Actions
        public void Scan()
        {
            model.Devices.Clear();
            this.Devices.Refresh();
            NotifyOfPropertyChange(() => Devices);
            this.Scanning = true;

            Task task = new Task(delegate { model.ScanTask(); });
            task.Start();
        }
        public void StartStop()
        {
            if (!this.InProgress)
            {
                this.InProgress = true;
                this.State = ProcessState.UNKNOWN;

                Task task = new Task(delegate { this.model.ProcessContainerTask(this.currentTab); });
                task.Start();
            }
            else
            {
                var result = MessageBox.Show("Do you really want to force abort main process thread?\n\nIt is not recommended to break firmware ops, as the device will most likely not work correctly! Moreover, aborting thread which uses unmanaged resources will surely cause memory and that could make system unstable.", "BSL430.NET", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        BSL430NET.Interrupt();
                    }
                    catch (Exception) { }
                }
            }
        }
        public void OpenLog()
        {
            bool err = this.model.logError;
            if (!err)
            {
                try
                {
                    logViewModel.LogData = File.ReadAllText(this.model.logPath);
                    logViewModel.LogPath = this.model.logPath;
                }
                catch (Exception) { err = true; }
            }
            if (err)
            {
                logViewModel.LogData = this.model.logOveride;
                logViewModel.LogPath = "Log file path error! (AppData/Temp). Using internal buffer.";
            }

            if (logView == null)
            {
                logView = new LogView((IOnLogClose)this, logViewModel, themeProvider.IsDarkMode())
                {
                    DataContext = logViewModel
                };
            }
            logView.Show();
            logView.Activate();
        }
        #endregion

        #region Properties
        public ListCollectionView Devices { get; set; }
        public string FwPathUpload { get; set; }
        public string FwPathDownload { get; set; }
        public int StartAddress { get; set; }
        public int ByteSize { get; set; }
        public BSL430_NET.FirmwareTools.FwTools.FwFormat OutputFormat { get; set; }

        private ProcessState _State = ProcessState.UNKNOWN;
        public ProcessState State
        {
            get => _State;
            set
            {
                _State = value;
                NotifyOfPropertyChange(() => State);
            }
        }

        private bool _InProgress = false;
        public bool InProgress
        {
            get => _InProgress;
            set
            {
                _InProgress = value;
                NotifyOfPropertyChange(() => InProgress);
                this.InProgressChanged?.Invoke(this, new InProgressChangedArgs { InProgress = value });
            }
        }
        private double _Progress = 0;
        public double Progress
        {
            get => _Progress;
            set
            {
                _Progress = value;
                NotifyOfPropertyChange(() => Progress);
            }
        }
        private string _Status = "READY";
        public string Status
        {
            get => _Status;
            set
            {
                _Status = value;
                NotifyOfPropertyChange(() => Status);
            }
        }
        private int _SelectedIndex = 0;
        public int SelectedIndex
        {
            get => _SelectedIndex;
            set
            {
                _SelectedIndex = value;
                try
                {
                    BslSettings.Instance.MainLastDevice = this.model.Devices[value].Name;
                }
                catch(Exception) { }
                NotifyOfPropertyChange(() => SelectedIndex);
            }
        }
        private MCU _MCU = BslSettings.Instance.MainMCU;
        public MCU MCU
        {
            get => _MCU;
            set
            {
                _MCU = value;
                BslSettings.Instance.MainMCU = value;
                NotifyOfPropertyChange(() => MCU);
            }
        }
        private BaudRate _BaudRate = BslSettings.Instance.MainBaudRate;
        public BaudRate BaudRate
        {
            get => _BaudRate;
            set
            {
                _BaudRate = value;
                BslSettings.Instance.MainBaudRate = value;
                NotifyOfPropertyChange(() => BaudRate);
            }
        }
        private InvokeMechanism _InvokeMechanism = BslSettings.Instance.MainInvokeMechanism;
        public InvokeMechanism InvokeMechanism
        {
            get => _InvokeMechanism;
            set
            {
                _InvokeMechanism = value;
                BslSettings.Instance.MainInvokeMechanism = value;
                NotifyOfPropertyChange(() => InvokeMechanism);
            }
        }
        private string _Password = BslSettings.Instance.MainPassword;
        public string Password
        {
            get => _Password;
            set
            {
                _Password = value;
                BslSettings.Instance.MainPassword = value;
                NotifyOfPropertyChange(() => Password);
            }
        }
        private bool _Scanning = false;

        public bool Scanning
        {
            get => _Scanning;
            set
            {
                _Scanning = value;
                NotifyOfPropertyChange(() => Scanning);
            }
        }
        #endregion

        #region Public Aux Methods
        public bool ValidateBslPassword(int ByteLength, MCU Mcu) // true = valid
        {
            return this.model.ValidateBslPassword(ByteLength, Mcu);
        }
        #endregion

        #region Private Methods
        private void OnThemeChanged(object sender, EventArgs e)
        {
            if (logView != null)
                logView.SetTheme(((ThemeChangedArgs)e).IsDarkMode);
        }
        #endregion
    }
}
