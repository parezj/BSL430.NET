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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Media;

using BSL430_NET;
using BSL430_NET_WPF.Settings;
using BSL430_NET_WPF.Models;
using BSL430_NET_WPF.Helpers;

using MahApps.Metro.Controls;
using Caliburn.Micro;


namespace BSL430_NET_WPF.ViewModels
{
    public interface IMainViewModel
    {
        bool IsDarkMode();
        bool IsInProgress();
        void RefreshBuggedBg(bool darkMode);
        void ShellIntegrated();
    }
    public class MainViewModel : Screen, IMainViewModel
    {
        #region Public Data
        public IShellViewModel IshellViewModel { get; private set; }
        public ObservableCollection<MetroTabItem> Tabs { get; private set; }
        #endregion

        #region Private Data
        private readonly IControlProcessViewModel IcontrolProcess;

        private readonly ITabDownloadViewModel Idownload_vm;
        #endregion

        #region Constructor
        public MainViewModel(IShellViewModel _shellViewModel, 
                             IEnumerable<MetroTabItem> tabs, 
                             IControlProcessViewModel ctrlplc, 
                             ITabDownloadViewModel _download_vm,
                             bool darkMode)
        {
            this.IshellViewModel = _shellViewModel;
            this.IcontrolProcess = ctrlplc;
            this.Idownload_vm = _download_vm;
            this.IcontrolProcess.InProgressChanged += OnInProgressChanged;
            this.Tabs = new ObservableCollection<MetroTabItem>();
            tabs.ToList().ForEach(this.Tabs.Add);
            if (this.TabSelectedIndex >= this.Tabs.Count)
                this.TabSelectedIndex = 0;
            this.TabUpdateHeader();
            this.IshellViewModel.ShowWindow();
            this.RefreshBuggedBg(darkMode);
        }
        #endregion

        #region Properties
        private int _TabSelectedIndex = BslSettings.Instance.GeneralTabSelectedIndex;
        public int TabSelectedIndex
        {
            get => _TabSelectedIndex;
            set
            {
                _TabSelectedIndex = value;
                BslSettings.Instance.GeneralTabSelectedIndex = value;
                IcontrolProcess.ResetState();
                NotifyOfPropertyChange(() => TabSelectedIndex);
            }
        }
        private string _TabSelectedHeader = "";
        public string TabSelectedHeader
        {
            get => _TabSelectedHeader;
            set
            {
                _TabSelectedHeader = value;
                IcontrolProcess?.CurrentTabHeader(value);
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
            }
        }
        private int _DownloadSizeMax = BslSettings.Instance.DownloadSizeRange;
        public int DownloadSizeMax
        {
            get => _DownloadSizeMax;
            set
            {
                _DownloadSizeMax = value;
                BslSettings.Instance.DownloadSizeRange = value;
                NotifyOfPropertyChange(() => DownloadSizeMax);
                this.Idownload_vm.RangeMaxK = value;
            }
        }
        private int _FwWriteLineLength = BslSettings.Instance.FwWriteLineLength;
        public int FwWriteLineLength
        {
            get => _FwWriteLineLength;
            set
            {
                _FwWriteLineLength = value;
                BslSettings.Instance.FwWriteLineLength = value;
                NotifyOfPropertyChange(() => FwWriteLineLength);
            }
        }
        public ResourceDictionary TaskBarIconResources { get; set; }

