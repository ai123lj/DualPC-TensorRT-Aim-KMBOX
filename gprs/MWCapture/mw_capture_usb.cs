using System;
using System.Runtime.InteropServices;
using System.Threading;
namespace MWModle
{
    public class CMWCaptureUsb : CMWCapture
    {
        IntPtr m_video_handle = IntPtr.Zero;
        IntPtr m_audio_handle = IntPtr.Zero;
        CRingBuffer.st_frame_t audio_frame = new CRingBuffer.st_frame_t();
        LibMWCapture.VIDEO_CAPTURE_CALLBACK video_callback;
        LibMWCapture.AUDIO_CAPTURE_CALLBACK audio_callback;
        public CMWCaptureUsb()
        {
            m_use_common_api = true;
            audio_frame.buffer_len = 0;
            m_mw_fourcc = MWFOURCC.MWFOURCC_YUY2;
            Console.WriteLine("create usb capture\n");
        }
        public override void Dispose()
        {
            if (IntPtr.Zero != m_video_handle){
                LibMWCapture.MWDestoryVideoCapture(m_video_handle);
                m_video_handle = IntPtr.Zero;
            }
            if (IntPtr.Zero != m_audio_handle){
                LibMWCapture.MWDestoryAudioCapture(m_audio_handle);
                m_audio_handle = IntPtr.Zero;
            }
            base.Dispose();
        }
        ~CMWCaptureUsb()
        {
            Dispose();
            Console.WriteLine("destory usb capture\n");
        }

