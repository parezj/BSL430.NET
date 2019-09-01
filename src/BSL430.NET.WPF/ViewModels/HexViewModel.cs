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
using BSL430_NET_WPF.Settings;
using BSL430_NET_WPF.Views;
using Caliburn.Micro;

namespace BSL430_NET_WPF.ViewModels
{
    public class HexViewModel : Screen
    {
        public Stream StreamCache { get; set; } = new MemoryStream();
        public HexViewModel()
        {
        }

        #region Actions
        public void Save()
        {
            if (!this.ELF)
            {
                using (MemoryStream stream = this.Iview.HexEditor.SubmitChanges())
                {
                    var result = MessageBox.Show("Firmware file will be overwritten. Do you want to proceed?", "BSL430.NET",
                        MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        SaveFile(stream);

                        this.StreamCache.Close();
                        this.StreamCache = new MemoryStream();
                        stream.Position = 0;
                        stream.CopyTo(this.StreamCache);
                    }
                }
            }
            else
            {
                MessageBox.Show("Saving in ELF format is currently not supported.", "BSL430.NET",
                       MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public void WindowClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (this.ForceClose)
                return;

            using (MemoryStream stream = Iview.HexEditor.SubmitChanges())
            {
                if (stream != null && CompareStreams(this.StreamCache, stream) == false)
                {
                    if (!this.ELF)
                    {
                        var result = MessageBox.Show("There are unsaved changes. Do you you want to save this file before quit?", "BSL430.NET",
                            MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

                        if (result == MessageBoxResult.Yes)
                        {
                            SaveFile(stream);
                        }
                        else if (result == MessageBoxResult.Cancel)
                        {
                            e.Cancel = true;
                            return;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Any change will be lost. Saving in ELF format is currently not supported.", "BSL430.NET",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            
            if (!this.ForceClose)
            {
                e.Cancel = true;
                this.Visibility = Visibility.Hidden;
            }
        }
        #endregion

        #region Properties

        public bool ELF { get; set; } = false;
        public bool ForceClose { get; set; } = false;
        public IHexView Iview { get; set; }
        public BSL430_NET.FirmwareTools.FwTools.FwInfo FwInfo { get; set; }

        private string _FwPath = "";
        public string FwPath
        {
            get => _FwPath;
            set
            {
                _FwPath = value;
                NotifyOfPropertyChange(() => FwPath);
            }
        }
        private Visibility _Visibility = Visibility.Visible;
        public Visibility Visibility
        {
            get => _Visibility;
            set
            {
                _Visibility = value;
                NotifyOfPropertyChange(() => Visibility);
            }
        }
        #endregion

        #region Private Methods
        private void SaveFile(MemoryStream stream)
        {
            try
            {
                var fw = new BSL430_NET.FirmwareTools.FwTools.Firmware(stream, this.FwInfo.Format, this.FwInfo.AddrFirst);
                if (this.FwInfo.FilledFFAddr != null && this.FwInfo.FilledFFAddr.Count > 0)
                {
                    var hashSet = new HashSet<long>(this.FwInfo.FilledFFAddr);
                    for (int i = fw.Nodes.Count - 1; i >= 0; i--)
                    {
                        if (fw.Nodes[i].Data == 0xFF && hashSet.Contains(fw.Nodes[i].Addr))
                            fw.Nodes.RemoveAt(i);
                    }
                }

                using (StreamWriter wr = new StreamWriter(this.FwPath, false))
                {
                    wr.Write(BSL430_NET.FirmwareTools.FwTools.Create(fw, this.FwInfo.Format, BslSettings.Instance.FwWriteLineLength));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "BSL430.NET", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private bool CompareStreams(Stream a, Stream b)
        {
            if (a == null && b == null)
                return true;
            if (a == null || b == null)
                return false;

            if (a.Length < b.Length)
                return false;
            if (a.Length > b.Length)
                return false;

            a.Position = 0;
            b.Position = 0;

            for (int i = 0; i < a.Length; i++)
            {
                int aByte = a.ReadByte();
                int bByte = b.ReadByte();
                if (aByte.CompareTo(bByte) != 0)
                    return false;
            }
            return true;
        }
        #endregion
    }
}
