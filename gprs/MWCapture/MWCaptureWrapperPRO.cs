using MWModle;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using static MWModle.LibMWCapture;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace gprs
{
    public class MWCaptureWrapperPro : CMWCapture
    {
        #region 私有字段
        Boolean m_video_capturing = false;

        Byte m_bottom_up = 0;

        Thread m_video_thread = null;

        // 修改回调函数字段，添加时间戳参数
        private Action<CRingBuffer.st_frame_t, int, int> m_frameCallback;
        
        #endregion

        #region 构造函数
        public MWCaptureWrapperPro()
        {
            Console.WriteLine("create pro capture\n");
        }
        ~MWCaptureWrapperPro()
        {
            Dispose();
            Console.WriteLine("destory pro capture");
        }
        #endregion

        #region 公共属性


        #endregion

        #region 公共方法

        /// <summary>
        /// 设置帧回调函数
        /// </summary>
        /// <param name="callback">回调函数，参数为处理后的Bitmap图像和时间戳</param>
        public void SetFrameCallback(Action<CRingBuffer.st_frame_t, int, int> callback)
        {
            m_frameCallback = callback;
        }

        public override void Dispose()
        {
            if (m_video_thread != null)
            {
                m_video_capturing = false;
                m_video_thread.Join();
                m_video_thread = null;
            }

            base.Dispose();
        }

        bool check()
        {
            if (IntPtr.Zero == m_channel_handle)
            {
                set_device(m_device_index);
            }
            if (IntPtr.Zero == m_channel_handle)
            {
                return false;
            }
            if (LibMWCapture.MW_FAMILY_ID.MW_FAMILY_ID_PRO_CAPTURE != m_device_family)
            {
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
            // 设置要采集的媒体类型（视频和/或音频）
            m_capture_video = video;
            m_capture_audio = audio;

            // 检查设备是否可用，包括设备句柄和设备类型验证
            if (!check())
            {
                return false;
            }

            // 如果反转标志未设置，使用默认的底部向上标志
            if (m_is_reverse < 0)
            {
                m_is_reverse = m_bottom_up;
            }

            // 设置镜像标志为0（Pro设备不支持镜像）
            m_is_mirror = 0;

            // 如果需要采集视频
            if (video)
            {
                // 检查视频缓冲区是否配置正确
                if (!check_video_buffer())
                {
                    Console.WriteLine("chenck video buffer fail\n");
                    return false;
                }

                // 设置视频采集标志为true，表示开始采集
                m_video_capturing = true;

                // 创建视频采集线程，执行video_capture_pro方法
                m_video_thread = new Thread(new ThreadStart(capture_by_input));
                if (null == m_video_thread)
                {
                    // 线程创建失败，打印错误信息并返回false
                    Console.WriteLine("capture video fail\n");
                    return false;
                }

                // 启动视频采集线程
                m_video_thread.Start();
            }

            // 采集启动成功，返回true
            return true;
        }
        void capture_by_input()
        {
            #region 初始化
            // 打印输入触发采集模式开始日志
            Console.WriteLine("capture video by input in\n");

            // 创建视频采集事件对象，用于等待视频帧捕获完成
            IntPtr capture_event = Libkernel32.CreateEvent(IntPtr.Zero, 0, 0, IntPtr.Zero);
            if (IntPtr.Zero == capture_event)
            {
                // 事件创建失败，打印错误信息并返回
                Console.WriteLine("create event fail\n");
                return;
            }

            // 启动视频采集，将采集事件与通道句柄关联
            if (LibMWCapture.MWStartVideoCapture(m_channel_handle, capture_event) != LibMWCapture.MW_RESULT.MW_SUCCEEDED)
            {
                // 视频采集启动失败，打印错误信息并清理资源
                Console.WriteLine("start video capture fail\n");
                Libkernel32.CloseHandle(capture_event);
                return;
            }

            // 创建通知事件对象，用于接收视频帧缓冲和信号变化通知
            IntPtr notify_event = Libkernel32.CreateEvent(IntPtr.Zero, 0, 0, IntPtr.Zero);
            // 注册通知事件，监听视频帧缓冲和视频信号变化事件
            // 通知类型说明：
            //   - MWCAP_NOTIFY_VIDEO_FRAME_BUFFERED: 帧完全缓冲到板载内存后通知（普通模式）
            //   - MWCAP_NOTIFY_VIDEO_FRAME_BUFFERING: 帧开始缓冲时通知（低延迟模式，可更早开始DMA传输）
            // 当前使用 BUFFERING 模式，配合 iNewestBuffering 获取正在缓冲的帧
            UInt64 notify = LibMWCapture.MWRegisterNotify(m_channel_handle, notify_event, LibMWCapture.MWCAP_NOTIFY_VIDEO_FRAME_BUFFERING | LibMWCapture.MWCAP_NOTIFY_VIDEO_SIGNAL_CHANGE);
            if (notify == 0)
            {
                // 通知事件注册失败，打印错误信息并跳转到清理代码
                Console.WriteLine("register notify fail\n");
                goto end_and_free;
            }

            // 计算视频帧的步长和大小
            UInt32 stride = MWFOURCC.FOURCC_CalcMinStride(m_mw_fourcc, m_width, 2);
            UInt32 frame_size = MWFOURCC.FOURCC_CalcImageSize(m_mw_fourcc, m_width, m_height, stride);

            // 获取视频缓冲区的第一个帧
            CRingBuffer.st_frame_t frame = m_video_buffer.get_buffer_by_index(0);
            frame.buffer_len = 0;
            frame.p_buffer = null;

            // 固定所有视频缓冲区，以便直接内存访问
            for (Int32 i = 0; i < m_video_buffer.m_buffer_num; i++)
            {
                frame = m_video_buffer.get_buffer_by_index(i);
                if (0 == frame.buffer_len)
                {
                    break;
                }
                LibMWCapture.MWPinVideoBuffer(m_channel_handle, frame.p_buffer, frame_size);
            }

            // 初始化时间变量和状态变量
            Int64 now_tm = 0;
            // 分配视频采集状态结构的内存
            IntPtr p_capture_status = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(LibMWCapture.MWCAP_VIDEO_CAPTURE_STATUS)));

            MWCAP_VIDEO_BUFFER_INFO video_buffer_info = new();
            MWCAP_VIDEO_FRAME_INFO video_frame_info = new();

            // 信号状态标志，表示是否有有效的视频信号
            bool have_signal = true;
            // 事件等待时间，根据信号状态动态调整
            UInt32 event_wait_time = 100;

            // 获取视频缓冲区信息
            LibMWCapture.MWGetVideoBufferInfo(m_channel_handle, ref video_buffer_info);
            LibMWCapture.MWGetVideoFrameInfo(m_channel_handle, video_buffer_info.iNewestBuffering, ref video_frame_info);

            //从源图像截取的区域
            int left= (video_frame_info.cx - m_width) / 2;
            int top = (video_frame_info.cy - m_height) / 2;
            int right = video_frame_info.cx / 2 + m_width / 2;
            int bottom = video_frame_info.cy / 2 + m_height / 2;
            #endregion
            // 主采集循环，持续采集视频帧直到停止采集
            while (m_video_capturing)
            {
                // 等待通知事件，超时时间为event_wait_time毫秒
                if (0 != Libkernel32.WaitForSingleObject(notify_event, event_wait_time))
                {
                    // 如果有信号且等待超时，继续下一次循环
                    if (have_signal)
                    {
                        continue;
                    }
                }
                else
                {
                    // 事件触发，获取通知状态
                    UInt64 notify_status = 0;
                    if (LibMWCapture.MWGetNotifyStatus(m_channel_handle, notify, ref notify_status) != LibMWCapture.MW_RESULT.MW_SUCCEEDED)
                    {
                        // 获取通知状态失败，继续下一次循环
                        continue;
                    }

                    // 检查是否是视频信号变化通知
                    if (0 != (notify_status & LibMWCapture.MWCAP_NOTIFY_VIDEO_SIGNAL_CHANGE))
                    {
                        // 获取视频信号状态
                        LibMWCapture.MWCAP_VIDEO_SIGNAL_STATUS video_signal_status = new LibMWCapture.MWCAP_VIDEO_SIGNAL_STATUS();
                        LibMWCapture.MWGetVideoSignalStatus(m_channel_handle, ref video_signal_status);

                        // 根据信号状态更新标志和等待时间
                        if (LibMWCapture.MWCAP_VIDEO_SIGNAL_STATE.MWCAP_VIDEO_SIGNAL_LOCKED == video_signal_status.state)
                        {
                            // 信号锁定，表示有有效视频信号
                            have_signal = true;
                            event_wait_time = 100;
                        }
                        else
                        {
                            // 信号未锁定，表示无有效视频信号
                            have_signal = false;
                            event_wait_time = 25;
                        }
                        continue;
                    }
                    // 检查是否是视频帧缓冲通知
                    else if (0 == (notify_status & LibMWCapture.MWCAP_NOTIFY_VIDEO_FRAME_BUFFERING))
                    {
                        // 不是帧缓冲通知，继续下一次循环
                        continue;
                    }
                }

                // 获取视频缓冲区信息
                // 帧标识说明：
                //   - iNewestBufferedFullFrame: 最新完全缓冲的帧（普通模式使用）
                //   - iNewestBuffering: 正在缓冲的帧（低延迟模式，配合BUFFERING通知）
                // 当前使用 iNewestBuffering，可在源帧未完全到达板载内存时就开始DMA传输
                LibMWCapture.MWGetVideoBufferInfo(m_channel_handle, ref video_buffer_info);               

                // 捕获视频帧到主机内存（DMA传输）
                // 关键参数说明：
                //   - iFrame: 使用 iNewestBuffering 获取正在缓冲的帧（低延迟）
                //   - cyParitalNotify: DMA部分完成通知行数
                //       * 0: 不使用部分通知，等整帧完成后触发capture_event（当前设置）
                //       * 64/128/256: 每传输完成指定行数就触发capture_event，用于边传输边处理
                //     对于YOLO检测需要完整图像，设置为0即可
                //   - pRectSrc: 从源图像截取的区域（当前截取中心640x640）
                LibMWCapture.MWCaptureVideoFrameToVirtualAddressEx(
                    m_channel_handle,                                                                   // hChannel: 通道句柄，标识要操作的采集设备通道
                    video_buffer_info.iNewestBuffering,                                                 // iFrame: 帧标识符
                    frame.p_buffer,                                                                     // pbFrame: 目标缓冲区指针，指向存储捕获帧数据的内存区域
                    frame_size,                                                                         // cbFrame: 缓冲区大小，以字节为单位
                    stride,                                                                             // cbStride: 行跨度，每行像素数据的字节数
                    m_bottom_up,                                                                        // bBottomUp: 是否垂直翻转，0表示不翻转，1表示翻转
                    0,                                                                                  // pvContext: 上下文指针，用于标识特定的捕获操作
                    m_mw_fourcc,                                                                        // dwFOURCC: 图像格式，指定捕获帧的像素格式
                    m_width,                                                                            // cx: 图像宽度，以像素为单位
                    m_height,                                                                           // cy: 图像高度，以像素为单位
                    0,                                                                                  // dwProcessSwitchs: 处理开关，控制图像处理选项（如翻转、镜像等）
                    0,                                                                                  // cyParitalNotify: DMA部分完成通知行数
                                                                                                        //   0=等整帧完成, 64/128/256=边传边处理（适用于视频编码等场景）
                                                                                                        //   注意：此行数基于目标输出尺寸(640x640)，而非源帧(2K)
                                                                                                        //   截取中心区域时，需等源帧缓冲到中间位置才能开始有效传输
                                                                                                        //   YOLO需要完整图像，故设为0
                    0,                                                                                  // hOSDImage: OSD图像句柄，用于叠加显示信息
                    null,                                                                               // pOSDRects: OSD矩形区域数组，定义OSD图像的位置和大小
                    0,                                                                                  // cOSDRects: OSD矩形区域数量
                    100,                                                                                // sContrast: 对比度调整值，范围通常为-100到100
                    0,                                                                                  // sBrightness: 亮度调整值，范围通常为-100到100
                    100,                                                                                // sSaturation: 饱和度调整值，范围通常为-100到100
                    0,                                                                                  // sHue: 色调调整值，范围通常为-100到100
                    LibMWCapture.MWCAP_VIDEO_DEINTERLACE_MODE.MWCAP_VIDEO_DEINTERLACE_WEAVE,            // deinterlaceMode: 去隔行模式，指定如何处理隔行扫描视频
                    LibMWCapture.MWCAP_VIDEO_ASPECT_RATIO_CONVERT_MODE.MWCAP_VIDEO_ASPECT_RATIO_IGNORE, // aspectRatioConvertMode: 宽高比转换模式，控制图像缩放时的宽高比处理
                    [new LibMWCapture.RECT { left = left, top = top, right = right, bottom = bottom }], // pRectSrc: 源矩形区域，定义要捕获的源图像区域
                    [new LibMWCapture.RECT { left = 0, top = 0, right = m_width, bottom = m_height }],  // pRectDest: 目标矩形区域，定义图像在目标缓冲区中的位置和大小
                    0,                                                                                  // nAspectX: 宽高比X值，用于保持图像的宽高比
                    0,                                                                                  // nAspectY: 宽高比Y值，用于保持图像的宽高比
                    LibMWCapture.MWCAP_VIDEO_COLOR_FORMAT.MWCAP_VIDEO_COLOR_FORMAT_UNKNOWN,             // colorFormat: 颜色格式，指定视频的颜色空间标准
                    LibMWCapture.MWCAP_VIDEO_QUANTIZATION_RANGE.MWCAP_VIDEO_QUANTIZATION_UNKNOWN,       // quantRange: 量化范围，定义像素值的有效范围
                    LibMWCapture.MWCAP_VIDEO_SATURATION_RANGE.MWCAP_VIDEO_SATURATION_UNKNOWN);          // satRange: 饱和度范围，定义色彩饱和度的处理方式

                // 等待DMA传输完成
                // 如果 cyParitalNotify > 0，每传输完成指定行数就会触发capture_event
                // 可通过 captureStatus.cyCompleted 查看已完成行数，实现边传边处理
                // 当前 cyParitalNotify=0，所以只会在整帧完成时触发一次
                while (true)
                {
                    // INFINITE(0xFFFFFFFF): 无限等待，因为DMA传输很快（毫秒级）
                    if (0 != Libkernel32.WaitForSingleObject(capture_event, 0xFFFFFFFF))
                        continue;
                    if (LibMWCapture.MW_RESULT.MW_SUCCEEDED != LibMWCapture.MWGetVideoCaptureStatus(m_channel_handle, p_capture_status))
                        continue;
                    LibMWCapture.MWCAP_VIDEO_CAPTURE_STATUS status = (LibMWCapture.MWCAP_VIDEO_CAPTURE_STATUS)Marshal.PtrToStructure(p_capture_status, typeof(LibMWCapture.MWCAP_VIDEO_CAPTURE_STATUS));                 
                    if (status.bFrameCompleted != 0)
                        break;
                }

                //LibMWCapture.MWGetDeviceTime(m_channel_handle, ref now_tm);
                ////Delay(4);
                //LibMWCapture.MWGetVideoFrameInfo(m_channel_handle, video_buffer_info.iNewestBuffering, ref video_frame_info);
                //System.Diagnostics.Debug.WriteLine($"时间: " +
                //    //$"信号输入{(float)(video_frame_info.allFieldBufferedTimes[0] - video_frame_info.allFieldStartTimes[0]) / 10000}ms," +
                //    //$"开始采集{(float)(frame.ts - video_frame_info.allFieldStartTimes[0]) / 10000}ms," +
                //    $"软硬总共{(float)(now_tm - video_frame_info.allFieldStartTimes[0]) / 10000}ms," +
                //    //$"除帧传输{(float)(now_tm - video_frame_info.allFieldBufferedTimes[0]) / 10000}ms"+                    
                //    //$"软件时间{(float)(now_tm - frame.ts) / 10000}ms" +
                //    //$"{video_frame_info.allFieldStartTimes[0] / 10000000}s,{(float)(video_frame_info.allFieldStartTimes[0] % 10000000) / 10000}ms"
                //    "");


                // 触发回调函数（如果已设置）
                if (m_frameCallback != null)
                {
                    m_frameCallback(frame, m_width, m_height);
                }

                // 重置缓冲区长度
                //frame.buffer_len = 0;
            }

            // 释放所有固定的视频缓冲区
            for (Int32 i = 0; i < m_video_buffer.m_buffer_num; i++)
            {
                frame = m_video_buffer.get_buffer_by_index(i);
                if (0 == frame.buffer_len)
                {
                    break;
                }
                LibMWCapture.MWUnpinVideoBuffer(m_channel_handle, frame.p_buffer);
            }

            // 打印输入触发采集模式结束日志
            Console.WriteLine("capture video by input out\n");

        end_and_free:
            // 注销通知事件
            if (0 != notify)
            {
                LibMWCapture.MWUnregisterNotify(m_channel_handle, notify);
            }
            // 关闭通知事件句柄
            if (IntPtr.Zero != notify_event)
            {
                Libkernel32.CloseHandle(notify_event);
            }
            // 关闭采集事件句柄
            if (IntPtr.Zero != capture_event)
            {
                Libkernel32.CloseHandle(capture_event);
            }
        }
        
        public long Get_device_time()
        {
            long now_tm = 0;
            LibMWCapture.MWGetDeviceTime(m_channel_handle, ref now_tm);
            return now_tm/10;
        }

        public unsafe Bitmap ConvertFrameToBitmapRGB24(CRingBuffer.st_frame_t frame, ref Bitmap bitmap)
        {
            if (bitmap == null)
            {
                bitmap?.Dispose();
                bitmap = new Bitmap(m_width, m_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            }

            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, m_width, m_height),
                ImageLockMode.WriteOnly, bitmap.PixelFormat);
            try
            {
                int outputStride = bmpData.Stride;
                byte* destPtr = (byte*)bmpData.Scan0;

                // 相同尺寸的直接内存拷贝               
                int copyBytes = Math.Min((int)frame.buffer_len, outputStride * m_height);
                Buffer.MemoryCopy(
                    (void*)Unsafe.AsPointer(ref frame.p_buffer[0]),
                    (void*)destPtr,
                    copyBytes,
                    copyBytes
                );
                
            }
            finally
            {
                bitmap.UnlockBits(bmpData);
            }

            return bitmap;
        }
        #endregion

        #region 私有方法
        public void Delay(int delayMilliseconds)
        {
            // 创建一个Stopwatch实例来测量时间
            Stopwatch stopwatch = new Stopwatch();

            // 开始Stopwatch
            stopwatch.Start();

            // 当Stopwatch记录的时间小于设定的延迟时间时，持续循环
            while (stopwatch.ElapsedMilliseconds < delayMilliseconds)
            {
                // 通过Thread.Sleep(0)释放当前线程的时间片，防止占用CPU
                Thread.Sleep(0);
            }
            // 停止Stopwatch
            stopwatch.Stop();
        }
        #endregion
    }
}