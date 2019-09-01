using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BSL430_NET_WPF.Helpers
{
    class Admin
    {
        public static bool ElevateAndIfNotRestart(string args = "", bool msgBox = true)
        {
            if (!IsAdministrator())
            {
                if (msgBox)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke((System.Action)(() =>
                    {
                        var result = MessageBox.Show("Application will now restart with elevated privileges.", "BSL430.NET", MessageBoxButton.OK,
                            MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                    }));
                }

                if (Elevate(args))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool Elevate(string args = "", bool msgBox = false)
        {
            var SelfProc = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName,
                Verb = "runas",
                Arguments = args
            };
            try
            {
                Process.Start(SelfProc);
                return true;
            }
            catch
            {
                if (msgBox)
                    System.Windows.MessageBox.Show("Unable to elevate!");
                return false;
            }
        }

        public static bool Restart(string args = "")
        {
            var SelfProc = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName,
                Arguments = args
            };
            try
            {
                Process.Start(SelfProc);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
