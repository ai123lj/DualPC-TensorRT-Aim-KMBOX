using System;
using System.Runtime.InteropServices;
using System.Threading;
namespace MWModle
{
    public class CMWCaptureEco : CMWCapture
    {
        Boolean m_video_capturing = false;
        Boolean m_audio_capturing = false;
        Thread m_video_thread = null;
        Thread m_audio_thread = null;

        public CMWCaptureEco()
        {

            Console.WriteLine("create eco capture\n");
        }
        public override void Dispose()
        {
            if (m_video_thread != null){
                m_video_capturing = false;
                m_video_thread.Join();
                m_video_thread = null;
            }
            if (m_audio_thread != null){
                m_audio_capturing = false;
                m_audio_thread.Join();
                m_audio_thread = null;
            }
            base.Dispose();
        }
        ~CMWCaptureEco()
        {
            Dispose();
            Console.WriteLine("destory eco capture\n");
        }

        bool check()
        {
            if (IntPtr.Zero == m_channel_handle){
                set_device(0);

            }
            if (IntPtr.Zero == m_channel_handle){
                return false;
            }
            if (LibMWCapture.MW_FAMILY_ID.MW_FAMILY_ID_ECO_CAPTURE != m_device_family){
                Console.WriteLine("the device is not eco device\n");
                return false;
            }
            check_signal();
            return true;
        }
        public override bool set_mirror_and_reverse(bool is_mirror, bool is_reverse)
        {
            if (is_mirror)
            {
                Console.WriteLine("eco device not support mirror\n");
                return false;
            }
            m_is_reverse = is_reverse ? 1 : 0;
            m_is_mirror = 0;
            return true;
        }
        public override bool start_capture(bool video, bool audio)
        {
            m_capture_video = video;
            m_capture_audio = audio;
            if (!check()){
                return false;
            }
            if (m_is_reverse < 0)
            {
                m_is_reverse = 0;
            }
            m_is_mirror = 0;
            if (video){
                if (!check_video_buffer()){
                    Console.WriteLine("chenck video buffer fail\n");
                    return false;
                }
                m_video_capturing = true;
                m_video_thread = new Thread(new ThreadStart(video_capture_eco));
                if (null == m_video_thread){
                    Console.WriteLine("capture video fail\n");
                    return false;
                }
                m_video_thread.Start();
            }

            if (audio){
                if (!check_audio_buffer()){
                    Console.WriteLine("chenck audio buffer fail\n");
                    return false;
                }
                m_audio_capturing = true;
                m_audio_thread = new Thread(new ThreadStart(audio_capture_eco));
                if (null == m_audio_thread){
                    Console.WriteLine("capture audio fail\n");
                    return false;
                }
                m_audio_thread.Start();
            }
            return true;
        }
        void video_capture_eco()
        {
            LibMWCapture.MWCAP_VIDEO_ECO_CAPTURE_FRAME[] eco_frame = new LibMWCapture.MWCAP_VIDEO_ECO_CAPTURE_FRAME[4];
            GCHandle t_gc = GCHandle.Alloc(eco_frame, GCHandleType.Pinned);
            Console.WriteLine("eco capture video in\n");
            IntPtr capture_event = Libkernel32.CreateEvent(IntPtr.Zero, 0, 0, IntPtr.Zero);
            if (IntPtr.Zero == capture_event) {
                Console.WriteLine("create event fail\n");
                return;
            }
            LibMWCapture.MWCAP_VIDEO_ECO_CAPTURE_OPEN eco_capture_open = new LibMWCapture.MWCAP_VIDEO_ECO_CAPTURE_OPEN();
            eco_capture_open.hEvent = (UInt64)capture_event;
            eco_capture_open.dwFOURCC = m_mw_fourcc;
            eco_capture_open.cx = (UInt16)m_width;
            eco_capture_open.cy = (UInt16)m_height;
            eco_capture_open.llFrameDuration = (m_frame_duration != 0) ? m_frame_duration : -1;
            if (LibMWCapture.MWStartVideoEcoCapture(m_channel_handle, ref eco_capture_open) != LibMWCapture.MW_RESULT.MW_SUCCEEDED) {
                Console.WriteLine("start video eco capture fail\n");
                Libkernel32.CloseHandle(capture_event);
                return;
            }
            UInt32 stride = MWFOURCC.FOURCC_CalcMinStride(m_mw_fourcc, m_width, 2);
            UInt32 frame_size = MWFOURCC.FOURCC_CalcImageSize(m_mw_fourcc, m_width, m_height, stride);

            for (int i = 0; i < 4; i++) {
                eco_frame[i].pvFrame = (UInt64)Marshal.AllocCoTaskMem((Int32)frame_size);
                if (0 == eco_frame[i].pvFrame) {
                    goto end_and_free;
                }
                eco_frame[i].cbFrame = frame_size;
                eco_frame[i].cbStride = stride;
                eco_frame[i].bBottomUp = (byte)m_is_reverse;
                //IntPtr ptr = GCHandle.Alloc(eco_frame[i], GCHandleType.Pinned).AddrOfPinnedObject();
                //eco_frame[i].pvContext = (UInt64)ptr;
                eco_frame[i].pvContext = (UInt64)Marshal.UnsafeAddrOfPinnedArrayElement(eco_frame, i);
                if (LibMWCapture.MWCaptureSetVideoEcoFrame(m_channel_handle, (IntPtr)(eco_frame[i].pvContext)) != LibMWCapture.MW_RESULT.MW_SUCCEEDED) {
                    goto end_and_free;
                }
            }
            LibMWCapture.MWCAP_VIDEO_ECO_CAPTURE_STATUS capture_status = new LibMWCapture.MWCAP_VIDEO_ECO_CAPTURE_STATUS();
            while (m_video_capturing) {
                if (0 != Libkernel32.WaitForSingleObject(capture_event, 100)){
                    continue;
                }
                while (m_video_capturing) {
                    if ((LibMWCapture.MWGetVideoEcoCaptureStatus(m_channel_handle,  ref capture_status) != LibMWCapture.MW_RESULT.MW_SUCCEEDED) || (capture_status.pvFrame == 0)) {
                        break;
                    }
                    CRingBuffer.st_frame_t frame = m_video_buffer.get_buffer_to_fill();
                    if (frame.buffer_len != 0) {
                        Marshal.Copy((IntPtr)capture_status.pvFrame, frame.p_buffer, 0, (Int32)frame_size);
                        frame.ts = capture_status.llTimestamp / 10000;
                        m_video_buffer.buffer_filled(frame);
                    }
                    if (LibMWCapture.MWCaptureSetVideoEcoFrame(m_channel_handle, (IntPtr)(capture_status.pvContext)) != LibMWCapture.MW_RESULT.MW_SUCCEEDED) {
                        break;
                    }
                }
            }
            Console.WriteLine("eco capture video out\n");
            LibMWCapture.MWStopVideoEcoCapture(m_channel_handle);
            Libkernel32.CloseHandle(capture_event);
        end_and_free:
            t_gc.Free();
            for (int i = 0; i < 4; i++) {
                if (0 != eco_frame[i].pvFrame) {
                    Marshal.FreeCoTaskMem((IntPtr)eco_frame[i].pvFrame);
                }
                else {
                    break;
                }
            }
        }

