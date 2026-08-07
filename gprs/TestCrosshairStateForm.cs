using MWModle;
using gprs.KmBox;
using gprs.Firing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace gprs
{
    /// <summary>
    /// 准心状态监视测试窗体（TEST_CROSSHAIR_STATE）。
    ///
    /// 目的：
    /// - 诊断主模块中"准心检测"引发的一系列链路 BUG（典型场景：真人模式拿狙击枪
    ///   右键后马上左键，计划应等待开镜→YOLO→有目标自瞄代发/无目标盲射，但成功率低）。
    /// - 因此本窗体尽量复用主模块的"原生链路"，让观测结果可直接对应 Form1 的行为：
    ///     · 准心判定：ImageHelper.ReadGameCrosshairInfo（与 Form1 step 1 完全一致）
    ///     · 屏蔽驱动：LeftMaskController.ApplyMask（Form1 step 1.2）
    ///     · 真人模式状态机：真实 QuickScopeController 实例 + TryHandle（Form1 step 1.5）
    ///     · 未开镜盲射 fast-path（Form1 step 2.5）与放行后兜底代发（Form1 step 7）
    ///     · KmBox 连接/监听/屏蔽流程与 Form1.ConnectKmBox / DisconnectKmBox 一致
    ///
    /// 观察手段：
    /// - 实时状态栏：准心四态（狙击已开镜/狙击待开镜/步枪/其它）+ RGB + redness/yellowness
    /// - RGB 折线图 + 双状态色带（准心态 / 状态机态）同时间轴对照，判定错误一眼可见
    /// - 事件列表：硬件边沿、状态机迁移、代发行为及关键时延（仅记录状态变化边沿）
    /// - 关键诊断：右键按下→snipeEnabled 上升沿的"开镜延迟"，若超过 WaitForScopeMs
    ///   则 Waiting 必超时放行，左键意图落入盲射 fast-path——真人模式低成功率主因候选
    /// - CSV 导出：事件列表落盘，事后 Excel 分析
    ///
    /// 注意：
    /// - 勾选"干预"时为原生行为：狙击模式屏蔽物理左键、由软件代发（真实开枪）；
    ///   取消勾选则纯观察：左键不屏蔽、意图不记录、不代发。
    /// - 步枪模式右键会话（RifleSessionController）不在本模块复刻范围，右键按下仅记录。
    /// </summary>
    public class TestCrosshairStateForm : Form
    {
        // === 准心显示四态 ===
        private enum CrossState { Other, SniperWaiting, SniperScoped, Rifle }

        // === 事件记录（仅边沿）===
        private sealed class EventRec
        {
            public double TsMs;
            public string Source = "";
            public string Event = "";
            public string Detail = "";
            public byte R, G, B;
            public int Redness, Yellowness;
        }

        // === 折线图采样点（每帧一个）===
        private struct ChartSample
        {
            public long TsMs;
            public byte R, G, B;
            public CrossState Cross;
            public string Qs;
            public bool RightDown;
        }

        // === 实时快照（loop 线程写，UI 定时器读）===
        private struct RtSnapshot
        {
            public CrossState Cross;
            public string Qs;
            public byte R, G, B;
            public int Redness, Yellowness;
            public bool RightDown;
            public bool Masked;
            public bool PendingFire;
            public long CrossChangeTs;
        }

        // === UI ===
        private Button _btnCapture;
        private TextBox _txtIp, _txtPort, _txtUuid;
        private Button _btnKm;
        private Label _lblKmStatus;
        private TextBox _txtWaitScope;
        private CheckBox _chkIntervene, _chkStep7;
        private Button _btnClear, _btnCsv, _btnPause, _btnUnmask;
        private ComboBox _cmbChartWin;
        private Label _lblState, _lblRgb, _lblExtra, _lblRight;
        private Panel _pnlSwatch, _chartPanel;
        private ListView _list;
        private System.Windows.Forms.Timer _uiTimer;

        // === 采集（与主模块相同的帧同步模式）===
        private readonly MWCaptureWrapperPro _mwCapture = new();
        private Bitmap _captureBitmap = new(GameConfig.CaptureWidth, GameConfig.CaptureHeight, PixelFormat.Format24bppRgb);
        private int _frameState = 0;
        private readonly AutoResetEvent _frameEvent = new(false);
        private CancellationTokenSource _cts;
        private volatile bool _captureRunning;

        // === 原生链路组件（与 Form1 同款实例）===
        private KmBoxNet _kmBox;
        private LeftMaskController _leftMask;
        private QuickScopeController _quickScope;
        private TrtYoloPoseInferencer _predictor;
        private readonly Random _random = new();
        private readonly int _sensX = GameConfig.Sensitivity.CF_2K_X;   // 与 Form1 固定的 CF 灵敏度一致
        private readonly int _sensY = GameConfig.Sensitivity.CF_2K_Y;

        // === 线程共享状态 ===
        private readonly object _sync = new();
        private readonly List<EventRec> _events = new();
        private readonly List<ChartSample> _samples = new();
        private RtSnapshot _rt;
        private byte _lastR, _lastG, _lastB;
        private int _lastRedness, _lastYellowness;
        private int _flushed;                       // 已刷入 ListView 的事件数
        private volatile bool _hwRightDown;         // 硬件监听线程写
        private long _hwRightDownTs;                // 右键按下时间戳（诊断开镜延迟的 t0，Interlocked 读写）
        private volatile bool _lastRifle;           // 上一帧是否步枪准心（右键派发判据，同 Form1）
        private volatile bool _intervene = true;    // 干预示例：屏蔽左键 + 真实代发
        private volatile bool _step7Enabled = true; // 放行后（已开镜）兜底是否跑 YOLO

        // === loop 线程帧间状态 ===
        private CrossState _prevCrossState = CrossState.Other;
        private bool _prevSnipe;
        private long _crossStateChangeTs;

        private readonly Stopwatch _sw = Stopwatch.StartNew();
        private const long MAX_CHART_WINDOW_MS = 30_000;  // 采样最大保留窗口（裁剪基准）
        private long _chartWindowMs = 30_000;             // 当前图表显示窗口（30s/10s/3s/1s 可切，方便放大观察短状态）
        private bool _chartPaused;                        // 图表暂停：冻结时间轴
        private long _pauseEndTs;                         // 暂停时刻的时间轴右边界

        public TestCrosshairStateForm()
        {
            InitializeUI();
            _uiTimer = new System.Windows.Forms.Timer { Interval = 100 };
            _uiTimer.Tick += UiTimer_Tick;
            _uiTimer.Start();
            this.FormClosing += (s, e) => Shutdown();
            StartLoadPredictor();
        }

        #region UI 布局

        private void InitializeUI()
        {
            this.Text = "准心状态监视（真人模式原生链路诊断）";
            this.ClientSize = new Size(1060, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Microsoft YaHei UI", 9);

            // ---- 第一行：采集 + KmBox 连接 ----
            _btnCapture = new Button { Text = "启动采集", Location = new Point(10, 10), Size = new Size(100, 28) };
            _btnCapture.Click += BtnCapture_Click;

            var lblIp = new Label { Text = "IP:", Location = new Point(122, 16), AutoSize = true };
            _txtIp = new TextBox { Text = "192.168.3.188", Location = new Point(145, 12), Size = new Size(100, 25) };
            var lblPort = new Label { Text = "端口:", Location = new Point(253, 16), AutoSize = true };
            _txtPort = new TextBox { Text = "8888", Location = new Point(292, 12), Size = new Size(48, 25) };
            var lblUuid = new Label { Text = "UUID:", Location = new Point(327, 16), AutoSize = true };
            _txtUuid = new TextBox { Text = "12345678", Location = new Point(367, 12), Size = new Size(75, 25) };

            _btnKm = new Button { Text = "连接KmBox", Location = new Point(450, 10), Size = new Size(90, 28) };
            _btnKm.Click += BtnKm_Click;
            _lblKmStatus = new Label { Text = "未连接", ForeColor = Color.Gray, Location = new Point(545, 16), AutoSize = true };

            var lblChartWin = new Label { Text = "图表窗口:", Location = new Point(625, 16), AutoSize = true };
            _cmbChartWin = new ComboBox
            {
                Location = new Point(700, 12), Size = new Size(68, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            _cmbChartWin.Items.AddRange(new object[] { "30s", "10s", "3s", "1s" });
            _cmbChartWin.SelectedIndex = 0;
            _cmbChartWin.SelectedIndexChanged += (s, e) => _chartWindowMs = _cmbChartWin.SelectedIndex switch
            {
                1 => 10_000,
                2 => 3_000,
                3 => 1_000,
                _ => 30_000,
            };

            _btnPause = new Button { Text = "暂停图表", Location = new Point(775, 10), Size = new Size(88, 28) };
            _btnPause.Click += BtnPause_Click;

            _btnUnmask = new Button { Text = "解除屏蔽", Location = new Point(870, 10), Size = new Size(95, 28) };
            _btnUnmask.Click += BtnUnmask_Click;

            // ---- 第二行：窗口参数 + 干预开关 ----
            var lblWaitScope = new Label { Text = "等开镜窗口(ms):", Location = new Point(10, 50), AutoSize = true };
            _txtWaitScope = new TextBox { Text = "70", Location = new Point(120, 46), Size = new Size(50, 25) };
            _txtWaitScope.TextChanged += (s, e) => { if (_quickScope != null) _quickScope.WaitForScopeMs = ParseInt(_txtWaitScope.Text, 70); };

            _chkIntervene = new CheckBox { Text = "干预(屏蔽左键+真实代发)", Location = new Point(325, 48), AutoSize = true, Checked = true };
            _chkIntervene.CheckedChanged += (s, e) =>
            {
                _intervene = _chkIntervene.Checked;
                LogEvent("系统", _intervene ? "干预开启：狙击模式屏蔽左键、软件代发" : "干预关闭：左键不屏蔽、意图不记录、不代发（纯观察）", "");
            };

            _chkStep7 = new CheckBox { Text = "放行后兜底跑YOLO", Location = new Point(545, 48), AutoSize = true, Checked = true };
            _chkStep7.CheckedChanged += (s, e) => _step7Enabled = _chkStep7.Checked;

            _btnClear = new Button { Text = "清空", Location = new Point(790, 46), Size = new Size(75, 26) };
            _btnClear.Click += BtnClear_Click;
            _btnCsv = new Button { Text = "导出CSV", Location = new Point(875, 46), Size = new Size(90, 26) };
            _btnCsv.Click += BtnCsv_Click;

            // ---- 第三行：实时状态栏 ----
            _lblState = new Label
            {
                Text = "其它", Location = new Point(10, 82), Size = new Size(150, 26),
                Font = new Font("Microsoft YaHei UI", 12, FontStyle.Bold), ForeColor = Color.DimGray
            };
            _pnlSwatch = new Panel { Location = new Point(170, 84), Size = new Size(22, 22), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.Black };
            _lblRgb = new Label { Text = "RGB=(-,-,-)  redness=-  yellowness=-", Location = new Point(202, 88), AutoSize = true, Font = new Font("Consolas", 9.5f) };
            _lblExtra = new Label { Text = "状态机: -   左键屏蔽: -   待发意图: -   持续: -", Location = new Point(520, 88), AutoSize = true };
            _lblRight = new Label { Text = "右键: --", Location = new Point(930, 88), AutoSize = true, ForeColor = Color.Gray };

            // ---- 图表区：RGB 折线 + 双状态色带（双缓冲防闪烁）----
            _chartPanel = new BufferedPanel
            {
                Location = new Point(10, 116),
                Size = new Size(1040, 250),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.Black,
            };
            _chartPanel.Paint += ChartPanel_Paint;

            // ---- 事件列表 ----
            _list = new ListView
            {
                Location = new Point(10, 376),
                Size = new Size(1040, 414),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                View = View.Details,
                FullRowSelect = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                Font = new Font("Consolas", 9),
            };
            _list.Columns.Add("时间ms", 80);
            _list.Columns.Add("来源", 78);
            _list.Columns.Add("事件", 330);
            _list.Columns.Add("详情", 290);
            _list.Columns.Add("R,G,B", 90);
            _list.Columns.Add("redness", 68);
            _list.Columns.Add("yellowness", 78);

            Controls.AddRange(new Control[] {
                _btnCapture, lblIp, _txtIp, lblPort, _txtPort, lblUuid, _txtUuid, _btnKm, _lblKmStatus,
                lblChartWin, _cmbChartWin, _btnPause, _btnUnmask,
                lblWaitScope, _txtWaitScope, _chkIntervene, _chkStep7, _btnClear, _btnCsv,
                _lblState, _pnlSwatch, _lblRgb, _lblExtra, _lblRight, _chartPanel, _list,
            });
        }

        #endregion

        #region 采集启停

        private void BtnCapture_Click(object sender, EventArgs e)
        {
            if (_captureRunning)
            {
                StopCapture();
                return;
            }

            try
            {
                MWCaptureWrapperPro.Init();
                MWCaptureWrapperPro.RefreshDevices();
                _mwCapture.set_mw_fourcc(MWFOURCC.MWFOURCC_BGR24);
                _mwCapture.set_resolution(GameConfig.CaptureWidth, GameConfig.CaptureHeight);

                if (MWCaptureWrapperPro.GetChannelCount() == 0)
                {
                    LogEvent("系统", "错误：未发现 MWCapture 设备", "");
                    return;
                }

                _mwCapture.SetFrameCallback(OnFrameCaptured);
                if (!_mwCapture.set_device(0) || !_mwCapture.start_capture(true, false))
                {
                    LogEvent("系统", "错误：采集卡启动失败", "");
                    return;
                }

                _captureRunning = true;
                _cts = new CancellationTokenSource();
                var token = _cts.Token;
                Task.Run(() => Loop(token), token);

                _btnCapture.Text = "停止采集";
                LogEvent("系统", "采集卡启动成功", "等待连接 KmBox 后链路才工作");
            }
            catch (Exception ex)
            {
                LogEvent("系统", "采集启动异常", ex.Message);
            }
        }

        private void StopCapture()
        {
            _captureRunning = false;
            try { _cts?.Cancel(); } catch { }
            _frameEvent.Set();
            try { _mwCapture.Dispose(); } catch { }
            _btnCapture.Text = "启动采集";
            LogEvent("系统", "采集已停止", "");
        }

        private void OnFrameCaptured(CRingBuffer.st_frame_t frame, int width, int height)
        {
            if (Interlocked.CompareExchange(ref _frameState, 1, 0) != 0) return;
            _mwCapture.ConvertFrameToBitmapRGB24(frame, ref _captureBitmap);
            _frameEvent.Set();
        }

        private void StartLoadPredictor()
        {
            Task.Run(() =>
            {
                try
                {
                    const string dir = "./Models";
                    var files = Directory.Exists(dir)
                        ? Directory.GetFiles(dir, "*-pose.engine")
                        : Array.Empty<string>();
                    string pick = files.FirstOrDefault(f => f.Contains("l-pose", StringComparison.OrdinalIgnoreCase))
                                  ?? files.FirstOrDefault();
                    if (pick == null)
                    {
                        LogEvent("系统", "未找到 .engine 模型", "./Models 目录不存在或为空");
                        return;
                    }
                    var p = new TrtYoloPoseInferencer(pick, GameConfig.CaptureWidth, GameConfig.CaptureHeight);
                    _predictor = p;
                    LogEvent("系统", "狙击模型已加载", Path.GetFileName(pick));
                }
                catch (Exception ex)
                {
                    LogEvent("系统", "模型加载失败", ex.Message);
                }
            });
        }

        private void Shutdown()
        {
            _uiTimer?.Stop();
            StopCapture();
            DisconnectKmBox();
            try { _predictor?.Dispose(); } catch { }
        }

        #endregion

        #region KmBox 连接（流程对齐 Form1.ConnectKmBox）

        private void BtnKm_Click(object sender, EventArgs e)
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
                LogEvent("系统", "KmBox 操作异常", ex.Message);
            }
        }

        private void ConnectKmBox()
        {
            string ip = _txtIp.Text.Trim();
            if (!int.TryParse(_txtPort.Text.Trim(), out int port))
            {
                LogEvent("系统", "端口格式错误", _txtPort.Text);
                return;
            }
            string uuid = _txtUuid.Text.Trim();

            var km = new KmBoxNet();
            if (!km.Connect(ip, port, uuid))
            {
                km.Dispose();
                UpdateKmStatus("连接失败", Color.Red);
                LogEvent("系统", "KmBox 连接失败", $"{ip}:{port}");
                return;
            }

            // 构造原生控制器（与 Form1 相同的构造方式）
            var leftMask = new LeftMaskController(km);
            var quickScope = new QuickScopeController(km);
            quickScope.Enabled = true;          // 真人模式
            quickScope.WaitForScopeMs = ParseInt(_txtWaitScope.Text, 70);

            // 原子发布：先 Controllers 后 _kmBox（同 Form1）
            _leftMask = leftMask;
            _quickScope = quickScope;
            _kmBox = km;

            // 发布完成后才订阅硬件事件 + 开监控
            _kmBox.HwMouseButtonChanged += OnHwMouseButton;
            _kmBox.MonitorEnable(9527);
            _kmBox.UnmaskAll();
            _kmBox.Trace(0, 0);
            _kmBox.MaskMouseSide1(true);
            _kmBox.MaskMouseSide2(true);

            UpdateKmStatus("已连接", Color.Green);
            _btnKm.Text = "断开KmBox";
            LogEvent("系统", "KmBox 已连接", $"{ip}:{port} uuid={uuid}");
        }

        private void DisconnectKmBox()
        {
            var km = _kmBox;
            if (km == null) return;

            // 清理顺序与 Form1.DisconnectKmBox 一致：
            // 先退订事件+停监听线程 → 消化残留回调 → 释放左键屏蔽 → 兜底 UnmaskAll → 断开
            km.HwMouseButtonChanged -= OnHwMouseButton;
            try { km.MonitorDisable(); } catch { }
            Thread.Sleep(30);
            _leftMask?.ReleaseBeforeDisconnect();
            try { km.UnmaskAll(); } catch { }
            try { km.Disconnect(); } catch { }
            try { km.Dispose(); } catch { }

            _kmBox = null;
            _quickScope = null;
            _leftMask = null;
            _hwRightDown = false;
            Interlocked.Exchange(ref _hwRightDownTs, 0);

            UpdateKmStatus("未连接", Color.Gray);
            _btnKm.Text = "连接KmBox";
            LogEvent("系统", "KmBox 已断开", "");
        }

        private void UpdateKmStatus(string text, Color color)
        {
            if (_lblKmStatus.InvokeRequired)
                _lblKmStatus.BeginInvoke(new Action(() => { _lblKmStatus.Text = text; _lblKmStatus.ForeColor = color; }));
            else
            {
                _lblKmStatus.Text = text;
                _lblKmStatus.ForeColor = color;
            }
        }

        #endregion

        #region KmBox 硬件事件（派发逻辑对齐 Form1.OnKmBoxMouseButtonChanged）

        private void OnHwMouseButton(int button, bool isDown)
        {
            long now = NowMs();

            // === 右键 ===
            if (button == 0x02)
            {
                if (isDown)
                {
                    _hwRightDown = true;
                    Interlocked.Exchange(ref _hwRightDownTs, now);
                    LogEvent("硬件", "右键按下", _lastRifle ? "当前为步枪准心" : "");

                    // 武器模式判据同 Form1：当前帧步枪准心 → 步枪会话（本模块不复刻，仅记录）
                    if (_lastRifle)
                        LogEvent("派发", "步枪模式右键 → 本模块仅测试狙击真人链路，不处理", "");
                    else
                        _quickScope?.OnHwRightDown();   // 非步枪 → 触发瞬狙等待窗口（同 Form1）
                }
                else
                {
                    _hwRightDown = false;
                    Interlocked.Exchange(ref _hwRightDownTs, 0);
                    LogEvent("硬件", "右键释放", "");
                }
                return;
            }

            // === 左键 ===
            if (button == 0x01)
            {
                bool masked = _leftMask?.IsMasked == true;
                string hint = isDown && !masked ? "（未屏蔽→意图不会被记录，请勾选\"干预\"）" : "";
                LogEvent("硬件", isDown ? "左键按下" : "左键释放", $"masked={masked}{hint}");
                _leftMask?.OnHwLeftEdge(isDown);
            }
        }

        #endregion

        #region 主循环：复刻 Form1 真人模式决策链

        private void Loop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (!_frameEvent.WaitOne(100)) continue;
                if (token.IsCancellationRequested) break;

                try
                {
                    ProcessFrame();
                }
                catch (Exception ex)
                {
                    LogEvent("异常", "帧处理异常", ex.Message);
                }
                finally
                {
                    Interlocked.Exchange(ref _frameState, 0);
                }
            }
        }

        private void ProcessFrame()
        {
            // ===== Form1 step 1：准心识别（与主模块完全一致）=====
            var crosshair = ImageHelper.ReadGameCrosshairInfo(_captureBitmap, checkSteady: false);
            bool rifle = crosshair.RifleEnabled;
            bool snipe = crosshair.SnipeEnabled;
            var (r, g, b) = ReadCenter2x2Average(_captureBitmap);
            long now = NowMs();
            long rightDownTs = Interlocked.Read(ref _hwRightDownTs);
            _lastRifle = rifle;

            // 四态合成：已开镜(纯红准心) / 步枪(黄准心) / 待开镜(右键按下且无准心) / 其它
            CrossState cs = snipe ? CrossState.SniperScoped
                          : rifle ? CrossState.Rifle
                          : (_hwRightDown ? CrossState.SniperWaiting : CrossState.Other);

            // 准心状态边沿（仅变化时记录）
            if (cs != _prevCrossState)
            {
                LogEvent("准心", $"{CrossName(_prevCrossState)}→{CrossName(cs)}",
                    $"RGB=({r},{g},{b}) redness={crosshair.MaxRedness} yellowness={crosshair.MaxYellowness}");
                _crossStateChangeTs = now;
                _prevCrossState = cs;
            }

            // 开镜上升沿：测"右键按下→snipeEnabled true"延迟（真人模式低成功率核心诊断）
            if (snipe && !_prevSnipe && rightDownTs > 0)
            {
                long delta = now - rightDownTs;
                int waitMs = _quickScope?.WaitForScopeMs ?? 100;
                string warn = delta > waitMs
                    ? $"  ⚠超过等开镜窗口({waitMs}ms)→Waiting已/将超时放行"
                    : "";
                LogEvent("诊断", $"开镜延迟={delta}ms（右键按下→纯红准心出现）", $"WaitForScopeMs={waitMs}{warn}");
            }
            _prevSnipe = snipe;

            // ===== Form1 step 1.2：屏蔽驱动 =====
            // 干预关闭时不屏蔽左键（纯观察，物理左键直通游戏）
            if (_kmBox != null && _kmBox.IsConnected)
                _leftMask?.ApplyMask(_intervene && !rifle);

            // ===== Form1 step 1.5：QuickScope 状态机（原生实例 + 原生调用）=====
            var qs = _quickScope;
            string qsBefore = qs?.StateName ?? "-";
            bool handled = false;
            if (qs != null && _kmBox != null && _kmBox.IsConnected && _predictor != null)
                handled = qs.TryHandle(snipe, debugMode: false);
            string qsAfter = qs?.StateName ?? "-";

            if (qsAfter != qsBefore)
            {
                string d = rightDownTs > 0 ? $"Δ右键按下={now - rightDownTs}ms" : "";
                LogEvent("状态机", $"{qsBefore}→{qsAfter}", d);
            }

            if (!handled)
            {
                // ===== Form1 step 2.5：狙击未开镜 fast-path（左键意图→盲射准心）=====
                if (!snipe)
                {
                    if (_leftMask != null && _leftMask.ConsumeManualFireRequest())
                    {
                        LogEvent("Fast-path", "未开镜左键意图→盲射准心中心",
                            _intervene ? "" : "[演练]未真实开枪");
                        if (_intervene)
                            FireActions.SniperFire(_kmBox, GameConfig.CaptureWidth / 2, GameConfig.CaptureHeight / 2,
                                _sensX, _sensY, false, _random);
                    }
                }
                // ===== Form1 step 7 真人模式代发（已开镜；闸门放行后唯一路径）=====
                else if (_step7Enabled && _predictor != null)
                {
                    var result = ImageHelper.ProcessYoloDetection(_captureBitmap, _predictor);
                    var lockResult = TargetSelector.ProcessTargets(result, lockHead: false,
                        GameConfig.CaptureWidth, GameConfig.CaptureHeight, debugMode: false);

                    if (_leftMask != null && _leftMask.ConsumeManualFireRequest())
                    {
                        if (lockResult.HasTarget)
                        {
                            LogEvent("兜底代发", $"有目标→自瞄代发 ({lockResult.TargetX},{lockResult.TargetY})",
                                _intervene ? "" : "[演练]未真实开枪");
                            if (_intervene)
                                FireActions.SniperFire(_kmBox, lockResult.TargetX, lockResult.TargetY,
                                    _sensX, _sensY, false, _random);
                        }
                        else
                        {
                            LogEvent("兜底代发", "无目标→盲射准心中心", _intervene ? "" : "[演练]未真实开枪");
                            if (_intervene)
                                FireActions.SniperFire(_kmBox, GameConfig.CaptureWidth / 2, GameConfig.CaptureHeight / 2,
                                    _sensX, _sensY, false, _random);
                        }
                    }
                }
            }

            // ===== 图表采样 + 实时快照 =====
            lock (_sync)
            {
                _lastR = r; _lastG = g; _lastB = b;
                _lastRedness = crosshair.MaxRedness;
                _lastYellowness = crosshair.MaxYellowness;

                _samples.Add(new ChartSample
                {
                    TsMs = now, R = r, G = g, B = b,
                    Cross = cs, Qs = qsAfter, RightDown = _hwRightDown,
                });
                if (_samples.Count > 8000)
                    _samples.RemoveRange(0, _samples.Count - 6000);

                _rt = new RtSnapshot
                {
                    Cross = cs, Qs = qsAfter, R = r, G = g, B = b,
                    Redness = crosshair.MaxRedness, Yellowness = crosshair.MaxYellowness,
                    RightDown = _hwRightDown,
                    Masked = _leftMask?.IsMasked == true,
                    PendingFire = _leftMask?.HasPendingFireRequest == true,
                    CrossChangeTs = _crossStateChangeTs,
                };
            }
        }

        /// <summary>读中心 2×2 像素 RGB 均值（与 ImageHelper 判定窗口一致）。</summary>
        private static unsafe (byte R, byte G, byte B) ReadCenter2x2Average(Bitmap bitmap)
        {
            BitmapData data = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly, bitmap.PixelFormat);
            try
            {
                byte* ptr = (byte*)data.Scan0;
                int stride = data.Stride;
                int cx = bitmap.Width / 2;
                int cy = bitmap.Height / 2;
                int sumR = 0, sumG = 0, sumB = 0, n = 0;
                for (int yOff = 0; yOff <= 1; yOff++)
                {
                    int y = cy + yOff;
                    if (y < 0 || y >= bitmap.Height) continue;
                    byte* line = ptr + y * stride;
                    for (int xOff = 0; xOff <= 1; xOff++)
                    {
                        int x = cx + xOff;
                        if (x < 0 || x >= bitmap.Width) continue;
                        int pos = x * 3;
                        sumB += line[pos];
                        sumG += line[pos + 1];
                        sumR += line[pos + 2];
                        n++;
                    }
                }
                if (n == 0) return (0, 0, 0);
                return ((byte)(sumR / n), (byte)(sumG / n), (byte)(sumB / n));
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }

        #endregion

        #region 图表绘制（RGB 折线 + 双状态色带）

        private void ChartPanel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            int W = _chartPanel.Width, H = _chartPanel.Height;
            g.Clear(Color.Black);

            const int ML = 36, MR = 8, MT = 6;
            const int bandH = 16, bandGap = 4;
            int plotH = H - MT - (bandH + bandGap) * 2 - 20;
            int plotW = W - ML - MR;
            if (plotW <= 20 || plotH <= 20) return;

            // 暂停时：时间轴冻结在暂停时刻；实时时：右边界=现在
            long tEnd = _chartPaused ? _pauseEndTs : NowMs();
            long t0 = tEnd - _chartWindowMs;

            using var gridPen = new Pen(Color.FromArgb(38, 38, 38));
            using var grayBrush = new SolidBrush(Color.Gray);
            using var font = new Font("Consolas", 8f);

            // 纵轴网格（0~255）
            for (int v = 0; v <= 255; v += 64)
            {
                int y = MT + plotH - v * plotH / 255;
                g.DrawLine(gridPen, ML, y, ML + plotW, y);
                g.DrawString(v.ToString(), font, grayBrush, 2, y - 7);
            }

            List<ChartSample> snap;
            lock (_sync)
            {
                snap = _samples.Where(s => s.TsMs >= t0 && s.TsMs <= tEnd).ToList();
            }
            if (snap.Count == 0) return;

            int X(long ts) => ML + (int)((ts - t0) * plotW / _chartWindowMs);

            // 右键按住期间的白色竖线（辅助对齐"右键按下→开镜"时延）
            using (var rightPen = new Pen(Color.FromArgb(60, 255, 255, 255)))
            {
                foreach (var s in snap)
                    if (s.RightDown)
                        g.DrawLine(rightPen, X(s.TsMs), MT, X(s.TsMs), MT + plotH);
            }

            // RGB 三通道折线
            DrawCurve(g, snap, s => s.R, Color.Red, X, MT, plotH);
            DrawCurve(g, snap, s => s.G, Color.Lime, X, MT, plotH);
            DrawCurve(g, snap, s => s.B, Color.DeepSkyBlue, X, MT, plotH);

            // 双状态色带：准心四态 / QuickScope 状态机
            // 辅助可读性：迁移边沿白竖线（短状态再短也有一条线可定位）
            //           + 宽段内文字标注（不单独依赖颜色区分，照顾色弱观察）
            int band1Y = MT + plotH + 4;
            int band2Y = band1Y + bandH + bandGap;
            using var tickPen = new Pen(Color.White);
            for (int i = 0; i < snap.Count; i++)
            {
                int x1 = X(snap[i].TsMs);
                int x2 = i + 1 < snap.Count ? X(snap[i + 1].TsMs) : ML + plotW;
                if (x2 <= x1) x2 = x1 + 1;
                g.FillRectangle(CrossBrush(snap[i].Cross), x1, band1Y, x2 - x1, bandH);
                g.FillRectangle(QsBrush(snap[i].Qs), x1, band2Y, x2 - x1, bandH);

                if (i > 0 && snap[i].Cross != snap[i - 1].Cross)
                    g.DrawLine(tickPen, x1, MT, x1, band1Y + bandH);               // 准心态迁移线（穿过曲线区）
                if (i > 0 && snap[i].Qs != snap[i - 1].Qs)
                    g.DrawLine(tickPen, x1, band1Y + bandH + 1, x1, band2Y + bandH); // 状态机迁移线

                if (x2 - x1 >= 34)
                {
                    g.DrawString(CrossShort(snap[i].Cross), font, CrossTextBrush(snap[i].Cross), x1 + 2, band1Y + 2);
                    g.DrawString(QsShort(snap[i].Qs), font, QsTextBrush(snap[i].Qs), x1 + 2, band2Y + 2);
                }
            }
            g.DrawString("准心", font, grayBrush, 2, band1Y + 2);
            g.DrawString("状态机", font, grayBrush, 2, band2Y + 2);

            // 时间刻度（步长随窗口自适应）
            long tickMs = _chartWindowMs >= 10_000 ? 5000 : _chartWindowMs >= 3000 ? 1000 : 200;
            long firstTick = (t0 / tickMs + 1) * tickMs;
            for (long t = firstTick; t <= tEnd; t += tickMs)
            {
                int x = X(t);
                g.DrawLine(gridPen, x, MT, x, MT + plotH);
                g.DrawString($"{t / 1000.0:F1}s", font, grayBrush, x - 12, H - 16);
            }

            if (_chartPaused)
                g.DrawString("⏸ 已暂停", font, Brushes.Orange, ML + plotW - 55, MT + 2);
        }

        private static void DrawCurve(Graphics g, List<ChartSample> snap, Func<ChartSample, byte> sel,
                                      Color color, Func<long, int> X, int mt, int plotH)
        {
            if (snap.Count < 2) return;
            using var pen = new Pen(color, 1.2f);
            var pts = new Point[snap.Count];
            for (int i = 0; i < snap.Count; i++)
                pts[i] = new Point(X(snap[i].TsMs), mt + plotH - sel(snap[i]) * plotH / 255);
            g.DrawLines(pen, pts);
        }

        #endregion

        #region 事件日志 / UI 刷新 / CSV

        private void LogEvent(string source, string ev, string detail)
        {
            var rec = new EventRec
            {
                TsMs = _sw.Elapsed.TotalMilliseconds,
                Source = source,
                Event = ev,
                Detail = detail,
            };
            lock (_sync)
            {
                rec.R = _lastR; rec.G = _lastG; rec.B = _lastB;
                rec.Redness = _lastRedness; rec.Yellowness = _lastYellowness;
                _events.Add(rec);
                if (_events.Count > 6000)
                {
                    int drop = _events.Count - 6000;
                    _events.RemoveRange(0, drop);
                    _flushed = Math.Max(0, _flushed - drop);
                }
            }
        }

        private void UiTimer_Tick(object sender, EventArgs e)
        {
            List<EventRec> toAdd = null;
            RtSnapshot rt;
            lock (_sync)
            {
                if (_events.Count > _flushed)
                {
                    toAdd = _events.GetRange(_flushed, _events.Count - _flushed);
                    _flushed = _events.Count;
                }

                // 裁剪过期图表采样（最大窗口外 2s 余量；暂停时以暂停边界为基准，保住冻结数据）
                long refTs = _chartPaused ? _pauseEndTs : NowMs();
                long cutoff = refTs - MAX_CHART_WINDOW_MS - 2000;
                int keep = _samples.FindIndex(s => s.TsMs >= cutoff);
                if (keep > 0) _samples.RemoveRange(0, keep);

                rt = _rt;
            }

            if (toAdd != null && toAdd.Count > 0)
            {
                _list.BeginUpdate();
                foreach (var ev in toAdd)
                {
                    var item = new ListViewItem(ev.TsMs.ToString("F1"));
                    item.SubItems.Add(ev.Source);
                    item.SubItems.Add(ev.Event);
                    item.SubItems.Add(ev.Detail);
                    item.SubItems.Add($"{ev.R},{ev.G},{ev.B}");
                    item.SubItems.Add(ev.Redness.ToString());
                    item.SubItems.Add(ev.Yellowness.ToString());
                    item.ForeColor = EventColor(ev);
                    _list.Items.Add(item);
                }
                while (_list.Items.Count > 3000)
                    _list.Items.RemoveAt(0);
                _list.EndUpdate();
                if (_list.Items.Count > 0)
                    _list.TopItem = _list.Items[_list.Items.Count - 1];
            }

            UpdateRealtimeLabels(rt);
            _chartPanel.Invalidate();
        }

        private void UpdateRealtimeLabels(RtSnapshot rt)
        {
            _lblState.Text = CrossName(rt.Cross);
            _lblState.ForeColor = CrossBrush(rt.Cross).Color;
            _pnlSwatch.BackColor = Color.FromArgb(rt.R, rt.G, rt.B);
            _lblRgb.Text = $"RGB=({rt.R},{rt.G},{rt.B})  redness={rt.Redness}  yellowness={rt.Yellowness}";

            long dur = rt.CrossChangeTs > 0 ? NowMs() - rt.CrossChangeTs : 0;
            _lblExtra.Text = $"状态机: {rt.Qs}   左键屏蔽: {(rt.Masked ? "是" : "否")}   待发意图: {(rt.PendingFire ? "有" : "无")}   持续: {dur}ms";
            _lblRight.Text = rt.RightDown ? "右键:按下" : "右键:--";
            _lblRight.ForeColor = rt.RightDown ? Color.OrangeRed : Color.Gray;
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            lock (_sync)
            {
                _events.Clear();
                _samples.Clear();
                _flushed = 0;
            }
            _list.Items.Clear();
            LogEvent("系统", "已清空记录", "");
        }

        private void BtnCsv_Click(object sender, EventArgs e)
        {
            using var dlg = new SaveFileDialog
            {
                Filter = "CSV 文件|*.csv",
                FileName = $"crosshair_state_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("时间ms,来源,事件,详情,R,G,B,redness,yellowness");
                lock (_sync)
                {
                    foreach (var ev in _events)
                    {
                        sb.Append(ev.TsMs.ToString("F1")).Append(',')
                          .Append(EscCsv(ev.Source)).Append(',')
                          .Append(EscCsv(ev.Event)).Append(',')
                          .Append(EscCsv(ev.Detail)).Append(',')
                          .Append(ev.R).Append(',').Append(ev.G).Append(',').Append(ev.B).Append(',')
                          .Append(ev.Redness).Append(',').Append(ev.Yellowness).AppendLine();
                    }
                }
                File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
                LogEvent("系统", "CSV 已导出", dlg.FileName);
            }
            catch (Exception ex)
            {
                LogEvent("系统", "CSV 导出失败", ex.Message);
            }
        }

        private void BtnPause_Click(object sender, EventArgs e)
        {
            _chartPaused = !_chartPaused;
            if (_chartPaused)
            {
                _pauseEndTs = NowMs();
                _btnPause.Text = "继续图表";
                LogEvent("系统", "图表已暂停", $"显示窗口 {_chartWindowMs / 1000}s，右边界 {_pauseEndTs}ms；切小窗口可放大短状态");
            }
            else
            {
                _btnPause.Text = "暂停图表";
                LogEvent("系统", "图表已恢复实时", "");
            }
        }

        private void BtnUnmask_Click(object sender, EventArgs e)
        {
            // 应急解除：不断开连接，直接发解除全部屏蔽指令（按键卡在屏蔽态时手动救回）
            try { _leftMask?.ReleaseBeforeDisconnect(); } catch { /* 设备可能已断 */ }
            try { _kmBox?.UnmaskAll(); } catch { /* 设备可能已断 */ }
            LogEvent("系统", "已手动解除全部屏蔽", "左/右/中/侧键/XY/滚轮");
        }

        private static string EscCsv(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }

        #endregion

        #region 状态命名 / 颜色

        private static string CrossName(CrossState s) => s switch
        {
            CrossState.SniperScoped => "狙击已开镜",
            CrossState.SniperWaiting => "狙击待开镜",
            CrossState.Rifle => "步枪",
            _ => "其它",
        };

        private static readonly Dictionary<CrossState, SolidBrush> _crossBrushes = new()
        {
            [CrossState.SniperScoped] = new SolidBrush(Color.Red),
            [CrossState.SniperWaiting] = new SolidBrush(Color.Orange),
            [CrossState.Rifle] = new SolidBrush(Color.Yellow),
            [CrossState.Other] = new SolidBrush(Color.DimGray),
        };
        private static SolidBrush CrossBrush(CrossState s) => _crossBrushes[s];

        private static readonly Dictionary<string, SolidBrush> _qsBrushes = new()
        {
            ["Idle"] = new SolidBrush(Color.DimGray),
            ["Waiting"] = new SolidBrush(Color.Orange),
            ["Released"] = new SolidBrush(Color.DeepSkyBlue),
            ["-"] = new SolidBrush(Color.Black),
        };
        private static SolidBrush QsBrush(string qs) => _qsBrushes.TryGetValue(qs, out var b) ? b : _qsBrushes["-"];

        private static string CrossShort(CrossState s) => s switch
        {
            CrossState.SniperScoped => "已开镜",
            CrossState.SniperWaiting => "待开镜",
            CrossState.Rifle => "步枪",
            _ => "无",
        };

        private static string QsShort(string qs) => qs switch
        {
            "Waiting" => "Wait",
            "Released" => "Rel",
            "Idle" => "Idle",
            _ => "",
        };

        private static Brush CrossTextBrush(CrossState s) =>
            s is CrossState.SniperScoped or CrossState.Other ? Brushes.White : Brushes.Black;

        private static Brush QsTextBrush(string qs) =>
            qs is "Idle" or "-" ? Brushes.White : Brushes.Black;

        private static Color EventColor(EventRec ev)
        {
            if (ev.Event.Contains("⚠")) return Color.OrangeRed;
            return ev.Source switch
            {
                "代发" => Color.OrangeRed,
                "代发[演练]" => Color.Salmon,
                "Fast-path" => Color.OrangeRed,
                "兜底代发" => Color.OrangeRed,
                "诊断" => Color.Gold,
                "状态机" => Color.DeepSkyBlue,
                "硬件" => Color.LightGray,
                "准心" => Color.LimeGreen,
                _ => Color.Gray,
            };
        }

        #endregion

        private long NowMs() => _sw.ElapsedMilliseconds;

        /// <summary>
        /// 双缓冲面板：先绘到离屏缓冲再整体呈现，
        /// 消除每次 Invalidate 时"清背景→重绘"造成的可见闪烁。
        /// </summary>
        private sealed class BufferedPanel : Panel
        {
            public BufferedPanel()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint
                       | ControlStyles.UserPaint
                       | ControlStyles.OptimizedDoubleBuffer, true);
                UpdateStyles();
            }
        }

        private static int ParseInt(string s, int fallback)
        {
            return int.TryParse(s?.Trim(), out int v) && v > 0 ? v : fallback;
        }
    }
}
