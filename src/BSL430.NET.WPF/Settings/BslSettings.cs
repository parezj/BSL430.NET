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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Config.Net;

namespace BSL430_NET_WPF.Settings
{
    class BslSettings
    {
        private readonly IBslSettings settings;
        private static FileVersionInfo ver = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);

        public static string SettingsDir
        {
            get => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                ver.CompanyName, ver.ProductName);
        }
        public static string SettingsFile
        {
            get => Path.Combine(SettingsDir, $"{ver.ProductName.Replace(' ', '_')}.json");
        }

        private BslSettings()
        {
            if (Environment.GetCommandLineArgs().Contains("--reset"))
            {
                try
                {
                    File.Delete(SettingsFile);
                }
                catch (Exception) { }
            }

            try
            {
                settings = new ConfigurationBuilder<IBslSettings>().UseJsonFile(SettingsFile).Build();
            }
            catch (Exception)
            {
                try
                {
                    File.Delete(SettingsFile);
                }
                catch (Exception) { }

                try
                {
                    settings = new ConfigurationBuilder<IBslSettings>().UseJsonFile(SettingsFile).Build();
                }
                catch (Exception)
                {
                    settings = new ConfigurationBuilder<IBslSettings>().UseAppConfig().Build();
                }
            }
        }

        private static BslSettings instance;
        public static IBslSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new BslSettings();
                }   

                return instance.settings;
            }
        }
    }
}