        void audio_capture_eco()
        {
            Console.WriteLine("audio capture in\n");
            IntPtr capture_event = Libkernel32.CreateEvent(IntPtr.Zero, 0, 0, IntPtr.Zero);
            if (IntPtr.Zero == capture_event){
                Console.WriteLine("create event fail\n");
                return;
            }
            if (LibMWCapture.MWStartAudioCapture(m_channel_handle) != LibMWCapture.MW_RESULT.MW_SUCCEEDED){
                Libkernel32.CloseHandle(capture_event);
                Console.WriteLine("start audio capture fail\n");
                return;
            }
            UInt64 notify = LibMWCapture.MWRegisterNotify(m_channel_handle, capture_event, LibMWCapture.MWCAP_NOTIFY_AUDIO_SIGNAL_CHANGE | LibMWCapture.MWCAP_NOTIFY_AUDIO_FRAME_BUFFERED);
            if (0 == notify){
                Console.WriteLine("register notify fail\n");
                goto end_and_free;
            }
            LibMWCapture.MWCAP_AUDIO_CAPTURE_FRAME audio_frame = new LibMWCapture.MWCAP_AUDIO_CAPTURE_FRAME();
            while (m_audio_capturing){
                if (0 != Libkernel32.WaitForSingleObject(capture_event, 100)){
                    continue;
                }
                UInt64 notify_status = 0;
                LibMWCapture.MWGetNotifyStatus(m_channel_handle, notify, ref notify_status);

                if (0 == (notify_status & LibMWCapture.MWCAP_NOTIFY_AUDIO_FRAME_BUFFERED)){
                    continue;
                }
                do{
                    if (LibMWCapture.MWCaptureAudioFrame(m_channel_handle, ref audio_frame) != LibMWCapture.MW_RESULT.MW_SUCCEEDED){
                        break;
                    }
                    CRingBuffer.st_frame_t frame = m_audio_buffer.get_buffer_to_fill();

                    if (0 == frame.buffer_len){
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
                    for (UInt32 i = 0; i < LibMWCapture.MWCAP_AUDIO_SAMPLES_PER_FRAME; i++){
                        for (UInt32 j = 0; j < m_audio_channel_num; j++){
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