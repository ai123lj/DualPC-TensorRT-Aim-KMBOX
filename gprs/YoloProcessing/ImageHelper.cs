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
            // 狙击准心（纯红）
            public bool SnipeEnabled;              // 是否启用狙击
            public int MaxRedness;                 // 最大红色度: R-(G+B)/2
            public int ReddestX, ReddestY;         // 最红点坐标
            
            // 步枪准心（黄色）
            public bool RifleEnabled;              // 是否启用步枪
            public int MaxYellowness;              // 最大黄色度: (R+G)/2-B
            public int YellowestX, YellowestY;     // 最黄点坐标
            
            // CF HD停稳检测
            public bool IsSteady;                  // 是否停稳
            public byte AdjacentRed, AdjacentGreen, AdjacentBlue;  // 相邻像素RGB
        }

        // 准心判定阈值（旧：redness / yellowness版，指标仍保留输出方便调试）
        public const int CROSSHAIR_THRESHOLD = 254;  // 旧狙击阈值：R-(G+B)/2 > 254（已废弃，用于日志）
        public const int RIFLE_THRESHOLD = 254;      // 旧步枪阈值：(R+G)/2-B > 254（已废弃，用于日志）

        // 新判定：严格用 R=255 && B=0 划分“是否为准心”，再用 G 通道区分狙击与步枪。
        // 游戏场景中极少出现 R 满值同时 B 绝对为 0 的像素（捕捉到就是准心或命中特效），
        // 步枪命中渐变整段满足 R=255 && B=0，G 从 10→255连续变化，涵盖在步枪判定内。
        public const int CROSSHAIR_R_EXACT = 255;    // 红通道必须等于
        public const int CROSSHAIR_B_EXACT = 0;      // 蓝通道必须等于

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

                // 严格判定标志：2×2 内是否存在一个 R=255 && B=0 的像素（采样命中或纯红准心）
                bool hasSnipePixel = false;   // R=255 && G=0   && B=0 → 狙击纯红准心
                bool hasRiflePixel = false;   // R=255 && G>0   && B=0 → 步枪黄色准心 或 命中渐变（红→橙→黄）

                // 检查2x2区域（中心点周围），同时检测红色和黄色
                for (int yOffset = 0; yOffset <= 1; yOffset++)
                {
                    int y = centerY + yOffset;
                    if (y < 0 || y >= bmpData.Height) continue;

                    byte* currentLine = ptr + (y * stride);

                    for (int xOffset = 0; xOffset <= 1; xOffset++)
                    {
                        int x = centerX + xOffset;
                        if (x < 0 || x >= bmpData.Width) continue;

                        int pixelPos = x * bytesPerPixel;
                        byte blue = currentLine[pixelPos];
                        byte green = currentLine[pixelPos + 1];
                        byte red = currentLine[pixelPos + 2];

                        // 调试指标：红色度（老 redness 计算）
                        int redness = red - (green + blue) / 2;
                        if (redness > info.MaxRedness)
                        {
                            info.MaxRedness = redness;
                            info.ReddestX = x;
                            info.ReddestY = y;
                        }

                        // 调试指标：黄色度（老 yellowness 计算）
                        int yellowness = (red + green) / 2 - blue;
                        if (yellowness > info.MaxYellowness)
                        {
                            info.MaxYellowness = yellowness;
                            info.YellowestX = x;
                            info.YellowestY = y;
                        }

                        // 新判定：R=255 && B=0 → 准心像素，G 通道区分狙/步
                        if (red == CROSSHAIR_R_EXACT && blue == CROSSHAIR_B_EXACT)
                        {
                            if (green == 0)
                            {
                                hasSnipePixel = true;
                                info.ReddestX = x;   // 覆盖为真正的纯红点坐标
                                info.ReddestY = y;
                            }
                            else
                            {
                                hasRiflePixel = true;
                            }
                        }
                    }
                }

                // 新判定规则：狙击优先（纯红），步枪关系互斥同时涵盖命中渐变色
                info.SnipeEnabled = hasSnipePixel;
                info.RifleEnabled = !hasSnipePixel && hasRiflePixel;

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
