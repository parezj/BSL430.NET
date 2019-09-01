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

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSL430_NET_WPF.Helpers
{
    public static class Dialogs
    {
        public static string OpenFileDialog(string path = "", string title = "", bool exists = false, string file = "", string filter = "All files (*.*)|*.*")
        {
            OpenFileDialog dlg = new OpenFileDialog();
            try
            {
                dlg.InitialDirectory = path;
            }
            catch (Exception)
            {
                try
                {
                    dlg.InitialDirectory = Path.GetDirectoryName(path);
                }
                catch (Exception) { }
            }     
            
            dlg.Title = title;
            dlg.Filter = filter;
            dlg.CheckFileExists = exists;

            try
            {
                if (file != "")
                    dlg.FileName = Path.GetFileName(path);
            }
            catch (Exception)
            {
                dlg.FileName = file;
            }

            if (dlg.ShowDialog() == true)
                return dlg.FileName;
            else return "";
        }
    }
}