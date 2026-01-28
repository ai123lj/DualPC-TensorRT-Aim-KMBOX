using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MWModle
{
    public partial class LibMWCapture
    {
        static public UInt32 MWCAP_VIDEO_PROCESS_FLIP = 1;
        static public UInt32 MWCAP_VIDEO_PROCESS_MIRROR = 2;
        public enum MW_RESULT
        {
            MW_SUCCEEDED = 0x00,
            MW_FAILED,
            MW_INVALID_PARAMS,
        }


        public enum MWCAP_PRODUCT_ID
        {
            MWCAP_PRODUCT_ID_PRO_CAPTURE_AIO = 0x00000102,
            MWCAP_PRODUCT_ID_PRO_CAPTURE_DVI = 0x00000103,
            MWCAP_PRODUCT_ID_PRO_CAPTURE_HDMI = 0x00000104,
            MWCAP_PRODUCT_ID_PRO_CAPTURE_SDI = 0x00000105,
            MWCAP_PRODUCT_ID_PRO_CAPTURE_DUAL_SDI = 0x00000106,
            MWCAP_PRODUCT_ID_PRO_CAPTURE_DUAL_DVI = 0x00000107,
            MWCAP_PRODUCT_ID_PRO_CAPTURE_DUAL_HDMI = 0x00000108,
            MWCAP_PRODUCT_ID_PRO_CAPTURE_QUAD_SDI = 0x00000109,
            MWCAP_PRODUCT_ID_PRO_CAPTURE_QUAD_HDMI = 0x00000110,
            MWCAP_PRODUCT_ID_PRO_CAPTURE_MINI_HDMI = 0x00000111,
            MWCAP_PRODUCT_ID_PRO_CAPTURE_HDMI_4K = 0x00000112,
            MWCAP_PRODUCT_ID_PRO_CAPTURE_MINI_SDI = 0x00000113
        }


        public enum MWCAP_VIDEO_INPUT_TYPE
        {
            MWCAP_VIDEO_INPUT_TYPE_NONE = 0x00,
            MWCAP_VIDEO_INPUT_TYPE_HDMI = 0x01,
            MWCAP_VIDEO_INPUT_TYPE_VGA = 0x02,
            MWCAP_VIDEO_INPUT_TYPE_SDI = 0x04,
            MWCAP_VIDEO_INPUT_TYPE_COMPONENT = 0x08,
            MWCAP_VIDEO_INPUT_TYPE_CVBS = 0x10,
            MWCAP_VIDEO_INPUT_TYPE_YC = 0x20

        }

        public enum MWCAP_AUDIO_INPUT_TYPE
        {
            MWCAP_AUDIO_INPUT_TYPE_NONE = 0x00,
            MWCAP_AUDIO_INPUT_TYPE_HDMI = 0x01,
            MWCAP_AUDIO_INPUT_TYPE_SDI = 0x02,
            MWCAP_AUDIO_INPUT_TYPE_LINE_IN = 0x04,
            MWCAP_AUDIO_INPUT_TYPE_MIC_IN = 0x08
        };

        public enum MWCAP_VIDEO_FRAME_TYPE
        {
            MWCAP_VIDEO_FRAME_2D = 0x00,
            MWCAP_VIDEO_FRAME_3D_TOP_AND_BOTTOM_FULL = 0x01,
            MWCAP_VIDEO_FRAME_3D_TOP_AND_BOTTOM_HALF = 0x02,
            MWCAP_VIDEO_FRAME_3D_SIDE_BY_SIDE_FULL = 0x03,
            MWCAP_VIDEO_FRAME_3D_SIDE_BY_SIDE_HALF = 0x04
        }

        public enum MWCAP_VIDEO_COLOR_FORMAT
        {
            MWCAP_VIDEO_COLOR_FORMAT_UNKNOWN = 0x00,
            MWCAP_VIDEO_COLOR_FORMAT_RGB = 0x01,
            MWCAP_VIDEO_COLOR_FORMAT_YUV601 = 0x02,
            MWCAP_VIDEO_COLOR_FORMAT_YUV709 = 0x03,
            MWCAP_VIDEO_COLOR_FORMAT_YUV2020 = 0x04,
            MWCAP_VIDEO_COLOR_FORMAT_YUV2020C = 0x05				// Constant luminance, not supported yet.
        }

        public enum MWCAP_VIDEO_QUANTIZATION_RANGE
        {
            MWCAP_VIDEO_QUANTIZATION_UNKNOWN = 0x00,
            MWCAP_VIDEO_QUANTIZATION_FULL = 0x01, 			// Black level: 0, White level: 255/1023/4095/65535
            MWCAP_VIDEO_QUANTIZATION_LIMITED = 0x02				// Black level: 16/64/256/4096, White level: 235(240)/940(960)/3760(3840)/60160(61440)
        }


        public enum MWCAP_VIDEO_SATURATION_RANGE
        {
            MWCAP_VIDEO_SATURATION_UNKNOWN = 0x00,
            MWCAP_VIDEO_SATURATION_FULL = 0x01, 			// Min: 0, Max: 255/1023/4095/65535
            MWCAP_VIDEO_SATURATION_LIMITED = 0x02, 			// Min: 16/64/256/4096, Max: 235(240)/940(960)/3760(3840)/60160(61440)
            MWCAP_VIDEO_SATURATION_EXTENDED_GAMUT = 0x03  			// Min: 1/4/16/256, Max: 254/1019/4079/65279
        }

        public enum MW_FAMILY_ID
        {
            MW_FAMILY_ID_PRO_CAPTURE = 0x00,                            ///<Pro Capture family
            MW_FAMILY_ID_ECO_CAPTURE = 0x01,                            ///<Eco Capture family
            MW_FAMILY_ID_USB_CAPTURE = 0x02                         ///<USB Capture family
        }
		
		public enum MWCAP_VIDEO_PROC_PARAM_TYPE
        {
            MWCAP_VIDEO_PROC_BRIGHTNESS,                                                                                        ///<Brightness
            MWCAP_VIDEO_PROC_CONTRAST,                                                                                          ///<Contrast
            MWCAP_VIDEO_PROC_HUE,                                                                                               ///<Hue
            MWCAP_VIDEO_PROC_SATURATION                                                                                         ///<Saturation
        }

		
#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern MW_RESULT MWGetVersion(ref Byte pbyMaj, ref Byte pbyMin, ref UInt16 pwBuild);

        ///////////////////////////////////////////////////////////////////////////////
        //  Initialized  and UnInitialized LibXIStream library
#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern int MWCaptureInitInstance();



#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern void MWCaptureExitInstance();


        ///////////////////////////////////////////////////////////////////////////////
        // Device query
#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern MW_RESULT MWRefreshDevice();



#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern int MWGetChannelCount();



        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct MWCAP_CHANNEL_INFO
        {
            public UInt16 wFamilyID;
            public UInt16 wProductID;
            public SByte chHardwareVersion;
            public Byte byFirmwareID;
            public UInt32 dwFirmwareVersion;
            public UInt32 dwDriverVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szFamilyName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szProductName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szFirmwareName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string szBoardSerialNo;
            public Byte byBoardIndex;
            public Byte byChannelIndex;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct MWCAP_PRO_CAPTURE_INFO
        {
            public Byte byPCIBusID;
            public Byte byPCIDevID;
            public Byte byLinkType;
            public Byte byLinkWidth;
            public Byte byBoardIndex;
            public UInt16 wMaxPayloadSize;
            public UInt16 wMaxReadRequestSize;
            public UInt32 cbTotalMemorySize;
            public UInt32 cbFreeMemorySize;
        }

#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern MW_RESULT MWGetChannelInfoByIndex(int nIndex, ref LibMWCapture.MWCAP_CHANNEL_INFO pChannelInfo);


#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern MW_RESULT MWGetFamilyInfoByIndex(int nIndex, IntPtr pFamilyInfo, UInt32 dwSize);

#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern MW_RESULT MWGetDevicePath(int nIndex, UInt16[] pDevicePath);


#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern MW_RESULT MWGetChannelInfo(IntPtr hChannel, ref MWCAP_CHANNEL_INFO pChannelInfo);


//////////////////////////////////////////////////////////////////////////////
// Channel Open and Close

#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern IntPtr MWOpenChannel(int nBoardValue, int nChannelIndex); // string szDShowID

#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern IntPtr MWOpenChannelByPath(UInt16[] pszDevicePath);

#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern void MWCloseChannel(IntPtr hChannel);



        // Video signal status
        public enum MWCAP_VIDEO_SIGNAL_STATE
        {
            MWCAP_VIDEO_SIGNAL_NONE,						// No signal detectd
            MWCAP_VIDEO_SIGNAL_UNSUPPORTED,					// Video signal status not valid
            MWCAP_VIDEO_SIGNAL_LOCKING,						// Video signal status valid but not locked yet
            MWCAP_VIDEO_SIGNAL_LOCKED						// Every thing OK
        }

        public enum MWCAP_AUDIO_CAPTURE_NODE { 
            MWCAP_AUDIO_CAPTURE_NODE_DEFAULT,
            MWCAP_AUDIO_CAPTURE_NODE_EMBEDDED_CAPTURE,
            MWCAP_AUDIO_CAPTURE_NODE_MICROPHONE,
            MWCAP_AUDIO_CAPTURE_NODE_USB_CAPTURE,
            MWCAP_AUDIO_CAPTURE_NODE_LINE_IN,
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct MWCAP_VIDEO_BUFFER_INFO
        {
            public UInt32 cMaxFrames;                                   ///<Maximum number of frames in on-board cache

            public Byte iNewestBuffering;                          ///<The number of the slices being bufferred. A frame of video data may contain multiple slices.
            public Byte iBufferingFieldIndex;                      ///<The sequence number of fields being bufferred.

            public Byte iNewestBuffered;                           ///<The sequence number of slices the latest bufferred piece.
            public Byte iBufferedFieldIndex;                       ///<The sequence number of the latest bufferred field

            public Byte iNewestBufferedFullFrame;                  ///<The sequence number of the latest bufferred frame
            public UInt32 cBufferedFullFrames;                      ///<Number of fully bufferred full frames
        }

#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern MW_RESULT MWGetVideoBufferInfo(IntPtr hChannel, ref MWCAP_VIDEO_BUFFER_INFO pVideoBufferInfo);

        public enum MWCAP_VIDEO_FRAME_STATE
        {
            MWCAP_VIDEO_FRAME_STATE_INITIAL,                                                            ///<Initial
            MWCAP_VIDEO_FRAME_STATE_F0_BUFFERING,                                                       ///<Buffering top subframe
            MWCAP_VIDEO_FRAME_STATE_F1_BUFFERING,                                                       ///<Buffering bottom subframe
            MWCAP_VIDEO_FRAME_STATE_BUFFERED                                                            ///<Fully bufferred video frame 
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct MWCAP_SMPTE_TIMECODE
        {
            Byte byFrames;                                  ///<Frames number
            Byte bySeconds;                                 ///<Seconds
            Byte byMinutes;                                 ///<Minutes
            Byte byHours;                                   ///<Hours
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct MWCAP_VIDEO_FRAME_INFO
        {
            public MWCAP_VIDEO_FRAME_STATE state;                                      ///<The state of the video framess

            public Byte bInterlaced;                                ///<Whether an interlaced signal
            public Byte bSegmentedFrame;                            ///<Whether a segmented frame
            public Byte bTopFieldFirst;                             ///<Whether the top subframe is in front
            public Byte bTopFieldInverted;                          ///<Whether to reverse the top subframe

            public int cx;                                         ///<Width of video frames
            public int cy;                                         ///<Height of video frames
            public int nAspectX;                                   ///<Width of the ratio 
            public int nAspectY;                                   ///<Height of the ratio
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public Int64[] allFieldStartTimes;                     ///<Start time of capturing top and bottom subframe respectively
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public Int64[] allFieldBufferedTimes;                  ///<Fully bufferred time of top and bottom frame respectively
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public MWCAP_SMPTE_TIMECODE[] aSMPTETimeCodes;                            ///<Time code of top and bottom frame respectively
        }
#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern MW_RESULT MWGetVideoFrameInfo(IntPtr hChannel, Byte i, ref MWCAP_VIDEO_FRAME_INFO pVideoFrameInfo);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi,Pack = 1)]
        public struct MWCAP_VIDEO_SIGNAL_STATUS
        {
            public MWCAP_VIDEO_SIGNAL_STATE state;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public int cxTotal;
            public int cyTotal;
            public Byte bInterlaced;
            public UInt32 dwFrameDuration;
            public int nAspectX;
            public int nAspectY;
            public Byte bSegmentedFrame;
            public MWCAP_VIDEO_FRAME_TYPE frameType;
            public MWCAP_VIDEO_COLOR_FORMAT colorFormat;
            public MWCAP_VIDEO_QUANTIZATION_RANGE quantRange;
            public MWCAP_VIDEO_SATURATION_RANGE satRange;
        }

#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern MW_RESULT MWGetVideoSignalStatus(IntPtr hChannel, ref MWCAP_VIDEO_SIGNAL_STATUS pSignalStatus);


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct MWCAP_AUDIO_SIGNAL_STATUS
        {
            public UInt16 wChannelValid;                         ///<Valid audio channel mask.The lowest bit indicates whether the 1st and 2nd channels are valid, the second bit indicates whether the 3rd and 4th channels are valid, the third bit indicates whether the 5th and 6th channels are valid, and the fourth bit indicates whether the 7th and 8th channels are valid.
            public Byte bLPCM;                                  ///<Whether the signal is LPCM
            public Byte cBitsPerSample;                            ///<Bit depth of each audio sampling
            public UInt32 dwSampleRate;                         ///<Sample rate
            public Byte bChannelStatusValid;                    ///<Whether channel status is valid
            //public IEC60958_CHANNEL_STATUS channelStatus;                          ///<The audio channel status
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
            public Byte[] channelStatus;
        }


#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern MW_RESULT MWGetAudioSignalStatus(IntPtr hChannel, ref MWCAP_AUDIO_SIGNAL_STATUS pSignalStatus);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct AUDIO_FORMAT_INFO
        {
            public UInt32 nChannels;                                                                                         ///<Nmuber of audio channels
            public UInt32 nBitCount;                                                                                         ///<Bit stepth of audio sample
            public UInt32 nSamplerate;                                                                                       ///<Audio sample rate
        }

#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern Byte MWGetAudioCaptureSupportFormat(IntPtr hChannel, MWCAP_AUDIO_CAPTURE_NODE captureNode, IntPtr pFormat, ref Int32 nCount);


        //////////////////////////////////////////////////////////////////////////////////
        // video capture
        public delegate void VIDEO_CAPTURE_STDCALLBACK(IntPtr pbFrame, int vbFrame, ulong u64TimeStamp, IntPtr pParam);

        public delegate void AUDIO_CAPTURE_STDCALLBACK(IntPtr pbFrame, int vbFrame, ulong u64TimeStamp, IntPtr pParam);

#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern LibMWCapture.MW_RESULT MWStartVideoCapture(IntPtr hChannel, IntPtr hEvent);

#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern LibMWCapture.MW_RESULT MWStartAudioCapture(IntPtr hChannel);

#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern LibMWCapture.MW_RESULT MWStopAudioCapture(IntPtr hChannel);

#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern IntPtr MWCreateVideoCaptureWithStdCallBack(IntPtr hChannel, int nWidth, int nHeight, uint nFourcc, int nFrameDuration, VIDEO_CAPTURE_STDCALLBACK callback, IntPtr pParam);

#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern IntPtr MWDestoryVideoCapture(IntPtr hVideo);

#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern IntPtr MWCreateAudioCaptureWithStdCallBack(IntPtr hChannel, MWCAP_AUDIO_CAPTURE_NODE captureNode, int dwSamplesPerSec, short wBitsPerSample,short wChannels, AUDIO_CAPTURE_STDCALLBACK call_back,IntPtr pParam);

#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern IntPtr MWDestoryAudioCapture(IntPtr hVideo);

#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern LibMWCapture.MW_RESULT MWStopVideoCapture(IntPtr hChannel);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        public enum MWCAP_VIDEO_DEINTERLACE_MODE
        {
            MWCAP_VIDEO_DEINTERLACE_WEAVE = 0x00,///<Weave mode
            MWCAP_VIDEO_DEINTERLACE_BLEND = 0x01,///<Blend mode
            MWCAP_VIDEO_DEINTERLACE_TOP_FIELD = 0x02,///<Only uses top subframe data
            MWCAP_VIDEO_DEINTERLACE_BOTTOM_FIELD = 0x03 ///<Only uses bottom subframe data
        }

        public enum MWCAP_VIDEO_ASPECT_RATIO_CONVERT_MODE
        {
            MWCAP_VIDEO_ASPECT_RATIO_IGNORE = 0x00,///<Ignore: Ignores the original aspect ratio and stretches to full-screen.
            MWCAP_VIDEO_ASPECT_RATIO_CROPPING = 0x01,///<Cropping: Expands to full-screen and remove parts of the image when necessary to keep the original aspect ratio.
            MWCAP_VIDEO_ASPECT_RATIO_PADDING = 0x02 ///<Padding: Fits to screen and add black borders to keep the original aspect ratio.
        }





        static public int MWCAP_VIDEO_FRAME_ID_NEWEST_BUFFERED = -1;

#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern LibMWCapture.MW_RESULT MWCaptureVideoFrameToVirtualAddress(
                    IntPtr hChannel,
                    int iFrame,
                    IntPtr pbFrame,
                    UInt32 cbFrame,
                    UInt32 cbStride,
                    Byte bBottomUp,
                    Int64 pvContext,
                    UInt32 dwFOURCC,
                    int cx,
                    int cy
                    );


        [StructLayout(LayoutKind.Explicit, CharSet=CharSet.Ansi,Pack=1)]
        public struct MWCAP_VIDEO_CAPTURE_STATUS 
        {
            [FieldOffset(0)]
	        public Int64								pvContext;
            [FieldOffset(8)]
    	    public Byte	                                bPhysicalAddress;

            [FieldOffset(9)]
            public IntPtr                               pvFrame;
            [FieldOffset(9)]
            public Int64 liPhysicalAddress;

            [FieldOffset(17)]
	        public  int									iFrame;
            [FieldOffset(21)]
	        public Byte									bFrameCompleted;

            [FieldOffset(22)]
	        public UInt16								cyCompleted;

            [FieldOffset(24)]
	        public UInt16								cyCompletedPrev;
        }

#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern MW_RESULT MWGetVideoCaptureStatus(IntPtr 	hChannel, IntPtr pStatus);


///////////////////////////////////////////////////////////////////////////
// timer event
#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern UInt64 MWRegisterTimer(IntPtr	hChannel, IntPtr  hEvent);


#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern MW_RESULT MWUnregisterTimer(IntPtr hChannel, UInt64 hTimer);



#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern MW_RESULT MWScheduleTimer(IntPtr hChannel, UInt64 hTimer, Int64 llExpireTime);



        ///////////////////////////////////////////////////////////////////////////////
        // Notify event
        static public UInt32 MWCAP_NOTIFY_VIDEO_SIGNAL_CHANGE = 0x0020;
        static public UInt32 MWCAP_NOTIFY_VIDEO_FRAME_BUFFERING = 0x0100;
        static public UInt32 MWCAP_NOTIFY_VIDEO_FRAME_BUFFERED = 0x0400;
        static public UInt32 MWCAP_NOTIFY_AUDIO_SIGNAL_CHANGE = 0x0040;
        static public UInt32 MWCAP_NOTIFY_AUDIO_FRAME_BUFFERED = 0x1000;
        

#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern UInt64 MWRegisterNotify(IntPtr hChannel, IntPtr hEvent, UInt32	dwEnableBits);


#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern MW_RESULT MWUnregisterNotify(IntPtr hChannel, UInt64 hNotify);


#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern MW_RESULT MWGetNotifyStatus(IntPtr hChannel, UInt64 hNotify, ref UInt64 ullStatus);


////////////////////////////////////////////////////////////////////////////////////
// Device clock
#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern MW_RESULT MWGetDeviceTime(IntPtr hChannel, ref long pllTime);

#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern MW_RESULT MWRegulateDeviceTime(IntPtr hChannel, long pllTime);

#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern MW_RESULT MWPinVideoBuffer(IntPtr hChannel, byte[] pbFrame, UInt32 cbFrame);

#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern MW_RESULT MWUnpinVideoBuffer(IntPtr hChannel, byte[] pbFrame);

#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern MW_RESULT MWCaptureVideoFrameToVirtualAddressEx(IntPtr hChannel, Int32 iFrame, byte[] pbFrame, UInt32 cbFrame, UInt32 cbStride,
            Byte bBottomUp,UInt64 pvContext,UInt32 dwFOURCC,Int32 cx,Int32 cy,UInt32 dwProcessSwitchs,Int32 cyParitalNotify,UInt64 hOSDImage,
            RECT[] pOSDRects,Int32 cOSDRects,Int16 sContrast,Int16 sBrightness,Int16 sSaturation,Int16 sHue,
            MWCAP_VIDEO_DEINTERLACE_MODE deinterlaceMode, MWCAP_VIDEO_ASPECT_RATIO_CONVERT_MODE aspectRatioConvertMode,
            RECT[] pRectSrc,RECT[] pRectDest,Int32 nAspectX,Int32 nAspectY, MWCAP_VIDEO_COLOR_FORMAT  colorFormat,
	        MWCAP_VIDEO_QUANTIZATION_RANGE quantRange,MWCAP_VIDEO_SATURATION_RANGE satRange);


        static public UInt32 MWCAP_AUDIO_SAMPLES_PER_FRAME = 192;
        static public UInt32 MWCAP_AUDIO_MAX_NUM_CHANNELS = 8;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct MWCAP_AUDIO_CAPTURE_FRAME
        {
            public UInt32 cFrameCount;                                                              ///<Number of bufferred frames
            public UInt32 iFrame;                                                                       ///<Current frame index
            public UInt32 dwSyncCode;                                                                   ///<Sync code of audio frame data
            public UInt32 dwFlags;                                                                  ///<Reserved
            public Int64 llTimestamp;                                                               ///<The timestamp of audio frame
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = ((int)(192*8)))]
            public UInt32[] adwSamples; ///<Audio sample data. Each sample is 32-bit width, and high bit effective. The priority of the path is: Left0, Left1, Left2, Left3, right0, right1, right2, right3
        }

#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern MW_RESULT MWCaptureAudioFrame(IntPtr hChannel, ref MWCAP_AUDIO_CAPTURE_FRAME pAudioCaptureFrame);



        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct MWCAP_VIDEO_ECO_CAPTURE_OPEN
        {
            public UInt64 hEvent;                                                             ///<Handle of capture event

            public UInt32 dwFOURCC;                                                         ///<Capture format
            public UInt16 cx;                                                                    ///<Width
            public UInt16 cy;                                                                    ///<Height
            public Int64 llFrameDuration;                                                   ///<Interval, -1 indicates follow format of input source
        }
#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern MW_RESULT MWStartVideoEcoCapture(IntPtr hChannel, ref MWCAP_VIDEO_ECO_CAPTURE_OPEN pEcoCaptureOpen);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct MWCAP_VIDEO_ECO_CAPTURE_FRAME
        {
            public UInt64 pvFrame;                                                            ///<The storage address for video capturing
            public UInt32 cbFrame;                                                          ///<The size of storage for video capturing
            public UInt32 cbStride;                                                         ///<Width of capture video frame

            public byte bBottomUp;                                                          ///<Whether to flip
            public MWCAP_VIDEO_DEINTERLACE_MODE deinterlaceMode;                                                   ///<DeinterlaceMode

            public UInt64 pvContext;                                                          ///<Context of ECO 
        }

#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern MW_RESULT MWCaptureSetVideoEcoFrame(IntPtr hChannel, IntPtr pFrame);

        public struct MWCAP_VIDEO_ECO_CAPTURE_STATUS
        {
            public UInt64 pvContext;                                                          ///<frame label for DWORD
            public UInt64 pvFrame;                                                            ///<Frame data address
            public Int64 llTimestamp;                                                       ///<Timestamp
        }

#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern MW_RESULT MWGetVideoEcoCaptureStatus(IntPtr hChannel, ref MWCAP_VIDEO_ECO_CAPTURE_STATUS pStatus);

#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern MW_RESULT MWStopVideoEcoCapture(IntPtr hChannel);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void VIDEO_CAPTURE_CALLBACK(IntPtr pbFrame, int vbFrame, ulong u64TimeStamp, IntPtr pParam);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void AUDIO_CAPTURE_CALLBACK(IntPtr pbFrame, int vbFrame, ulong u64TimeStamp, IntPtr pParam);

#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern IntPtr MWCreateVideoCapture(IntPtr hChannel, Int32 nWidth, Int32 nHeight, Int32 nFourcc, Int32 nFrameDuration, VIDEO_CAPTURE_CALLBACK callback, IntPtr pParam);

#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern IntPtr MWCreateAudioCapture(IntPtr hChannel, MWCAP_AUDIO_CAPTURE_NODE captureNode, UInt32 dwSamplesPerSec, UInt16 wBitsPerSample, UInt16 wChannels, AUDIO_CAPTURE_CALLBACK call_back, IntPtr pParam);

#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern MW_RESULT MWGetVideoProcParamRange(IntPtr hVideo,MWCAP_VIDEO_PROC_PARAM_TYPE videoProcParamType,
            ref long plParamValueMin,ref long plParamValueMax,ref long plParamValueDef);
#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern MW_RESULT MWGetVideoProcParam(IntPtr hVideo,MWCAP_VIDEO_PROC_PARAM_TYPE videoProcParamType,ref long plParamValue);
#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern MW_RESULT MWSetVideoProcParam(IntPtr hVideo,MWCAP_VIDEO_PROC_PARAM_TYPE videoProcParamType,long lParamValue);

        public struct MWCAP_VIDEO_PROCESS_SETTINGS
        {
            public UInt32 dwProcessSwitchs;                                                     ///<Mask of video processing refers to #MWCAP_VIDEO_PROCESS_FLIP, #MWCAP_VIDEO_PROCESS_MIRROR
            public RECT rectSource;                                                                ///<The source area to be processed
            public Int32 nAspectX;                                                               ///<Width of video ratio
            public Int32 nAspectY;                                                               ///<Height of video ratio
            public byte bLowLatency;                                                            ///<Whether to enable lowtancy 
            public MWCAP_VIDEO_COLOR_FORMAT colorFormat;                                                           ///<Standard of video color format  
            public MWCAP_VIDEO_QUANTIZATION_RANGE quantRange;                                                              ///<Quantization
            public MWCAP_VIDEO_SATURATION_RANGE satRange;                                                              ///<Saturation rage
            public MWCAP_VIDEO_DEINTERLACE_MODE deinterlaceMode;                                                       ///<Interlaced mode 
            public MWCAP_VIDEO_ASPECT_RATIO_CONVERT_MODE aspectRatioConvertMode;                                                   ///<Aspect ratio conversion 
        }

#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern MW_RESULT MWGetVideoCaptureProcessSettings(IntPtr hChannel, ref MWCAP_VIDEO_PROCESS_SETTINGS pStatus);
#if DEBUG
        [DllImport("LibMWCaptured.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWCapture.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern MW_RESULT MWSetVideoCaptureProcessSettings(IntPtr hChannel, ref MWCAP_VIDEO_PROCESS_SETTINGS pStatus);
    }
}
