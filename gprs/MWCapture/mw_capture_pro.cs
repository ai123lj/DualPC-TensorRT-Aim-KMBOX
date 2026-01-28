using System;
using System.Runtime.InteropServices;
using System.Threading;
namespace MWModle
{
    public class CMWCapturePro : CMWCapture
    {
        Boolean m_video_capturing = false;
        Boolean m_audio_capturing = false;

        Byte m_bottom_up = 0;

        UInt32 m_process_switchs = 0;
        Int32 m_parital_notify = 0;
        UInt64 m_OSD_image = 0;
        LibMWCapture.RECT[] m_p_OSD_rects=null;
        Int32 m_OSD_rects_num = 0;
        Int16 m_contrast = 100;
        Int16 m_brightness = 0;
        Int16 m_saturation = 100;
        Int16 m_hue = 0;
        LibMWCapture.MWCAP_VIDEO_DEINTERLACE_MODE m_deinterlace_mode = LibMWCapture.MWCAP_VIDEO_DEINTERLACE_MODE.MWCAP_VIDEO_DEINTERLACE_BLEND;
        LibMWCapture.MWCAP_VIDEO_ASPECT_RATIO_CONVERT_MODE m_aspect_ratio_convert_mode = LibMWCapture.MWCAP_VIDEO_ASPECT_RATIO_CONVERT_MODE.MWCAP_VIDEO_ASPECT_RATIO_IGNORE;
        LibMWCapture.RECT[] m_p_rect_src = null;
        LibMWCapture.RECT[] m_p_rect_dest = null;
        Int32 m_aspect_x = 0;//0
        Int32 m_aspect_y = 0;//0
        LibMWCapture.MWCAP_VIDEO_COLOR_FORMAT m_color_format = LibMWCapture.MWCAP_VIDEO_COLOR_FORMAT.MWCAP_VIDEO_COLOR_FORMAT_UNKNOWN;
        LibMWCapture.MWCAP_VIDEO_QUANTIZATION_RANGE m_quant_range = LibMWCapture.MWCAP_VIDEO_QUANTIZATION_RANGE.MWCAP_VIDEO_QUANTIZATION_UNKNOWN;
        LibMWCapture.MWCAP_VIDEO_SATURATION_RANGE m_sat_range = LibMWCapture.MWCAP_VIDEO_SATURATION_RANGE.MWCAP_VIDEO_SATURATION_UNKNOWN;


        Thread m_video_thread = null;
        Thread m_audio_thread = null;
        public CMWCapturePro()
        {
            Console.WriteLine("create pro capture\n");
        }
        public override void Dispose()
        {
            if (m_video_thread != null)
            {
                m_video_capturing = false;
                m_video_thread.Join();
                m_video_thread = null;
            }
            if (m_audio_thread != null)
            {
                m_audio_capturing = false;
                m_audio_thread.Join();
                m_audio_thread = null;
            }
            if (m_OSD_image != 0)
            {
                //MWCloseImage(m_channel_handle, m_OSD_image, &ret);
            }
            m_p_OSD_rects = null;
            m_p_rect_src = null;
            m_p_rect_dest = null;
            base.Dispose();
            
        }
        ~CMWCapturePro()
        {
            Dispose();
            Console.WriteLine("destory pro capture");
        }

        void video_capture_pro()
        {
            if (0 != m_frame_duration){
                capture_by_timer();
            }
			else {
                capture_by_input();
            }
        }