        bool check()
        {
            if (IntPtr.Zero == m_channel_handle) {
                set_device(0);
            }
            if (IntPtr.Zero == m_channel_handle) {
                return false;
            }
            check_signal();
            return true;
        }
        void video_frame_callback(IntPtr out_frame, int frame_len, ulong ts, IntPtr param)
        {
            CRingBuffer.st_frame_t frame = m_video_buffer.get_buffer_to_fill();
            m_now_capture_frames++;
            if (0 == frame.buffer_len) {
                return;
            }
            Marshal.Copy(out_frame, frame.p_buffer, 0, frame_len);
            frame.frame_len = (uint)frame_len;
            frame.ts = System.Environment.TickCount;
            //Marshal.Copy((IntPtr)((ulong)pbFrame+ (ulong)cbFrame / 2), frame.p_buffer, cbFrame / 2, cbFrame/2);
            m_video_buffer.buffer_filled(frame);
        }
        void audio_frame_callback(IntPtr out_frame, int frame_len, ulong ts, IntPtr param)
        {
            int have_copy_len = 0;
            while (have_copy_len < frame_len) {
                if (0 == audio_frame.buffer_len) {
                    audio_frame = m_audio_buffer.get_buffer_to_fill();
                    audio_frame.ts = System.Environment.TickCount;
                }
                if (0 == audio_frame.buffer_len){
                    return;
                }
                if ((frame_len - have_copy_len) >= (audio_frame.buffer_len - audio_frame.frame_len)) {
                    Marshal.Copy((IntPtr)((ulong)out_frame + (ulong)have_copy_len / 2), audio_frame.p_buffer, (Int32)audio_frame.frame_len, (Int32)(audio_frame.buffer_len - audio_frame.frame_len));
                    have_copy_len += (int)(audio_frame.buffer_len - audio_frame.frame_len);
                    audio_frame.frame_len = audio_frame.buffer_len;
                    m_audio_buffer.buffer_filled(audio_frame);
                    audio_frame.buffer_len = 0;
                }
                else {
                    Marshal.Copy((IntPtr)((ulong)out_frame + (ulong)have_copy_len / 2), audio_frame.p_buffer, (Int32)audio_frame.frame_len, frame_len - have_copy_len);
                    audio_frame.frame_len += (UInt32)(frame_len - have_copy_len);
                    return;
                }
            }
        }
        public override bool set_mirror_and_reverse(bool is_mirror, bool is_reverse)
        {
            if (null == m_channel_handle){
                Console.WriteLine("please set_device");
                return false;
            }
            if (LibMWCapture.MW_FAMILY_ID.MW_FAMILY_ID_USB_CAPTURE != m_device_family){
                Console.WriteLine("just usb device can set set_mirror_and_reverse use common api\n");
                return false;
            }
            m_is_mirror = is_mirror ? 1 : 0;
            m_is_reverse = is_reverse ? 1 : 0;
            return true;
        }
        public override bool start_capture(bool video, bool audio)
        {
            m_capture_video = video;
            m_capture_audio = audio;
            if (!check()) {
                return false;
            }

            if (LibMWCapture.MW_FAMILY_ID.MW_FAMILY_ID_USB_CAPTURE == m_device_family)
            {
                LibMWCapture.MWCAP_VIDEO_PROCESS_SETTINGS process_settings = new LibMWCapture.MWCAP_VIDEO_PROCESS_SETTINGS();
                LibMWCapture.MWGetVideoCaptureProcessSettings(m_channel_handle, ref process_settings);
                bool is_mirror = (process_settings.dwProcessSwitchs & LibMWCapture.MWCAP_VIDEO_PROCESS_MIRROR) > 0;
                bool is_reverse = (process_settings.dwProcessSwitchs & LibMWCapture.MWCAP_VIDEO_PROCESS_FLIP) > 0;
                if (-1 == m_is_mirror)
                {
                    m_is_mirror = is_mirror?1:0;
                }
                else
                {
                    is_mirror = m_is_mirror == 1;
                }
                if ((MWFOURCC.MWFOURCC_BGR24 == m_mw_fourcc) || (MWFOURCC.MWFOURCC_BGRA == m_mw_fourcc))
                {
                    if (-1 == m_is_reverse)
                    {
                        m_is_reverse = (!is_reverse)?1:0;
                    }
                    else
                    {
                        is_reverse = m_is_reverse == 0;
                    }
                }
                else
                {
                    if (-1 == m_is_reverse)
                    {
                        m_is_reverse = is_reverse?1:0;
                    }
                    else
                    {
                        is_reverse = m_is_reverse == 1;
                    }
                }
                process_settings.dwProcessSwitchs = (is_mirror ? LibMWCapture.MWCAP_VIDEO_PROCESS_MIRROR : 0) | (is_reverse ? LibMWCapture.MWCAP_VIDEO_PROCESS_FLIP : 0);
                LibMWCapture.MWSetVideoCaptureProcessSettings(m_channel_handle, ref process_settings);
            }
            else if ((MWFOURCC.MWFOURCC_BGR24 == m_mw_fourcc) || (MWFOURCC.MWFOURCC_BGRA == m_mw_fourcc))
            {
                m_is_mirror = 0;
                m_is_reverse = 1;
            }
            else
            {
                m_is_mirror = 0;
                m_is_reverse = 0;
            }

            if (video) {
                if (!check_video_buffer()) {
                    Console.WriteLine("chenck video buffer fail\n");
                    return false;
                }
                video_callback = new LibMWCapture.VIDEO_CAPTURE_CALLBACK(video_frame_callback);//
                m_video_handle = LibMWCapture.MWCreateVideoCapture(m_channel_handle, m_width, m_height, (Int32)m_mw_fourcc, (Int32)((m_frame_duration != 0) ? m_frame_duration : m_signal_frame_duration), video_callback, IntPtr.Zero);
                if (IntPtr.Zero == m_video_handle) {
                    Console.WriteLine("capture video fail\n");
                    return false;
                }
            }

            if (audio) {
                if (!check_audio_buffer()) {
                    Console.WriteLine("chenck audio buffer fail\n");
                    return false;
                }
                audio_callback = new LibMWCapture.AUDIO_CAPTURE_CALLBACK(audio_frame_callback);
                m_audio_handle = LibMWCapture.MWCreateAudioCapture(m_channel_handle, LibMWCapture.MWCAP_AUDIO_CAPTURE_NODE.MWCAP_AUDIO_CAPTURE_NODE_DEFAULT, (UInt32)m_audio_sample_rate, (UInt16)m_audio_bit_per_sample, (UInt16)m_audio_channel_num, audio_callback, IntPtr.Zero);

                if (IntPtr.Zero == m_audio_handle) {
                    Console.WriteLine("capture audio fail\n");
                    return false;
                }
            }
            return true;
        }
    }
}