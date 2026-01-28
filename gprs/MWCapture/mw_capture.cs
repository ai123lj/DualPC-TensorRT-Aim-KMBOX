using System;
using System.Runtime.InteropServices;
namespace MWModle
{
    public abstract class CMWCapture
    {
        public CRingBuffer m_video_buffer = null;
        public CRingBuffer m_audio_buffer = null;

        protected Int64 m_frame_duration = 0;
        protected Int32 m_width = 0;
        protected Int32 m_height = 0;
        protected UInt32 m_mw_fourcc = MWFOURCC.MWFOURCC_NV12;

        protected Int32 m_is_mirror;
        protected Int32 m_is_reverse;

        protected Int32 m_audio_channel_num = 2;
        protected Int32 m_audio_bit_per_sample = 16;
        protected UInt32 m_signal_frame_duration = 0;
        protected Int32 m_audio_sample_rate = 48000;

        protected IntPtr m_channel_handle = IntPtr.Zero;
        protected LibMWCapture.MW_FAMILY_ID m_device_family = LibMWCapture.MW_FAMILY_ID.MW_FAMILY_ID_PRO_CAPTURE;
        protected Int32 m_device_index = 0;
        protected Boolean m_use_common_api = false;
        protected Boolean m_user_video_buffer = false;
        protected Boolean m_user_audio_buffer = false;
        protected Boolean m_capture_video = true;
        protected Boolean m_capture_audio = true;
        protected UInt32 m_now_capture_frames = 0;
        UInt32 m_prev_capture_frames = 0;
        UInt32 m_prev_ts = 0;
        Double m_video_capture_fps = 0.0;
        public CMWCapture()
        {
        }
        public virtual void Dispose()
        {
            if (null != m_video_buffer) {
                m_video_buffer.Dispose();
                m_video_buffer = null;
            }
            if (null != m_audio_buffer)
            {
                m_audio_buffer.Dispose();
                m_audio_buffer = null;
            }
            if (IntPtr.Zero != m_channel_handle) {
                LibMWCapture.MWCloseChannel(m_channel_handle);
                m_channel_handle = IntPtr.Zero;
            }
        }

        ~CMWCapture()
        {
            Dispose();
        }

        static public void Init()
        {
            LibMWCapture.MWCaptureInitInstance();
        }

        static public void Exit()
        {
            LibMWCapture.MWCaptureExitInstance();
        }

        static public Boolean RefreshDevices()
        {
            LibMWCapture.MW_RESULT mr;
            mr = LibMWCapture.MWRefreshDevice();
            if (mr != LibMWCapture.MW_RESULT.MW_SUCCEEDED){
                return false;
            }
            return true;
        }

        static public int GetChannelCount()
        {
            return LibMWCapture.MWGetChannelCount();
        }

        static public bool GetChannelInfobyIndex(int index, ref LibMWCapture.MWCAP_CHANNEL_INFO channel_info)
        {
            if (LibMWCapture.MW_RESULT.MW_SUCCEEDED != LibMWCapture.MWGetChannelInfoByIndex(index, ref channel_info)) {
                return false;
            }
            return true;
        }
        static public CMWCapture mw_capture_factory(int index)
        {
            CMWCapture capture = null;
            LibMWCapture.MWCAP_CHANNEL_INFO channel_info = new LibMWCapture.MWCAP_CHANNEL_INFO();
            LibMWCapture.MWGetChannelInfoByIndex(index, ref channel_info);
            if ((UInt16)LibMWCapture.MW_FAMILY_ID.MW_FAMILY_ID_PRO_CAPTURE == channel_info.wFamilyID) {
                capture = new CMWCapturePro();
            }
            else if ((UInt16)LibMWCapture.MW_FAMILY_ID.MW_FAMILY_ID_ECO_CAPTURE == channel_info.wFamilyID){
                capture = new CMWCaptureEco();
            }
            else/* if ((UInt16)LibMWCapture.MW_FAMILY_ID.MW_FAMILY_ID_USB_CAPTURE == channel_info.wFamilyID)*/{
                capture = new CMWCaptureUsb();
            }
            capture.set_device(index);
            return capture;
        }
        public virtual Boolean set_device(int magewell_device_index)
        {
            if (m_channel_handle != IntPtr.Zero) {
                if (m_device_index == magewell_device_index) {
                    return true;
                }
                Console.WriteLine("have set_device("+ m_device_index + ")");
                return false;
            }
            m_device_index = magewell_device_index;
            UInt16[] wpath = new UInt16[512];
            //IntPtr pwpath = GCHandle.Alloc(wpath, GCHandleType.Pinned).AddrOfPinnedObject();
            LibMWCapture.MWGetDevicePath(magewell_device_index, wpath);
            m_channel_handle = LibMWCapture.MWOpenChannelByPath(wpath);
            if (m_channel_handle == IntPtr.Zero){
                Console.WriteLine("open fail");
                return false;
            }
            LibMWCapture.MWCAP_CHANNEL_INFO channel_info = new LibMWCapture.MWCAP_CHANNEL_INFO();
            LibMWCapture.MWGetChannelInfo(m_channel_handle, ref channel_info);
            m_device_family = (LibMWCapture.MW_FAMILY_ID)channel_info.wFamilyID;
            return true;
        }
        public abstract bool start_capture(bool video, bool audio);
        public abstract bool set_mirror_and_reverse(bool is_mirror, bool is_reverse);

