using MWModle;
using gprs.KmBox;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace gprs
{
    public partial class Form1 : Form
    {
        #region 字段定义

        // === 调试模式 ===
        private bool _debugMode = false;

        // === FOV转换开关 ===
        private bool _useFovConvert = false;

        // === 灵敏度配置 ===
        private int _xSensitivity;
        private int _ySensitivity;

        // === 统计计数 ===
        private int _frameCount;
        private int _mouseActionCount;

        // === 图像处理 ===
        private Bitmap _captureBitmap = new(GameConfig.CaptureWidth, GameConfig.CaptureHeight, PixelFormat.Format24bppRgb);
        private int _frameState = 0;  // 0=可写入, 1=待处理
        private readonly AutoResetEvent _frameEvent = new(false);

        // === 硬件设备 ===
        private readonly MWCaptureWrapperPro _mwCapture = new();
        private KmBoxNet? _kmBox;

        #endregion

        #region 初始化

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            radioButton1.Checked = true;
            InitializeMWCapture();
            StartYoloThread();
            StartStatsThread();
        }

        #endregion

        #region 美乐威采集

        private void InitializeMWCapture()
        {
            MWCaptureWrapperPro.Init();
            MWCaptureWrapperPro.RefreshDevices();
            _mwCapture.set_mw_fourcc(MWFOURCC.MWFOURCC_BGR24);
            _mwCapture.set_resolution(GameConfig.CaptureWidth, GameConfig.CaptureHeight);

            int deviceCount = MWCaptureWrapperPro.GetChannelCount();
            if (deviceCount == 0)
            {
                Debug.WriteLine("未发现MWCapture Pro设备");
                return;
            }

            // 记录设备信息
            for (int i = 0; i < deviceCount; i++)
            {
                var info = new LibMWCapture.MWCAP_CHANNEL_INFO();
                MWCaptureWrapperPro.GetChannelInfobyIndex(i, ref info);
                Debug.WriteLine($"MWCapture设备 {i}: {info.byBoardIndex}:{info.byChannelIndex} {info.szProductName}");
            }

            _mwCapture.SetFrameCallback(OnFrameCaptured);

            if (_mwCapture.set_device(0) && _mwCapture.start_capture(true, false))
                Debug.WriteLine("MWCapture Pro初始化成功");
            else
                Debug.WriteLine("MWCapture Pro启动失败");
        }

        private void OnFrameCaptured(CRingBuffer.st_frame_t frame, int width, int height)
        {
            if (Interlocked.CompareExchange(ref _frameState, 1, 0) != 0) return;
            _mwCapture.ConvertFrameToBitmapRGB24(frame, ref _captureBitmap);
            _frameEvent.Set();
        }

        #endregion

        #region YOLO处理线程

        private void StartYoloThread()
        {
            Task.Run(() =>
            {
                try
                {
                    var predictor = new TrtYoloPoseInferencer("./Models/yolov8l-pose.engine", GameConfig.CaptureWidth, GameConfig.CaptureHeight);
                    var graphics = Graphics.FromImage(_captureBitmap);

                    while (true)
                    {
                        _frameEvent.WaitOne();
                        ProcessYoloFrame(predictor, graphics);
                        UpdateDebugDisplay();
                        _frameCount++;
                        Interlocked.Exchange(ref _frameState, 0);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            });
        }

        private void ProcessYoloFrame(TrtYoloPoseInferencer predictor, Graphics graphics)
        {
            // 1. 准心检测
            var crosshair = ImageHelper.ReadGameCrosshairInfo(_captureBitmap, checkSteady: false);

            // 2. 确定射击模式和锁定位置
            int fireMode = (int)GameConfig.FireMode.Sniper;
            bool lockHead = false;  // false=锁身体, true=锁头
            if (!_debugMode)
            {
                if (_kmBox == null || !_kmBox.IsConnected)
                    return;

                if (crosshair.SnipeEnabled)  // 红点优先
                {
                    fireMode = (int)GameConfig.FireMode.Sniper;
                    lockHead = false;  // 狙击锁身体
                    _kmBox.Trace(0, 0);  // 狙击模式：关闭曲线
                }
                else if (_kmBox.IsMouseLeftDown())  // 左键次优先
                {
                    fireMode = (int)GameConfig.FireMode.Rifle;
                    lockHead = true;  // 步枪锁头
                    _kmBox.Trace(GameConfig.RifleConfig.TraceType, GameConfig.RifleConfig.TraceSmooth);  // 步枪模式：开启贝塞尔曲线
                    graphics.FillRectangle(GameConfig.MaskBrush, GameConfig.BurstMaskRect);
                }
                else if (_kmBox.IsMouseSide2Down())  // 侧键点射
                {
                    fireMode = (int)GameConfig.FireMode.Burst;
                    lockHead = true;  // 点射锁头
                    _kmBox.Trace(0, 0);
                    graphics.FillRectangle(GameConfig.MaskBrush, GameConfig.BurstMaskRect);
                }
                else
                {
                    return;
                }
            }


            // 3. YOLO识别
            var result = ImageHelper.ProcessYoloDetection(_captureBitmap, predictor);

            // 4. 目标选择
            var lockResult = TargetSelector.ProcessTargets(result, lockHead, GameConfig.CaptureWidth, GameConfig.CaptureHeight, _debugMode);

            // 5. 调试绘制
            if (_debugMode && result.Count > 0)
            {
                DebugRenderer.DrawDebugInfo(_captureBitmap, result, lockResult);
                return;
            }

            // 6. 执行射击
            if (lockResult.HasTarget)
                ExecuteFireAction(fireMode, lockResult.TargetX, lockResult.TargetY);
        }

        private void ExecuteFireAction(int fireMode, int targetX, int targetY)
        {
            int mouseX, mouseY;

            if (_useFovConvert)
            {
                // FOV非线性转换
                (mouseX, mouseY) = CalculateFovMove(targetX, targetY);
            }
            else
            {
                // 线性计算（旧方式）
                mouseX = (targetX - GameConfig.CaptureWidth / 2) * _xSensitivity / 100;
                mouseY = (targetY - GameConfig.CaptureHeight / 2) * _ySensitivity / 100;
            }

            if (fireMode == (int)GameConfig.FireMode.Sniper)
            {
                _kmBox!.MouseMove(mouseX, mouseY);
                _kmBox.MouseLeft(true);
                _kmBox.MouseLeft(false);
                Thread.Sleep(35);
                _kmBox.MouseWheel(-1);
                _mouseActionCount++;
                Thread.Sleep(200);
            }
            else if (fireMode == (int)GameConfig.FireMode.Rifle)
            {
                // 步枪模式：贝塞尔曲线移动，不开枪
                _kmBox!.MouseMove(mouseX, mouseY);
                _mouseActionCount++;
                // 延时公式：delay = 基准延时 * (实际距离 / 基准距离)
                double distance = Math.Sqrt(mouseX * mouseX + mouseY * mouseY);
                int delay = (int)(GameConfig.RifleConfig.BaseDelayMs * distance / GameConfig.RifleConfig.BaseDistance);
                delay = Math.Max(delay, 10);  // 最小 10ms
                Thread.Sleep(delay);
            }
            else if (fireMode == (int)GameConfig.FireMode.Burst)
            {
                _kmBox!.MouseMove(mouseX, mouseY);
                _kmBox.MouseLeft(true);
                _kmBox.MouseLeft(false);
                _mouseActionCount++;
                Thread.Sleep(150);
            }
        }

        /// <summary>
        /// FOV非线性转换：使用 atan2 将像素偏移转换为鼠标移动量
        /// 
        /// 算法原理：
        /// 1. 计算目标相对于屏幕中心的像素偏移 (dx, dy)
        /// 2. 使用 atan2 产生非线性映射：
        ///    - 目标接近中心时 -> atan2 线性近似 -> 精细移动
        ///    - 目标远离中心时 -> atan2 趋于饱和 -> 快速移动
        /// 3. 垂直轴考虑了水平偏移的影响（球面修正）
        /// 
        /// 效果：让瞄准在接近目标时更稳定，远离目标时更快追踪
        /// </summary>
        private (int mx, int my) CalculateFovMove(int targetX, int targetY)
        {
            // === Step 1: 计算目标相对于屏幕中心的像素偏移 ===
            // 屏幕中心是 (320, 320)，目标在右边则 dx > 0，在下边则 dy > 0
            float dx = targetX - GameConfig.CaptureWidth / 2f;   // 水平偏移（像素）
            float dy = targetY - GameConfig.CaptureHeight / 2f;  // 垂直偏移（像素）

            // === Step 2: 计算角度转换因子 R ===
            // R = Sensitivity / (2 * PI)
            // R 越大 -> 同样像素偏移产生的鼠标移动越小（灵敏度越低）
            // R 越小 -> 同样像素偏移产生的鼠标移动越大（灵敏度越高）
            // 当前 Sensitivity = 5140，R ≈ 818
            float R = GameConfig.FovConfig.Sensitivity / 2f / MathF.PI;

            // === Step 3: 水平轴鼠标移动量 ===
            // atan2(dx, R) 返回弧度，范围 (-PI, PI)
            // 乘以 R 转换为鼠标像素
            // 当 dx 很小时：atan2(dx, R) ≈ dx/R，所以 mx ≈ dx（线性）
            // 当 dx 很大时：atan2 趋于 PI/2，mx 趋于 R*PI/2 ≈ 1286（饱和）
            float mx = MathF.Atan2(dx, R) * R;

            // === Step 4: 垂直轴鼠标移动量（带球面修正） ===
            // 垂直轴使用 sqrt(dx² + R²) 作为基准，而不是简单的 R
            // 这样当水平偏移大时，垂直移动会略微减少（模拟球面投影）
            // 避免“对角线方向移动过快”的问题
            float my = MathF.Atan2(dy, MathF.Sqrt(dx * dx + R * R)) * R;

            // === Step 5: 返回整数鼠标移动量 ===
            return ((int)mx, (int)my);
        }

        private void UpdateDebugDisplay()
        {
            if (!_debugMode) return;

            var cloned = (Bitmap)_captureBitmap.Clone();
            this.Invoke((MethodInvoker)delegate
            {
                pictureBox1.Image?.Dispose();
                pictureBox1.Image = cloned;
            });
        }

        #endregion

        #region 统计线程

        private void StartStatsThread()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(1000);
                    UpdateSensitivity();
                    UpdateStatsDisplay();
                }
            });
        }

        private void UpdateSensitivity()
        {
            if (radioButton1.Checked)
            {
                _xSensitivity = GameConfig.Sensitivity.CF_2K_X;
                _ySensitivity = GameConfig.Sensitivity.CF_2K_Y;
            }
            else if (radioButton2.Checked)
            {
                _xSensitivity = GameConfig.Sensitivity.GAME2_X;
                _ySensitivity = GameConfig.Sensitivity.GAME2_Y;
            }
            else if (radioButton3.Checked)
            {
                _xSensitivity = GameConfig.Sensitivity.GAME3_X;
                _ySensitivity = GameConfig.Sensitivity.GAME3_Y;
            }
        }

        private void UpdateStatsDisplay()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                textBox2.Text = $"{_frameCount} {_mouseActionCount}";
                _frameCount = 0;
                _mouseActionCount = 0;
            });
        }

        #endregion

        #region KMBOX连接

        private void btnKmBoxConnect_Click(object sender, EventArgs e)
        {
            try
            {
                if (_kmBox != null && _kmBox.IsConnected)
                    DisconnectKmBox();
                else
                    ConnectKmBox();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"KMBOX 连接失败: {ex.Message}");
                lblKmBoxStatus.Text = "连接失败";
                lblKmBoxStatus.ForeColor = Color.Red;
            }
        }

        private void ConnectKmBox()
        {
            string ip = txtKmBoxIP.Text.Trim();
            int port = int.Parse(txtKmBoxPort.Text.Trim());
            string uuid = txtKmBoxUUID.Text.Trim();

            _kmBox = new KmBoxNet();
            if (_kmBox.Connect(ip, port, uuid))
            {
                _kmBox.HwMouseButtonChanged += OnKmBoxMouseButtonChanged;
                _kmBox.HwKeyDown += OnKmBoxKeyDown;
                _kmBox.MonitorEnable(9527);

                btnKmBoxConnect.Text = "断开";
                lblKmBoxStatus.Text = "已连接";
                lblKmBoxStatus.ForeColor = Color.Green;
            }
            else
            {
                _kmBox.Dispose();
                _kmBox = null;
                lblKmBoxStatus.Text = "连接失败";
                lblKmBoxStatus.ForeColor = Color.Red;
            }
        }

        private void DisconnectKmBox()
        {
            _kmBox!.MonitorDisable();
            _kmBox.Disconnect();
            _kmBox.Dispose();
            _kmBox = null;

            btnKmBoxConnect.Text = "连接";
            lblKmBoxStatus.Text = "未连接";
            lblKmBoxStatus.ForeColor = Color.Gray;
        }

        #endregion

        #region KMBOX硬件事件

        private void OnKmBoxMouseButtonChanged(int button, bool isDown)
        {
            // 预留鼠标按钮事件处理
        }

        private void OnKmBoxKeyDown(byte hidKey)
        {
            // 预留键盘事件处理
        }

        #endregion

        #region UI事件

        private void chkDebugMode_CheckedChanged(object sender, EventArgs e)
        {
            _debugMode = chkDebugMode.Checked;
        }

        private void chkFovConvert_CheckedChanged(object sender, EventArgs e)
        {
            _useFovConvert = chkFovConvert.Checked;
        }

        #endregion
    }
}
