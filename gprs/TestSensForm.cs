using MWModle;
using gprs.KmBox;
using gprs.Firing;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace gprs
{
    /// <summary>
    /// 灵敏度测试窗体
    /// 按侧键2 → 抓一帧跑 YOLO → 严格锁头移动一次（不开枪），CD 1s 防游戏检测。
    /// 用于标定灵敏度：移动过头 = 灵敏度偏高，移动不足 = 灵敏度偏低。
    ///
    /// 与主模块（Form1）的差异：
    /// - 无准心/开镜/武器模式判定：按侧键2即触发，不限制游戏状态
    /// - 严格只瞄头：TargetSelector 锁头优先级回退到非头部部位时放弃本次移动
    /// - 绝不发左键：只做 XY 屏蔽 + MoveInSegments 移动，落点由用户肉眼/手动开枪验证
    /// - 灵敏度 UI 可调（默认 GameConfig CF 2K 实测值），方便逐步逼近游戏内真实灵敏度
    /// - 无位移上限：大位移由 MoveInSegments 自动分成多次 ≤120px 段完成
    /// - 模型固定选 L（高精度优先：测试目标静止、不追求速度）
    /// </summary>
    public partial class TestSensForm : Form
    {
        // === 硬件 ===
        private KmBoxNet _kmBox;
        private readonly MWCaptureWrapperPro _mwCapture = new();
        private Bitmap _captureBitmap = new(GameConfig.CaptureWidth, GameConfig.CaptureHeight, PixelFormat.Format24bppRgb);
        private int _frameState = 0;
        private readonly AutoResetEvent _frameEvent = new(false);

        // === YOLO ===
        private const string MODELS_DIR = "./Models";
        private TrtYoloPoseInferencer _predictor;

        // === 触发控制 ===
        // 侧键2（0x10）按下边沿置位，主循环下一帧消费；Interlocked 保证跨线程一次性
        private int _triggerPending = 0;
        private volatile bool _testEnabled;
        private long _lastAimTs = long.MinValue;   // 上次执行瞄准的时间戳（CD 基准）
        private const int AIM_COOLDOWN_MS = 1000;  // CD 1s：防高频移动被游戏检测
        private int _aimCount;

        // === 灵敏度（UI 可调，默认 GameConfig CF 2K）===
        // UI 线程 TextChanged 写入，主循环线程读取；int 单字读写天然原子，无需加锁
        private int _xSensitivity = GameConfig.Sensitivity.CF_2K_X;
        private int _ySensitivity = GameConfig.Sensitivity.CF_2K_Y;

        // === 严格头部部位集合（TargetSelector 锁头优先级中属于"头"的部位）===
        private static bool IsHeadPart(int part) => part == PartIndex.FOREHEAD
                                                 || part == PartIndex.NOSE
                                                 || part == PartIndex.LEFT_EYE
                                                 || part == PartIndex.RIGHT_EYE
                                                 || part == PartIndex.LEFT_EAR
                                                 || part == PartIndex.RIGHT_EAR;

        private readonly Random _random = new();
        private readonly Stopwatch _sw = Stopwatch.StartNew();
        private CancellationTokenSource _cts;

        public TestSensForm()
        {
            InitializeComponent();
        }

        #region 连接/断开

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (_kmBox != null && _kmBox.IsConnected)
            {
                DisconnectAll();
                return;
            }

            try
            {
                string ip = txtIP.Text.Trim();
                int port = int.Parse(txtPort.Text.Trim());
                string uuid = txtUUID.Text.Trim();

                _kmBox = new KmBoxNet();
                if (!_kmBox.Connect(ip, port, uuid))
                {
                    _kmBox.Dispose();
                    _kmBox = null;
                    Log("KmBox 连接失败");
                    lblStatus.Text = "连接失败";
                    lblStatus.ForeColor = Color.Red;
                    return;
                }

                _kmBox.MonitorEnable(9527);
                _kmBox.UnmaskAll();
                _kmBox.Trace(0, 0);
                _kmBox.HwMouseButtonChanged += OnHwMouseButtonChanged;

                // 侧键2 全程屏蔽：作本窗体触发键，不透传游戏
                // （主模块侧键1 是"步枪打头"热键，本窗体不屏蔽，保持游戏内原功能）
                _kmBox.MaskMouseSide2(true);

                InitializeCapture();
                StartLoop();

                btnConnect.Text = "断开";
                lblStatus.Text = "已连接";
                lblStatus.ForeColor = Color.Green;
                Log($"KmBox 连接成功 {ip}:{port}，侧键2 已屏蔽");
            }
            catch (Exception ex)
            {
                Log($"连接异常: {ex.Message}");
                lblStatus.Text = "异常";
                lblStatus.ForeColor = Color.Red;
            }
        }

        private void InitializeCapture()
        {
            MWCaptureWrapperPro.Init();
            MWCaptureWrapperPro.RefreshDevices();
            _mwCapture.set_mw_fourcc(MWFOURCC.MWFOURCC_BGR24);
            _mwCapture.set_resolution(GameConfig.CaptureWidth, GameConfig.CaptureHeight);

            int deviceCount = MWCaptureWrapperPro.GetChannelCount();
            if (deviceCount == 0)
            {
                Log("警告: 未发现 MWCapture 设备，无法采帧瞄准");
                return;
            }

            _mwCapture.SetFrameCallback(OnFrameCaptured);
            if (_mwCapture.set_device(0) && _mwCapture.start_capture(true, false))
                Log("采集卡启动成功");
            else
                Log("采集卡启动失败");
        }

        private void OnFrameCaptured(CRingBuffer.st_frame_t frame, int width, int height)
        {
            if (Interlocked.CompareExchange(ref _frameState, 1, 0) != 0) return;
            _mwCapture.ConvertFrameToBitmapRGB24(frame, ref _captureBitmap);
            _frameEvent.Set();
        }

        /// <summary>
        /// 后台加载 TRT engine：扫描 Models 目录，优先 l-pose（高精度优先：测试目标静止、
        /// 不追求速度），无匹配取第一个；加载失败仅记日志，按侧键2时会提示"模型未加载"。
        /// </summary>
        private void LoadModel()
        {
            var engineFiles = Directory.Exists(MODELS_DIR)
                ? Directory.GetFiles(MODELS_DIR, "*-pose.engine")
                    .OrderBy(f => f)
                    .ToArray()
                : Array.Empty<string>();

            if (engineFiles.Length == 0)
            {
                Log("错误: Models 目录无 *-pose.engine 模型文件");
                return;
            }

            string path = engineFiles.FirstOrDefault(f => f.Contains("l-pose", StringComparison.OrdinalIgnoreCase))
                          ?? engineFiles[0];
            _predictor = new TrtYoloPoseInferencer(path, GameConfig.CaptureWidth, GameConfig.CaptureHeight);
            Log($"模型已加载: {Path.GetFileName(path)}");
        }

        private void DisconnectAll()
        {
            _testEnabled = false;
            if (chkEnable.Checked) chkEnable.Checked = false;

            _cts?.Cancel();
            _frameEvent.Set();

            try { _mwCapture.Dispose(); } catch { }
            try { _predictor?.Dispose(); } catch { }
            _predictor = null;

            if (_kmBox != null)
            {
                try
                {
                    _kmBox.HwMouseButtonChanged -= OnHwMouseButtonChanged;
                    _kmBox.UnmaskAll();   // 一次性兜底解除所有屏蔽位（含侧键2、XY）
                    _kmBox.MonitorDisable();
                    _kmBox.Disconnect();
                    _kmBox.Dispose();
                }
                catch { }
                _kmBox = null;
            }

            _triggerPending = 0;

            btnConnect.Text = "连接";
            lblStatus.Text = "未连接";
            lblStatus.ForeColor = Color.Gray;
            Log("已断开");
        }

        #endregion

        #region 主循环 + 瞄准执行

        private void StartLoop()
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            Task.Run(() => Loop(token), token);
        }

        private void Loop(CancellationToken token)
        {
            LoadModel();

            while (!token.IsCancellationRequested)
            {
                if (!_frameEvent.WaitOne(100)) continue;
                if (token.IsCancellationRequested) break;
                if (_kmBox == null || !_kmBox.IsConnected)
                {
                    Interlocked.Exchange(ref _frameState, 0);
                    continue;
                }

                try
                {
                    // 消费侧键2触发请求：每帧最多执行一次瞄准（CD 在硬件事件里拦截）
                    if (Interlocked.Exchange(ref _triggerPending, 0) != 0 && _testEnabled)
                        ExecuteAimOnce();

                    UpdateStateLabel();
                }
                catch (Exception ex)
                {
                    Log($"循环异常: {ex.Message}");
                }
                finally
                {
                    Interlocked.Exchange(ref _frameState, 0);
                }
            }
        }

        /// <summary>
        /// 执行一次灵敏度测试瞄准：YOLO 推理 → 严格锁头 → XY 屏蔽下分段移动。
        /// 绝不发左键；任一步不满足即放弃并记日志。
        /// </summary>
        private void ExecuteAimOnce()
        {
            if (_predictor == null)
            {
                Log("✗ 模型未加载，跳过");
                return;
            }

            var result = ImageHelper.ProcessYoloDetection(_captureBitmap, _predictor);
            var lockResult = TargetSelector.ProcessTargets(result, lockHead: true,
                GameConfig.CaptureWidth, GameConfig.CaptureHeight);

            if (!lockResult.HasTarget)
            {
                Log("✗ 未识别到目标，跳过");
                return;
            }

            // 严格只瞄头：锁头优先级回退到肩/胸等非头部位时放弃，保证每次移动都正对头部
            if (!IsHeadPart(lockResult.SelectedPart))
            {
                Log($"✗ 头部关键点不可见（回退至 {TargetSelector.GetPartName(lockResult.SelectedPart)}），放弃本次移动");
                return;
            }

            // 灵敏度换算与主模块 SniperFire 完全一致：像素差 × 灵敏度 / 100
            int mouseX = (lockResult.TargetX - GameConfig.CaptureWidth / 2) * _xSensitivity / 100;
            int mouseY = (lockResult.TargetY - GameConfig.CaptureHeight / 2) * _ySensitivity / 100;

            // 不设位移上限：大位移由 MoveInSegments 自动分成多次 ≤120px 段完成

            _aimCount++;
            Log($"#{_aimCount} 锁定 {TargetSelector.GetPartName(lockResult.SelectedPart)} " +
                $"像素差({lockResult.TargetX - GameConfig.CaptureWidth / 2:+#;-#;0}, " +
                $"{lockResult.TargetY - GameConfig.CaptureHeight / 2:+#;-#;0}) " +
                $"→ 移动({mouseX:+#;-#;0}, {mouseY:+#;-#;0})");

            // XY 细粒度屏蔽：只覆盖移动期间，防手抖污染自瞄向量（移动后立即解除，不碰左右键）
            _kmBox.MaskMouseX(true);
            _kmBox.MaskMouseY(true);
            FireActions.MoveInSegments(_kmBox, mouseX, mouseY, _random);
            _kmBox.MaskMouseX(false);
            _kmBox.MaskMouseY(false);

            _lastAimTs = Stopwatch.GetTimestamp();
        }

        /// <summary>
        /// KmBox 硬件按键回调：侧键2按下边沿 → 提交瞄准请求（CD 内拦截并提示）。
        /// CD 判断放在回调里：冷却期连按能即时得到"被忽略"反馈。
        /// </summary>
        private void OnHwMouseButtonChanged(int button, bool isDown)
        {
            if (button == 0x10 && isDown)
            {
                if (!_testEnabled)
                {
                    Log("[侧键2] 按下（测试未启用，忽略）");
                    return;
                }

                long elapsedMs = _lastAimTs == long.MinValue
                    ? long.MaxValue
                    : (Stopwatch.GetTimestamp() - _lastAimTs) * 1000 / Stopwatch.Frequency;
                if (elapsedMs < AIM_COOLDOWN_MS)
                {
                    Log($"[侧键2] CD 中（剩余 {AIM_COOLDOWN_MS - elapsedMs}ms），忽略");
                    return;
                }

                Interlocked.Exchange(ref _triggerPending, 1);
                Log("[侧键2] 按下 → 下一帧执行瞄准");
            }
        }

        #endregion

        #region UI 事件

        private void chkEnable_CheckedChanged(object sender, EventArgs e)
        {
            _testEnabled = chkEnable.Checked;
            if (!_testEnabled)
                Interlocked.Exchange(ref _triggerPending, 0);   // 清掉未消费的触发请求
            Log(_testEnabled
                ? $"=== 测试已启用：侧键2 触发锁头移动，CD {AIM_COOLDOWN_MS}ms ==="
                : "=== 测试已禁用 ===");
        }

        private void btnEmergencyStop_Click(object sender, EventArgs e)
        {
            _testEnabled = false;
            if (chkEnable.Checked) chkEnable.Checked = false;
            if (_kmBox != null && _kmBox.IsConnected)
            {
                _kmBox.UnmaskAll();               // 解除 XY 等所有屏蔽
                _kmBox.MaskMouseSide2(true);      // 侧键2 维持屏蔽（触发键不透传游戏）
                Log("=== 紧急停止：解除所有屏蔽（保留侧键2屏蔽）===");
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtLog.Clear();
            _aimCount = 0;
        }

        private void txtXSens_TextChanged(object sender, EventArgs e)
        {
            // 非法输入（空/负数/非数字）保留上一次有效值，与主模块 txtMicroAimExtend 同款处理
            if (int.TryParse(txtXSens.Text.Trim(), out int v) && v > 0)
                _xSensitivity = v;
        }

        private void txtYSens_TextChanged(object sender, EventArgs e)
        {
            if (int.TryParse(txtYSens.Text.Trim(), out int v) && v > 0)
                _ySensitivity = v;
        }

        #endregion

        #region 日志 + 状态栏

        private void Log(string message)
        {
            double ms = _sw.Elapsed.TotalMilliseconds;
            string line = $"[{ms:F1}ms] {message}";
            if (txtLog.IsDisposed) return;
            if (txtLog.InvokeRequired)
                txtLog.BeginInvoke(new Action(() => AppendLog(line)));
            else
                AppendLog(line);
        }

        private void AppendLog(string line)
        {
            if (txtLog.IsDisposed) return;
            txtLog.AppendText(line + Environment.NewLine);
        }

        private void UpdateStateLabel()
        {
            if (lblState.IsDisposed) return;

            long elapsedMs = _lastAimTs == long.MinValue
                ? long.MaxValue
                : (Stopwatch.GetTimestamp() - _lastAimTs) * 1000 / Stopwatch.Frequency;
            string cdText = elapsedMs >= AIM_COOLDOWN_MS ? "就绪" : $"冷却中({AIM_COOLDOWN_MS - elapsedMs}ms)";

            string text = $"测试: {(_testEnabled ? "启用" : "禁用")}  |  " +
                          $"CD: {cdText}  |  " +
                          $"瞄准次数: {_aimCount}  |  " +
                          $"灵敏度: X={_xSensitivity} Y={_ySensitivity}";
            if (lblState.InvokeRequired)
                lblState.BeginInvoke(new Action(() => lblState.Text = text));
            else
                lblState.Text = text;
        }

        #endregion

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            DisconnectAll();
            base.OnFormClosing(e);
        }
    }
}
