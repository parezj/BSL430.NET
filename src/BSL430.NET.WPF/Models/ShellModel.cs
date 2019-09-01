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
using BSL430_NET_WPF.ViewModels;
using BSL430_NET_WPF.Models;
using BSL430_NET_WPF.Helpers;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Caliburn.Micro;
using System.Diagnostics;

namespace BSL430_NET_WPF.Models
{
    public class ShellModel
    {
        #region Public Data
        public const string ARG_KEY_ACTION = "--action";
        public const string ARG_KEY_UNKNOWN = "--unknown";
        public const string ARG_KEY_RESET = "--reset";
        public const string ARG_KEY_FORMAT = "--format";
        public const string ARG_KEY_FILE = "--file";

        public const string ARG_VAL_ACT_UPLOAD = "upload";
        public const string ARG_VAL_ACT_CONVERT = "convert";
        public const string ARG_VAL_ACT_COMBINE = "combine";
        public const string ARG_VAL_ACT_VALIDATE = "validate";
        public const string ARG_VAL_ACT_HEXEDIT = "hexedit";
        public const string ARG_VAL_ACT_GETPASSWORD = "getpassword";
        
        public const string APPDATA_ICON_FILE = "_file.ico";
        public const string APPDATA_ICON_APP = "_app.ico";

        public const string SHELL_EXTENSION_NAME = "Open with BSL430.NET";

        public string AppDataShellPath = "";

        public enum ShellDefaultAct : int
        {
            UPLOAD      = 0,
            CONVERT     = 1,
            COMBINE     = 2,
            VALIDATE    = 3,
            HEXEDIT     = 4,
            GETPASSWORD = 5
        }
        #endregion

        #region Private Data
        private const string FW_FILE_ICO = "BSL430_NET_WPF.Resources.file.ico";
        private const string FW_LOGO_ICO = "BSL430_NET_WPF.Resources.logo2.ico";

        private readonly ShellViewModel viewModel;
        #endregion

        #region Constructor
        public ShellModel(ShellViewModel viewModel)
        {
            this.viewModel = viewModel;
        }
        #endregion