        public bool get_mirror_and_reverse(out bool is_mirror, out bool is_reverse)
        {
            is_mirror = false;
            is_reverse = false;
            if ((m_is_mirror < 0) || (m_is_reverse < 0)){
                Console.WriteLine("please start capture");
                return false;
            }
            is_mirror = m_is_mirror == 1;
            is_reverse = m_is_reverse == 1;
            return true;
        }
        public Double get_video_capture_fps()
        {
            UInt32 now_ts = (UInt32)Libkernel32.GetTickCount();
            if (0 == m_prev_ts) {
                m_prev_ts = now_ts;
                m_prev_capture_frames = m_now_capture_frames;
                return m_video_capture_fps;
            }
            UInt32 time = now_ts - m_prev_ts;
            UInt32 frames = m_now_capture_frames - m_prev_capture_frames;
            if ((time > 0) && ((time > 2000) || (frames > 100))) {
                m_prev_ts = now_ts;
                m_prev_capture_frames = m_now_capture_frames;
                m_video_capture_fps = (Double)frames*1000.0 / time;
            }
            return m_video_capture_fps;
        }
        protected void check_signal()
        {
            if (m_capture_video) {
                //IntPtr p_video_signal_status = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(LibMWCapture.MWCAP_VIDEO_SIGNAL_STATUS)));
                LibMWCapture.MWCAP_VIDEO_SIGNAL_STATUS video_signal_status = new LibMWCapture.MWCAP_VIDEO_SIGNAL_STATUS();
                LibMWCapture.MWGetVideoSignalStatus(m_channel_handle, ref video_signal_status);
               // LibMWCapture.MWCAP_VIDEO_SIGNAL_STATUS video_signal_status = (LibMWCapture.MWCAP_VIDEO_SIGNAL_STATUS)Marshal.PtrToStructure(p_video_signal_status, typeof(LibMWCapture.MWCAP_VIDEO_SIGNAL_STATUS));
                //Marshal.FreeCoTaskMem(p_video_signal_status);
                switch (video_signal_status.state){
                    case LibMWCapture.MWCAP_VIDEO_SIGNAL_STATE.MWCAP_VIDEO_SIGNAL_NONE:
                        Console.WriteLine("Input signal status: NONE\n");
                        break;
                    case LibMWCapture.MWCAP_VIDEO_SIGNAL_STATE.MWCAP_VIDEO_SIGNAL_UNSUPPORTED:
                        Console.WriteLine("Input signal status: Unsupported\n");
                        break;
                    case LibMWCapture.MWCAP_VIDEO_SIGNAL_STATE.MWCAP_VIDEO_SIGNAL_LOCKING:
                        Console.WriteLine("Input signal status: Locking\n");
                        break;
                    case LibMWCapture.MWCAP_VIDEO_SIGNAL_STATE.MWCAP_VIDEO_SIGNAL_LOCKED:
                        Console.WriteLine("Input signal status: Locked");
                        break;
                }

                if (LibMWCapture.MWCAP_VIDEO_SIGNAL_STATE.MWCAP_VIDEO_SIGNAL_LOCKED == video_signal_status.state){
                    Console.WriteLine("Input signal resolution: " + video_signal_status.cx + "x" + video_signal_status.cy);
                    double fps = (video_signal_status.bInterlaced == 1) ? ((double)20000000 / video_signal_status.dwFrameDuration) : (double)10000000 / video_signal_status.dwFrameDuration;
                    Console.WriteLine("Input signal fps: " + fps);
                    Console.WriteLine("Input signal interlaced: " + video_signal_status.bInterlaced);
                    Console.WriteLine("Input signal frame segmented: " + video_signal_status.bSegmentedFrame);
                    m_signal_frame_duration = video_signal_status.dwFrameDuration;
                }
                if ((0 == m_width) || (0 == m_height)){
                    if (LibMWCapture.MWCAP_VIDEO_SIGNAL_STATE.MWCAP_VIDEO_SIGNAL_LOCKED == video_signal_status.state){
                        m_width = video_signal_status.cx;
                        m_height = video_signal_status.cy;
                    }
                    else{
                        m_width = 1920;
                        m_height = 1080;
                    }
                }
                if ((0 == m_frame_duration) && (LibMWCapture.MWCAP_VIDEO_SIGNAL_STATE.MWCAP_VIDEO_SIGNAL_LOCKED != video_signal_status.state)){
                    m_frame_duration = 40;
                }
                if (0 == m_mw_fourcc){
                    m_mw_fourcc = MWFOURCC.MWFOURCC_YUY2;
                }
                m_frame_duration = m_frame_duration * 10000;
            }

            if (m_capture_audio) {
                LibMWCapture.MWCAP_AUDIO_SIGNAL_STATUS audio_signal_status = new LibMWCapture.MWCAP_AUDIO_SIGNAL_STATUS();
                LibMWCapture.MWGetAudioSignalStatus(m_channel_handle, ref audio_signal_status);
                if (!m_use_common_api && (audio_signal_status.wChannelValid != 0)){
                    m_audio_sample_rate = (Int32)audio_signal_status.dwSampleRate;
                }

                if (m_use_common_api){
                    LibMWCapture.AUDIO_FORMAT_INFO[] format = null;
                    Int32 count = 0;
                    Boolean have_format = false;
        
                    if (0 == LibMWCapture.MWGetAudioCaptureSupportFormat(m_channel_handle, LibMWCapture.MWCAP_AUDIO_CAPTURE_NODE.MWCAP_AUDIO_CAPTURE_NODE_DEFAULT, IntPtr.Zero, ref count)){
                        return;
                    }
                    if (count > 0){
                        format = new LibMWCapture.AUDIO_FORMAT_INFO[count];
                    }
                    else{
                        return;
                    }
                    IntPtr p_format =  GCHandle.Alloc(format, GCHandleType.Pinned).AddrOfPinnedObject();
                    if (0 == LibMWCapture.MWGetAudioCaptureSupportFormat(m_channel_handle, LibMWCapture.MWCAP_AUDIO_CAPTURE_NODE.MWCAP_AUDIO_CAPTURE_NODE_DEFAULT, p_format, ref count)){
                        format = null;
                        return;
                    }
                    for (int i = 0; i < count; i++){
                        if ((UInt32)m_audio_sample_rate == format[i].nSamplerate){
                            have_format = true;
                            break;
                        }
                    }
                    if (!have_format && (count != 0)){
                        m_audio_sample_rate = (int)format[0].nSamplerate;
                    }
                    format = null;
                }
            }
        }
        protected Boolean check_video_buffer()
        {
            UInt32 stride = MWFOURCC.FOURCC_CalcMinStride(m_mw_fourcc, m_width, 2);
            UInt32 frame_size = MWFOURCC.FOURCC_CalcImageSize(m_mw_fourcc, m_width, m_height, stride);
            if ((!m_user_video_buffer) && (null == m_video_buffer)){
                m_video_buffer = new CRingBuffer();
                if (null == m_video_buffer){
                    return false;
                }
                return m_video_buffer.set_property(5, frame_size);
            }
            else if (null == m_video_buffer){
                Console.WriteLine("please set user video_buffer\n");
                return false;
            }
            else if(m_video_buffer.m_buffer_size < frame_size){
                Console.WriteLine("user video_buffer error\n");
                return false;
            }
            return true;
        }
        protected bool check_audio_buffer()
        {
            if ((!m_user_audio_buffer) && (null == m_audio_buffer)){
                m_audio_buffer = new CRingBuffer();
                if (null == m_audio_buffer){
                    return false;
                }
                if (m_use_common_api){
                    return m_audio_buffer.set_property(64, (UInt32)(m_audio_channel_num * 480 * (m_audio_bit_per_sample / 8)));
                }
                else{
                    return m_audio_buffer.set_property(160, (UInt32)(m_audio_channel_num * LibMWCapture.MWCAP_AUDIO_SAMPLES_PER_FRAME * (m_audio_bit_per_sample / 8)));
                }
            }
            else if (null == m_audio_buffer) {
                Console.WriteLine("please set user audio_buffer\n");
                return false;
            }
            return true;
        }
        public bool set_resolution(int width, int height)
        {
            m_width = width;
            m_height = height;
            return true;
        }
        public bool set_mw_fourcc(UInt32 mw_fourcc)
        {
            m_mw_fourcc = mw_fourcc;
            return true;
        }
        public bool set_audio_channels(int channel_num)
        {
            m_audio_channel_num = channel_num;
            return true;
        }
        public bool set_audio_sample_rate(int sample_rate)
        {
            m_audio_sample_rate = sample_rate;
            return true;
        }

        public bool get_resolution(out int width, out int height)
        {
            width = m_width;
            height = m_height;
            return true;
        }
        public bool get_mw_fourcc(out UInt32 mw_fourcc)
        {
            mw_fourcc = m_mw_fourcc;
            return true;
        }
        public bool get_audio_channels(out int channel_num)
        {
            channel_num = m_audio_channel_num;
            return true;
        }
        public bool get_audio_sample_rate(out int sample_rate)
        {
            sample_rate = m_audio_sample_rate;
            return true;
        }
        public bool get_audio_bit_per_sample(out int bit_per_sample)
        {
            bit_per_sample = m_audio_bit_per_sample;
            return true;
        }
        public bool get_frame_duration(out Int64 frame_duration)//us
        {
            frame_duration = ((0 == m_frame_duration) ? m_signal_frame_duration : m_frame_duration) / 10;
            return true;
        }
    }
}