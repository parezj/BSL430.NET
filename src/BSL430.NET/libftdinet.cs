//***********************************************************************
/*
    FTDI CIL Bindings
	 - Original source by: Jelmer Vernstok - http://jelmer.vernstok.nl/oss/ftdi-cil
     - Updated using MonoDevelop in Linux by: Kristoffer Dominic Amora (a.k.a. coolnumber9)
*/
// ***********************************************************************

using System;
using System.Runtime.InteropServices;
using System.Collections;

namespace libftdinet
{	
	// FTDI chip type
	internal enum ChipType 
	{ 
		TYPE_AM=0, 
		TYPE_BM=1, 
		TYPE_2232C=2, 	
		// + coolnumber9, 03/01/10 
		TYPE_R=3,
		TYPE_2232H=4,
		TYPE_4232H=5
		// - coolnumber9, 03/01/10
	};

    // Parity mode for ftdi_set_line_property()
    internal enum ParityType { NONE=0, ODD=1, EVEN=2, MARK=3, SPACE=4 };

    // Number of stop bits for ftdi_set_line_property()
    internal enum StopBitsType { STOP_BIT_1=0, STOP_BIT_15=1, STOP_BIT_2=2 };

    // Number of bits for ftdi_set_line_property()
    internal enum BitsType { BITS_7=7, BITS_8=8 };

    // Break type for ftdi_set_line_property2()
    // + coolnumber9, 03/01/10
    internal enum BreakType
	{
		BREAK_OFF=0,
		BREAK_ON=1
	}
    // - coolnumber9, 03/01/10

    // MPSSE bitbang modes
    internal enum MpsseMode : uint 
	{
	    BITMODE_RESET  = 0x00,
	    BITMODE_BITBANG= 0x01,
	    BITMODE_MPSSE  = 0x02,
	    BITMODE_SYNCBB = 0x04,
	    BITMODE_MCU    = 0x08,		
		/* CPU-style fifo mode gets set via EEPROM */	// coolnumber9, 03/01/10
	    BITMODE_OPTO   = 0x10,
		BITMODE_CBUS   = 0x20	// coolnumber9, 03/01/10
	};

    /* Port interface code for FT2232C */
    internal enum Interface : uint 
	{
	    INTERFACE_ANY = 0,
	    INTERFACE_A   = 1,
	    INTERFACE_B   = 2,		
		// + coolnumber9, 03/01/10
		INTERFACE_C   = 3,
    	INTERFACE_D   = 4
		// - coolnumber9, 03/01/10
	};

	/* Shifting commands IN MPSSE Mode*/
	[Flags] enum MPSSEShiftCmds 
	{
		MPSSE_WRITE_NEG = 0x01,   /* Write TDI/DO on negative TCK/SK edge*/
		MPSSE_BITMODE   = 0x02,   /* Write bits, not bytes */
		MPSSE_READ_NEG  = 0x04,   /* Sample TDO/DI on negative TCK/SK edge */
		MPSSE_LSB       = 0x08,   /* LSB first */
		MPSSE_DO_WRITE  = 0x10,   /* Write TDI/DO */
		MPSSE_DO_READ   = 0x20,   /* Read TDO/DI */
		MPSSE_WRITE_TMS = 0x40    /* Write TMS/CS */
	};

    /* FTDI MPSSE commands */
    internal enum MPSSECommands 
	{
		SET_BITS_LOW   = 0x80,
		/*BYTE DATA*/
		/*BYTE Direction*/
		SET_BITS_HIGH  = 0x82,
		/*BYTE DATA*/
		/*BYTE Direction*/
		GET_BITS_LOW   = 0x81,
		GET_BITS_HIGH  = 0x83,
		LOOPBACK_START = 0x84,
		LOOPBACK_END   = 0x85,
		TCK_DIVISOR    = 0x86,		
		/* Value Low */
		/* Value HIGH */ /*rate is 12000000/((1+value)*2) */
		// #define DIV_VALUE(rate) (rate > 6000000)?0:((6000000/rate -1) > 0xffff)? 0xffff: (6000000/rate -1)		
		
		/* Commands in MPSSE and Host Emulation Mode */
		SEND_IMMEDIATE = 0x87,
		WAIT_ON_HIGH   = 0x88,
		WAIT_ON_LOW    = 0x89,
		

