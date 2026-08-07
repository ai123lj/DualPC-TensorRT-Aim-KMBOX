using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace gprs
{
    public partial class TestMoveForm : Form
    {
        #region 移动点数据结构
        public class MovePoint
        {
            public int Index { get; set; }
            public int DeltaX { get; set; }
            public int DeltaY { get; set; }
            public int AccumX { get; set; }
            public int AccumY { get; set; }
            public double ElapsedMs { get; set; }
        }
        #endregion

        #region 字段
        private readonly List<MovePoint> _movePoints = new();
        private readonly object _pointsLock = new();  // 线程同步锁
        private readonly Random _rand = new();
        private Stopwatch _totalStopwatch = new();
        private int _accumX = 0;
        private int _accumY = 0;
        private bool _isRunning = false;
        #endregion

        public TestMoveForm()
        {
            InitializeComponent();
        }

        #region 事件处理
        private async void BtnStart_Click(object sender, EventArgs e)
        {
            if (_isRunning) return;
            _isRunning = true;
            _btnStart.Enabled = false;

            // 清空之前的数据
            ClearData();

            // 获取参数
            int targetX = (int)_numTargetX.Value;
            int targetY = (int)_numTargetY.Value;
            double smoothness = (double)_numSmoothness.Value;
            int smoothThreshold = (int)_numSmoothThreshold.Value;
            double easingPower = (double)_numEasingPower.Value;
            bool enableRandomArc = _chkRandomArc.Checked;
            bool enableEasing = _chkEasing.Checked;
            bool enableJitter = _chkJitter.Checked;
            bool enableOvershoot = _chkOvershoot.Checked;

            // 在后台线程执行移动模拟
            await Task.Run(() =>
            {
                _totalStopwatch.Restart();
                HumanLikeMoveSimulate(targetX, targetY, smoothness, smoothThreshold, easingPower,
                    enableRandomArc, enableEasing, enableJitter, enableOvershoot);
                _totalStopwatch.Stop();
            });

            // 更新统计
            UpdateStats();

            _isRunning = false;
            _btnStart.Enabled = true;
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            ClearData();
            _lblStats.Text = "统计: 等待测试...";
        }

        private void ClearData()
        {
            _movePoints.Clear();
            _accumX = 0;
            _accumY = 0;
            _gridPoints.Rows.Clear();
            _picTrajectory.Invalidate();
        }

        private void UpdateStats()
        {
            if (_movePoints.Count == 0)
            {
                _lblStats.Text = "统计: 无数据";
                return;
            }

            var lastPoint = _movePoints[_movePoints.Count - 1];
            double avgInterval = lastPoint.ElapsedMs / _movePoints.Count;

            _lblStats.Text = $"统计: 总点数: {_movePoints.Count}  |  " +
                            $"总时间: {lastPoint.ElapsedMs:F1}ms  |  " +
                            $"平均间隔: {avgInterval:F2}ms  |  " +
                            $"最终位置: ({lastPoint.AccumX}, {lastPoint.AccumY})";
        }
        #endregion

        #region 模拟 MouseMove（记录点位 + 精确延时）
        private void SimulateMouseMove(int deltaX, int deltaY)
        {
            // 精确延时 1ms
            Delay(1);

            // 记录点位
            _accumX += deltaX;
            _accumY += deltaY;

            var point = new MovePoint
            {
                Index = _movePoints.Count + 1,
                DeltaX = deltaX,
                DeltaY = deltaY,
                AccumX = _accumX,
                AccumY = _accumY,
                ElapsedMs = _totalStopwatch.Elapsed.TotalMilliseconds
            };
            lock (_pointsLock)
            {
                _movePoints.Add(point);
            }

            // 更新 UI（跨线程）
            this.BeginInvoke(new Action(() =>
            {
                _gridPoints.Rows.Add(point.Index, point.DeltaX, point.DeltaY,
                    point.AccumX, point.AccumY, $"{point.ElapsedMs:F1}");
                _gridPoints.FirstDisplayedScrollingRowIndex = _gridPoints.Rows.Count - 1;
                _picTrajectory.Invalidate();
            }));
        }

        /// <summary>
        /// 精确延时（使用 Stopwatch 忙等待）
        /// </summary>
        private void Delay(int delayMilliseconds)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            while (stopwatch.ElapsedMilliseconds < delayMilliseconds)
            {
                Thread.Sleep(0); // 释放时间片，防止占用 CPU
            }
            stopwatch.Stop();
        }
        #endregion

        #region HumanLikeMove 模拟实现（复制自 KmBoxNet，MouseMove 替换为 SimulateMouseMove）
        private void HumanLikeMoveSimulate(
            int targetX,
            int targetY,
            double smoothness = 1.0,
            int smoothThreshold = 20,
            double easingPower = 2.0,
            bool enableRandomArc = true,
            bool enableEasing = true,
            bool enableJitter = true,
            bool enableOvershoot = false)
        {
            if (smoothness < 0.1) smoothness = 0.1;
            if (targetX == 0 && targetY == 0) return;

            double distance = Math.Sqrt(targetX * targetX + targetY * targetY);

            // 短距离快速路径
            if (distance < smoothThreshold)
            {
                SimulateMouseMove(targetX, targetY);
                return;
            }

            double angle = Math.Atan2(targetY, targetX);

            // 随机弧度方向
            int arcDirection = enableRandomArc ? (_rand.Next(2) == 0 ? 1 : -1) : 1;
            double offsetRatio = enableRandomArc ? (0.05 + _rand.NextDouble() * 0.1) : 0.1;
            double offset = distance * offsetRatio * arcDirection;

            // 贝塞尔控制点
            double cp1X = targetX * 0.33 + Math.Sin(angle) * offset;
            double cp1Y = targetY * 0.33 - Math.Cos(angle) * offset;
            double cp2X = targetX * 0.66;
            double cp2Y = targetY * 0.66;

            // 步数计算：距离 × 平滑度
            int steps = Math.Max(10, (int)(distance * smoothness));
            double lastSentX = 0, lastSentY = 0;

            // 主循环：每步都发送
            for (int i = 1; i <= steps; i++)
            {
                double t = (double)i / steps;

                // 缓入缓出变换
                if (enableEasing)
                    t = t < 0.5
                        ? Math.Pow(2, easingPower - 1) * Math.Pow(t, easingPower)
                        : 1 - Math.Pow(-2 * t + 2, easingPower) / 2;

                double u = 1 - t;

                // 贝塞尔曲线计算
                double currentX = 3 * u * u * t * cp1X + 3 * u * t * t * cp2X + t * t * t * targetX;
                double currentY = 3 * u * u * t * cp1Y + 3 * u * t * t * cp2Y + t * t * t * targetY;

                // 微抖动
                if (enableJitter && i < steps)
                {
                    currentX += _rand.Next(-1, 2) * 0.5;
                    currentY += _rand.Next(-1, 2) * 0.5;
                }

                // 计算本步位移
                int deltaX = (int)Math.Round(currentX - lastSentX);
                int deltaY = (int)Math.Round(currentY - lastSentY);

                // 每步都发送（保留加减速效果）
                if (deltaX != 0 || deltaY != 0)
                {
                    SimulateMouseMove(deltaX, deltaY);
                    lastSentX += deltaX;
                    lastSentY += deltaY;
                }
                else if (i < steps)
                {
                    // 位移为0时仍需延迟，保持时间节奏
                    SimulateMouseMove(0, 0);
                }
            }

            // 过冲修正
            if (enableOvershoot && distance > 20)
            {
                int overshootX = (int)(Math.Sign(targetX) * (2 + _rand.Next(4)));
                int overshootY = (int)(Math.Sign(targetY) * (2 + _rand.Next(4)));
                SimulateMouseMove(overshootX, overshootY);
                SimulateMouseMove(-overshootX, -overshootY);
            }

            // 最终误差修正
            int finalDeltaX = targetX - (int)Math.Round(lastSentX);
            int finalDeltaY = targetY - (int)Math.Round(lastSentY);
            if (finalDeltaX != 0 || finalDeltaY != 0)
                SimulateMouseMove(finalDeltaX, finalDeltaY);
        }
        #endregion

        #region 轨迹绘制
        private void PicTrajectory_Paint(object sender, PaintEventArgs e)
        {
            // 复制列表避免遍历时被修改
            List<MovePoint> pointsCopy;
            lock (_pointsLock)
            {
                if (_movePoints.Count < 2) return;
                pointsCopy = new List<MovePoint>(_movePoints);
            }

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // 计算缩放和偏移，使轨迹居中显示
            int margin = 50;
            int width = _picTrajectory.Width - margin * 2;
            int height = _picTrajectory.Height - margin * 2;

            // 找到边界
            int minX = 0, maxX = 0, minY = 0, maxY = 0;
            foreach (var p in pointsCopy)
            {
                if (p.AccumX < minX) minX = p.AccumX;
                if (p.AccumX > maxX) maxX = p.AccumX;
                if (p.AccumY < minY) minY = p.AccumY;
                if (p.AccumY > maxY) maxY = p.AccumY;
            }

            int rangeX = Math.Max(maxX - minX, 1);
            int rangeY = Math.Max(maxY - minY, 1);
            float scale = Math.Min((float)width / rangeX, (float)height / rangeY);
            scale = Math.Min(scale, 3f); // 限制最大缩放

            // 坐标转换函数
            PointF ToScreen(int x, int y)
            {
                float sx = margin + (x - minX) * scale;
                float sy = margin + (y - minY) * scale;
                return new PointF(sx, sy);
            }

            // 绘制起点
            var startPoint = ToScreen(0, 0);
            g.FillEllipse(Brushes.Green, startPoint.X - 6, startPoint.Y - 6, 12, 12);

            // 绘制轨迹线
            using (var pen = new Pen(Color.Blue, 2))
            {
                var points = new List<PointF> { ToScreen(0, 0) };
                foreach (var p in pointsCopy)
                {
                    points.Add(ToScreen(p.AccumX, p.AccumY));
                }

                if (points.Count >= 2)
                    g.DrawLines(pen, points.ToArray());
            }

            // 绘制采样点
            using (var brush = new SolidBrush(Color.FromArgb(150, Color.Red)))
            {
                foreach (var p in pointsCopy)
                {
                    var pt = ToScreen(p.AccumX, p.AccumY);
                    g.FillEllipse(brush, pt.X - 2, pt.Y - 2, 4, 4);
                }
            }

            // 绘制终点
            if (pointsCopy.Count > 0)
            {
                var lastP = pointsCopy[pointsCopy.Count - 1];
                var endPoint = ToScreen(lastP.AccumX, lastP.AccumY);
                g.FillEllipse(Brushes.Red, endPoint.X - 6, endPoint.Y - 6, 12, 12);
            }

            // 绘制坐标信息
            g.DrawString($"起点(0,0)", this.Font, Brushes.Green, startPoint.X + 8, startPoint.Y - 8);
            if (pointsCopy.Count > 0)
            {
                var lastP = pointsCopy[pointsCopy.Count - 1];
                var endPoint = ToScreen(lastP.AccumX, lastP.AccumY);
                g.DrawString($"终点({lastP.AccumX},{lastP.AccumY})", this.Font, Brushes.Red, endPoint.X + 8, endPoint.Y - 8);
            }
        }
        #endregion
    }
}
