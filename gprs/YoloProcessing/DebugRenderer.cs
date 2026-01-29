using System;
using System.Drawing;

namespace gprs
{
    /// <summary>
    /// 调试信息渲染器 - 负责绘制检测框、关键点、骨架等调试信息
    /// </summary>
    public static class DebugRenderer
    {
        #region 预分配GDI+对象（避免GC压力）
        private static readonly Pen PenGreen = new(Color.Lime, 2);        // 选中目标检测框
        private static readonly Pen PenRed = new(Color.Red, 2);           // 被跳过的检测框
        private static readonly Pen PenYellow = new(Color.Yellow, 2);     // 低置信度检测框
        private static readonly Pen PenCyan = new(Color.Cyan, 1);         // 关键点连线
        private static readonly SolidBrush BrushWhite = new(Color.White); // 文字颜色
        private static readonly SolidBrush BrushBlack = new(Color.Black); // 文字背景
        private static readonly SolidBrush BrushRed = new(Color.Red);     // 跳过原因文字
        private static readonly SolidBrush BrushGreen = new(Color.Lime);  // 高置信度点
        private static readonly SolidBrush BrushOrange = new(Color.Orange);// 中置信度点
        private static readonly Font FontDebug = new("Consolas", 8f);     // 调试字体
        #endregion

        #region 关键点名称（COCO格式17个关键点）
        private static readonly string[] KeypointNames = {
            "鼻", "左眼", "右眼", "左耳", "右耳",
            "左肩", "右肩", "左肘", "右肘", "左腕", "右腕",
            "左髋", "右髋", "左膝", "右膝", "左踝", "右踝"
        };
        #endregion

        #region 骨架连接关系（COCO格式）
        private static readonly int[,] SkeletonConnections = {
            {0, 1}, {0, 2}, {1, 3}, {2, 4},             // 头部
            {5, 6}, {5, 7}, {7, 9}, {6, 8}, {8, 10},    // 上半身
            {5, 11}, {6, 12}, {11, 12},                 // 躯干
            {11, 13}, {13, 15}, {12, 14}, {14, 16}      // 下半身
        };
        #endregion

        #region 可锁定部位名称
        private static readonly string[] LockablePartNames = {
            // 原始姿态点 (0-16)
            "鼻子", "左眼", "右眼", "左耳", "右耳",
            "左肩", "右肩", "左肘", "右肘", "左腕", "右腕",
            "左髋", "右髋", "左膝", "右膝", "左踝", "右踝",
            // 组合部位 (17-20)
            "额头", "双肩中点", "胸", "双髈中点",
            // 兖底 (21)
            "框中心"
        };
        private static readonly Pen PenMagenta = new(Color.Magenta, 2);  // 可锁定部位框
        private static readonly SolidBrush BrushMagenta = new(Color.Magenta); // 可锁定部位点
        #endregion

        /// <summary>
        /// 绘制调试信息到图像上（检测框、关键点、置信度、跳过原因、可锁定部位）
        /// </summary>
        /// <param name="bitmap">目标位图</param>
        /// <param name="result">YOLO识别结果</param>
        /// <param name="lockResult">锁定结果（包含调试信息）</param>
        public static void DrawDebugInfo(Bitmap bitmap, YoloResult<Pose> result, LockResult lockResult)
        {
            using Graphics g = Graphics.FromImage(bitmap);
            int centerX = bitmap.Width / 2;
            int centerY = bitmap.Height / 2;

            int selectedId = lockResult?.SelectedTargetId ?? -1;
            string[] skipReasons = lockResult?.SkipReasons;

            for (int i = 0; i < result.Count; i++)
            {                var pose = result[i];
                var bounds = pose.Bounds;

                // 选择检测框颜色
                Pen boxPen;
                if (i == selectedId)
                    boxPen = PenGreen;         // 选中目标：绿色
                else if (skipReasons != null && skipReasons[i] != null)
                    boxPen = PenRed;           // 被跳过：红色
                else
                    boxPen = PenYellow;        // 其它：黄色

                // 绘制检测框
                g.DrawRectangle(boxPen, bounds.X, bounds.Y, bounds.Width, bounds.Height);

                // 绘制置信度（在检测框右上角）
                string confText = $"{pose.Confidence:F2}";
                g.FillRectangle(BrushBlack, bounds.X + bounds.Width - 35, bounds.Y, 35, 14);
                g.DrawString(confText, FontDebug, BrushWhite, bounds.X + bounds.Width - 34, bounds.Y + 1);

                // 绘制跳过原因（在检测框上方）
                if (skipReasons != null && skipReasons[i] != null)
                {
                    g.FillRectangle(BrushBlack, bounds.X, bounds.Y - 18, skipReasons[i].Length * 9 + 4, 16);
                    g.DrawString(skipReasons[i], FontDebug, BrushRed, bounds.X + 2, bounds.Y - 16);
                }

                // 绘制关键点和置信度
                for (int j = 0; j < 17; j++)
                {
                    var kp = pose[j];
                    if (kp.Confidence < 0.1f) continue; // 置信度太低不绘制

                    int x = kp.Point.X;
                    int y = kp.Point.Y;

                    // 根据置信度选择颜色
                    SolidBrush pointBrush = kp.Confidence > 0.5f ? BrushGreen : BrushOrange;

                    // 绘制关键点圆点
                    g.FillEllipse(pointBrush, x - 3, y - 3, 6, 6);

                    // 绘制置信度数值（只对选中目标显示详细信息）
                    if (i == selectedId || pose.Confidence > 0.7f)
                    {
                        string kpText = $"{KeypointNames[j]}:{kp.Confidence:F1}";
                        g.DrawString(kpText, FontDebug, BrushWhite, x + 4, y - 5);
                    }
                }

                // 绘制骨架连线（只对选中目标）
                if (i == selectedId)
                {
                    DrawSkeleton(g, pose);
                }
            }

            // 绘制屏幕中心准心
            g.DrawLine(PenCyan, centerX - 10, centerY, centerX + 10, centerY);
            g.DrawLine(PenCyan, centerX, centerY - 10, centerX, centerY + 10);

            // 绘制统计信息（左上角）
            int validCount = 0;
            int skippedCount = 0;
            if (skipReasons != null)
            {
                for (int i = 0; i < result.Count; i++)
                {
                    if (skipReasons[i] != null) skippedCount++;
                    else validCount++;
                }
            }
            else
            {
                validCount = result.Count;
            }
            string statsText = $"检测:{result.Count} 有效:{validCount} 跳过:{skippedCount}";
            g.FillRectangle(BrushBlack, 2, 2, statsText.Length * 8 + 4, 16);
            g.DrawString(statsText, FontDebug, BrushWhite, 4, 3);

            // 绘制可锁定部位
            if (lockResult?.Parts != null)
            {
                DrawLockableParts(bitmap, lockResult.Parts, lockResult.SelectedPart);
            }
        }

