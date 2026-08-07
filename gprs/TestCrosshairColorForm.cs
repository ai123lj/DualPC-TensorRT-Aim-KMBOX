using MWModle;
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
    /// 步枪准心命中颜色采样测试窗体。
    ///
    /// 目的：
    /// - 观察"步枪准心（黄色）命中敌人瞬间的颜色变化"，
    ///   判断现有 RifleEnabled 判定阈值是否会被命中特效撕裂（导致连发左键屏蔽中断）。
    ///
    /// 工作流程：
    /// 1. 启动采集卡接管游戏画面
    /// 2. 开启采样后，状态机等基线（持续检测到步枪准心）
    /// 3. 一旦基线离开（RifleEnabled 由 true 变 false）→ 判定为"命中瞬间" →
    ///    固定录制窗口内（默认 500ms）每帧输出中心 2×2 像素的 RGB 与判定结果
    /// 4. 窗口结束后自动回到等基线态，等下一次命中
    ///
    /// 使用方式：
    /// - 启动采集 → 进入游戏瞄准步枪（确保准心可见黄色）→ 勾选"启用采样"
    /// - 对准敌人打一枪（命中）→ 日志自动打印命中瞬间 RGB 序列
    /// - 结合日志判断命中色是否能并入 RifleEnabled 判定
    /// </summary>
    public class TestCrosshairColorForm : Form
    {
        // === UI ===
        private Button _btnStart;
        private CheckBox _chkEnable;
        private TextBox _txtRecordMs;
        private Label _lblStatus;
        private Label _lblState;
        private Button _btnClear;
        private TextBox _txtLog;
        private CheckBox _chkMonitor;
        private TextBox _txtMonitorInterval;

        // === 持续观察模式（独立于状态机，用于直接看狙击/步枪稳态 RGB）===
        private volatile bool _monitorEnabled;
        private long _lastMonitorTs;

        // === 采集 ===
        private readonly MWCaptureWrapperPro _mwCapture = new();
        private Bitmap _captureBitmap = new(GameConfig.CaptureWidth, GameConfig.CaptureHeight, PixelFormat.Format24bppRgb);
        private int _frameState = 0;
        private readonly AutoResetEvent _frameEvent = new(false);
        private CancellationTokenSource _cts;
        private volatile bool _captureRunning;

        // === 采样状态机 ===
        private enum SampleState { WaitBaseline, Baseline, Recording }
        private SampleState _state = SampleState.WaitBaseline;
        private volatile bool _sampleEnabled;
        private int _recordWindowMs = 500;
        private int _baselineStreak;            // 连续命中步枪判定的帧数（进入 Baseline 态需至少 2 帧）
        private const int BASELINE_MIN_FRAMES = 2;
        private long _recordStartTs;
        private int _sampleId;
        private int _frameIdxInSample;

        private readonly Stopwatch _sw = Stopwatch.StartNew();

        public TestCrosshairColorForm()
        {
            InitializeUI();
            this.FormClosing += (s, e) => Shutdown();
        }

        #region UI 布局

        private void InitializeUI()
        {
            this.Text = "步枪准心命中颜色采样";
            this.ClientSize = new Size(800, 560);
            this.StartPosition = FormStartPosition.CenterScreen;

            // 第一行：启动采集 + 录制窗口时长 + 启用
            _btnStart = new Button { Text = "启动采集", Location = new Point(10, 10), Size = new Size(100, 28) };
            _btnStart.Click += BtnStart_Click;

            var lblMs = new Label { Text = "录制窗口(ms):", Location = new Point(130, 14), AutoSize = true };
            _txtRecordMs = new TextBox { Text = "500", Location = new Point(225, 11), Size = new Size(60, 25) };

            _chkEnable = new CheckBox { Text = "启用采样", Location = new Point(300, 12), AutoSize = true, Enabled = false };
            _chkEnable.CheckedChanged += ChkEnable_CheckedChanged;

            var lblMon = new Label { Text = "观察间隔(ms):", Location = new Point(385, 14), AutoSize = true };
            _txtMonitorInterval = new TextBox { Text = "200", Location = new Point(475, 11), Size = new Size(50, 25) };
            _chkMonitor = new CheckBox { Text = "持续观察RGB", Location = new Point(535, 12), AutoSize = true, Enabled = false };
            _chkMonitor.CheckedChanged += ChkMonitor_CheckedChanged;

            _btnClear = new Button { Text = "清空日志", Location = new Point(690, 10), Size = new Size(100, 28) };
            _btnClear.Click += (s, e) => { _txtLog.Clear(); _sampleId = 0; };

            // 第二行：状态显示
            _lblStatus = new Label { Text = "未启动", ForeColor = Color.Gray, Location = new Point(10, 48), AutoSize = true, Font = new Font("Microsoft YaHei UI", 9, FontStyle.Bold) };
            _lblState = new Label { Text = "状态: 等基线", Location = new Point(130, 48), AutoSize = true };

            // 日志区
            _txtLog = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Location = new Point(10, 75),
                Size = new Size(780, 475),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                WordWrap = false,
            };

            Controls.Add(_btnStart);
            Controls.Add(lblMs);
            Controls.Add(_txtRecordMs);
            Controls.Add(_chkEnable);
            Controls.Add(lblMon);
            Controls.Add(_txtMonitorInterval);
            Controls.Add(_chkMonitor);
            Controls.Add(_btnClear);
            Controls.Add(_lblStatus);
            Controls.Add(_lblState);
            Controls.Add(_txtLog);
        }

        #endregion

        #region 采集启停

        private void BtnStart_Click(object sender, EventArgs e)
        {
            if (_captureRunning)
            {
                Shutdown();
                _btnStart.Text = "启动采集";
                _lblStatus.Text = "已停止";
                _lblStatus.ForeColor = Color.Gray;
                _chkEnable.Enabled = false;
                _chkEnable.Checked = false;
                return;
            }

            try
            {
                MWCaptureWrapperPro.Init();
                MWCaptureWrapperPro.RefreshDevices();
                _mwCapture.set_mw_fourcc(MWFOURCC.MWFOURCC_BGR24);
                _mwCapture.set_resolution(GameConfig.CaptureWidth, GameConfig.CaptureHeight);

                int deviceCount = MWCaptureWrapperPro.GetChannelCount();
                if (deviceCount == 0)
                {
                    Log("错误：未发现 MWCapture 设备");
                    _lblStatus.Text = "无设备";
                    _lblStatus.ForeColor = Color.Red;
                    return;
                }

                _mwCapture.SetFrameCallback(OnFrameCaptured);
                if (!_mwCapture.set_device(0) || !_mwCapture.start_capture(true, false))
                {
                    Log("错误：采集卡启动失败");
                    _lblStatus.Text = "启动失败";
                    _lblStatus.ForeColor = Color.Red;
                    return;
                }

                _captureRunning = true;
                _cts = new CancellationTokenSource();
                var token = _cts.Token;
                Task.Run(() => Loop(token), token);

                _btnStart.Text = "停止采集";
                _lblStatus.Text = "已启动（采样待启用）";
                _lblStatus.ForeColor = Color.Green;
                _chkEnable.Enabled = true;
                _chkMonitor.Enabled = true;
                Log("采集卡启动成功。");
                Log("- 勾选“启用采样”观察步枪命中色（需先稳住黄色基线）");
                Log("- 勾选“持续观察RGB”按固定间隔打印中心 2×2 的 RGB（不依赖判定，用于看狙击纯红准心实际值）");
            }
            catch (Exception ex)
            {
                Log($"异常: {ex.Message}");
                _lblStatus.Text = "异常";
                _lblStatus.ForeColor = Color.Red;
            }
        }

        private void Shutdown()
        {
            _captureRunning = false;
            _sampleEnabled = false;
            _monitorEnabled = false;
            try { _cts?.Cancel(); } catch { }
            _frameEvent.Set();
            try { _mwCapture.Dispose(); } catch { }
            _state = SampleState.WaitBaseline;
            _baselineStreak = 0;
        }

        private void OnFrameCaptured(CRingBuffer.st_frame_t frame, int width, int height)
        {
            if (Interlocked.CompareExchange(ref _frameState, 1, 0) != 0) return;
            _mwCapture.ConvertFrameToBitmapRGB24(frame, ref _captureBitmap);
            _frameEvent.Set();
        }

        #endregion

        #region 主循环 + 采样状态机

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
                    Log($"循环异常: {ex.Message}");
                }
                finally
                {
                    Interlocked.Exchange(ref _frameState, 0);
                }
            }
        }

        private void ProcessFrame()
        {
            // 读准心判定 + 中心 2×2 均值
            var crosshair = ImageHelper.ReadGameCrosshairInfo(_captureBitmap, checkSteady: false);
            var (avgR, avgG, avgB) = ReadCenter2x2Average(_captureBitmap);

            bool rifle = crosshair.RifleEnabled;
            bool snipe = crosshair.SnipeEnabled;

            // 持续观察模式（独立通道）：按固定间隔直接打印当前 RGB，不依赖采样状态机
            // 用于观察狙击开镜稳态的真实 RGB —— 当前判定规则 (R==255 && B==0) 对狙击可能过严，
            // 这里绕开判定直接输出原始值，帮助决定容差阈值
            if (_monitorEnabled)
            {
                long nowMs = _sw.ElapsedMilliseconds;
                int interval = ParseInt(_txtMonitorInterval.Text, 200);
                if (nowMs - _lastMonitorTs >= interval)
                {
                    _lastMonitorTs = nowMs;
                    string verdict = rifle ? "步枪" : (snipe ? "狙击" : " -- ");
                    Log($"[观察] R={avgR,3} G={avgG,3} B={avgB,3} | redness={crosshair.MaxRedness,3} | yellowness={crosshair.MaxYellowness,3} | 判定={verdict}");
                }
            }

            if (!_sampleEnabled)
            {
                // 未启用采样：只更新状态栏，不产出日志
                UpdateStateLabel(rifle, snipe);
                return;
            }

            switch (_state)
            {
                case SampleState.WaitBaseline:
                    // 等连续 BASELINE_MIN_FRAMES 帧都是步枪准心，进入 Baseline 态
                    if (rifle) _baselineStreak++;
                    else _baselineStreak = 0;

                    if (_baselineStreak >= BASELINE_MIN_FRAMES)
                    {
                        _state = SampleState.Baseline;
                        Log($"[基线稳定] R={avgR,3} G={avgG,3} B={avgB,3}  redness={crosshair.MaxRedness,3}  yellowness={crosshair.MaxYellowness,3}  —— 随时可以开枪");
                    }
                    break;

                case SampleState.Baseline:
                    // 基线态等"离开"瞬间（rifle 由 true 变 false）
                    if (!rifle)
                    {
                        _sampleId++;
                        _frameIdxInSample = 0;
                        _recordStartTs = _sw.ElapsedMilliseconds;
                        _state = SampleState.Recording;
                        _recordWindowMs = ParseInt(_txtRecordMs.Text, 500);
                        Log("");
                        Log($"=== 命中样本 #{_sampleId} 开始（录制 {_recordWindowMs}ms）===");
                        Log($"  #  |  t_ms |  R   G   B  | redness | yellowness | 判定");
                        // 首帧（离开基线瞬间）
                        LogSampleRow(0, avgR, avgG, avgB, crosshair.MaxRedness, crosshair.MaxYellowness, rifle, snipe);
                    }
                    break;

                case SampleState.Recording:
                    _frameIdxInSample++;
                    long elapsed = _sw.ElapsedMilliseconds - _recordStartTs;
                    LogSampleRow((int)elapsed, avgR, avgG, avgB, crosshair.MaxRedness, crosshair.MaxYellowness, rifle, snipe);

                    if (elapsed >= _recordWindowMs)
                    {
                        string ending = rifle ? "已恢复步枪态" : "仍未恢复";
                        Log($"=== 样本 #{_sampleId} 结束（{_frameIdxInSample} 帧, {elapsed}ms, {ending}）===");
                        Log("");
                        _state = SampleState.WaitBaseline;
                        _baselineStreak = rifle ? 1 : 0;
                    }
                    break;
            }

            UpdateStateLabel(rifle, snipe);
        }

        /// <summary>
        /// 读中心 2×2 像素的 RGB 平均值（与 ImageHelper 判定窗口一致）。
        /// </summary>
        private static unsafe (byte R, byte G, byte B) ReadCenter2x2Average(Bitmap bitmap)
        {
            BitmapData data = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
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

        private void LogSampleRow(int tMs, byte r, byte g, byte b, int redness, int yellowness, bool rifle, bool snipe)
        {
            string verdict = rifle ? "步枪" : (snipe ? "狙击" : " -- ");
            // 对齐列：#帧 |  t_ms | R   G   B  | redness | yellowness | 判定
            Log($" {_frameIdxInSample,2}  | {tMs,5} | {r,3} {g,3} {b,3} | {redness,7} | {yellowness,10} | {verdict}");
        }

        #endregion

        #region 日志 + 状态

        private void ChkEnable_CheckedChanged(object sender, EventArgs e)
        {
            _sampleEnabled = _chkEnable.Checked;
            if (_sampleEnabled)
            {
                _state = SampleState.WaitBaseline;
                _baselineStreak = 0;
                Log("=== 采样已启用 ===");
            }
            else
            {
                Log("=== 采样已禁用 ===");
            }
        }

        private void ChkMonitor_CheckedChanged(object sender, EventArgs e)
        {
            _monitorEnabled = _chkMonitor.Checked;
            if (_monitorEnabled)
            {
                _lastMonitorTs = 0;
                Log("=== 持续观察已启用：每帧按间隔直接打印中心 RGB ===");
            }
            else
            {
                Log("=== 持续观察已禁用 ===");
            }
        }

        private void Log(string message)
        {
            double ms = _sw.Elapsed.TotalMilliseconds;
            string line = message.StartsWith("=") || message.Length == 0 || message.StartsWith(" ")
                ? message
                : $"[{ms,8:F1}ms] {message}";

            if (_txtLog.IsDisposed) return;
            if (_txtLog.InvokeRequired)
                _txtLog.BeginInvoke(new Action(() => AppendLog(line)));
            else
                AppendLog(line);
        }

        private void AppendLog(string line)
        {
            if (_txtLog.IsDisposed) return;
            _txtLog.AppendText(line + Environment.NewLine);
        }

        private void UpdateStateLabel(bool rifle, bool snipe)
        {
            if (_lblState.IsDisposed) return;
            string crosshairText = rifle ? "步枪" : (snipe ? "狙击" : "无");
            string stateText = _sampleEnabled ? _state.ToString() : "关闭";
            string text = $"准心: {crosshairText}  |  采样态: {stateText}  |  样本数: {_sampleId}";
            if (_lblState.InvokeRequired)
                _lblState.BeginInvoke(new Action(() => _lblState.Text = text));
            else
                _lblState.Text = text;
        }

        #endregion

        private static int ParseInt(string s, int fallback)
        {
            return int.TryParse(s?.Trim(), out int v) && v > 0 ? v : fallback;
        }
    }
}
