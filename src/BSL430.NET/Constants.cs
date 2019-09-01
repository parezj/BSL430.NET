/*
    BSL430.NET - MSP430 bootloader (BSL) .NET toolchain
    Original source by: Jakub Parez - https://github.com/parezj/
	  
    The MIT License (MIT)
    
    Copyright (c) 2018 Jakub Parez

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
using System.Text;


namespace BSL430_NET
{
    namespace Constants
    {
        internal static class Const
        {
            // BSL430.NET general options
            public const bool IS_DEBUG = false;

            // SETTINGS
            public const int TIMEOUT_OPEN = 1000;
            public const int TIMEOUT_SCAN = 2000;
            public const int TIMEOUT_READ = 4000;
            public const int DELAY_ERR_RET = 200;
            public const int DELAY_OK_RET = 50;
            public const int DEV_STR_MAX_LEN = 70;

            // MSP430 GENERAL
            public const uint MSP430_RESET_VECTOR_ADDR = 0xFFFE;
            public const int BSL430_DELAY_BETWEEN_CMDS = 10; // min 1.2ms

            // BSL CORE COMMANDS - PROTOCOL 5xx 6xx
            public const byte BSL_CMD56__RX_DATA_BLOCK = 0x10;
            public const byte BSL_CMD56__RX_DATA_BLOCK_FAST = 0x1B;
            public const byte BSL_CMD56__RX_PASSWORD = 0x11;
            public const byte BSL_CMD56__ERASE_SEGMENT = 0x12;
            public const byte BSL_CMD56__LOCK_INFO = 0x13;
            public const byte BSL_CMD56__MASS_ERASE = 0x15;
            public const byte BSL_CMD56__CRC_CHECK = 0x16;
            public const byte BSL_CMD56__LOAD_PC = 0x17;
            public const byte BSL_CMD56__TX_DATA_BLOCK = 0x18;
            public const byte BSL_CMD56__TX_BSL_VERSION = 0x19;
            public const byte BSL_CMD56__TX_BUFFER_SIZE = 0x1A;
            public const byte BSL_CMD56__CHANGE_BAUD_RATE = 0x52;
            public const byte BSL_CMD56__RESP_HEAD1 = 0x3A;
            public const byte BSL_CMD56__RESP_HEAD2 = 0x3B;

            // BSL BAUD RATE VALUES - PROTOCOL 5xx 6xx
            public const byte BSL_BAUD56__9600 = 0x02;
            public const byte BSL_BAUD56__19200 = 0x03;
            public const byte BSL_BAUD56__38400 = 0x04;
            public const byte BSL_BAUD56__57600 = 0x05;
            public const byte BSL_BAUD56__115200 = 0x06;

            // BSL - PROTOCOL 5xx 6xx
            public const byte BSL_HEADER56__UART = 0x80;
            public const byte BSL_HEADER56__USB = 0x3F;

            // BSL RX SIZE - PROTOCOL 5xx 6xx
            public const byte BSL_SIZE56__HEADER_UART = 6;
            public const byte BSL_SIZE56__HEADER_USB = 2;
            public const byte BSL_SIZE56__CMD = 1;
            public const byte BSL_SIZE56__DATA_CHANGE_BAUD_RATE = 0;
            public const byte BSL_SIZE56__DATA_TX_BSL_VERSION = 4;
            public const byte BSL_SIZE56__DATA_MASS_ERASE = 1;
            public const byte BSL_SIZE56__DATA_CRC_CHECK = 2;
            public const byte BSL_SIZE56__DATA_RX_PASSWORD = 1;
            public const byte BSL_SIZE56__DATA_RX_DATA_BLOCK = 1;
            public const byte BSL_SIZE56__DATA_TX_DATA_BLOCK = 1;
            public const int BSL_SIZE56__DATA_LOAD_PC = -1;
            public const byte BSL_SIZE56__PASSWORD = 32;
            public const byte BSL_SIZE56__RX_BLOCK_OVERHEAD = 4;

            // BSL RX SIZE - PROTOCOL 1xx 2xx 4xx
            public const byte BSL_SIZE124__PASSWORD = 20;
            public const byte BSL_SIZE124__RX_BLOCK_OVERHEAD = 4;
            public const byte BSL_SIZE124__DEFAULT_RX = 1;
            public const byte BSL_SIZE124__FRAME_OVERHEAD = 6;
            public const byte BSL_SIZE124__BSL_VERSION = 10;

            // BSL GENERAL - PROTOCOL 1xx 2xx 4xx
            public const byte BSL_GENERAL124__SYNC = 0x80;
            public const byte BSL_GENERAL124__ACK = 0x90;
            public const byte BSL_GENERAL124__NAK = 0xA0;

            // BSL FRAME DATA - PROTOCOL 1xx 2xx 4xx
            public const byte BSL_DATA124__DEFAULT_L1_L2 = 0x04;
            public const byte BSL_DATA124__PW_L1_L2 = 0x24;
            public const byte BSL_DATA124__DUMMY = 0xFF;
            public const byte BSL_DATA124__MASS_ERASE_LL = 0x06;
            public const byte BSL_DATA124__MASS_ERASE_LH = 0xA5;

            // BSL CMD - PROTOCOL 1xx 2xx 4xx
            public const byte BSL_CMD124__RX_DATA_BLOCK = 0x12;
            public const byte BSL_CMD124__RX_PASSWORD = 0x10;
            public const byte BSL_CMD124__ERASE_SEGMENT = 0x16;
            public const byte BSL_CMD124__ERASE_MAIN_OR_INFO = 0x16;
            public const byte BSL_CMD124__MASS_ERASE = 0x18;
            public const byte BSL_CMD124__ERASE_CHECK = 0x1C;
            public const byte BSL_CMD124__CHANGE_BAUD_RATE = 0x20;
            public const byte BSL_CMD124__SET_MEM_OFFSET = 0x21;
            public const byte BSL_CMD124__LOAD_PC = 0x1A;
            public const byte BSL_CMD124__TX_DATA_BLOCK = 0x14;
            public const byte BSL_CMD124__TX_BSL_VERSION = 0x1E;
        }
    }
}