        /// <summary>
        /// 绘制骨架连线
        /// </summary>
        private static void DrawSkeleton(Graphics g, Pose pose)
        {
            for (int i = 0; i < SkeletonConnections.GetLength(0); i++)
            {
                int start = SkeletonConnections[i, 0];
                int end = SkeletonConnections[i, 1];

                // 只有两个关键点置信度都足够时才绘制连线
                if (pose[start].Confidence > 0.3f && pose[end].Confidence > 0.3f)
                {
                    g.DrawLine(PenCyan,
                        pose[start].Point.X, pose[start].Point.Y,
                        pose[end].Point.X, pose[end].Point.Y);
                }
            }
        }

        /// <summary>
        /// 绘制可锁定部位到图像上（显示所有可用的锁定位置坐标）
        /// </summary>
        /// <param name="bitmap">目标位图</param>
        /// <param name="parts">可锁定部位数组</param>
        /// <param name="selectedPart">选中的部位索引（-1表示无选中）</param>
        public static void DrawLockableParts(Bitmap bitmap, PartInfo[] parts, int selectedPart = -1)
        {
            if (parts == null) return;

            using Graphics g = Graphics.FromImage(bitmap);
            int y = 20; // 右上角起始 Y 位置

            // 绘制标题
            g.FillRectangle(BrushBlack, bitmap.Width - 130, y - 2, 128, 16);
            g.DrawString("可锁定部位:", FontDebug, BrushWhite, bitmap.Width - 128, y);
            y += 16;

            for (int i = 0; i < parts.Length && i < LockablePartNames.Length; i++)
            {
                var part = parts[i];
                bool isSelected = (i == selectedPart);

                // 背景色
                g.FillRectangle(BrushBlack, bitmap.Width - 130, y - 2, 128, 14);

                if (part.IsValid)
                {
                    // 显示部位名称和坐标
                    string text = $"{LockablePartNames[i]}:({part.X},{part.Y})";
                    var brush = isSelected ? BrushGreen : BrushWhite;
                    g.DrawString(text, FontDebug, brush, bitmap.Width - 128, y);

                    // 在图像上绘制可锁定部位标记
                    var pointBrush = isSelected ? BrushGreen : BrushMagenta;
                    g.FillEllipse(pointBrush, part.X - 4, part.Y - 4, 8, 8);
                    g.DrawEllipse(isSelected ? PenGreen : PenMagenta, part.X - 6, part.Y - 6, 12, 12);

                    // 在点旁边显示部位名称
                    g.DrawString(LockablePartNames[i], FontDebug, pointBrush, part.X + 8, part.Y - 5);
                }
                else
                {
                    // 不可用部位显示灰色
                    string text = $" {LockablePartNames[i]}: --";
                    g.DrawString(text, FontDebug, BrushOrange, bitmap.Width - 128, y);
                }

                y += 14;
            }
        }
    }
}
