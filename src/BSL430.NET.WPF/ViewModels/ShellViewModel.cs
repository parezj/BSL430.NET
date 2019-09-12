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
using System.Windows.Controls;
using System.Reflection;
using System.Security.Policy;
using System.Security;
using System.Security.Permissions;
using System.IO;

using BSL430_NET_WPF.Settings;
using BSL430_NET_WPF.Views;
using BSL430_NET_WPF.Models;
using BSL430_NET_WPF.Helpers;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Caliburn.Micro;


namespace BSL430_NET_WPF.ViewModels
{
    #region Interfaces
    public interface IShellViewModel
    {
        void Activate(Screen screen);
        void SetTheme(bool dark);
        void ShowWindow();
        void ShellExtInstall(bool val);
        void ShellAssocInstall(bool val);
        void ResetSettings();
        bool WinTaskBar { get; set; }
    }
    public interface IControlProcessViewModel
    {
        void CurrentTabHeader(string header);
        void ResetState();
        event EventHandler InProgressChanged;
    }
    public interface IThemeProvider
    {
        bool IsDarkMode();
        event EventHandler ThemeChanged;
    }
    public class ThemeChangedArgs : EventArgs
    {
        public bool IsDarkMode;
    }
    public interface IControlProccessActions
    {
        void Scan();
        void StartStop();
        void OpenLog();
    }
    public delegate void AppReady();
    #endregion

    public class ShellViewModel : Conductor<IScreen>, IShellViewModel, IThemeProvider
    {
        #region Public Data
        public event AppReady AppReadyEvent;
        #endregion

        #region Private Data
        private readonly ShellModel model;
        private readonly IResource convert_v_res;
        private readonly IResource combine_v_res;
        private readonly IResource validate_v_res;
        private readonly IResource getpw_v_res;
        private readonly IMainViewModel Imain_vm;
        private readonly IFwToolsViewModel Ifwtools_vm;

        private readonly Uri themeDark = new Uri("pack://application:,,,/BSL430.NET.WPF;component/Styles/MainDark.xaml", 
            UriKind.RelativeOrAbsolute);
        private readonly Uri themeLight = new Uri("pack://application:,,,/BSL430.NET.WPF;component/Styles/MainLight.xaml", 
            UriKind.RelativeOrAbsolute);
        #endregion

        #region Main App Constructor
        public ShellViewModel()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(ErrHandler);

            #region Model
            this.model = new ShellModel(this);
            this.model.SelfCopyToAppLocal();
            #endregion

            #region Args
            var args = this.model.HandleArgs(this.model.ParseArgs());
            bool reset = false;
            if (args.ContainsKey($"{ShellModel.ARG_KEY_RESET}"))
                reset = true;
            #endregion

                #region Views
            var upload_v = new TabUploadView();
            var download_v = new TabDownloadView();
            var erase_v = new TabEraseView();
            var fwtools_v = new TabFwToolsView();
            var bsl430_v = new TabBSL430NETView();  

            var convert_v = new DialogFwToolsConvertView();
            var combine_v = new DialogFwToolsCombineView();
            var validate_v = new DialogFwToolsValidateView();
            var getpw_v = new DialogFwToolsGetPasswordView();

            this.convert_v_res = convert_v;
            this.combine_v_res = combine_v;
            this.validate_v_res = validate_v;
            this.getpw_v_res = getpw_v;
            #endregion

            #region ViewModels
            var log_vm = new LogViewModel(this);
            var hex_vm = new HexViewModel();

            var processctl_vm = new ControlProcessViewModel(log_vm, this);
            AppReadyEvent += processctl_vm.AppReadyHandler;

            var upload_vm = new TabUploadViewModel(processctl_vm, args);
            var download_vm = new TabDownloadViewModel(processctl_vm);
            var erase_vm = new TabEraseViewModel(processctl_vm);
            var ftwtools_vm = new TabFwToolsViewModel(DialogCoordinator.Instance, convert_v, 
                combine_v, validate_v, getpw_v, this, hex_vm, args);

            this.Ifwtools_vm = ftwtools_vm;
            var bsl430_vm = new TabBSL430NETViewModel();
            #endregion

            #region TabItems
            Style s = (Style)Application.Current.Resources["CustomMetroTabItem"];
            var tabs = new List<MetroTabItem>()
            {
                new MetroTabItem() { Content = upload_v,   Header = "Upload",     Tag = "Install",   Style=s, DataContext = upload_vm },
                new MetroTabItem() { Content = download_v, Header = "Download",   Tag = "Upload",    Style=s, DataContext = download_vm },
                new MetroTabItem() { Content = erase_v,    Header = "Erase",      Tag = "Uninstall", Style=s, DataContext = erase_vm } ,
                new MetroTabItem() { Content = fwtools_v,  Header = "FW Tools",   Tag = "Labflask",  Style=s, DataContext = ftwtools_vm } ,
                new MetroTabItem() { Content = bsl430_v,   Header = "BSL430.NET", Tag = "Help",      Style=s, DataContext = bsl430_vm }
            };
            #endregion

            #region Theme
            bool darkMode = BslSettings.Instance.GeneralDarkMode;
            bool wasFirstRun = false;
            if (this.FirstRun)
            {
                darkMode = this.model.IsWinDarkMode();
                BslSettings.Instance.GeneralDarkMode = darkMode;
                this.FirstRun = false;
                wasFirstRun = true;
            }
            this.SetTheme(darkMode);
            #endregion

