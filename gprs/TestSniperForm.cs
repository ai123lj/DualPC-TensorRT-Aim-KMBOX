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
    /// <summary>
    /// 狙击反作弊触发测试窗体
    /// 复用瞬狙状态机骨架，但将"锁到目标头部"替换为"随机大幅偏移 + 开火"
    /// 不跑 YOLO，不选目标。用于验证是否"瞬移+即射"模板本身触发 CF 异常。
    /// </summary>
    public partial class TestSniperForm : Form
    {
        // === 硬件 ===
        private KmBoxNet _kmBox;
        private readonly MWCaptureWrapperPro _mwCapture = new();
        private Bitmap _captureBitmap = new(GameConfig.CaptureWidth, GameConfig.CaptureHeight, PixelFormat.Format24bppRgb);
        private int _frameState = 0;
        private readonly AutoResetEvent _frameEvent = new(false);

        // === 状态机 ===
        private enum State { Idle, Monitoring, PassThrough }
        private State _state = State.Idle;
        private long _monitorStartTs;
        private volatile bool _leftMasked;
        private volatile bool _testEnabled;

        // === 右键预屏蔽（复制现版瞬狙逻辑）===
        // 用户按下右键 → 立即屏蔽左键，覆盖开镜到准心出现的时间间隙
        private const int PRE_MASK_TIMEOUT_MS = 200;
        private volatile bool _preMasked;
        private long _preMaskTimestamp;
        private volatile bool _leftClickDetected;

        // === 杂项 ===
        private readonly Random _random = new();
        private readonly Stopwatch _sw = Stopwatch.StartNew();
        private CancellationTokenSource _cts;
        private int _triggerCount;

        public TestSniperForm()
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

                InitializeCapture();
                StartLoop();

                btnConnect.Text = "断开";
                lblStatus.Text = "已连接";
                lblStatus.ForeColor = Color.Green;
                Log($"KmBox 连接成功 {ip}:{port}");
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
                Log("警告: 未发现 MWCapture 设备，准心检测不可用");
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

        private void DisconnectAll()
        {
            _testEnabled = false;
            if (chkEnable.Checked) chkEnable.Checked = false;

            _cts?.Cancel();
            _frameEvent.Set();

            try { _mwCapture.Dispose(); } catch { }

            if (_kmBox != null)
            {
                try
                {
                    _kmBox.HwMouseButtonChanged -= OnHwMouseButtonChanged;
                    _kmBox.MouseLeft(false);
                    _kmBox.UnmaskAll();
                    _kmBox.MonitorDisable();
                    _kmBox.Disconnect();
                    _kmBox.Dispose();
                }
                catch { }
                _kmBox = null;
            }

            _leftMasked = false;
            _preMasked = false;
            _leftClickDetected = false;
            _state = State.Idle;

            btnConnect.Text = "连接";
            lblStatus.Text = "未连接";
            lblStatus.ForeColor = Color.Gray;
            Log("已断开");
        }

        #endregion

        #region 主循环 + 状态机

        private void StartLoop()
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            Task.Run(() => Loop(token), token);
        }

        private void Loop(CancellationToken token)
        {
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
                    var crosshair = ImageHelper.ReadGameCrosshairInfo(_captureBitmap, checkSteady: false);
                    ProcessStateMachine(crosshair);
                    UpdateStateLabel(crosshair);
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

        private void ProcessStateMachine(ImageHelper.CrosshairInfo crosshair)
        {
            // 测试未启用 → 清除所有屏蔽
            if (!_testEnabled)
            {
                if (_leftMasked || _preMasked)
                {
                    _kmBox.MaskMouseLeft(false);
                    _leftMasked = false;
                    _preMasked = false;
                }
                _leftClickDetected = false;
                _state = State.Idle;
                return;
            }

            // 准心未激活
            if (!crosshair.SnipeEnabled)
            {
                // 预屏蔽超时检查
                if (_preMasked)
                {
                    long pmElapsed = (Stopwatch.GetTimestamp() - _preMaskTimestamp) * 1000 / Stopwatch.Frequency;
                    if (pmElapsed >= PRE_MASK_TIMEOUT_MS)
                    {
                        bool hwNow = _kmBox.IsMouseLeftDown();
                        _kmBox.MaskMouseLeft(false);
                        _preMasked = false;
                        _leftClickDetected = false;
                        Log($"[预屏蔽] 超时 {pmElapsed}ms 解除 | 硬件左键={(hwNow ? "按下⚠" : "抬起")}");
                    }
                }
                else if (_state != State.Idle)
                {
                    // Monitoring 中但准心消失（关镜）→ 重置
                    if (_leftMasked)
                    {
                        _kmBox.MaskMouseLeft(false);
                        _leftMasked = false;
                    }
                    _state = State.Idle;
                }
                return;
            }

            // 准心激活
            switch (_state)
            {
                case State.Idle:
                    // 平滑过渡：如果预屏蔽命中 → 继承屏蔽状态（无缝切换到 Monitoring）
                    if (_preMasked)
                    {
                        _preMasked = false;
                        _leftMasked = true;
                        Log("[预屏蔽] 命中 → 进入 Monitoring");
                    }
                    _monitorStartTs = Stopwatch.GetTimestamp();
                    _state = State.Monitoring;
                    break;

                case State.Monitoring:
                    // 100 = 旧版监控窗口默认值（原 GameConfig.DefaultQuickScopeWindowMs，主模块重构后已删除；
                    // 本窗体为 ISSUE-014 隔离诊断工具，刻意复刻旧版 Monitoring 行为）
                    int windowMs = ParseInt(txtWindowMs.Text, 100);
                    long elapsedMs = (Stopwatch.GetTimestamp() - _monitorStartTs) * 1000 / Stopwatch.Frequency;

                    // 窗口超时 → 放行
                    if (elapsedMs >= windowMs)
                    {
                        if (_leftMasked)
                        {
                            bool hwNow = _kmBox.IsMouseLeftDown();
                            _kmBox.MaskMouseLeft(false);
                            _leftMasked = false;
                            Log($"[Monitoring] 窗口超时 {elapsedMs}ms 解除屏蔽 | 硬件左键={(hwNow ? "按下⚠" : "抬起")}");
                        }
                        _state = State.PassThrough;
                        return;
                    }

                    // 保证左键屏蔽
                    if (!_leftMasked)
                    {
                        _kmBox.MaskMouseLeft(true);
                        _leftMasked = true;
                    }

                    // 左键按下（预屏蔽期间已记录 或 当前仍按下）→ 触发
                    if (_leftClickDetected || _kmBox.IsMouseLeftDown())
                    {
                        TriggerSniperShot();
                        _state = State.PassThrough;
                        _leftClickDetected = false;
                    }
                    break;

                case State.PassThrough:
                    // 等准心消失后重置为 Idle
                    break;
            }
        }

        private void TriggerSniperShot()
        {
            // 随机方向（水平 ±30° 扇形，随机左右）+ 随机距离
            int minDist = ParseInt(txtMinDist.Text, 80);
            int maxDist = ParseInt(txtMaxDist.Text, 180);
            if (maxDist < minDist) maxDist = minDist;

            double angleRad = (_random.NextDouble() * 60 - 30) * Math.PI / 180.0;
            int distance = _random.Next(minDist, maxDist + 1);
            int sign = _random.Next(2) == 0 ? 1 : -1;
            int moveX = sign * (int)(distance * Math.Cos(angleRad));
            int moveY = (int)(distance * Math.Sin(angleRad));

            bool useMaskAll = chkMaskAll.Checked;
            int triggerId = _triggerCount + 1;

            // 快照①：执行前硬件左键状态
            bool hwBefore = _kmBox.IsMouseLeftDown();
            Log($"① 触发 #{triggerId} 开始 | 硬件左键={(hwBefore ? "按下" : "抬起")} | 位移({moveX:+#;-#;0}, {moveY:+#;-#;0})");

            // 复用 ExecuteFireAction 的 Sniper 模板
            if (useMaskAll) _kmBox.MaskAll();
            _kmBox.MouseMove(moveX, moveY);
            _kmBox.MouseLeft(true);
            Log($"② #{triggerId} 软件左键按下");
            Thread.Sleep(_random.Next(30, 51));
            Thread.Sleep(_random.Next(50, 100));
            _kmBox.MouseLeft(false);

            // 快照③：UnmaskAll 前硬件左键状态← 最关键的观察点
            bool hwAtUnmask = _kmBox.IsMouseLeftDown();
            Log($"③ #{triggerId} 软件左键抬起完成 | UnmaskAll 前 硬件左键={(hwAtUnmask ? "按下" : "抬起")}");
            if (hwAtUnmask)
                Log($"   ⚠⚠ #{triggerId} 异常风险：UnmaskAll 时硬件仍按下 → 游戏将看到凭空的“按下+抬起”");

            if (useMaskAll) _kmBox.UnmaskAll();

            // MaskAll+UnmaskAll 已覆盖左键屏蔽状态
            _leftMasked = false;
            _preMasked = false;
            _leftClickDetected = false;
            _triggerCount++;
            Log($"④ #{triggerId} UnmaskAll 完成 | MaskAll={useMaskAll}");
        }

        /// <summary>
        /// KmBox 硬件键鼠事件回调（右键按下立即屏蔽左键，预屏蔽期间记录左键按下）
        /// </summary>
        private void OnHwMouseButtonChanged(int button, bool isDown)
        {
            // 记录所有硬件左右键事件（用于计算按键时长、识别凭空抬起）
            if (button == 0x01 || button == 0x02)
            {
                string btnName = button == 0x01 ? "左键" : "右键";
                string action = isDown ? "按下" : "抬起";
                Log($"[HW] {btnName} {action}");
            }

            if (!_testEnabled) return;
            if (_kmBox == null || !_kmBox.IsConnected) return;

            // 右键按下 → 预屏蔽左键（覆盖开镜到准心出现的间隙）
            if (button == 0x02 && isDown && _state == State.Idle && !_preMasked)
            {
                _kmBox.MaskMouseLeft(true);
                _preMasked = true;
                _preMaskTimestamp = Stopwatch.GetTimestamp();
                _leftClickDetected = false;
                Log("[预屏蔽] 右键按下 → MaskMouseLeft(true)");
            }

            // 预屏蔽期间左键按下 → 记录，准心出现后立即触发
            if (button == 0x01 && isDown && _preMasked)
            {
                _leftClickDetected = true;
                Log("[预屏蔽] 期间检测到左键按下 → 待触发");
            }
        }

        #endregion

        #region UI 事件

        private void chkEnable_CheckedChanged(object sender, EventArgs e)
        {
            _testEnabled = chkEnable.Checked;
            Log(_testEnabled ? "=== 测试已启用 ===" : "=== 测试已禁用 ===");

            if (!_testEnabled && _kmBox != null && _kmBox.IsConnected)
            {
                if (_leftMasked || _preMasked)
                {
                    _kmBox.MaskMouseLeft(false);
                    _leftMasked = false;
                    _preMasked = false;
                }
                _leftClickDetected = false;
            }
        }

        private void btnEmergencyStop_Click(object sender, EventArgs e)
        {
            _testEnabled = false;
            if (chkEnable.Checked) chkEnable.Checked = false;
            if (_kmBox != null && _kmBox.IsConnected)
            {
                _kmBox.MouseLeft(false);
                _kmBox.UnmaskAll();
                _leftMasked = false;
                _preMasked = false;
                _leftClickDetected = false;
                Log("=== 紧急停止：释放左键 + 解除所有屏蔽 ===");
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtLog.Clear();
            _triggerCount = 0;
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

        private void UpdateStateLabel(ImageHelper.CrosshairInfo crosshair)
        {
            if (lblState.IsDisposed) return;
            string text = $"准心: {(crosshair.SnipeEnabled ? "开镜" : "无")}  |  " +
                          $"状态: {_state}  |  " +
                          $"预屏蔽: {(_preMasked ? "是" : "否")}  |  " +
                          $"左键屏蔽: {(_leftMasked ? "是" : "否")}  |  " +
                          $"触发次数: {_triggerCount}";
            if (lblState.InvokeRequired)
                lblState.BeginInvoke(new Action(() => lblState.Text = text));
            else
                lblState.Text = text;
        }

        #endregion

        private static int ParseInt(string s, int fallback)
        {
            return int.TryParse(s?.Trim(), out int v) && v >= 0 ? v : fallback;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            DisconnectAll();
            base.OnFormClosing(e);
        }
    }
}