		/* Commands in Host Emulation Mode */
		READ_SHORT     = 0x90,
		/* Address_Low */
		READ_EXTENDED  = 0x91,
		/* Address High */
		/* Address Low  */
		WRITE_SHORT    = 0x92,
		/* Address_Low */
		WRITE_EXTENDED = 0x93,
		/* Address High */
		/* Address Low  */
	};

	/* Definitions for flow control  */
	//	#define SIO_RESET          0 /* Reset the port */
	//	#define SIO_MODEM_CTRL     1 /* Set the modem control register */
	//	#define SIO_SET_FLOW_CTRL  2 /* Set flow control register */
	//	#define SIO_SET_BAUD_RATE  3 /* Set baud rate */
	//	#define SIO_SET_DATA       4 /* Set the data characteristics of the port */
		
	//	#define FTDI_DEVICE_OUT_REQTYPE (USB_TYPE_VENDOR | USB_RECIP_DEVICE | USB_ENDPOINT_OUT)
	//	#define FTDI_DEVICE_IN_REQTYPE (USB_TYPE_VENDOR | USB_RECIP_DEVICE | USB_ENDPOINT_IN)
	
	/* Requests */
	//	#define SIO_RESET_REQUEST             SIO_RESET
	//	#define SIO_SET_BAUDRATE_REQUEST      SIO_SET_BAUD_RATE
	//	#define SIO_SET_DATA_REQUEST          SIO_SET_DATA
	//	#define SIO_SET_FLOW_CTRL_REQUEST     SIO_SET_FLOW_CTRL
	//	#define SIO_SET_MODEM_CTRL_REQUEST    SIO_MODEM_CTRL
	//	#define SIO_POLL_MODEM_STATUS_REQUEST 0x05
	//	#define SIO_SET_EVENT_CHAR_REQUEST    0x06
	//	#define SIO_SET_ERROR_CHAR_REQUEST    0x07
	//	#define SIO_SET_LATENCY_TIMER_REQUEST 0x09
	//	#define SIO_GET_LATENCY_TIMER_REQUEST 0x0A
	//	#define SIO_SET_BITMODE_REQUEST       0x0B
	//	#define SIO_READ_PINS_REQUEST         0x0C
	//	#define SIO_READ_EEPROM_REQUEST       0x90
	//	#define SIO_WRITE_EEPROM_REQUEST      0x91
	//	#define SIO_ERASE_EEPROM_REQUEST      0x92
		
		
	//	#define SIO_RESET_SIO 0
	//	#define SIO_RESET_PURGE_RX 1
	//	#define SIO_RESET_PURGE_TX 2
		
	//	#define SIO_DISABLE_FLOW_CTRL 0x0
	//	#define SIO_RTS_CTS_HS (0x1 << 8)
	//	#define SIO_DTR_DSR_HS (0x2 << 8)
	//	#define SIO_XON_XOFF_HS (0x4 << 8)
		
	//	#define SIO_SET_DTR_MASK 0x1
	//	#define SIO_SET_DTR_HIGH ( 1 | ( SIO_SET_DTR_MASK  << 8))
	//	#define SIO_SET_DTR_LOW  ( 0 | ( SIO_SET_DTR_MASK  << 8))
	//	#define SIO_SET_RTS_MASK 0x2
	//	#define SIO_SET_RTS_HIGH ( 2 | ( SIO_SET_RTS_MASK << 8 ))
	//	#define SIO_SET_RTS_LOW ( 0 | ( SIO_SET_RTS_MASK << 8 ))
		
	//	#define SIO_RTS_CTS_HS (0x1 << 8)
	