            #region Shell Integration
            if (reset || BslSettings.Instance.GeneralShellAssocInstallForce)
            {
                BslSettings.Instance.GeneralShellAssocInstallForce = false;
                this.model.ShellAssocInstall(BslSettings.Instance.GeneralShellAssociation, this.model.AppDataShellPath);
            }
            if (reset || BslSettings.Instance.GeneralShellExtInstallForce)
            {
                BslSettings.Instance.GeneralShellExtInstallForce = false;
                this.model.ShellExtInstall(BslSettings.Instance.GeneralShellExtension, this.model.AppDataShellPath);
            }
            #endregion

            #region MainViewModel Activate
            var main_vm = new MainViewModel(this, tabs, processctl_vm, download_vm, darkMode); 
            Imain_vm = main_vm;
            this.ActivateItem(main_vm);
            #endregion

            ShowWindow();
            AppReadyEvent?.Invoke();
            if (wasFirstRun)
                Task.Delay(1000).ContinueWith(t => this.model.AskForShellIntegration());     
        }
        #endregion

        #region Actions
        public void WindowClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (Imain_vm != null && Imain_vm.IsInProgress())
            {
                var result = MessageBox.Show("Do you really want to abort current process?\n\nIt is not recommended to break firmware ops, " +
                    "as the device will most likely not work correctly! Moreover, aborting thread which uses unmanaged resources will surely " +
                    "cause memory and that could make system unstable.", "BSL430.NET", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                }
            }
            Ifwtools_vm.OnWindowClosing();
        }
        #endregion

        #region Properties
        private bool _FirstRun = BslSettings.Instance.GeneralFirstRun;
        public bool FirstRun
        {
            get => _FirstRun;
            set
            {
                _FirstRun = value;
                BslSettings.Instance.GeneralFirstRun = value;
                NotifyOfPropertyChange(() => FirstRun);
            }
        }

        private bool _WinTaskBar = BslSettings.Instance.GeneralTrayBar;
        public bool WinTaskBar
        {
            get => _WinTaskBar;
            set
            {
                _WinTaskBar = value;
                BslSettings.Instance.GeneralTrayBar = value;
                NotifyOfPropertyChange(() => WinTaskBar);
            }
        }
        private WindowState _WinState = WindowState.Normal;
        public WindowState WinState
        {
            get => _WinState;
            set
            {
                _WinState = value;
                NotifyOfPropertyChange(() => WinState);
            }
        }
        private Visibility _WinVisibility = Visibility.Visible;
        public Visibility WinVisibility
        {
            get => _WinVisibility;
            set
            {
                _WinVisibility = value;
                NotifyOfPropertyChange(() => WinVisibility);
            }
        }
        #endregion

        #region Public Interface
        public void Activate(Screen screen)
        {
            this.ActivateItem(screen);
        }
        public void SetTheme(bool dark)
        {
            this.SetThemeResources(dark);
        }
        public bool IsDarkMode()
        {
            return this.Imain_vm.IsDarkMode();
        }
        public void ShowWindow()
        {
            this.WinVisibility = Visibility.Visible;
            this.WinState = WindowState.Normal;

            try
            {
                Application.Current.MainWindow.Activate();
                Application.Current.MainWindow.Topmost = true;
                Application.Current.MainWindow.Topmost = false;
                //Application.Current.MainWindow.Focus();
            }
            catch (Exception) { }
        }
        public void ShellExtInstall(bool val)
        {
            this.model.SelfCopyToAppLocal(true);
            this.model.ShellExtInstall(val, this.model.AppDataShellPath);
        }
        public void ShellAssocInstall(bool val)
        {
            this.model.SelfCopyToAppLocal(true);
            this.model.ShellAssocInstall(val, this.model.AppDataShellPath);
        }
        public void ResetSettings()
        {
            this.model.ResetSettings();
        }
        public void ShellIntegrated()
        {
            this?.Imain_vm.ShellIntegrated();
        }
        #endregion

        #region Interface Events
        public event EventHandler ThemeChanged;
        private readonly object locq = new object();
        event EventHandler IThemeProvider.ThemeChanged
        {
            add
            {
                lock (locq)
                {
                    ThemeChanged += value;
                }
            }
            remove
            {
                lock (locq)
                {
                    ThemeChanged -= value;
                }
            }
        }
        #endregion

        #region Private functions
        private void SetThemeResources(bool dark)
        {
            if (dark)
            {
                Application.Current.Resources.MergedDictionaries[2].Source = themeDark;
            }
            else
            {
                Application.Current.Resources.MergedDictionaries[2].Source = themeLight;
            }

            convert_v_res.Resources = Application.Current.Resources;
            combine_v_res.Resources = Application.Current.Resources;
            validate_v_res.Resources = Application.Current.Resources;
            getpw_v_res.Resources = Application.Current.Resources;

            Imain_vm?.RefreshBuggedBg(dark);

            this.ThemeChanged?.Invoke(this, new ThemeChangedArgs { IsDarkMode = dark });
        }
        private void ErrHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            //MessageBox.Show($"{e.Message}\n\n{e.InnerException}\n\n{e.StackTrace}", "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            //MessageBox.Show($"{e.Message}\n\n{e.InnerException}", "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            MessageBox.Show($"{e.Message}", "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        #endregion
    }
}
