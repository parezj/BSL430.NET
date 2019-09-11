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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

using BSL430_NET.FirmwareTools;
using BSL430_NET_WPF.Converters;
using BSL430_NET_WPF.Settings;
using Caliburn.Micro;

namespace BSL430_NET_WPF.ViewModels
{
    public interface ITabDownloadViewModel
    {
        double RangeMaxK { get; set; }
    }

    public class TabDownloadViewModel : Screen, IControlProccessActions, ITabDownloadViewModel
    {
        public ControlProcessViewModel ControlProcess { get; private set; }

        private const string ERR1 = "Password must be either 0 (auto erase all first) or with valid lenght!\n\n" +
                                    "Password is last 16-byte (F543x-non-A only) or 32-byte (others) of IVT (FFE0-FFFF), " +
                                    "if newer 5xx/6xx MCU is used. If MCU from older series is used (1xx/2xx/4xx), " +
                                    "password is exactly 20-byte long. Mostly it is 32-byte, and if you enter blank or " +
                                    "incorrect password, memory will be auto-erased as a security measure!\n\nUse Firmware Tools.\n";
        private const string ERR2 = "Destination Firmware Path is missing or invalid!";
        private const string ERR3 = "Byte Size must be a positive number!";

        //private double RANGE_MAX_KiB = (double)BslSettings.Instance.DownloadSizeMax;

        public TabDownloadViewModel(ControlProcessViewModel _ctrlprc_vm)
        {
            this.ControlProcess = _ctrlprc_vm;
            this.ControlProcess.FwPathDownload = this.FwPath;
            this.ControlProcess.StartAddress = (int)this.StartAddress;
            this.ControlProcess.OutputFormat = this.OutputFormat;
            this.ControlProcess.ByteSize = (this.SizeInDecimal) ? (int)this.ByteSize : (int)this.ByteSize * 1024;
            this.RangeUpperValue = (double)(this.ByteSize + this.StartAddress);
            this.ByteSizeMaximum = (this.SizeInDecimal) ? this.RangeMaxD : this.RangeMaxK;
            this.RangeMaxK = (double)BslSettings.Instance.DownloadSizeRange;
        }