        #region Public Methods
        public bool IsWinDarkMode()
        {
            bool is_dark_mode = false;
            try
            {
                var v = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                    "AppsUseLightTheme", "1");
                if (v != null && v.ToString() == "0")
                    is_dark_mode = true;
            }
            catch { }
            return is_dark_mode;
        }
        public Dictionary<string, string> ParseArgs()
        {
            try
            {
                Dictionary<string, string> ret = new Dictionary<string, string>();
                string[] cmdArgs = Environment.GetCommandLineArgs();
                //MessageBox.Show(string.Join(Environment.NewLine, cmdArgs));
                int unknown = 0;
                for (int i = 1; i < cmdArgs.Length; i++)
                {
                    if(cmdArgs[i].StartsWith("--") || cmdArgs[i].StartsWith("-"))
                    {
                        if (cmdArgs.Length > i + 1 && (!cmdArgs[i + 1].StartsWith("--") && !cmdArgs[i + 1].StartsWith("-")))
                        {
                            ret.Add(cmdArgs[i], cmdArgs[i + 1].Trim('"').Trim('\''));
                            i++;
                        }
                        else
                        {
                            ret.Add(cmdArgs[i], "");
                        }
                    }
                    else
                    {
                        ret.Add($"{ARG_KEY_UNKNOWN}-{unknown}", cmdArgs[i].Trim('"').Trim('\''));
                        unknown++;
                    }
                }
                var lines = ret.Select(kvp => kvp.Key + ": " + kvp.Value.ToString());
                return ret;
            }
            catch (Exception)
            {
                return new Dictionary<string, string>();
            }
        }
        public Dictionary<string, string> HandleArgs(Dictionary<string, string> args)
        {
            if (args == null)
                return null;

            if (args.TryGetValue($"{ARG_KEY_UNKNOWN}-0", out string arg1))
            {
                try
                {
                    if (File.Exists(arg1))
                    {
                        args.Clear();
                        var def = BslSettings.Instance.GeneralShellDefaultAct;

                        if (def == ShellDefaultAct.UPLOAD)
                        {
                            args.Add(ARG_KEY_ACTION, ARG_VAL_ACT_UPLOAD);
                            args.Add(ARG_KEY_FILE, arg1);
                        }
                        else if (def == ShellDefaultAct.CONVERT)
                        {
                            args.Add(ARG_KEY_FORMAT, BslSettings.Instance.FwToolsConvertFormat.ToString());
                            args.Add(ARG_KEY_ACTION, ARG_VAL_ACT_CONVERT);
                            args.Add(ARG_KEY_FILE, arg1);
                        }
                        else if (def == ShellDefaultAct.COMBINE)
                        {
                            args.Add(ARG_KEY_FORMAT, BslSettings.Instance.FwToolsCombineFormat.ToString());
                            args.Add(ARG_KEY_ACTION, ARG_VAL_ACT_COMBINE);
                            args.Add(ARG_KEY_FILE, arg1);
                        }
                        else if (def == ShellDefaultAct.VALIDATE)
                        {
                            args.Add(ARG_KEY_ACTION, ARG_VAL_ACT_VALIDATE);
                            args.Add(ARG_KEY_FILE, arg1);
                        }
                        else if (def == ShellDefaultAct.HEXEDIT)
                        {
                            args.Add(ARG_KEY_ACTION, ARG_VAL_ACT_HEXEDIT);
                            args.Add(ARG_KEY_FILE, arg1);
                        }
                        else if (def == ShellDefaultAct.GETPASSWORD)
                        {
                            args.Add(ARG_KEY_ACTION, ARG_VAL_ACT_GETPASSWORD);
                            args.Add(ARG_KEY_FILE, arg1);
                        }
                    }
                }
                catch (Exception) { }
            }

            if (args.TryGetValue(ARG_KEY_ACTION, out string act))
            {
                if (act == ARG_VAL_ACT_UPLOAD)
                {
                    BslSettings.Instance.GeneralTabSelectedIndex = 0;
                }
                else if (act == ARG_VAL_ACT_CONVERT || act == ARG_VAL_ACT_COMBINE || act == ARG_VAL_ACT_VALIDATE ||
                         act == ARG_VAL_ACT_HEXEDIT || act == ARG_VAL_ACT_GETPASSWORD)
                {
                    BslSettings.Instance.GeneralTabSelectedIndex = 3;
                }
            }

            return args;
        }
        public void SelfCopyToAppLocal(bool forceOverwrite = false)
        {
            try
            {
                string from = System.Reflection.Assembly.GetExecutingAssembly().Location;
                FileInfo info = new FileInfo(from);
                string dest = Path.Combine(BslSettings.SettingsDir, ".shell", info.Name);
                DirectoryInfo dinfo = Directory.CreateDirectory(Path.GetDirectoryName(dest));
                dinfo.Attributes = FileAttributes.Directory | FileAttributes.Hidden;

                if (forceOverwrite || !File.Exists(dest) || (new FileInfo(dest))?.Length != info.Length)
                {
                    File.Copy(from, dest, true);
                }

                string iconPath = dest.Replace(".exe", APPDATA_ICON_FILE);
                using (Stream iconStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(FW_FILE_ICO))
                {
                    if (forceOverwrite || !File.Exists(iconPath) || (new FileInfo(iconPath))?.Length != iconStream.Length)
                    {
                        using (FileStream fileStream = File.Create(iconPath))
                        {
                            iconStream.Seek(0, SeekOrigin.Begin);
                            iconStream.CopyTo(fileStream);
                        }
                    }
                }

                string iconPath2 = dest.Replace(".exe", APPDATA_ICON_APP);
                using (Stream iconStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(FW_LOGO_ICO))
                {
                    if (forceOverwrite || !File.Exists(iconPath2) || (new FileInfo(iconPath2))?.Length != iconStream.Length)
                    {
                        using (FileStream fileStream = File.Create(iconPath2))
                        {
                            iconStream.Seek(0, SeekOrigin.Begin);
                            iconStream.CopyTo(fileStream);
                        }
                    }
                }

                this.AppDataShellPath = dest;
            }
            catch (Exception) { this.AppDataShellPath = ""; }
        }
        public void ShellExtInstall(bool val, string path)
        {
            try
            {
                if (path == "")
                    path = System.Reflection.Assembly.GetEntryAssembly().Location;

                string[] ext = new string[1] { "*" };
                string icon = path.Replace(".exe", APPDATA_ICON_APP);

                if (!val)
                {
                    if (ShellIntegration.IsExtension(ext, SHELL_EXTENSION_NAME, path))
                    {
                        if (Admin.ElevateAndIfNotRestart())
                        {
                            BslSettings.Instance.GeneralShellExtInstallForce = true;
                            Application.Current.Shutdown();
                        }
                        ShellIntegration.ExtensionsRemove(ext, SHELL_EXTENSION_NAME);
                    }
                }
                else
                {
                    if (!ShellIntegration.IsExtension(ext, SHELL_EXTENSION_NAME, path))
                    {
                        if (Admin.ElevateAndIfNotRestart())
                        {
                            BslSettings.Instance.GeneralShellExtInstallForce = true;
                            Application.Current.Shutdown();
                        }
                        ShellIntegration.ExtensionsRemove(ext, SHELL_EXTENSION_NAME);
                        ShellIntegration.ExtensionsAdd(ext, SHELL_EXTENSION_NAME, path, icon);
                    }
                }
            }
            catch (Exception) { }
        }
        public void ShellAssocInstall(bool val, string path)
        {
            try
            {
                if (path == "")
                    path = System.Reflection.Assembly.GetEntryAssembly().Location;

                string[] extension = new string[7] { ".hex", ".out", ".elf", ".s", ".srec", ".s19", ".eep" };
                string[] progID = new string[7] { "BSL430.NET.Firmware.IntelHEX", "BSL430.NET.Firmware.ELF", "BSL430.NET.Firmware.ELF",
                    "BSL430.NET.Firmware.SREC", "BSL430.NET.Firmware.SREC", "BSL430.NET.Firmware.SREC", "BSL430.NET.Firmware.EEPROM" };
                string[] desc = new string[7] { "Intel-HEX Firmware", "ELF Firmware", "ELF Firmware", "SREC Firmware",
                    "SREC Firmware", "SREC Firmware", "EEPROM File" };
                string icon = path.Replace(".exe", APPDATA_ICON_FILE);

                if (!val)
                {
                    if (ShellIntegration.IsAssociated(extension, progID, path))
                    {
                        if (Admin.ElevateAndIfNotRestart())
                        {
                            BslSettings.Instance.GeneralShellAssocInstallForce = true;
                            Application.Current.Shutdown();
                        }
                        ShellIntegration.Deassociate(extension, progID);
                    }
                }
                else
                {
                    if (!ShellIntegration.IsAssociated(extension, progID, path))
                    {
                        if (Admin.ElevateAndIfNotRestart())
                        {
                            BslSettings.Instance.GeneralShellAssocInstallForce = true;
                            Application.Current.Shutdown();
                        }
                        ShellIntegration.Deassociate(extension, progID);
                        ShellIntegration.Associate(extension, progID, desc, icon, path);
                    }
                }

                bool isWin10 = (ShellIntegration.CurrentOS().Contains("Windows 10"));
                // rebuild icon cache
                try
                {
                    using (Process process = new Process())
                    {
                        ProcessStartInfo info = new ProcessStartInfo
                        {
                            WindowStyle = ProcessWindowStyle.Hidden,
                            FileName = @"c:\windows\sysnative\ie4uinit.exe",
                            Arguments = (isWin10 ? "-show" : "-ClearIconCache")
                        };
                        process.StartInfo = info;
                        process.Start();
                    }
                }
                catch (Exception) { }
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke((System.Action)(() =>
                {
                    var result = MessageBox.Show($"Shell Association Failed!\n{ex.Message}", "BSL430.NET",
                        MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                }));
            }
        }
        public void AskForShellIntegration()
        {
            System.Windows.Application.Current.Dispatcher.Invoke((System.Action)(() =>
            {
                var result = MessageBox.Show("Welcome to BSL430.NET!\nTI MSP430 Bootloader Toolchain & Firmware Tools\n\nIt is a replacement for " +
                    "expensive original MSP FET programmer with cheap FTDI FT232 or Serial COM port. It can upload, download or erase MSP430 " +
                    "memory with minimal effort with generic UART converters. There are also Firmware Tools module, that can convert, combine, " +
                    "validate, hex edit or get password from TI-TXT, Intel-HEX, SREC or ELF formatted firmware files.\n\nWould you like to integrate " +
                    "BSL430.NET with Windows Shell? You can undo anytime later at settings. They can be shown by right clicking at tray bar icon.",
                    "BSL430.NET", MessageBoxButton.YesNo, MessageBoxImage.Information, MessageBoxResult.Yes);

                if (result == MessageBoxResult.Yes)
                {
                    ShellExtInstall(true, this.AppDataShellPath);
                    ShellAssocInstall(true, this.AppDataShellPath);
                    BslSettings.Instance.GeneralShellDefaultAct = ShellDefaultAct.VALIDATE;
                    BslSettings.Instance.GeneralShellAssociation = true;
                    BslSettings.Instance.GeneralShellExtension = true;
                    this.viewModel.ShellIntegrated();
                }
            }));
        }
        public void ResetSettings()
        {
            System.Windows.Application.Current.Dispatcher.Invoke((System.Action)(() =>
            {
                var result = MessageBox.Show("Do you really want to reset user settings to defaults?", "BSL430.NET",
                    MessageBoxButton.YesNo, MessageBoxImage.Information, MessageBoxResult.No, MessageBoxOptions.DefaultDesktopOnly);

                if (result == MessageBoxResult.Yes)
                {
                    BslSettings.Instance.GeneralShellAssociation = false;
                    BslSettings.Instance.GeneralShellExtension = false;
                    BslSettings.Instance.GeneralShellAssocInstallForce = true;
                    BslSettings.Instance.GeneralShellExtInstallForce = true;

                    if (Admin.ElevateAndIfNotRestart("--reset", false))
                    {
                        Application.Current.Shutdown();
                    }
                    else if (Admin.Restart("--reset"))
                    {                    
                        Application.Current.Shutdown();
                    }
                }
            }));
        }
        #endregion
    }
}
