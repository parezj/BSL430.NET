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
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;

namespace BSL430_NET_WPF.Helpers
{
    public class ShellIntegration
    {
        #region Association
        // https://stackoverflow.com/questions/21545580/associate-a-file-extension-to-an-application-within-c-sharp-application
        public static void Deassociate(string[] extension, string[] progID)
        {
            Debug.Assert(extension != null && progID != null);
            Debug.Assert(extension.Length == progID.Length);

            for (int i = 0; i < extension.Length; i++)
            {
                Deassociate(extension[i], progID[i]);
            }
        }

        public static void Deassociate(string extension, string progID)
        {
            try
            {
                Registry.ClassesRoot.DeleteSubKeyTree(extension);
            }
            catch (Exception) { }

            try
            {
                Registry.ClassesRoot.DeleteSubKeyTree(progID);
            }
            catch (Exception) { }

            try
            {
                var hkcu = Registry.CurrentUser;
                var subkey = hkcu.OpenSubKey($"Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FileExts\\{extension}\\", true);
                subkey.DeleteSubKey("UserChoice");
            }
            catch (Exception) { }
        }
        public static void Associate(string[] extension, string[] progID, string[] description, string icon, string application)
        {
            Debug.Assert(extension != null && progID != null && description != null);
            Debug.Assert(extension.Length == progID.Length && progID.Length == description.Length);

            for (int i = 0; i < extension.Length; i++)
            {
                Associate(extension[i], progID[i], description[i], icon, application);
            }
        }

        public static void Associate(string extension, string progID, string description, string icon, string application)
        {
            try
            {
                Registry.ClassesRoot.CreateSubKey(extension).SetValue("", progID);
                if (progID != null && progID.Length > 0)
                    using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(progID))
                    {
                        if (description != null)
                            key.SetValue("", description);
                        if (icon != null)
                            key.CreateSubKey("DefaultIcon").SetValue("", GetShortPath(icon));
                        if (application != null)
                            key.CreateSubKey(@"Shell\Open\Command").SetValue("", GetShortPath(application) + " \"%1\"");
                    }
            }
            catch (Exception) { }
        }

        public static bool IsAssociated(string[] extension, string[] progID, string path)
        {
            Debug.Assert(extension != null && progID != null);
            Debug.Assert(extension.Length == progID.Length);

            for (int i = 0; i < extension.Length; i++)
            {
                if (!IsAssociated(extension[i], progID[i], path))
                    return false;
            }
            return true;
        }

        public static bool IsAssociated(string extension, string progID, string path)
        {
            try
            {
                string app = GetShortPath(path) + " \"%1\"";
                var foo = Registry.ClassesRoot.OpenSubKey(extension, false);
                var foo2 = Registry.ClassesRoot.OpenSubKey(progID + "\\Shell\\Open\\Command", false);
                if (foo == null || foo2 == null) return false;
                else
                {
                    //System.Windows.MessageBox.Show("REG:" + (string)foo2.GetValue("") + "  TO:" + app);
                    if ((string)foo2.GetValue("") == app) return true;
                    else return false;
                }
            }
            catch (Exception) { return false; }
        }
        #endregion

        #region Extension
        public static bool IsExtension(string[] ext, string name, string path)
        {
            Debug.Assert(ext != null);

            for (int i = 0; i < ext.Length; i++)
            {
                if (!IsExtension(ext[i], name, path))
                    return false;
            }
            return true;
        }

        public static bool IsExtension(string ext, string name, string path)
        {
            try
            {
                string app = GetShortPath(path) + " \"%1\"";
                var reg = Registry.ClassesRoot.OpenSubKey($"{ext}\\shell\\{name}\\command", false);
                if (reg == null) return false;
                else
                {
                    if ((string)reg.GetValue("") == app) return true;
                    else return false;
                }
            }
            catch (Exception) { return false; }
        }

        public static void ExtensionsRemove(string[] ext, string name)
        {
            Debug.Assert(ext != null);

            for (int i = 0; i < ext.Length; i++)
            {
                ExtensionsRemove(ext[i], name);
            }
        }

        public static void ExtensionsRemove(string ext, string name)
        {
            try
            {
                Registry.ClassesRoot.DeleteSubKeyTree($"{ext}\\shell\\{name}");
            }
            catch (Exception) { }
        }

        public static void ExtensionsAdd(string[] ext, string name, string path, string icon)
        {
            Debug.Assert(ext != null);

            for (int i = 0; i < ext.Length; i++)
            {
                ExtensionsAdd(ext[i], name, path, icon);
            }
        }

        public static void ExtensionsAdd(string ext, string name, string path, string icon)
        {
            try
            {
                string app = GetShortPath(path) + " \"%1\"";
                using (var key = Registry.ClassesRoot.CreateSubKey($"{ext}\\shell\\{name}"))
                using (var key2 = Registry.ClassesRoot.CreateSubKey($"{ext}\\shell\\{name}\\command"))
                {
                    key.SetValue("Icon", GetShortPath(icon));
                    key2.SetValue("", app);
                }
            }
            catch (Exception) { }
        }
        #endregion

        #region Utils
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern uint GetShortPathName(string lpszLongPath, [Out] StringBuilder lpszShortPath, uint cchBuffer);

        public static string GetShortPath(string longPath)
        {
            StringBuilder shortPath = new StringBuilder(255);
            GetShortPathName(longPath, shortPath, 255);
            return shortPath.ToString();
        }

        public static string CurrentOS()
        {
            try
            {
                var reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                return (string)reg.GetValue("ProductName");
            }
            catch (Exception)
            {
                return "";
            }
        }
        #endregion
    }
}
