using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace gprs
{
    /// <summary>
    /// 图像处理辅助工具类
    /// </summary>
    public static class ImageHelper
    {
        /// <summary>
        /// 高效克隆Bitmap（使用unsafe内存拷贝）
        /// </summary>
        /// <param name="srcBitmap">源位图</param>
        /// <param name="targetBitmap">目标位图（必须已分配且尺寸匹配）</param>
        public static unsafe void BitmapClone(Bitmap srcBitmap, Bitmap targetBitmap)
        {
            // 锁定两者的位图数据区域
            BitmapData srcData = srcBitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, srcBitmap.Width, srcBitmap.Height),
                ImageLockMode.ReadOnly,
                srcBitmap.PixelFormat
            );

            BitmapData dstData = targetBitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, targetBitmap.Width, targetBitmap.Height),
                ImageLockMode.WriteOnly,
                targetBitmap.PixelFormat
            );

            try
            {
                // 使用 unsafe 代码直接内存拷贝
                byte* srcPtr = (byte*)srcData.Scan0;
                byte* dstPtr = (byte*)dstData.Scan0;

                // 计算总字节数（考虑 stride 对齐）
                int totalBytes = Math.Abs(srcData.Stride) * srcData.Height;

                // 使用 Buffer.MemoryCopy 进行高效内存拷贝
                Buffer.MemoryCopy(
                    srcPtr,      // 源指针
                    dstPtr,      // 目标指针
                    totalBytes,  // 目标缓冲区大小
                    totalBytes   // 要复制的字节数
                );
            }
            finally
            {
                // 确保总是解锁位图
                srcBitmap.UnlockBits(srcData);
                targetBitmap.UnlockBits(dstData);
            }
        }

        /// <summary>
        /// 执行YOLO姿态检测（TensorRT 加速）
        /// </summary>
        /// <param name="frame">输入帧位图</param>
        /// <param name="predictor">TensorRT 推理器</param>
        /// <returns>姿态检测结果</returns>
        public static YoloResult<Pose> ProcessYoloDetection(Bitmap frame, TrtYoloPoseInferencer predictor)
        {
            return predictor.Pose(frame);
        }

        #region 准心检测
        /// <summary>
        /// 准心检测结果
        /// </summary>
        public struct CrosshairInfo
        {
            public bool SnipeEnabled;      // 是否启用狙击（红色值>阈值）
            public bool IsSteady;          // 是否停稳（CF HD用，相邻像素为黑色）
            public int MaxRedness;         // 最大红色值
            public int ReddestX;           // 最红点X坐标
            public int ReddestY;           // 最红点Y坐标
            // 相邻像素信息（调试用）
            public byte AdjacentRed, AdjacentGreen, AdjacentBlue;
        }

        // 准心检测阈值
        public const int CROSSHAIR_THRESHOLD = 253;  // 狙击枪255，其他枪约243，253兼顾冗余和区分

        /// <summary>
        /// 从位图中心区域读取游戏准心状态
        /// </summary>
        /// <param name="bitmap">输入位图</param>
        /// <param name="checkSteady">是否检测停稳状态（CF HD用）</param>
        /// <returns>准心检测结果</returns>
        public static unsafe CrosshairInfo ReadGameCrosshairInfo(Bitmap bitmap, bool checkSteady = false)
        {
            var info = new CrosshairInfo();

            BitmapData bmpData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                bitmap.PixelFormat);

            try
            {
                byte* ptr = (byte*)bmpData.Scan0;
                int bytesPerPixel = 3; // 24bpp = 3字节/像素
                int stride = bmpData.Stride;
                int centerX = bmpData.Width / 2;
                int centerY = bmpData.Height / 2;

                // 检查3x3区域，找最红点
                for (int yOffset = 0; yOffset <= 2; yOffset++)
                {
                    int y = centerY + yOffset;
                    if (y < 0 || y >= bmpData.Height) continue;

                    byte* currentLine = ptr + (y * stride);

                    for (int xOffset = 0; xOffset <= 2; xOffset++)
                    {
                        int x = centerX + xOffset;
                        if (x < 0 || x >= bmpData.Width) continue;

                        int pixelPos = x * bytesPerPixel;
                        byte blue = currentLine[pixelPos];
                        byte green = currentLine[pixelPos + 1];
                        byte red = currentLine[pixelPos + 2];

                        // 计算红色程度
                        int redness = red - (green + blue) / 2;

                        if (redness > info.MaxRedness)
                        {
                            info.MaxRedness = redness;
                            info.ReddestX = x;
                            info.ReddestY = y;
                        }
                    }
                }

                // 判断是否启用狙击
                info.SnipeEnabled = info.MaxRedness > CROSSHAIR_THRESHOLD;

                // CF HD停稳检测：检测最红点右边两个像素的颜色
                if (checkSteady && info.SnipeEnabled)
                {
                    int adjX = info.ReddestX + 2;
                    if (adjX < bmpData.Width)
                    {
                        byte* adjLine = ptr + (info.ReddestY * stride);
                        int adjPos = adjX * bytesPerPixel;
                        info.AdjacentBlue = adjLine[adjPos];
                        info.AdjacentGreen = adjLine[adjPos + 1];
                        info.AdjacentRed = adjLine[adjPos + 2];

                        // 相邻像素全黑表示停稳，非黑色表示未停稳
                        info.IsSteady = (info.AdjacentRed == 0 && info.AdjacentGreen == 0 && info.AdjacentBlue == 0);
                        
                        // 如果未停稳，禁用狙击
                        if (!info.IsSteady)
                            info.SnipeEnabled = false;
                    }
                    else
                    {
                        info.IsSteady = true; // 边界情况默认停稳
                    }
                }
                else
                {
                    info.IsSteady = true; // 不检测时默认停稳
                }
            }
            finally
            {
                bitmap.UnlockBits(bmpData);
            }

            return info;
        }
        #endregion
    }
}
