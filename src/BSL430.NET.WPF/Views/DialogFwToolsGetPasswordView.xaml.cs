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
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using BSL430_NET_WPF.ViewModels;
using MahApps.Metro.Controls.Dialogs;

namespace BSL430_NET_WPF.Views
{
    public partial class DialogFwToolsGetPasswordView : CustomDialog, IResource
    {
        private const string TOOLTIP_TXT = "Successfuly copied to clipboard.";

        public DialogFwToolsGetPasswordView()
        {
            InitializeComponent();
            Loaded += Dialog_Loaded;
        }
        private void Dialog_Loaded(Object sender, RoutedEventArgs e)
        {
            OK.Focus();
        }
        private void PasswordClipboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var tooltip = new ToolTip { Content = TOOLTIP_TXT };
                tooltip.Opened += async delegate (object o, RoutedEventArgs args)
                {
                    var s = o as ToolTip;
                    await Task.Delay(1000);
                    s.IsOpen = false;
                    await Task.Delay(1000);
                    s.Content = "";
                };
                Clipboard.SetText(Password.Text);
                Password.ToolTip = tooltip;
                tooltip.IsOpen = true;
                Password.Focus();
                Password.SelectAll();
            }
            catch(Exception) { }  
        }

        private void DialogFwToolsGetPasswordViewN_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                OK.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
