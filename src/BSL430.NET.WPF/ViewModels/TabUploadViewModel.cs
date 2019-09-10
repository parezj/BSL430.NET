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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using BSL430_NET_WPF.Models;
using BSL430_NET_WPF.Settings;
using Caliburn.Micro;

namespace BSL430_NET_WPF.ViewModels
{
    public class TabUploadViewModel : Screen, IControlProccessActions
    {
        public ControlProcessViewModel ControlProcess { get; private set; }

        private const string ERR1 = "Password must be either 0 (auto erase all first) or with valid lenght!\n\n" +
                                    "Password is last 16-byte (F543x-non-A only) or 32-byte (others) of IVT (FFE0-FFFF), " +
                                    "if newer 5xx/6xx MCU is used. If MCU from older series is used (1xx/2xx/4xx), " +
                                    "password is exactly 20-byte long. Mostly it is 32-byte.\n\nUse Firmware Tools.\n";
        private const string ERR2 = "Firmware Path is missing or invalid!";

        public TabUploadViewModel(ControlProcessViewModel _ctrlprc_vm, Dictionary<string, string> args)
        {
            this.ControlProcess = _ctrlprc_vm;
            this.ControlProcess.FwPathUpload = this.FwPath;
            HandleArgs(args);
        }

        #region Public Interface
        public void Scan()
        {
            this.ControlProcess?.Scan();
        }
        public void StartStop()
        {
            string err = "";

            if (this.ControlProcess.Password.Length != 0 && 
                !this.ControlProcess.ValidateBslPassword(this.ControlProcess.Password.Length/2, this.ControlProcess.MCU))
            {
                err += $"{ERR1}\n";
            }

            if (this.FwPath == "" || !File.Exists(this.FwPath))
                err += $"{ERR2}\n";

            if (err != "")
            {
                MessageBox.Show(err.TrimEnd('\n'), "BSL430.NET", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            else
            {
                this.ControlProcess?.StartStop();
            }              
        }
        public void OpenLog()
        {
            this.ControlProcess?.OpenLog();
        }
        #endregion

        #region Actions
        public void Browse()
        {
            string initpath = "";
            try
            {
                initpath = Path.GetDirectoryName(this.FwPath);
            }
            catch (Exception) { }

            var ret = Helpers.Dialogs.OpenFileDialog(initpath, "Select Firmware Path", true, "", TabFwToolsViewModel.FW_PATH_FILTER);
            if (ret != "")
                this.FwPath = ret;
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
        #endregion

        #region Properties
        private string _FwPath = BslSettings.Instance.UploadFwPath;
        public string FwPath
        {
            get => _FwPath;
            set
            {
                _FwPath = value;
                BslSettings.Instance.UploadFwPath = value;
                this.ControlProcess.FwPathUpload = this.FwPath;
                NotifyOfPropertyChange(() => FwPath);
            }
        }
        #endregion

        #region Private Methods
        private void HandleArgs(Dictionary<string, string> args)
        {
            if (args == null)
                return;

            if (args.TryGetValue(ShellModel.ARG_KEY_ACTION, out string act))
            {
                if (act == ShellModel.ARG_VAL_ACT_UPLOAD)
                {
                    if (args.TryGetValue(ShellModel.ARG_KEY_FILE, out string file))
                    {
                        this.FwPath = file;
                    }
                }
            }
        }
        #endregion
    }
}
