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
using System.IO;
using System.Threading.Tasks;
using System.Windows;

using Caliburn.Micro;
using MahApps.Metro.Controls.Dialogs;

using BSL430_NET;
using BSL430_NET.FirmwareTools;
using BSL430_NET_WPF.Views;
using BSL430_NET_WPF.Settings;
using BSL430_NET_WPF.Models;
using BSL430_NET_WPF.Helpers;

namespace BSL430_NET_WPF.ViewModels
{
    public interface IOnHexViewClose
    {
        void OnHexViewClose();
    }
    public interface IFwToolsViewModel
    {
        void OnWindowClosing();
    }
    public class TabFwToolsViewModel : Screen, IOnHexViewClose, IFwToolsViewModel
    {
        public const string FW_PATH_FILTER = "Firmware Files (*.hex *.txt *.out *.elf *.bin *.s *.srec *.s19 *.eep) " +
            "|*.hex; *.txt; *.out; *.elf; *.bin; *.s; *.srec; *.s19; *.eep|All Files(*.*) |*.*";

        #region Private data
        private readonly IDialogCoordinator coordinator;
        private readonly DialogFwToolsConvertView dialogConvert;
        private readonly DialogFwToolsCombineView dialogCombine;
        private readonly DialogFwToolsValidateView dialogValidate;
        private readonly DialogFwToolsGetPasswordView dialogGetPassword;
        private readonly MetroDialogSettings dialogSettings;
        private readonly HexViewModel hexViewModel;
        private readonly IThemeProvider themeProvider;
        private readonly Dictionary<string, string> args;
        private HexView hexView;
        private IHexView IhexView;
        private bool hexViewClosed = true;
        private bool argsHandled = false;

        private const string CONVERT_SUCCESS = "Firmware was successfuly converted and saved here:";
        private const string COMBINE_SUCCESS = "Firmware files were successfuly combined and saved here:";
        private const string ERR1 = "Firmware Path is missing or invalid!";
        private const string ERR2 = "Second Firmware Path is missing or invalid!";
        private const string ERR3 = "Destination Firmware Path is missing or invalid!";
        private const string ERR4 = "This firmware does not contain 16, 20 or 32 byte long BSL password.";
        #endregion

        #region Constructor
        public TabFwToolsViewModel(IDialogCoordinator _coordinator, 
                                   DialogFwToolsConvertView convertView, 
                                   DialogFwToolsCombineView combineView, 
                                   DialogFwToolsValidateView validateView, 
                                   DialogFwToolsGetPasswordView getpwView,
                                   IThemeProvider _themeProvider,
                                   HexViewModel hex_vm,
                                   Dictionary<string, string> _args)
        {
            this.dialogSettings = new MetroDialogSettings() { AnimateShow = false, AnimateHide = false };
            this.coordinator = _coordinator;
            this.dialogConvert = convertView;
            this.dialogCombine = combineView;
            this.dialogValidate = validateView;
            this.dialogGetPassword = getpwView;
            this.dialogConvert.DataContext = this;
            this.dialogCombine.DataContext = this;
            this.dialogValidate.DataContext = this;
            this.dialogGetPassword.DataContext = this;
            this.args = _args;
            this.themeProvider = _themeProvider;
            this.themeProvider.ThemeChanged += OnThemeChanged;
            this.hexViewModel = hex_vm;
        }
        #endregion