	/* marker for unused usb urb structures
   	//	(taken from libusb) */
	//	#define FTDI_URB_USERCONTEXT_COOKIE ((void *)0x1)

	
	[StructLayout(LayoutKind.Sequential)] struct ftdi_context 
	{
		// USB specific
		/* libusb's usb_dev_handle */
		IntPtr usb_dev;
		int usb_read_timeout;
		int usb_write_timeout;
	
		// FTDI specific
		/* FTDI chip type */
		ChipType type;
		int baudrate;
		byte bitbang_enabled;
		IntPtr readbuffer; /* byte * */ /* pointer to read buffer for ftdi_read_data */
		uint readbuffer_offset;
		uint readbuffer_remaining;	/* number of remaining data in internal read buffer */
		uint readbuffer_chunksize;	/* read buffer chunk size */
		uint writebuffer_chunksize;	/* write buffer chunk size */
	
		// FTDI FT2232C requirecments
		/* FT2232C interface number: 0 or 1 */
		int iface;       // 0 or 1
		/* FT2232C index number: 1 or 2 */
		int index;       // 1 or 2
		// Endpoints
		/* FT2232C end points: 1 or 2 */
		int in_ep;
		int out_ep;      // 1 or 2
	
		/* 1: (default) Normal bitbang mode, 2: FT2232C SPI bitbang mode */
		byte bitbang_mode;
		int eeprom_size;	// coolnumber9, 03/01/10
	
		// misc
		/* String representation of last error */
		IntPtr error_str; 			/* const char * */		
    	IntPtr async_usb_buffer; 	/* Buffer needed for async communication */
		uint async_usb_buffe_rsize;	/* Number of URB-structures we can buffer */
	};	
	
	[StructLayout(LayoutKind.Sequential)]
    internal struct ftdi_eeprom 
	{
		// init and build eeprom from ftdi_eeprom structure
		[DllImport("libftdi1", CallingConvention = CallingConvention.Cdecl)] internal static extern int ftdi_eeprom_build(ref ftdi_eeprom eeprom, ref byte[] output);
		[DllImport("libftdi1", CallingConvention = CallingConvention.Cdecl)] internal static extern void ftdi_eeprom_initdefaults(out ftdi_eeprom eeprom);

		int vendor_id;
		int product_id;
	
		int self_powered;
		int remote_wakeup;
		int BM_type_chip;
	
		int in_is_isochronous;
		int out_is_isochronous;
		int suspend_pull_downs;
	
		int use_serial;
		int change_usb_version;
		int usb_version;
		int max_power;
	
		string manufacturer;
		string product;
		string serial;	
		/* EEPROM size in bytes. This doesn't get stored in the EEPROM
        but is the only way to pass it to ftdi_eeprom_build. */
		int size;	// coolnumber9, 03/01/10
	};

    internal class FTDIContext 
	{
		private ftdi_context ftdi = new ftdi_context();

		[DllImport("libftdi1", CallingConvention = CallingConvention.Cdecl)] internal static extern int ftdi_init(ref ftdi_context ftdi);
		[DllImport("libftdi1", CallingConvention = CallingConvention.Cdecl)] internal static extern void ftdi_deinit(ref ftdi_context ftdi);
		[DllImport("libftdi1", CallingConvention = CallingConvention.Cdecl)] internal static extern int ftdi_usb_open(ref ftdi_context ftdi, int vendor, int product);
		[DllImport("libftdi1", CallingConvention = CallingConvention.Cdecl)] internal static extern int ftdi_usb_open_desc(ref ftdi_context ftdi, int vendor, int product, string description, string serial);

		private FTDIContext() 
		{
			ftdi_init(ref ftdi);
		}
        public FTDIContext(Object obj) : this()
        {
        }
        public FTDIContext(int vendor, int product) : this() 
		{
			CheckRet(ftdi_usb_open(ref ftdi, vendor, product));
		}

		public FTDIContext(int vendor, int product, string description, string serial) : this() 
		{
			int ret = ftdi_usb_open_desc(ref ftdi, vendor, product, description, serial);
			CheckRet(ret);
		}

		[DllImport("libftdi1", CallingConvention = CallingConvention.Cdecl)] internal static extern int ftdi_usb_open_dev(ref ftdi_context ftdi, IntPtr dev);
		public FTDIContext(IntPtr dev) : this() 
		{
			CheckRet(ftdi_usb_open_dev(ref ftdi, dev));
		}

		// destructor
		~FTDIContext() 
		{
			ftdi_deinit(ref ftdi);
		}

		[DllImport("libftdi1", CallingConvention = CallingConvention.Cdecl)] internal static extern int ftdi_set_baudrate(ref ftdi_context ftdi, int baudrate);
		public int Baudrate 
		{ 
			set 
			{
				CheckRet(ftdi_set_baudrate(ref ftdi, value));
			}
		}
        [DllImport("libftdi1", CallingConvention = CallingConvention.Cdecl)] internal static extern int ftdi_set_flowctrl(ref ftdi_context ftdi, int flowctrl);
        public int FlowControl
        {
            set
            {
                CheckRet(ftdi_set_flowctrl(ref ftdi, value));
            }
        }

