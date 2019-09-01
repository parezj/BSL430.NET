﻿/*
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
using System.Windows.Shapes;
using MahApps.Metro.Controls;

using BSL430_NET_WPF.ViewModels;
using System.IO;
using System.Reflection;

namespace BSL430_NET_WPF.Views
{
    public partial class LogView : MetroWindow
    {
        private IOnLogClose callback;
        private LogViewModel logViewModel;


        private readonly string xmlDarkUri = "BSL430_NET_WPF.Resources.XML_dark.xshd";
        private readonly string xmlLightUri = "BSL430_NET_WPF.Resources.XML_light.xshd";

        public LogView(IOnLogClose _callback, LogViewModel _logViewModel, bool darkMode)
        {
            this.callback = _callback;
            this.logViewModel = _logViewModel;
            InitializeComponent();


            using (Stream stream_d = Assembly.GetExecutingAssembly().GetManifestResourceStream(this.xmlDarkUri))
            using (Stream stream_l = Assembly.GetExecutingAssembly().GetManifestResourceStream(this.xmlLightUri))
            {
                using (var reader_d = new System.Xml.XmlTextReader(stream_d))
                using (var reader_l = new System.Xml.XmlTextReader(stream_l))
                {
                    ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.RegisterHighlighting("XML_DARK", new string[0],
                        ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(reader_d,
                            ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance));

                    ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.RegisterHighlighting("XML_LIGHT", new string[0],
                        ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(reader_l,
                            ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance));
                }
            }


            avalon.Text = logViewModel.LogData;
            if (logViewModel.LogData.Length > 0)
            {
                avalon.CaretOffset = logViewModel.LogData.Length - 1;
                avalon.TextArea.Caret.BringCaretToView();
                avalon.ScrollToEnd();
            }
            this.SetTheme(darkMode);
        }
        public void SetTheme(bool darkMode)
        {
            if (darkMode)
            {
                avalon.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("XML_DARK");
            }      
            else
            {
                avalon.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("XML_LIGHT");
            }       
        }
        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            callback.OnLogClose();
        }

        private void Avalon_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //WindowState = WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Do you really want to dump this log?", "BSL430.NET", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    this.logViewModel.LogData = "";
                    File.WriteAllText(this.logViewModel.LogPath, String.Empty);
                }
                catch (Exception) { }
                this.Close();
            }
        }
    }
}