        #region Actions
        public void Browse()
        {
            string initPath = "";
            try
            {
                initPath = Path.GetDirectoryName(this.FwPath);
            }
            catch (Exception) { }

            var ret = Helpers.Dialogs.OpenFileDialog(initPath, "Select Firmware Path", true, "", FW_PATH_FILTER);
            if (ret != "")
                this.FwPath = ret;
        }
        public void ConvertSaveAs()
        {
            string initPath = "";
            try
            {
                initPath = Path.GetDirectoryName(this.FwPath);
            }
            catch (Exception) { }

            var ret = Helpers.Dialogs.OpenFileDialog(initPath, "Choose Destionation Firmware Path", false,
                                                     $"fw{this.ConvertFormat.ToExt()}", FW_PATH_FILTER);
            if (ret != "")
                this.ConvertDestination = ret;
        }
        public void CombineBrowse()
        {
            string initPath = "";
            try
            {
                initPath = Path.GetDirectoryName(this.CombineWith);
            }
            catch (Exception)
            {
                try
                {
                    initPath = Path.GetDirectoryName(this.FwPath);
                }
                catch (Exception) { }
            }

            var ret = Helpers.Dialogs.OpenFileDialog(initPath, "Select Second Firmware Path", true, "", FW_PATH_FILTER);
            if (ret != "")
                this.CombineWith = ret;
        }
        public void CombineSaveAs()
        {
            string initPath = "";
            try
            {
                initPath = Path.GetDirectoryName(this.FwPath);
            }
            catch (Exception) { }

            var ret = Helpers.Dialogs.OpenFileDialog(initPath, "Choose Destionation Firmware Path", 
                                                     false, $"fw{this.CombineFormat.ToExt()}", FW_PATH_FILTER);
            if (ret != "")
                this.CombineDestination = ret;
        }
        public async void ConvertDialog()
        {
            if (this.FwPath != "" && File.Exists(this.FwPath))
            {
                try
                {
                    string saveName = Path.GetFileNameWithoutExtension(this.FwPath);
                    saveName = $"{saveName}_converted_{DateTime.Now.ToString("yyyyMMddTHHmmss")}{this.ConvertFormat.ToExt()}";
                    this.ConvertDestination = Path.Combine(Path.GetDirectoryName(this.FwPath), saveName);
                }
                catch (Exception) { }

                this.IsDialogOpen = true;
                await coordinator.ShowMetroDialogAsync(this, dialogConvert, dialogSettings);
            }  
            else
            {
                MessageBox.Show(ERR1, "BSL430.NET", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            this.IsDialogOpen = false;
        }
        public async void CombineDialog()
        {
            if (this.FwPath != "" && File.Exists(this.FwPath))
            {
                try
                {
                    string saveName = Path.GetFileNameWithoutExtension(this.FwPath);
                    saveName = $"{saveName}_combined_{DateTime.Now.ToString("yyyyMMddTHHmmss")}{this.CombineFormat.ToExt()}";
                    this.CombineDestination = Path.Combine(Path.GetDirectoryName(this.FwPath), saveName);
                }
                catch (Exception) { }

                this.IsDialogOpen = true;
                await coordinator.ShowMetroDialogAsync(this, dialogCombine, dialogSettings);
            }    
            else
            {
                MessageBox.Show(ERR1, "BSL430.NET", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            this.IsDialogOpen = false;
        }
        public async void ValidateDialog()
        {
            if (this.FwPath != "" && File.Exists(this.FwPath))
            {
                try
                {
                    StringWriter sw = new StringWriter();
                    this.ValidateData = FwTools.Validate(this.FwPath, sw);
                    this.FwParseLog = sw.ToString();
                    this.FwName = Path.GetFileName(this.FwPath);
                }
                catch (Exception ex)
                {
                    this.ValidateData = new FwTools.FwInfo();
                    this.FwParseLog = ex.GetExceptionMsg();
                }

                this.IsDialogOpen = true;
                await coordinator.ShowMetroDialogAsync(this, dialogValidate, dialogSettings);
            }  
            else
            {
                MessageBox.Show(ERR1, "BSL430.NET", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            this.IsDialogOpen = false;
        }
        public void HexView()
        {
            if (this.FwPath != "" && File.Exists(this.FwPath))
            {
                try
                {
                    FwTools.Firmware fw = null;
                    if (hexViewClosed)
                    {
                        fw = FwTools.Parse(this.FwPath, FillFF: true);
                    }

                    if (this.hexView == null)
                    {
                        this.hexView = new HexView((IOnHexViewClose)this, themeProvider.IsDarkMode())
                        {
                            DataContext = hexViewModel
                        };
                    }

                    if (hexViewClosed)
                    {
                        this.hexViewModel.FwPath = this.FwPath;
                        this.hexViewModel.FwInfo = fw.Info;
                        if (fw.Info != null && fw.Info.Format == FwTools.FwFormat.ELF)
                            this.hexViewModel.ELF = true;
                        else
                            this.hexViewModel.ELF = false;

                        this.IhexView = this.hexView;
                        this.hexView.SetAddrOffset(fw.Info.AddrFirst);
                        this.hexViewModel.Iview = IhexView;

                        if (this.hexViewModel.StreamCache != null)
                            this.hexViewModel.StreamCache.Close();
                        this.hexViewModel.StreamCache = new MemoryStream();
                        fw.MemoryStream.CopyTo(hexViewModel.StreamCache);

                        if (this.hexView.HexEditor.Stream != null)
                            this.hexView.HexEditor.Stream.Close();
                        this.hexView.HexEditor.Stream = fw.MemoryStream;
                    }

                    this.IsDialogOpen = true;
                    this.hexViewClosed = false;
                    this.hexView.ShowDialog();
                    //this.hexView.Activate();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.GetExceptionMsg(), "BSL430.NET", MessageBoxButton.OK, MessageBoxImage.Error);           
                }
            }
            else
            {
                MessageBox.Show(ERR1, "BSL430.NET", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            this.IsDialogOpen = false;
        }
        public async void GetPasswordDialog()
        {
            if (this.FwPath != "" && File.Exists(this.FwPath))
            {
                try
                {
                    var password = FwTools.GetPassword(this.FwPath);
                    if (password == null)
                    {
                        MessageBox.Show(ERR4, "BSL430.NET", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {                        
                        if (this.PasswordMCU == MCU.MSP430_F1xx ||
                            this.PasswordMCU == MCU.MSP430_F2xx ||
                            this.PasswordMCU == MCU.MSP430_F4xx ||
                            this.PasswordMCU == MCU.MSP430_G2xx3)
                        {
                            this.PasswordHeader = "20-byte old 1xx/2xx/4xx BSL password";
                            this.PasswordData = BitConverter.ToString(password.Password20Byte).Replace("-", "");
                        }
                        else if (this.PasswordMCU == MCU.MSP430_F543x_NON_A)
                        {
                            this.PasswordHeader = "16-byte special F543x (non A) BSL password";
                            this.PasswordData = BitConverter.ToString(password.Password16Byte).Replace("-", "");
                        }
                        else
                        {
                            this.PasswordHeader = "32-byte standard 5xx/6xx BSL password";
                            this.PasswordData = BitConverter.ToString(password.Password32Byte).Replace("-", "");
                        }                
                        this.IsDialogOpen = true;
                        await coordinator.ShowMetroDialogAsync(this, dialogGetPassword, dialogSettings);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.GetExceptionMsg(), "BSL430.NET", MessageBoxButton.OK, MessageBoxImage.Error);
                }  
            }
            else
            {
                MessageBox.Show(ERR1, "BSL430.NET", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            this.IsDialogOpen = false;
        }
        public void Convert()
        {
            if (this.ConvertDestination == "" || !Directory.Exists(Path.GetDirectoryName(this.ConvertDestination)))
            {
                MessageBox.Show(ERR3, "BSL430.NET", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var (Fw, Format) = FwTools.Convert(this.FwPath, 
                                                   this.ConvertFormat, 
                                                   this.ConvertFillFF, 
                                                   BslSettings.Instance.FwWriteLineLength);

                using (StreamWriter wr = new StreamWriter(this.ConvertDestination, false))
                {
                    wr.Write(Fw);
                }
                MessageBox.Show($"{CONVERT_SUCCESS}\n{this.ConvertDestination}", "BS430.NET", MessageBoxButton.OK, MessageBoxImage.Information);
                coordinator.HideMetroDialogAsync(this, dialogConvert, dialogSettings);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetExceptionMsg(), "BSL430.NET", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
        public void Combine()
        {
            string err = "";

            if (this.CombineWith == "" || !File.Exists(this.CombineWith))
                err += $"{ERR2}\n";

            if (this.CombineDestination == "" || !Directory.Exists(Path.GetDirectoryName(this.CombineDestination)))
                err += $"{ERR3}\n";

            if (err != "")
            {
                MessageBox.Show(err.TrimEnd('\n'), "BS430.NET", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }               

            try
            {
                var (Fw, Format1, Format2) = FwTools.Combine(this.FwPath, 
                                                             this.CombineWith, 
                                                             this.CombineFormat, 
                                                             this.CombineFillFF, 
                                                             BslSettings.Instance.FwWriteLineLength);
                using (StreamWriter wr = new StreamWriter(this.CombineDestination, false))
                {
                    wr.Write(Fw);
                }
                MessageBox.Show($"{COMBINE_SUCCESS}\n{this.CombineDestination}", "BS430.NET", MessageBoxButton.OK, MessageBoxImage.Information);
                coordinator.HideMetroDialogAsync(this, dialogCombine, dialogSettings);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetExceptionMsg(), "BSL430.NET", MessageBoxButton.OK, MessageBoxImage.Error);
            }            
        }
        public void ConvertCancel()
        {
            coordinator.HideMetroDialogAsync(this, dialogConvert, dialogSettings);
        }
        public void CombineCancel()
        {
            coordinator.HideMetroDialogAsync(this, dialogCombine, dialogSettings);
        }
        public void ValidateClose()
        {
            coordinator.HideMetroDialogAsync(this, dialogValidate, dialogSettings);
        }
        public void GetPasswordClose()
        {
            coordinator.HideMetroDialogAsync(this, dialogGetPassword, dialogSettings);
        }
        public void Drop(DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files != null && files.Length > 0)
                        this.FwPath = files[0];
                }
            }
            catch (Exception) { }
        }
        public void PreviewDragOver(DragEventArgs e)
        {
            e.Handled = true;
        }
        public void Loaded()
        {
            if (this.argsHandled)
            {
                this.argsHandled = true;
                HandleArgs(this.args);
            }
        }
        public void DisplayParseLog()
        {
            MessageBox.Show(this.FwParseLog, "BSL430.NET", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        public void Compare()
        {
            string initPath = "";
            try
            {
                initPath = Path.GetDirectoryName(this.FwPath);
            }
            catch (Exception) { }

            var ret = Helpers.Dialogs.OpenFileDialog(initPath, "Select Second Firmware Path", true, "", FW_PATH_FILTER);
            if (ret != "")
            {
                try
                {
                    var (Equal, Match, BytesDiff) = FwTools.Compare(this.FwPath, ret);
                    MessageBox.Show($"Firmwares are {((Equal) ? "" : "NOT")} equal!\n\n" +
                                    $"Match:       {(Match * 100.0):F1} %\nBytes Diff:  {BytesDiff}", "BSL430.NET",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.GetExceptionMsg(), "BSL430.NET", MessageBoxButton.OK, MessageBoxImage.Error);
                }         
            }
        }
        #endregion

        #region Public Interface
        public void OnHexViewClose()
        {
            this.hexViewClosed = true;
        }
        public void OnWindowClosing()
        {
            if (this.hexView != null)
            {
                this.hexViewModel.ForceClose = true;
                this.hexView.Close();
            }              
        }
        #endregion

        #region Properties
        public bool IsDialogOpen { get; set; } = false;

        private string _FwParseLog = "";
        public string FwParseLog
        {
            get => _FwParseLog;
            set
            {
                _FwParseLog = value;
                NotifyOfPropertyChange(() => FwParseLog);
            }
        }
        private string _FwName;
        public string FwName
        {
            get => _FwName;
            set
            {
                _FwName = value;
                NotifyOfPropertyChange(() => FwName);
            }
        }
        private string _FwPath = BslSettings.Instance.FwToolsFwPath;
        public string FwPath
        {
            get => _FwPath;
            set
            {
                _FwPath = value;
                BslSettings.Instance.FwToolsFwPath = value;
                NotifyOfPropertyChange(() => FwPath);
            }
        }
        private FwTools.FwFormat _ConvertFormat = BslSettings.Instance.FwToolsConvertFormat;
        public FwTools.FwFormat ConvertFormat
        {
            get => _ConvertFormat;
            set
            {
                _ConvertFormat = value;
                BslSettings.Instance.FwToolsConvertFormat = value;
                NotifyOfPropertyChange(() => ConvertFormat);
            }
        }
        private FwTools.FwFormat _CombineFormat = BslSettings.Instance.FwToolsCombineFormat;
        public FwTools.FwFormat CombineFormat
        {
            get => _CombineFormat;
            set
            {
                _CombineFormat = value;
                BslSettings.Instance.FwToolsCombineFormat = value;
                NotifyOfPropertyChange(() => CombineFormat);
            }
        }
        private string _ConvertDestination = "";
        public string ConvertDestination
        {
            get => _ConvertDestination;
            set
            {
                _ConvertDestination = value;
                NotifyOfPropertyChange(() => ConvertDestination);
            }
        }
        private string _CombineWith = BslSettings.Instance.FwToolsCombineWith;
        public string CombineWith
        {
            get => _CombineWith;
            set
            {
                _CombineWith = value;
                BslSettings.Instance.FwToolsCombineWith = value;
                NotifyOfPropertyChange(() => CombineWith);
            }
        }
        private string _CombineDestination = "";
        public string CombineDestination
        {
            get => _CombineDestination;
            set
            {
                _CombineDestination = value;
                NotifyOfPropertyChange(() => CombineDestination);
            }
        }
        private bool _ConvertFillFF = BslSettings.Instance.FwToolsConvertFillFF;
        public bool ConvertFillFF
        {
            get => _ConvertFillFF;
            set
            {
                _ConvertFillFF = value;
                BslSettings.Instance.FwToolsConvertFillFF = value;
                NotifyOfPropertyChange(() => ConvertFillFF);
            }
        }
        private bool _CombineFillFF = BslSettings.Instance.FwToolsCombineFillFF;
        public bool CombineFillFF
        {
            get => _CombineFillFF;
            set
            {
                _CombineFillFF = value;
                BslSettings.Instance.FwToolsCombineFillFF = value;
                NotifyOfPropertyChange(() => CombineFillFF);
            }
        }
        private FwTools.FwInfo _ValidateData;
        public FwTools.FwInfo ValidateData
        {
            get => _ValidateData;
            set
            {
                _ValidateData = value;
                NotifyOfPropertyChange(() => ValidateData);
            }
        }
        private string _PasswordData = "";
        public string PasswordData
        {
            get => _PasswordData;
            set
            {
                _PasswordData = value;
                NotifyOfPropertyChange(() => PasswordData);
            }
        }
        private string _PasswordHeader = "";
        public string PasswordHeader
        {
            get => _PasswordHeader;
            set
            {
                _PasswordHeader = value;
                NotifyOfPropertyChange(() => PasswordHeader);
            }
        }
        private MCU _PasswordMCU = BslSettings.Instance.FwToolsPasswordMCU;
        public MCU PasswordMCU
        {
            get => _PasswordMCU;
            set
            {
                _PasswordMCU = value;
                BslSettings.Instance.FwToolsPasswordMCU = value;
                NotifyOfPropertyChange(() => PasswordMCU);
            }
        }
        #endregion

        #region Private Methods
        private void OnThemeChanged(object sender, EventArgs e)
        {
            if (hexView != null)
                hexView.SetTheme(((ThemeChangedArgs)e).IsDarkMode);
        }
        private async void HandleArgs(Dictionary<string, string> args)
        {
            if (args == null)
                return;

            args.TryGetValue(ShellModel.ARG_KEY_FILE, out string file);
            args.TryGetValue(ShellModel.ARG_KEY_FORMAT, out string format);
            Enum.TryParse(format, true, out FwTools.FwFormat fwFormat);

            if (args.TryGetValue(ShellModel.ARG_KEY_ACTION, out string act))
            {
                if (file != null && file != "")
                {
                    if (act == ShellModel.ARG_VAL_ACT_CONVERT)
                    {
                        this.FwPath = file;
                        this.ConvertFormat = fwFormat;
                        ConvertDialog();
                    }
                    else if (act == ShellModel.ARG_VAL_ACT_COMBINE)
                    {
                        this.FwPath = file;
                        this.CombineFormat = fwFormat;
                        CombineDialog();
                    }
                    else if (act == ShellModel.ARG_VAL_ACT_VALIDATE)
                    {
                        this.FwPath = file;
                        ValidateDialog();
                    }
                    else if (act == ShellModel.ARG_VAL_ACT_HEXEDIT)
                    {
                        this.FwPath = file;
                        await Task.Delay(1);
                        HexView();
                    }
                    else if (act == ShellModel.ARG_VAL_ACT_GETPASSWORD)
                    {
                        this.FwPath = file;
                        GetPasswordDialog();
                    }
                }
            }
        }
        #endregion
    }
}