        [DllImport("libftdi1", CallingConvention = CallingConvention.Cdecl)] internal static extern int ftdi_set_setrts(ref ftdi_context ftdi, int state);
        public int SetRTS
        {
            set
            {
                CheckRet(ftdi_set_setrts(ref ftdi, value));
            }
        }

        [DllImport("libftdi1", CallingConvention = CallingConvention.Cdecl)] internal static extern int ftdi_setdtr(ref ftdi_context ftdi, int state);
        public int SetDTR
        {
            set
            {
                CheckRet(ftdi_setdtr(ref ftdi, value));
            }
        }

        internal void CheckRet(int ret) 
		{
			if (ret >= 0) 
				return;

            try
            {
                ftdi_free(ref ftdi);
            }
            catch (Exception) { }

			IntPtr str = ftdi_get_error_string(ref ftdi);
			
			Console.WriteLine("{0}", ret);
			throw new Exception(Marshal.PtrToStringAnsi(str));
		}

		[DllImport("libftdi1", CallingConvention = CallingConvention.Cdecl)] internal static extern int ftdi_usb_reset(ref ftdi_context ftdi);
		
		public void Reset() 
		{
			CheckRet(ftdi_usb_reset(ref ftdi));
		}

		[DllImport("libftdi1", CallingConvention = CallingConvention.Cdecl)] internal static extern int ftdi_usb_close(ref ftdi_context ftdi);
        [DllImport("libftdi1", CallingConvention = CallingConvention.Cdecl)] internal static extern int ftdi_free(ref ftdi_context ftdi);

        public void Close() 
		{
            try
            {
                CheckRet(ftdi_usb_close(ref ftdi));
            }
            catch (Exception ex) { throw new Exception(ex.Message); }
            finally
            {
                try
                {
                    //ftdi_free(ref ftdi); TODO
                }
                catch (Exception) { }
            }
        }

		[DllImport("libftdi1", CallingConvention = CallingConvention.Cdecl)] internal static extern int ftdi_usb_purge_buffers(ref ftdi_context ftdi);

		public void PurgeBuffers() 
		{
			CheckRet(ftdi_usb_purge_buffers(ref ftdi));
		}

		[DllImport("libftdi1", CallingConvention = CallingConvention.Cdecl)] internal static extern int ftdi_read_data_set_chunksize(ref ftdi_context ftdi, uint chunksize);
		[DllImport("libftdi1", CallingConvention = CallingConvention.Cdecl)] internal static extern int ftdi_read_data_get_chunksize(ref ftdi_context ftdi, out uint chunksize);

		public uint ReadChunkSize 
		{
			set 
			{ 
				CheckRet(ftdi_read_data_set_chunksize(ref ftdi, value)); 
			}
			get 
			{
				uint chunksize;
				CheckRet(ftdi_read_data_get_chunksize(ref ftdi, out chunksize));
				 return chunksize;
			}
		}

		[DllImport("libftdi1", CallingConvention = CallingConvention.Cdecl)] internal static extern int ftdi_write_data_set_chunksize(ref ftdi_context ftdi, uint chunksize);
		[DllImport("libftdi1", CallingConvention = CallingConvention.Cdecl)] internal static extern int ftdi_write_data_get_chunksize(ref ftdi_context ftdi, out uint chunksize);
		public uint WriteChunkSize 
		{
			set 
			{
				CheckRet(ftdi_write_data_set_chunksize(ref ftdi, value));
			}
			get 
			{
				uint chunksize;
				CheckRet(ftdi_write_data_get_chunksize(ref ftdi, out chunksize));
				return chunksize;
			}
		}

		[DllImport("libftdi1", CallingConvention = CallingConvention.Cdecl)] internal static extern int ftdi_set_latency_timer(ref ftdi_context ftdi, byte latency);
		[DllImport("libftdi1", CallingConvention = CallingConvention.Cdecl)] internal static extern int ftdi_get_latency_timer(ref ftdi_context ftdi, out byte latency);
		public byte LatencyTimer 
		{
			set 
			{
				CheckRet(ftdi_set_latency_timer(ref ftdi, value));
			}
			get 
			{
				byte latency;
				CheckRet(ftdi_get_latency_timer(ref ftdi, out latency));
				return latency;
			}
		}