        private Brush _BuggedBorderColor = Brushes.White; // context menu items styles not refresh when resource changes..
        public Brush BuggedBorderColor
        {
            get => _BuggedBorderColor;
            set
            {
                _BuggedBorderColor = value;
                NotifyOfPropertyChange(() => BuggedBorderColor);
            }
        }
        private Brush _BuggedTextColor = Brushes.White;
        public Brush BuggedTextColor
        {
            get => _BuggedTextColor;
            set
            {
                _BuggedTextColor = value;
                NotifyOfPropertyChange(() => BuggedTextColor);
            }
        }
        private bool _DarkMode = BslSettings.Instance.GeneralDarkMode;
        public bool DarkMode
        {
            get => _DarkMode;
            set
            {
                _DarkMode = value;
                BslSettings.Instance.GeneralDarkMode = value;
                this.IshellViewModel.SetTheme(value);
                NotifyOfPropertyChange(() => DarkMode);
            }
        }
        private bool _MenuWinTaskBar = BslSettings.Instance.GeneralTrayBar;
        public bool MenuWinTaskBar
        {
            get => _MenuWinTaskBar;
            set
            {
                this.IshellViewModel.WinTaskBar = value;
                _MenuWinTaskBar = value;
                NotifyOfPropertyChange(() => MenuWinTaskBar);
            }
        }
        private bool _ShellExtension = BslSettings.Instance.GeneralShellExtension;
        public bool ShellExtension
        {
            get => _ShellExtension;
            set
            {
                _ShellExtension = value;
                BslSettings.Instance.GeneralShellExtension = value;
                NotifyOfPropertyChange(() => ShellExtension);
                this.IshellViewModel.ShellExtInstall(value);
            }
        }
        private bool _ShellAssociation = BslSettings.Instance.GeneralShellAssociation;
        public bool ShellAssociation
        {
            get => _ShellAssociation;
            set
            {
                _ShellAssociation = value;
                BslSettings.Instance.GeneralShellAssociation = value;
                NotifyOfPropertyChange(() => ShellAssociation);
                this.IshellViewModel.ShellAssocInstall(value);
            }
        }
        private ShellModel.ShellDefaultAct _ShellDefaultAct = BslSettings.Instance.GeneralShellDefaultAct;
        public ShellModel.ShellDefaultAct ShellDefaultAct
        {
            get => _ShellDefaultAct;
            set
            {
                _ShellDefaultAct = value;
                BslSettings.Instance.GeneralShellDefaultAct = value;
                NotifyOfPropertyChange(() => ShellDefaultAct);
            }
        }
        #endregion

        #region Public Interface
        public bool IsDarkMode()
        {
            return this.DarkMode;
        }
        public bool IsInProgress()
        {
            return this.InProgress;
        }
        public void RefreshBuggedBg(bool darkMode)
        {
            if (darkMode)
            {
                this.BuggedBorderColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#252525"));
                this.BuggedTextColor = Brushes.White;
            }           
            else
            {
                this.BuggedBorderColor = Brushes.White;
                this.BuggedTextColor = Brushes.Black;
            }          
        }
        public void ShellIntegrated()
        {
            this.ShellAssociation = true;
            this.ShellExtension = true;
        }
        #endregion

        #region Actions
        public void TabSelect(int index)
        {
            IshellViewModel.ShowWindow();
            this.TabSelectedIndex = index;
        }
        public void TabUpdateHeader()
        {
            this.TabSelectedHeader = this.Tabs[this.TabSelectedIndex].Header.ToString();
        }
        public void MenuExit()
        {
            Application.Current.MainWindow.Close();
            Application.Current.Shutdown();
        }
        public void TrayDoubleClick()
        {
            IshellViewModel.ShowWindow();
        }
        public void ResetSettings()
        {
            IshellViewModel.ResetSettings();
        }
        #endregion

        #region Private Methods
        private void OnInProgressChanged(object sender, EventArgs e)
        {
            if (((InProgressChangedArgs)e).InProgress)
            {
                this.InProgress = true;
                for (int i = 0; i < Tabs.Count; i++)
                {
                    if (i != this.TabSelectedIndex)
                        Tabs[i].IsEnabled = false;
                }
            }
            else
            {
                this.InProgress = false;
                foreach (var tab in Tabs)
                {
                    tab.IsEnabled = true;
                }
            }
        }
        #endregion
    }
}
