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
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Globalization;

using BSL430_NET.Utility;
using BSL430_NET.FirmwareTools;
using BSL430_NET.Constants;
using BSL430_NET.FirmwareTools.Helpers;

namespace BSL430_NET
{
    namespace Main
    {
        internal abstract partial class Core : IDevice, ICommable
        {
            private Message BuildMsg(Command command) 
            { 
                return BuildMsg(command, null, null); 
            }
            private Message BuildMsg(Command command, int? address) 
            { 
                return BuildMsg(command, address, null); 
            }
            private Message BuildMsg(Command command, byte[] data) 
            { 
                return BuildMsg(command, null, data); 
            }
            private Message BuildMsg(Command command, int? address, byte[] data)
            {
                Message message = new Message
                {
                    status = new Status(),
                    command = command,
                    msg = null,
                    extra = 0
                };
                byte[] _address = new byte[0];
                try
                {
                    if (protocol == Protocol.UART_1_2_4)
                    {
                        List<byte> BSL_frame = new List<byte> { Const.BSL_GENERAL124__SYNC };

                        if (address != null)
                        {
                            _address = new byte[2] { (byte)(address & 0x00FF),
                                                      (byte)(address >> 8) };
                        }

                        switch (command)
                        {
                            case Command.BaudRate:
                                {
                                    message.status = Utils.StatusCreate(219);
                                    return message;
                                }

                            case Command.BSLVersion:  // address: -   data: -
                                {
                                    BSL_frame.Add(Const.BSL_CMD124__TX_BSL_VERSION);
                                    BSL_frame.AddRange(Enumerable.Repeat(Const.BSL_DATA124__DEFAULT_L1_L2, 2).ToArray());
                                    BSL_frame.AddRange(Enumerable.Repeat(Const.BSL_DATA124__DUMMY, 4).ToArray());
                                }
                                break;

                            case Command.MassErase:  // address: -   data: -
                                {
                                    BSL_frame.Add(Const.BSL_CMD124__MASS_ERASE);
                                    BSL_frame.AddRange(Enumerable.Repeat(Const.BSL_DATA124__DEFAULT_L1_L2, 2).ToArray());
                                    BSL_frame.AddRange(Enumerable.Repeat(Const.BSL_DATA124__DUMMY, 2).ToArray());
                                    BSL_frame.Add(Const.BSL_DATA124__MASS_ERASE_LL);
                                    BSL_frame.Add(Const.BSL_DATA124__MASS_ERASE_LH);
                                }
                                break;

                            case Command.CRC:
                                {
                                    message.status = Utils.StatusCreate(220);
                                    return message;
                                }

                            case Command.Password:  // address: -   data: D1 ... D20
                                {
                                    BSL_frame.Add(Const.BSL_CMD124__MASS_ERASE);
                                    BSL_frame.AddRange(Enumerable.Repeat(Const.BSL_DATA124__PW_L1_L2, 2).ToArray());
                                    BSL_frame.AddRange(Enumerable.Repeat(Const.BSL_DATA124__DUMMY, 4).ToArray());

                                    if ((data == null || data.Length != Const.BSL_SIZE124__PASSWORD))
                                    {
                                        message.status = Utils.StatusCreate(801);
                                        return message;
                                    }
                                    BSL_frame.AddRange(data);
                                }
                                break;

                            case Command.Upload:  // address: AL, AH   data: D1 ... Dn-4
                                {
                                    BSL_frame.Add(Const.BSL_CMD124__RX_DATA_BLOCK);
                                    BSL_frame.AddRange(Enumerable.Repeat((byte)data.Length, 2).ToArray());

                                    if ((address == null) || (data == null || data.Length < 1))
                                    {
                                        message.status = Utils.StatusCreate(801);
                                        return message;
                                    }
                                    BSL_frame.AddRange(_address.ToArray());
                                    BSL_frame.Add((byte)(data.Length - 4));
                                    BSL_frame.Add(0);
                                    BSL_frame.AddRange(data);
                                }
                                break;

                            case Command.Download:  // address: AL, AH   data: Length
                                {
                                    BSL_frame.Add(Const.BSL_CMD124__TX_DATA_BLOCK);
                                    BSL_frame.AddRange(Enumerable.Repeat(Const.BSL_DATA124__DEFAULT_L1_L2, 2).ToArray());

                                    if ((address == null) || (data == null || data.Length != 1))
                                    {
                                        message.status = Utils.StatusCreate(801);
                                        return message;
                                    }
                                    BSL_frame.AddRange(_address.ToArray());
                                    BSL_frame.Add(data[0]);
                                    BSL_frame.Add(0);
                                    message.extra = (ushort)(data[0]);
                                }
                                break;

                            case Command.LoadPC:  // address: AL, AH   data: -
                                {
                                    BSL_frame.Add(Const.BSL_CMD124__LOAD_PC);
                                    BSL_frame.AddRange(Enumerable.Repeat(Const.BSL_DATA124__DEFAULT_L1_L2, 2).ToArray());
                                    
                                    if (address == null)
                                    {
                                        message.status = Utils.StatusCreate(801);
                                        return message;
                                    }
                                    BSL_frame.AddRange(_address.ToArray());
                                    BSL_frame.AddRange(Enumerable.Repeat(Const.BSL_DATA124__DUMMY, 2).ToArray());
                                }
                                break;
                        }
                        BSL_frame.Add((byte)(~(BSL_frame.Where((item, index) => index % 2 == 0)
                                                        .Aggregate<byte,byte>(0, (a, b) => (byte)(a ^ b))))); 

                        BSL_frame.Add((byte)(~(BSL_frame.Where((item, index) => index % 2 != 0)
                                                        .Aggregate<byte, byte>(0, (a, b) => (byte)(a ^ b)))));

                        message.msg = BSL_frame.ToArray();
                        message.status = Utils.StatusCreate(0);

                        return message;
                    }
                    else // 5xx 6xx protocol
                    {
                        List<byte> BSL_cmd_wrapper = new List<byte>();
                        List<byte> BSL_core_cmd = new List<byte>();

                        if (address != null)
                        {
                            _address = new byte[3] { (byte)(address & 0x00FF),
                                                      (byte)(address >> 8),
                                                      (byte)(address >> 16) };
                        }   

                        switch (command)
                        {
                            case Command.BaudRate:  // address: -   data: -
                                {
                                    BSL_core_cmd.Add(Const.BSL_CMD56__CHANGE_BAUD_RATE);
                                    switch (baud_rate)
                                    {
                                        default:
                                        case BaudRate.BAUD_9600:   BSL_core_cmd.Add(Const.BSL_BAUD56__9600); break;
                                        case BaudRate.BAUD_19200:  BSL_core_cmd.Add(Const.BSL_BAUD56__19200); break;
                                        case BaudRate.BAUD_38400:  BSL_core_cmd.Add(Const.BSL_BAUD56__38400); break;
                                        case BaudRate.BAUD_57600:  BSL_core_cmd.Add(Const.BSL_BAUD56__57600); break;
                                        case BaudRate.BAUD_115200: BSL_core_cmd.Add(Const.BSL_BAUD56__115200); break;
                                    }
                                }
                                break;

                            case Command.BSLVersion:  // address: -   data: -
                                {
                                    BSL_core_cmd.Add(Const.BSL_CMD56__TX_BSL_VERSION);
                                }
                                break;

                            case Command.MassErase:  // address: -   data: -
                                {
                                    BSL_core_cmd.Add(Const.BSL_CMD56__MASS_ERASE);
                                }
                                break;

                            case Command.CRC:  // address: AL, AM, AH   data: Length (low), Length (high)
                                {
                                    BSL_core_cmd.Add(Const.BSL_CMD56__CRC_CHECK);

                                    if ((address == null) || (data == null || data.Length != 2))
                                    {
                                        message.status = Utils.StatusCreate(801);
                                        return message;
                                    }
                                    BSL_core_cmd.AddRange(_address.ToArray());
                                    BSL_core_cmd.AddRange(data);
                                }
                                break;

                            case Command.Password:  // address: -   data: D1 ... D33
                                {
                                    BSL_core_cmd.Add(Const.BSL_CMD56__RX_PASSWORD);

                                    if ((data == null || data.Length != Const.BSL_SIZE56__PASSWORD)) 
                                    {
                                        message.status = Utils.StatusCreate(801);
                                        return message;
                                    }
                                    BSL_core_cmd.AddRange(data);
                                }
                                break;

                            case Command.Upload:  // address: AL, AM, AH   data: D1 ... Dn
                                {
                                    if (protocol == Protocol.UART_5_6)
                                        BSL_core_cmd.Add(Const.BSL_CMD56__RX_DATA_BLOCK);
                                    else // (protocol == Protocol.USB_5_6)
                                        BSL_core_cmd.Add(Const.BSL_CMD56__RX_DATA_BLOCK_FAST);

                                    if ((address == null) || (data == null || data.Length < 1))
                                    {
                                        message.status = Utils.StatusCreate(801);
                                        return message;
                                    }
                                    BSL_core_cmd.AddRange(_address.ToArray());
                                    BSL_core_cmd.AddRange(data);
                                }
                                break;

                            case Command.Download:  // address: AL, AM, AH   data: Length (low), Length (high)
                                {
                                    BSL_core_cmd.Add(Const.BSL_CMD56__TX_DATA_BLOCK);

                                    if ((address == null) || (data == null || data.Length != 2))
                                    {
                                        message.status = Utils.StatusCreate(801);
                                        return message;
                                    }
                                    BSL_core_cmd.AddRange(_address.ToArray());
                                    BSL_core_cmd.AddRange(data);
                                    message.extra = (ushort)((data[1] << 8) | data[0]);
                                }
                                break;

                            case Command.LoadPC:  // address: AL, AM, AH   data: -
                                {
                                    BSL_core_cmd.Add(Const.BSL_CMD56__LOAD_PC);

                                    if ((address == null))
                                    {
                                        message.status = Utils.StatusCreate(801);
                                        return message;
                                    }
                                    BSL_core_cmd.AddRange(_address.ToArray());
                                }
                                break;
                        }

                        if (protocol == Protocol.UART_5_6)
                        {
                            int crc = BSL_core_cmd.Crc16Ccitt();
                            BSL_cmd_wrapper.Add(Const.BSL_HEADER56__UART);
                            BSL_cmd_wrapper.Add((byte)(BSL_core_cmd.Count & 0x00FF));
                            BSL_cmd_wrapper.Add((byte)(BSL_core_cmd.Count >> 8));
                            BSL_cmd_wrapper.AddRange(BSL_core_cmd);
                            BSL_cmd_wrapper.Add((byte)(crc & 0x00FF));
                            BSL_cmd_wrapper.Add((byte)(crc >> 8));
                        }
                        else // (protocol == Protocol.USB_5_6)
                        {
                            if (command != Command.Password &&
                                command != Command.Download &&
                                command != Command.LoadPC)
                            {
                                message.status = Utils.StatusCreate(218);
                                return message;
                            }
                            BSL_cmd_wrapper.Add(Const.BSL_HEADER56__USB);
                            BSL_cmd_wrapper.Add((byte)BSL_core_cmd.Count);
                            BSL_cmd_wrapper.AddRange(BSL_core_cmd);
                        }

                        message.msg = BSL_cmd_wrapper.ToArray();
                        message.status = Utils.StatusCreate(0);

                        return message;
                    }
                }
                catch (Exception ex)
                {
                    if (ex is Bsl430NetException)
                        message.status = Utils.StatusCreate(447, ((Bsl430NetException)ex).Status);
                    else
                        message.status = Utils.StatusCreate(447, ex.Message);

                    return message;
                }
            }
            private Response ProcessMsg(Message message)
            {
                Response response = new Response
                {
                    msg_sent = message.msg,
                    command = message.command,
                    status = message.status,
                    data = null
                };

                if (!message.status.OK)
                    return response;
                try
                {
                    if (protocol == Protocol.UART_1_2_4)
                    {
                        int resp_size = Const.BSL_SIZE124__DEFAULT_RX;

                        switch (message.command)
                        {
                            case Command.Download: resp_size = Const.BSL_SIZE124__FRAME_OVERHEAD + (byte)message.extra; break;
                            case Command.BSLVersion: resp_size = Const.BSL_SIZE124__FRAME_OVERHEAD + Const.BSL_SIZE124__BSL_VERSION; break;
                        }

                        response.status = CommXfer(out response.data, new byte[] { Const.BSL_GENERAL124__SYNC }, 1);

                        if (response.data == null || response.data.Length < 1 || response.data[0] != Const.BSL_GENERAL124__ACK)
                        {
                            response.status = Utils.StatusCreate(809);
                            return response;
                        }
                        Task.Delay(2).Wait(); // ?

                        response.status = CommXfer(out response.data, message.msg, resp_size);
                    }
                    else
                    {
                        int resp_head_size = 0;
                        int resp_dat_size = 0;

                        if (protocol == Protocol.UART_5_6)
                        {
                            resp_head_size = Const.BSL_SIZE56__HEADER_UART;
                        }
                        else // (protocol == Protocol.USB_5_6)
                        {
                            resp_head_size = Const.BSL_SIZE56__HEADER_USB;
                        }

                        switch (message.command)
                        {
                            case Command.BaudRate:
                                {
                                    resp_dat_size = Const.BSL_SIZE56__DATA_CHANGE_BAUD_RATE;
                                    resp_head_size = 0;
                                } break;
                            case Command.BSLVersion: resp_dat_size = Const.BSL_SIZE56__DATA_TX_BSL_VERSION; break;
                            case Command.MassErase:  resp_dat_size = Const.BSL_SIZE56__DATA_MASS_ERASE; break;
                            case Command.CRC:        resp_dat_size = Const.BSL_SIZE56__DATA_CRC_CHECK; break;
                            case Command.Password:   resp_dat_size = Const.BSL_SIZE56__DATA_RX_PASSWORD; break;
                            case Command.Download:   resp_dat_size = message.extra; break;
                            case Command.Upload:     resp_dat_size = Const.BSL_SIZE56__DATA_RX_DATA_BLOCK; break;
                            case Command.LoadPC:
                                {
                                    resp_dat_size = Const.BSL_SIZE56__DATA_LOAD_PC;
                                    resp_head_size = 0;
                                } break;
                        }
                        resp_dat_size += Const.BSL_SIZE56__CMD;

                        response.status = CommXfer(out response.data, message.msg, resp_dat_size + resp_head_size);
                    }
                    return response;
                }
                catch (Bsl430NetException ex)
                {
                    if (ex.Status.Error == 666)
                        throw ex;

                    response.status = Utils.StatusCreate(443, ex.Message);
                    return response;
                }
                catch (Exception ex)
                {
                    if (ex is Bsl430NetException)
                        response.status = Utils.StatusCreate(443, ((Bsl430NetException)ex).Status);
                    else
                        response.status = Utils.StatusCreate(443, ex.Message);

                    return response;
                }
            }
            private Result<T> ParseResp<T>(Response answer) where T : IData
            {
                Result<T> result = new Result<T>
                {
                    command = answer.command,
                    status = answer.status,
                    msg_sent = answer.msg_sent,
                    msg_received = answer.data,
                    ok = false
                };

                if (!answer.status.OK && 
                    (answer.status.Error != 395 ||
                        answer.data == null ||
                        answer.data.Length < 1 ||
                        protocol != Protocol.UART_5_6))
                    return result;
                try
                {
                    if (protocol == Protocol.UART_1_2_4)
                    {
                        int data_len = Const.BSL_SIZE124__DEFAULT_RX;
                        bool ok = false;
                        if ((answer.command == Command.BSLVersion || 
                            answer.command == Command.Download) &&
                               answer.data != null &&
                               answer.data.Length > 3 &&
                               answer.data[2] == answer.data[3])
                        {
                            if (answer.data[0] != Const.BSL_GENERAL124__SYNC)
                            {
                                result.status = Utils.StatusCreate(810);
                                return result;
                            }
                            data_len = answer.data[2];
                        }
                        if (answer.data == null || answer.data.Length != data_len)
                        {
                            result.status = Utils.StatusCreate(805);
                            return result;
                        }
                        if (answer.data[0] == Const.BSL_GENERAL124__NAK)
                        {
                            result.status = Utils.StatusCreate(811);
                            return result;
                        }

                        switch (answer.command)
                        {
                            case Command.BSLVersion:
                                {
                                    Debug.Assert(typeof(T) == typeof(Data_BSLVersion));
                                    //throw new BSL430Exception(460, "Result<Data_BSLVersion> res = msg(Command.BSLVersion)");
                                    result.data = (T)(object)new Data_BSLVersion { version = answer.data
                                                                                                   .Skip(Const.BSL_SIZE124__RX_BLOCK_OVERHEAD)
                                                                                                   .Take(Const.BSL_SIZE124__BSL_VERSION)
                                                                                                   .ToArray() };
                                    ok = (answer.data.Length == Const.BSL_SIZE124__RX_BLOCK_OVERHEAD + Const.BSL_SIZE124__BSL_VERSION + 2);
                                }; break;

                            case Command.Download:
                                {
                                    Debug.Assert(typeof(T) == typeof(Data_Download));
                                    //throw new BSL430Exception(460, "Result<Data_Download> res = msg(Command.Download)");
                                    result.data = (T)(object)new Data_Download { bytes = answer.data
                                                                                               .Skip(Const.BSL_SIZE124__RX_BLOCK_OVERHEAD)
                                                                                               .Take(data_len)
                                                                                               .ToArray() };
                                    ok = true;
                                }; break;

                            case Command.LoadPC:
                            case Command.MassErase:
                            case Command.Password:
                            case Command.Upload:
                                {
                                    Debug.Assert(typeof(T) == typeof(Data_Void));
                                    //throw new BSL430Exception(460, "Result<Data_Void> res = msg(Command.*)");
                                    result.data = (T)(object)new Data_Void();
                                    ok = (answer.data.Length == 1 && answer.data[0] == Const.BSL_GENERAL124__SYNC);
                                }
                                break;
                        }
                        if (!ok)
                        {
                            result.status = Utils.StatusCreate(812);
                            return result;
                        }
                        result.ok = true;
                        result.status = Utils.StatusCreate(0, result.status);
                        return result;
                    }
                    else  // 5xx 6xx protocol
                    {
                        int data_start_offset = 0;
                        int data_len = 0;

                        if (protocol == Protocol.UART_5_6)
                        {
                            bool wrong_size = false;
                            if (answer.command == Command.LoadPC)
                            {
                                result.ok = true;
                                result.status = Utils.StatusCreate(0, result.status);
                                return result;
                            }
                            if (answer.command == Command.BaudRate)
                            {
                                if (answer.data == null || answer.data.Length != 1)
                                    wrong_size = true;
                            }
                            else
                            {
                                if (answer.data == null || answer.data.Length < 4)
                                    wrong_size = true;
                                else
                                {
                                    data_len = (answer.data[3] << 8) | answer.data[2];
                                    if (answer.data.Length != data_len + Const.BSL_SIZE56__RX_BLOCK_OVERHEAD + 2)
                                        wrong_size = true;
                                }
                            }
                            
                            if (wrong_size)
                            {
                                result.status = Utils.StatusCreate(805);
                                return result;
                            }

                            if (Enum.IsDefined(typeof(BslUartMessage), answer.data[0]))
                                result.status.UartStatus = (BslUartMessage)Enum.ToObject(typeof(BslUartMessage), answer.data[0]);

                            if (!answer.status.OK)
                            {
                                result.status = Utils.StatusCreate(result.status.Error, result.status);
                                return result;
                            }

                            if (result.status.UartStatus != BslUartMessage.ACK)
                            {
                                result.status = Utils.StatusCreate(802, result.status);
                                result.status.InnerStatus.Error = 821;
                                result.status.InnerStatus.OK = false;
                                result.status.InnerStatus.Msg = "BSL UART Message Error";
                                return result;
                            }

                            if (answer.command != Command.BaudRate && answer.data[1] != Const.BSL_HEADER56__UART)
                            {
                                result.status = Utils.StatusCreate(803);
                                return result;
                            }

                            data_start_offset = 4;
                        }
                        else // (protocol == Protocol.USB_5_6)
                        {
                            bool wrong_size = false;
                            if (answer.data == null || answer.data.Length < 2)
                                wrong_size = true;
                            else
                            {
                                data_len = answer.data[1];
                                if (answer.data.Length != data_len + 2)
                                    wrong_size = true;
                            }
                            
                            if (wrong_size)
                            {
                                result.status = Utils.StatusCreate(805);
                                return result;
                            }

                            if (answer.data[0] != Const.BSL_HEADER56__USB)
                            {
                                result.status = Utils.StatusCreate(804);
                                return result;
                            }

                            data_start_offset = 2;
                        }

                        if (((answer.command == Command.BSLVersion ||
                              answer.command == Command.CRC ||
                              answer.command == Command.Download) &&
                                 answer.data[data_start_offset] != Const.BSL_CMD56__RESP_HEAD1) ||
                            ((answer.command == Command.LoadPC ||
                              answer.command == Command.MassErase ||
                              answer.command == Command.Password ||
                              answer.command == Command.Upload) &&
                                 answer.data[data_start_offset] != Const.BSL_CMD56__RESP_HEAD2))
                        {
                            result.status = Utils.StatusCreate(806, result.status);
                            return result;
                        }
                        data_start_offset++;
                        data_len--;

                        switch (answer.command)
                        {
                            case Command.BSLVersion:
                                {
                                    Debug.Assert(typeof(T) == typeof(Data_BSLVersion));
                                        //throw new BSL430Exception(460, "Result<Data_BSLVersion> res = msg(Command.BSLVersion)");
                                    result.data = (T)(object)new Data_BSLVersion { version = answer.data
                                                                                                   .Skip(data_start_offset)
                                                                                                   .Take(4).ToArray() };
                                }; break;

                            case Command.CRC:
                                {
                                    Debug.Assert(typeof(T) == typeof(Data_CRC));
                                        //throw new BSL430Exception(460, "Result<Data_CRC> res = msg(Command.CRC)");
                                    result.data = (T)(object)new Data_CRC { crc = (ushort)((answer.data[data_start_offset + 1] << 8) | 
                                                                                            answer.data[data_start_offset]) };
                                }; break;

                            case Command.Download:
                                {
                                    Debug.Assert(typeof(T) == typeof(Data_Download));
                                        //throw new BSL430Exception(460, "Result<Data_Download> res = msg(Command.Download)");
                                    result.data = (T)(object)new Data_Download { bytes = answer.data
                                                                                               .Skip(data_start_offset)
                                                                                               .Take(data_len).ToArray() };
                                }; break;

                            case Command.BaudRate:
                                {
                                    // Rx => 0x00 (only 1 byte)
                                }; break;
                            case Command.LoadPC:
                            case Command.MassErase:
                            case Command.Password:
                            case Command.Upload:
                                {
                                    Debug.Assert(typeof(T) == typeof(Data_Void));
                                        //throw new BSL430Exception(460, "Result<Data_Void> res = msg(Command.*)");
                                    result.data = (T)(object)new Data_Void();

                                    if (Enum.IsDefined(typeof(BslCoreMessage), answer.data[data_start_offset]))
                                    {
                                        result.status.CoreStatus = (BslCoreMessage)Enum.ToObject(typeof(BslCoreMessage), 
                                                                                                 answer.data[data_start_offset]);
                                    }
                                    else
                                    {
                                        result.status = Utils.StatusCreate(807, result.status);
                                    }
                                        
                                    if (result.status.CoreStatus != BslCoreMessage.Success)
                                    {
                                        result.status = Utils.StatusCreate(808, result.status);
                                        result.status.InnerStatus.Error = 822;
                                        result.status.InnerStatus.OK = false;
                                        result.status.InnerStatus.Msg = "BSL Core Message Error";
                                        return result;
                                    }     
                                }
                                break;
                        }
                        result.ok = true;
                        result.status = Utils.StatusCreate(0, result.status);
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    if (ex is Bsl430NetException)
                        result.status = Utils.StatusCreate(448, ((Bsl430NetException)ex).Status);
                    else
                        result.status = Utils.StatusCreate(448, ex.Message);

                    return result;
                }
            }
            private Status ParseFirmware(string firmware_path, out List<Rx_Block> rx_blocks, out FwTools.FwInfo fw_info)
            {
                fw_info = null;
                rx_blocks = new List<Rx_Block>();
                FwTools.Firmware code_to_upload = null;
                FwTools.Firmware code_full_for_crc = null;
                bool fill_FF = false;

                if (protocol == Protocol.USB_5_6)
                    fill_FF = true;

                try
                {
                    code_to_upload = FwTools.Parse(firmware_path, FillFF: fill_FF);
                    code_full_for_crc = FwTools.Parse(firmware_path, FillFF: true);

                    // slice data to bigger blocks that fit buffer size
                    int buff_size = GetBufferSize(protocol);
                    if (fill_FF)
                    {
                        for (int i = 0; i < code_to_upload.Nodes.Count; i += buff_size)
                        {
                            rx_blocks.Add(new Rx_Block
                            {
                                addr = (int)code_to_upload.Nodes[i].Addr,
                                data = code_to_upload.Nodes.Skip(i).Take(buff_size).Select(nod => nod.Data).ToArray()
                            });
                        }
                    }
                    else
                    {
                        for (int i = 0, j = 0; i < code_to_upload.Nodes.Count; i++)
                        {
                            if (i == code_to_upload.Nodes.Count - 1 ||
                                code_to_upload.Nodes[i + 1].Addr - code_to_upload.Nodes[i].Addr > 1 || 
                                j + 1 >= buff_size)
                            {
                                rx_blocks.Add(new Rx_Block
                                {
                                    addr = (int)code_to_upload.Nodes[i - j].Addr,
                                    data = code_to_upload.Nodes.Skip(i - j).Take(j + 1).Select(nod => nod.Data).ToArray()

                                });
                                j = 0;
                            }
                            else j++;
                        }
                    }

                    code_to_upload.Info.Crc16 = code_full_for_crc.Info.Crc16;
                    fw_info = code_to_upload.Info;

                    if (rx_blocks == null || rx_blocks.Count < 1)
                        throw new Bsl430NetException(445);
                    else
                        return Utils.StatusCreate(0);
                }
                catch (Exception ex)
                {
                    if (ex is Bsl430NetException)
                    {
                        return Utils.StatusCreate(450, ((Bsl430NetException)ex).Status);
                    }               
                    else if (ex is FirmwareToolsException)
                    {
                        Status fw_ex = new Status()
                        {
                            Error = ((FirmwareToolsException)ex).Error,
                            Msg = ((FirmwareToolsException)ex).Msg
                        };
                        return Utils.StatusCreate(450, fw_ex);
                    }                   
                    else
                    {
                        return Utils.StatusCreate(450, ex.Message);
                    }                     
                }  
            }
            private int GetBufferSize(Protocol _protocol)
            {
                if (_protocol == Protocol.UART_1_2_4)
                    return (int)protocol - Const.BSL_SIZE124__RX_BLOCK_OVERHEAD;
                else
                    return (int)protocol - Const.BSL_SIZE56__RX_BLOCK_OVERHEAD;
            }
            private void InvokeBSL(InvokeMechanism mech)
            {
                if (mech == InvokeMechanism.SHARED_JTAG)
                {
                    // todo predelat. opravit.
                    CommRts(true);
                    CommDtr(false);

                    Task.Delay(25).Wait();

                    CommRts(true);
                    CommDtr(true);

                    Task.Delay(20).Wait();



                    CommRts(false);
                    Task.Delay(2).Wait();
                    CommRts(true);
                    Task.Delay(2).Wait();
                    CommRts(false);
                    Task.Delay(2).Wait();
                    CommRts(true);
                    Task.Delay(2).Wait();

                    CommRts(false);
                    Task.Delay(2).Wait();
                    CommRts(true);
                    Task.Delay(2).Wait();
                    CommRts(false);
                    Task.Delay(1).Wait();
                    CommDtr(false);
                    Task.Delay(1).Wait();
                    CommRts(true);
                }
                else if (invoke == InvokeMechanism.DEDICATED_JTAG)
                {
                    // todo predelat. opravit.
                    CommRts(false);
                    CommDtr(false);

                    Task.Delay(25).Wait();

                    CommRts(false);
                    CommDtr(true);

                    Task.Delay(20).Wait();



                    CommRts(true);
                    Task.Delay(2).Wait();
                    CommRts(false);
                    Task.Delay(2).Wait();
                    CommRts(true);
                    Task.Delay(2).Wait();
                    CommRts(false);
                    Task.Delay(2).Wait();

                    CommRts(true);
                    Task.Delay(2).Wait();
                    CommRts(false);
                    Task.Delay(2).Wait();
                    CommRts(true);
                    Task.Delay(1).Wait();
                    CommDtr(false);
                    Task.Delay(1).Wait();
                    CommRts(false);
                }
            }
            private void ResetMCU(InvokeMechanism mech)
            {
                if (mech == InvokeMechanism.SHARED_JTAG)
                {
                    CommRts(true);
                    Task.Delay(10).Wait();
                    CommRts(false);
                    Task.Delay(10).Wait();
                    CommRts(true);
                    Task.Delay(10).Wait();
                    CommRts(false);
                    Task.Delay(10).Wait();
                    CommRts(true);
                    Task.Delay(10).Wait();
                    CommRts(false);
                    Task.Delay(10).Wait();
                    CommRts(true);
                    Task.Delay(10).Wait();
                    CommRts(false);
                    Task.Delay(10).Wait();
                    CommRts(true);
                    Task.Delay(10).Wait();
                    CommDtr(false);
                    Task.Delay(10).Wait();
                    CommDtr(true);
                    Task.Delay(10).Wait();
                    CommDtr(false);
                    Task.Delay(10).Wait();
                    CommDtr(true);
                    Task.Delay(10).Wait();
                    CommDtr(false);
                    Task.Delay(10).Wait();
                }
                else if (mech == InvokeMechanism.DEDICATED_JTAG)
                {
                    CommRts(true);
                    Task.Delay(10).Wait();
                    CommRts(false);
                    Task.Delay(10).Wait();
                    CommRts(true);
                    Task.Delay(10).Wait();
                    CommRts(false);
                    Task.Delay(10).Wait();
                    CommRts(true);
                    Task.Delay(10).Wait();
                    CommRts(false);
                    Task.Delay(10).Wait();
                    CommRts(true);
                    Task.Delay(10).Wait();
                    CommRts(false);
                    Task.Delay(10).Wait();
                    CommRts(true);
                    Task.Delay(10).Wait();
                }
            }
            private byte[] ValidatePassword(byte[] password, out bool pw_overide)
            {
                int pw_size;

                if (protocol == Protocol.UART_1_2_4)
                    pw_size = Const.BSL_SIZE124__PASSWORD;
                else
                {
                    if (mcu != MCU.MSP430_F543x_NON_A)
                        pw_size = Const.BSL_SIZE56__PASSWORD;
                    else
                        pw_size = Const.BSL_SIZE56__PASSWORD / 2;
                }

                if (password == null || password.Length < 1)
                {
                    pw_overide = false;
                    return Enumerable.Repeat((byte)0xFF, pw_size).ToArray();
                }
                else
                {
                    pw_overide = true;

                    int diff = pw_size - password.Length;
                    if (diff < 0)
                        return password.Take(pw_size).ToArray();
                    else if (diff > 0)
                        return password.ToList().Concat(Enumerable.Repeat((byte)0xFF, diff)).ToArray();
                    else
                        return password.ToArray();
                }
            }
        }
    }
}