		[DllImport("libftdi1", CallingConvention = CallingConvention.Cdecl)] internal static extern IntPtr ftdi_get_error_string(ref ftdi_context ftdi);

		// "eeprom" needs to be valid 128 byte eeprom (generated by the eeprom generator)
		// the checksum of the eeprom is valided
		[DllImport("libftdi1", CallingConvention = CallingConvention.Cdecl)] internal static extern int ftdi_read_eeprom(ref ftdi_context ftdi, ref byte[] eeprom);
		[DllImport("libftdi1", CallingConvention = CallingConvention.Cdecl)] internal static extern int ftdi_write_eeprom(ref ftdi_context ftdi, ref byte[] eeprom);
		[DllImport("libftdi1", CallingConvention = CallingConvention.Cdecl)] internal static extern int ftdi_erase_eeprom(ref ftdi_context ftdi);

		public void WriteEEPROM(ftdi_eeprom eeprom)
		{
			byte[] data = new byte[128];

			ftdi_eeprom.ftdi_eeprom_build(ref eeprom, ref data); 

			CheckRet(ftdi_write_eeprom(ref ftdi, ref data));
		}

		public void EraseEEPROM()
		{
			CheckRet(ftdi_erase_eeprom(ref ftdi));
		}

		public byte[] ReadEEPROM()
		{
			/* FIXME */
			return null;
		}

		[DllImport("libftdi1", CallingConvention = CallingConvention.Cdecl)] internal static extern int ftdi_read_data(ref ftdi_context ftdi, byte[] buf, int size);
		public int ReadData(byte[] buf, int size)
		{
			int ret = ftdi_read_data(ref ftdi, buf, size); 
			CheckRet(ret);
			return ret;
		}
		
		[DllImport("libftdi1", CallingConvention = CallingConvention.Cdecl)] internal static extern int ftdi_write_data(ref ftdi_context ftdi, byte[] buf, int size);
		public int WriteData(byte[] buf, int size)
		{
			int ret = ftdi_write_data(ref ftdi, buf, size); 
			CheckRet(ret);
			return ret;
		}

		[DllImport("libftdi1", CallingConvention = CallingConvention.Cdecl)] internal static extern int ftdi_set_interface(ref ftdi_context ftdi, Interface iface);
		public Interface Interface 
		{ 
			set 
			{
				CheckRet(ftdi_set_interface(ref ftdi, value));
			}
		}

		[DllImport("libftdi1", CallingConvention = CallingConvention.Cdecl)] internal static extern int ftdi_read_pins(ref ftdi_context ftdi, out byte pins);
		public byte GetPins() 
		{
			byte pins;
			CheckRet(ftdi_read_pins(ref ftdi, out pins));
			return pins;
		}

		[DllImport("libftdi1", CallingConvention = CallingConvention.Cdecl)] internal static extern int ftdi_enable_bitbang(ref ftdi_context ftdi, byte bitmask);
		[DllImport("libftdi1", CallingConvention = CallingConvention.Cdecl)] internal static extern int ftdi_disable_bitbang(ref ftdi_context ftdi);

		public void EnableBitBang(byte bitmask) 
		{
			CheckRet(ftdi_enable_bitbang(ref ftdi, bitmask));
		}

		public void DisableBitBang() 
		{
			CheckRet(ftdi_disable_bitbang(ref ftdi));
		}

		[DllImport("libftdi1", CallingConvention = CallingConvention.Cdecl)] internal static extern int ftdi_set_bitmode(ref ftdi_context ftdi, byte bitmask, byte mode);
		public void SetBitMode(byte bitmask, byte mode) 
		{
			CheckRet(ftdi_set_bitmode(ref ftdi, bitmask, mode));
		}

		[DllImport("libftdi1", CallingConvention = CallingConvention.Cdecl)] internal static extern int ftdi_set_line_property(ref ftdi_context ftdi, BitsType bits, StopBitsType sbit, ParityType parity);
		public void SetLineProperty(BitsType bits, StopBitsType sbit, ParityType parity) 
		{
			CheckRet(ftdi_set_line_property(ref ftdi, bits, sbit, parity));
		}
	}
}