        bool check()
        {
            if (IntPtr.Zero == m_channel_handle){
                set_device(m_device_index);
            }
            if (IntPtr.Zero == m_channel_handle){
                return false;
            }
            if (LibMWCapture.MW_FAMILY_ID.MW_FAMILY_ID_PRO_CAPTURE != m_device_family){
                Console.WriteLine("the device is not pro device\n");
                return false;
            }
            check_signal();
            return true;
        }
        public override bool set_mirror_and_reverse(bool is_mirror, bool is_reverse)
        {
            if (is_mirror)
            {
                Console.WriteLine("pro device not support mirror\n");
                return false;
            }
            m_is_reverse = is_reverse ? 1 : 0;
            m_bottom_up = (byte)m_is_reverse;
            m_is_mirror = 0;
            return true;
        }
        public override bool start_capture(bool video, bool audio)
        {
            m_capture_video = video;
            m_capture_audio = audio;
            if (!check()) {
                return false;
            }
            if (m_is_reverse < 0)
            {
                m_is_reverse = m_bottom_up;
            }
            m_is_mirror = 0;
            if (video) {
                if (!check_video_buffer()) {
                    Console.WriteLine("chenck video buffer fail\n");
                    return false;
                }
                m_video_capturing = true;
                m_video_thread = new Thread(new ThreadStart(video_capture_pro));
                if (null == m_video_thread) {
                    Console.WriteLine("capture video fail\n");
                    return false;
                }
                m_video_thread.Start();
            }

            if (audio) {
                if (!check_audio_buffer()) {
                    Console.WriteLine("chenck audio buffer fail\n");
                    return false;
                }
                m_audio_capturing = true;
                m_audio_thread = new Thread(new ThreadStart(audio_capture_pro));
                if (null == m_audio_thread) {
                    Console.WriteLine("capture audio fail\n");
                    return false;
                }
                m_audio_thread.Start();
            }
            return true;
        }

        void prev_frame_capture_process()
        {
        }
        void capture_by_timer()
        {
            Console.WriteLine("capture video by timer in\n");
            IntPtr capture_event = Libkernel32.CreateEvent(IntPtr.Zero, 0, 0, IntPtr.Zero);
            if (IntPtr.Zero == capture_event) {
                Console.WriteLine("create event fail\n");
                return;
            }
            if (LibMWCapture.MWStartVideoCapture(m_channel_handle, capture_event) != LibMWCapture.MW_RESULT.MW_SUCCEEDED) {
                Console.WriteLine("start video capture fail\n");
                Libkernel32.CloseHandle(capture_event);
                return;
            }


            IntPtr timer_event = Libkernel32.CreateEvent(IntPtr.Zero, 0, 0, IntPtr.Zero);
            UInt64 timer = LibMWCapture.MWRegisterTimer(m_channel_handle, timer_event);
            if (timer == 0) {
                Console.WriteLine("register notify fail\n");
                goto end_and_free;
            }
            UInt32 stride = MWFOURCC.FOURCC_CalcMinStride(m_mw_fourcc, m_width, 2);
            UInt32 frame_size = MWFOURCC.FOURCC_CalcImageSize(m_mw_fourcc, m_width, m_height, stride);
            CRingBuffer.st_frame_t frame= m_video_buffer.get_buffer_by_index(0);
            frame.buffer_len = 0;
            frame.p_buffer = null;
            for (Int32 i = 0; i < m_video_buffer.m_buffer_num; i++) {
                frame = m_video_buffer.get_buffer_by_index(i);
                if (0 == frame.buffer_len) {
                    break;
                }
                LibMWCapture.MWPinVideoBuffer(m_channel_handle, frame.p_buffer, frame_size);
            }
            frame.buffer_len = 0;
            frame.p_buffer = null;
            Int64 wait_tm = 0;
            LibMWCapture.MWGetDeviceTime(m_channel_handle, ref wait_tm);
            Int64 now_tm = 0;
            IntPtr p_capture_status = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(LibMWCapture.MWCAP_VIDEO_CAPTURE_STATUS)));
            while (m_video_capturing) {
                wait_tm += m_frame_duration;
                if (LibMWCapture.MWScheduleTimer(m_channel_handle, timer, wait_tm) != LibMWCapture.MW_RESULT.MW_SUCCEEDED) {
                    continue;
                }
                if (0 != Libkernel32.WaitForSingleObject(timer_event, 100)) {
                    continue;
                }

                if (0 == frame.buffer_len) {
                    frame = m_video_buffer.get_buffer_to_fill();
                }

                if (0 == frame.buffer_len) {
                    continue;
                }
                prev_frame_capture_process();
                LibMWCapture.MWGetDeviceTime(m_channel_handle, ref now_tm);
                frame.ts = now_tm / 10000;
                LibMWCapture.MWCaptureVideoFrameToVirtualAddressEx(m_channel_handle, LibMWCapture.MWCAP_VIDEO_FRAME_ID_NEWEST_BUFFERED, frame.p_buffer,
                    frame_size, stride, m_bottom_up, 0, m_mw_fourcc, m_width, m_height, m_process_switchs, m_parital_notify, m_OSD_image, m_p_OSD_rects, m_OSD_rects_num,
                    m_contrast, m_brightness, m_saturation, m_hue,
                    m_deinterlace_mode, m_aspect_ratio_convert_mode, m_p_rect_src, m_p_rect_dest,
                    m_aspect_x, m_aspect_y, m_color_format, m_quant_range, m_sat_range);
                if (0 != Libkernel32.WaitForSingleObject(capture_event, 100)) {
                    continue;
                }
                m_video_buffer.buffer_filled(frame);
                m_now_capture_frames++;
                frame.buffer_len = 0;
                LibMWCapture.MWGetVideoCaptureStatus(m_channel_handle, p_capture_status);
            }

