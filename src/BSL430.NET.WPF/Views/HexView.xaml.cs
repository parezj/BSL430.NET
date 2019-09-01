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
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using BSL430_NET_WPF.ViewModels;
using MahApps.Metro.Controls;
using WpfHexaEditor;

namespace BSL430_NET_WPF.Views
{
    public interface IHexView
    {
        WpfHexaEditor.HexEditor HexEditor { get; }
    }

    public partial class HexView : MetroWindow, IHexView
    {
        private readonly IOnHexViewClose callback;

        HexEditor IHexView.HexEditor { get => HexEditor; }

        public HexView(IOnHexViewClose _callback, bool darkMode, int addrOffset = 0)
        {
            this.callback = _callback;
            InitializeComponent();
            SetTheme(darkMode);
            SetAddrOffset(addrOffset);
        }
        public void SetAddrOffset(long addrOffset)
        {
            HexEditor.AddressOffset = addrOffset;
        }
        public void SetTheme(bool darkMode)
        {
            if (darkMode)
            {
                HexEditor.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#252525"));
                HexEditor.Foreground = Brushes.White;
                HexEditor.ForegroundOffSetHeaderColor = Brushes.OrangeRed;
                HexEditor.ForegroundHighLightOffSetHeaderColor = Brushes.Coral;
                HexEditor.ForegroundSecondColor = Brushes.MediumTurquoise;
            }
            else
            {
                HexEditor.Background = Brushes.White;
                HexEditor.Foreground = Brushes.Black;
                HexEditor.ForegroundOffSetHeaderColor = Brushes.OrangeRed;
                HexEditor.ForegroundHighLightOffSetHeaderColor = Brushes.DarkRed;
                HexEditor.ForegroundSecondColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#008787"));
            }
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            callback.OnHexViewClose();
        }
    }
}