        #region Public Interface
        public void Scan()
        {
            this.ControlProcess?.Scan();
        }
        public void StartStop()
        {
            string err = "";

            if (!this.ControlProcess.ValidateBslPassword(this.ControlProcess.Password.Length / 2, this.ControlProcess.MCU))
            {
                err += $"{ERR1}\n";
            }

            if (this.FwPath == "" || !Directory.Exists(Path.GetDirectoryName(this.FwPath)))
                err += $"{ERR2}\n";

            if (this.ByteSize < 1)
                err += $"{ERR3}\n";

            if (err != "")
            {
                MessageBox.Show(err.TrimEnd('\n'), "BSL430.NET", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (this.FirstDownload)
            {
                var result = MessageBox.Show("As this is your first download, you must known, that incorrectly entered password is " +
                    "considered as attack and so it is handled with.\n\nBSL versions 2.0 and higher with default settings executes " +
                    "Mass Erase command after single wrong password attempt as a security measure, so entire memory is erased except " +
                    "Info A.\n\nDo you still want to continue?", "BSL430.NET", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    this.FirstDownload = false;
                }
                else
                {
                    return;
                }
            }

            this.ControlProcess?.StartStop();
        }
        public void OpenLog()
        {
            this.ControlProcess?.OpenLog();
        }
        #endregion

        #region Actions
        public void SaveAs()
        {
            string initpath = "";
            try
            {
                initpath = Path.GetDirectoryName(this.FwPath);
            }
            catch (Exception)
            {
            }

            var ret = Helpers.Dialogs.OpenFileDialog(initpath, "Select Destination Firmware Path", false, 
                                                     $"fw{this.OutputFormat.ToExt()}", TabFwToolsViewModel.FW_PATH_FILTER);
            if (ret != "")
                this.FwPath = ret;
        }
        #endregion

        #region Properties
        private bool _FirstDownload = BslSettings.Instance.DownloadFirst;
        public bool FirstDownload
        {
            get => _FirstDownload;
            set
            {
                _FirstDownload = value;
                BslSettings.Instance.DownloadFirst = value;
            }
        }
        private double _RangeMaxD;
        public double RangeMaxD
        {
            get => _RangeMaxD;
            set
            {
                _RangeMaxD = value;
                NotifyOfPropertyChange(() => RangeMaxD);
            }
        }
        private double _RangeMaxK;
        public double RangeMaxK
        {
            get => _RangeMaxK;
            set
            {
                _RangeMaxK = value;
                RangeMaxD = value * 1024;

                this.RangeUpperValue = (double)(this.ByteSize + this.StartAddress);
                this.ByteSizeMaximum = (this.SizeInDecimal) ? this.RangeMaxD : this._RangeMaxK;
                this.StartAddress = this.StartAddress;
                this.ByteSize = this.ByteSize;
                NotifyOfPropertyChange(() => RangeMaxK);
                NotifyOfPropertyChange(() => RangeMaxD);
            }
        }
        private string _FwPath = BslSettings.Instance.DownloadFwPath;
        public string FwPath
        {
            get => _FwPath;
            set
            {
                _FwPath = value;
                BslSettings.Instance.DownloadFwPath = value;
                this.ControlProcess.FwPathDownload = this.FwPath;
                NotifyOfPropertyChange(() => FwPath);
            }
        }
        private double _StartAddress = (double)BslSettings.Instance.DownloadStartAddress;
        public double StartAddress
        {
            get => _StartAddress;
            set
            {
                _StartAddress = value;
                BslSettings.Instance.DownloadStartAddress = (int)value;
                this.ByteSize = this.RangeUpperValue - value;
                this.ByteSizeMaximum = (this.SizeInDecimal) ? this.RangeMaxD - this.StartAddress : this.RangeMaxK - this.StartAddress;
                this.ControlProcess.StartAddress = (this.SizeInDecimal) ? (int)this.StartAddress : (int)this.StartAddress * 1024;

                this.AddrLabelStartVal = this.ControlProcess.StartAddress;
                this.AddrLabelStart = (int)value;
                this.AddrLabelEnd = (int)this.RangeUpperValue;
                this.AddrLabelEndVal = this.ControlProcess.StartAddress + this.ControlProcess.ByteSize;

                NotifyOfPropertyChange(() => StartAddress);
            }
        }
        private double _ByteSize = (double)BslSettings.Instance.DownloadByteSize;
        public double ByteSize
        {
            get => _ByteSize;
            set
            {
                _ByteSize = value;
                BslSettings.Instance.DownloadByteSize = (int)value;
                this._RangeUpperValue = value + this.StartAddress;          
                this.ControlProcess.ByteSize = (this.SizeInDecimal) ? (int)this.ByteSize : (int)this.ByteSize * 1024;

                this.AddrLabelEndVal = this.ControlProcess.StartAddress + this.ControlProcess.ByteSize;
                this.AddrLabelEnd = (int)this._RangeUpperValue;

                NotifyOfPropertyChange(() => RangeUpperValue);
                NotifyOfPropertyChange(() => ByteSize);
            }
        }
        private bool _SizeInDecimal = BslSettings.Instance.DownloadSizeInDecimal;
        public bool SizeInDecimal
        {
            get => _SizeInDecimal;
            set
            {
                if (_SizeInDecimal == false && value == true)
                {
                    _SizeInDecimal = value;
                    this._RangeUpperValue *= 1024;
                    this._ByteSize *= 1024;
                    this.StartAddress *= 1024;

                }
                else if (_SizeInDecimal == true && value == false)
                {
                    _SizeInDecimal = value;
                    this._RangeUpperValue /= 1024;
                    this._ByteSize /= 1024;
                    this.StartAddress /= 1024;

                    if (this.ByteSize < 1)
                        this.ByteSize = 1;
                }
                BslSettings.Instance.DownloadSizeInDecimal = value;
                NotifyOfPropertyChange(() => ByteSizeMaximum);
                NotifyOfPropertyChange(() => SizeInDecimal);
                NotifyOfPropertyChange(() => RangeUpperValue);
                NotifyOfPropertyChange(() => ByteSize);
            }
        }
        private double _RangeUpperValue;
        public double RangeUpperValue
        {
            get => _RangeUpperValue;
            set
            {
                _RangeUpperValue = value;
                this.ByteSize = value - this.StartAddress;
                BslSettings.Instance.DownloadByteSize = (int)this.ByteSize;
                NotifyOfPropertyChange(() => ByteSize);
                NotifyOfPropertyChange(() => RangeUpperValue);
            }
        }
        private double _ByteSizeMaximum;
        public double ByteSizeMaximum
        {
            get
            {
                return (this.SizeInDecimal) ? this.RangeMaxD - this.StartAddress : this.RangeMaxK - this.StartAddress;
            }
            set
            {
                _ByteSizeMaximum = value;
                NotifyOfPropertyChange(() => ByteSizeMaximum);
            }
        }
        private BSL430_NET.FirmwareTools.FwTools.FwFormat _OutputFormat = BslSettings.Instance.DownloadOutputFormat;
        public BSL430_NET.FirmwareTools.FwTools.FwFormat OutputFormat
        {
            get => _OutputFormat;
            set
            {
                _OutputFormat = value;
                BslSettings.Instance.DownloadOutputFormat = value;
                this.ControlProcess.OutputFormat = this.OutputFormat;
                NotifyOfPropertyChange(() => OutputFormat);
            }
        }
        public int RangeSliderWidth { get; set; } = 540;

        private int _AddrLabelStart = 0;
        public int AddrLabelStart
        {
            get => _AddrLabelStart;
            set
            {
                double x = (double)value / ((this.SizeInDecimal) ? (int)this.RangeMaxD : (int)this.RangeMaxK); 
                _AddrLabelStart = (int)(x * RangeSliderWidth) + 49;
                NotifyOfPropertyChange(() => AddrLabelStart);
            }
        }
        private int _AddrLabelEnd = 0;
        public int AddrLabelEnd
        {
            get => _AddrLabelEnd;
            set
            {
                double x = (double)value / ((this.SizeInDecimal) ? (int)this.RangeMaxD : (int)this.RangeMaxK);
                _AddrLabelEnd = (int)((x * RangeSliderWidth) - this.AddrLabelStart) + 30;
                NotifyOfPropertyChange(() => AddrLabelEnd);
            }
        }
        private int _AddrLabelStartVal = 0;
        public int AddrLabelStartVal
        {
            get => _AddrLabelStartVal;
            set
            {
                _AddrLabelStartVal = value;
                NotifyOfPropertyChange(() => AddrLabelStartVal);
            }
        }
        private int _AddrLabelEndVal = 0;
        public int AddrLabelEndVal
        {
            get => _AddrLabelEndVal;
            set
            {
                _AddrLabelEndVal = value - 1;
                NotifyOfPropertyChange(() => AddrLabelEndVal);
            }
        }
        #endregion
    }
}