            for (Int32 i = 0; i < m_video_buffer.m_buffer_num; i++){
                frame = m_video_buffer.get_buffer_by_index(i);
                if (0 == frame.buffer_len){
                    break;
                }
                LibMWCapture.MWUnpinVideoBuffer(m_channel_handle, frame.p_buffer);
            }
            Console.WriteLine("capture video by timer out\n");
            end_and_free:
            LibMWCapture.MWStopVideoCapture(m_channel_handle);
            if (0 != timer) {
                LibMWCapture.MWUnregisterTimer(m_channel_handle, timer);
            }
            if (IntPtr.Zero != timer_event) {
                Libkernel32.CloseHandle(timer_event);
            }
            if (IntPtr.Zero != capture_event) {
                Libkernel32.CloseHandle(capture_event);
            }
        }

        void capture_by_input()
        {
            Console.WriteLine("capture video by input in\n");
            IntPtr capture_event = Libkernel32.CreateEvent(IntPtr.Zero, 0, 0, IntPtr.Zero);
            if (IntPtr.Zero == capture_event) {
                Console.WriteLine("create event fail\n");
                return;
            }
            if (LibMWCapture.MWStartVideoCapture(m_channel_handle, capture_event) != LibMWCapture.MW_RESULT.MW_SUCCEEDED){
                Console.WriteLine("start video capture fail\n");
                Libkernel32.CloseHandle(capture_event);
                return;
            }

            IntPtr notify_event = Libkernel32.CreateEvent(IntPtr.Zero, 0, 0, IntPtr.Zero);
            UInt64 notify = LibMWCapture.MWRegisterNotify(m_channel_handle, notify_event, LibMWCapture.MWCAP_NOTIFY_VIDEO_FRAME_BUFFERED | LibMWCapture.MWCAP_NOTIFY_VIDEO_SIGNAL_CHANGE);
            if (notify == 0) {
                Console.WriteLine("register notify fail\n");
                goto end_and_free;
            }
            UInt32 stride = MWFOURCC.FOURCC_CalcMinStride(m_mw_fourcc, m_width, 2);
            UInt32 frame_size = MWFOURCC.FOURCC_CalcImageSize(m_mw_fourcc, m_width, m_height, stride);
            CRingBuffer.st_frame_t frame = m_video_buffer.get_buffer_by_index(0);
            frame.buffer_len = 0;
            frame.p_buffer = null;
            for (Int32 i = 0; i < m_video_buffer.m_buffer_num; i++){
                frame = m_video_buffer.get_buffer_by_index(i);
                if (0 == frame.buffer_len){
                    break;
                }
                LibMWCapture.MWPinVideoBuffer(m_channel_handle, frame.p_buffer, frame_size);
            }
            Int64 now_tm = 0;
            IntPtr p_capture_status = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(LibMWCapture.MWCAP_VIDEO_CAPTURE_STATUS)));
            bool have_signal = true;
            UInt32 event_wait_time = 100;
            while (m_video_capturing) {
                if (0 != Libkernel32.WaitForSingleObject(notify_event, event_wait_time)){
                    if (have_signal) {
                        continue;
                    }
                }
                else {
                    UInt64 notify_status = 0;
                    if (LibMWCapture.MWGetNotifyStatus(m_channel_handle, notify, ref notify_status) != LibMWCapture.MW_RESULT.MW_SUCCEEDED){
                        continue;
                    }
                    if (0 != (notify_status & LibMWCapture.MWCAP_NOTIFY_VIDEO_SIGNAL_CHANGE)){
                        LibMWCapture.MWCAP_VIDEO_SIGNAL_STATUS video_signal_status = new LibMWCapture.MWCAP_VIDEO_SIGNAL_STATUS();
                        LibMWCapture.MWGetVideoSignalStatus(m_channel_handle, ref video_signal_status);
                        if (LibMWCapture.MWCAP_VIDEO_SIGNAL_STATE.MWCAP_VIDEO_SIGNAL_LOCKED == video_signal_status.state){
                            have_signal = true;
                            event_wait_time = 100;
                        }
                        else {
                            have_signal = false;
                            event_wait_time = 25;
                        }
                        continue;
                    }
                    else if (0 == (notify_status & LibMWCapture.MWCAP_NOTIFY_VIDEO_FRAME_BUFFERED)){
                        continue;
                    }
                }
                if (0 == frame.buffer_len){
                    frame = m_video_buffer.get_buffer_to_fill();
                }

                if (0 == frame.buffer_len){
                    continue;
                }
                prev_frame_capture_process();
                LibMWCapture.MWGetDeviceTime(m_channel_handle, ref now_tm);
                frame.ts = now_tm / 10000;
                LibMWCapture.MWCaptureVideoFrameToVirtualAddressEx(m_channel_handle, LibMWCapture.MWCAP_VIDEO_FRAME_ID_NEWEST_BUFFERED, frame.p_buffer,
                    frame_size, stride, m_bottom_up, 0, m_mw_fourcc, m_width, m_height, m_process_switchs, m_parital_notify, m_OSD_image, m_p_OSD_rects, m_OSD_rects_num,
                    m_contrast, m_brightness, m_saturation, m_hue,
                    m_deinterlace_mode, m_aspect_ratio_convert_mode, m_p_rect_src, m_p_rect_dest,
                    m_aspect_x, m_aspect_y, m_color_format, m_quant_range, m_sat_range);
                if (0 != Libkernel32.WaitForSingleObject(capture_event, 100)){
                    continue;
                }

                m_video_buffer.buffer_filled(frame);
                m_now_capture_frames++;
                frame.buffer_len = 0;
                LibMWCapture.MWGetVideoCaptureStatus(m_channel_handle, p_capture_status);
            }
            for (Int32 i = 0; i < m_video_buffer.m_buffer_num; i++){
                frame = m_video_buffer.get_buffer_by_index(i);
                if (0 == frame.buffer_len){
                    break;
                }
                LibMWCapture.MWUnpinVideoBuffer(m_channel_handle, frame.p_buffer);
            }
            Console.WriteLine("capture video by input out\n");

            end_and_free:
            if (0 != notify) {
                LibMWCapture.MWUnregisterNotify(m_channel_handle, notify);
            }
            LibMWCapture.MWStopVideoCapture(m_channel_handle);
            if (IntPtr.Zero != notify_event){
                Libkernel32.CloseHandle(notify_event);
            }
            if (IntPtr.Zero != capture_event){
                Libkernel32.CloseHandle(capture_event);
            }
        }
        void audio_capture_pro()
        {
            Console.WriteLine("audio capture in\n");
            IntPtr capture_event = Libkernel32.CreateEvent(IntPtr.Zero, 0, 0, IntPtr.Zero);
            if (IntPtr.Zero == capture_event) {
                Console.WriteLine("create event fail\n");
                return;
            }
            if (LibMWCapture.MWStartAudioCapture(m_channel_handle) != LibMWCapture.MW_RESULT.MW_SUCCEEDED) {
                Libkernel32.CloseHandle(capture_event);
                Console.WriteLine("start audio capture fail\n");
                return;
            }
            UInt64 notify = LibMWCapture.MWRegisterNotify(m_channel_handle, capture_event, LibMWCapture.MWCAP_NOTIFY_AUDIO_SIGNAL_CHANGE | LibMWCapture.MWCAP_NOTIFY_AUDIO_FRAME_BUFFERED);
            if (0 == notify) {
                Console.WriteLine("register notify fail\n");
                goto end_and_free;
            }
            LibMWCapture.MWCAP_AUDIO_CAPTURE_FRAME audio_frame = new LibMWCapture.MWCAP_AUDIO_CAPTURE_FRAME();
            while (m_audio_capturing) {
                if (0 != Libkernel32.WaitForSingleObject(capture_event, 100)) {
                    continue;
                }
                UInt64 notify_status = 0;
                LibMWCapture.MWGetNotifyStatus(m_channel_handle, notify, ref notify_status);

                if (0 == (notify_status & LibMWCapture.MWCAP_NOTIFY_AUDIO_FRAME_BUFFERED)) {
                    continue;
                }
                do {
                    if (LibMWCapture.MWCaptureAudioFrame(m_channel_handle, ref audio_frame) != LibMWCapture.MW_RESULT.MW_SUCCEEDED) {
                        break;
                    }
                    CRingBuffer.st_frame_t frame = m_audio_buffer.get_buffer_to_fill();

                    if (0 == frame.buffer_len) {
                        continue;
                    }
                    int byte_per_sample = m_audio_bit_per_sample / 8;
                    int offset = (sizeof(UInt32) - byte_per_sample) * 8;
                    int dst_offset = 0;
                    frame.ts = audio_frame.llTimestamp / 10000;
                    frame.frame_len = (UInt32)(m_audio_channel_num * LibMWCapture.MWCAP_AUDIO_SAMPLES_PER_FRAME * byte_per_sample);
                    UInt32[] channel_data = new UInt32[LibMWCapture.MWCAP_AUDIO_MAX_NUM_CHANNELS];
                    channel_data[0] = 0;
                    channel_data[1] = 4;
                    channel_data[2] = 1;
                    channel_data[3] = 5;
                    channel_data[4] = 2;
                    channel_data[5] = 6;
                    channel_data[6] = 3;
                    channel_data[7] = 7;
                    for (UInt32 i = 0; i < LibMWCapture.MWCAP_AUDIO_SAMPLES_PER_FRAME; i++) {
                        for (UInt32 j = 0; j < m_audio_channel_num; j++) {
                            frame.p_buffer[dst_offset] = (Byte)((audio_frame.adwSamples[channel_data[j]] >> 16) & 0xff);
                            frame.p_buffer[dst_offset + 1] = (Byte)(audio_frame.adwSamples[channel_data[j]] >> 24);
                            channel_data[j] += 8;
                            dst_offset += byte_per_sample;
                        }
                    }
                    m_audio_buffer.buffer_filled(frame);
                } while (m_audio_capturing);
            }
            Console.WriteLine("audio capture out\n");
            LibMWCapture.MWUnregisterNotify(m_channel_handle, notify);
            end_and_free:
            LibMWCapture.MWStopAudioCapture(m_channel_handle);
            Libkernel32.CloseHandle(capture_event);
        }
    }
}