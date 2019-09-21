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

using BSL430_NET;
using BSL430_NET_WPF.Settings;
using Caliburn.Micro;

namespace BSL430_NET_WPF.ViewModels
{
    public class TabEraseViewModel : Screen, IControlProccessActions
    {
        public ControlProcessViewModel ControlProcess { get; private set; }

        public TabEraseViewModel(ControlProcessViewModel _ctrlprc_vm)
        {
            this.ControlProcess = _ctrlprc_vm;
        }

        #region Public Interface
        public void Scan()
        {
            this.ControlProcess?.Scan();
        }
        public void StartStop()
        {
            MCU mcu = this.ControlProcess.MCU;
            if (this.FirstErase && (mcu == MCU.MSP430_F1xx || mcu == MCU.MSP430_F2xx || mcu == MCU.MSP430_F4xx || mcu == MCU.MSP430_G2xx3))
            {
                var result = MessageBox.Show("As this is your first erase, and you are targetting old 1xx/2xx/4xx protocol, you should be careful. " +
                    "Old 1xx/2xx/4xx bootloader protocols erase complete memory including Info A (with CALIBRATION data) if Mass Erase is executed, " +
                    "provided LOCK A bit is not set.\n\nDo you still want to continue?", "BSL430.NET", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    this.FirstErase = false;
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

        #region Properties
        private bool _FirstErase = BslSettings.Instance.EraseFirst;
        public bool FirstErase
        {
            get => _FirstErase;
            set
            {
                _FirstErase = value;
                BslSettings.Instance.EraseFirst = value;
            }
        }
        #endregion
    }
}
