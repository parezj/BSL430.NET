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
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using Caliburn.Micro;

namespace BSL430_NET_WPF.ViewModels
{
    public class LogViewModel : Screen
    {
        public ShellViewModel ShellViewModel { get; private set; }

        public LogViewModel(ShellViewModel _shellViewModel)
        {
            ShellViewModel = _shellViewModel;
        }

        #region Properties
        private string _LogPath = "";
        public string LogPath
        {
            get => _LogPath;
            set
            {
                _LogPath = value;
                NotifyOfPropertyChange(() => LogPath);
            }
        }
        private string _LogData = "";
        public string LogData
        {
            get => _LogData;
            set
            {
                _LogData = value;
                NotifyOfPropertyChange(() => LogData);
            }
        }
        #endregion
    }
}
