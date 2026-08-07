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
    public partial class Form1 : Form
    {
        #region 字段定义

        // === 调试模式 ===
        private bool _debugMode = false;
        private bool _autoSwitchWeapon = false;
        private bool _rifleLockHead = true;  // true=打头, false=打身体

        // === 灵敏度配置 ===
        // 程序已特化为 CF 专用（游戏选择单选项已移除），固定使用 CF 2K 灵敏度
        private int _xSensitivity = GameConfig.Sensitivity.CF_2K_X;
        private int _ySensitivity = GameConfig.Sensitivity.CF_2K_Y;

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

        /// === 其他 ===
        private readonly Random _random = new();

        // === 模型路径 ===
        private const string MODELS_DIR = "./Models";
        private string _sniperModelPath;
        private string _rifleModelPath;

        // === 开火模式配置（UI chkQuickScopeMode：“真人模式”复选框）===
        //
        // 历史包袱提示：字段 _quickScopeMode / 类 QuickScopeController 均是旧版 “瞬狙干预” 的命名，
        // 为避免大范围重构保留原名；UI 显示文本和实际行为已重新描述。
        //
        // 两种模式的完整语义对照表：
        //
        //   _quickScopeMode = false （UI 未勾选 “真人模式”） → “瞬狙模式”
        //     · 开火方式：开镜（snipeEnabled true）后，即便用户不按左键也会自动代发一枪
        //     · 目的：开一枪→退镜→再开镜→再开一枪，视觉上像连续瞬狙
        //     · 代码路径：ProcessYoloFrame step 7 的 else 分支（使用 _snipeFiredInCurrentScope 锁定每次开镜只代发1枚）
        //     · QuickScopeController.Enabled = false，状态机不介入
        //
        //   _quickScopeMode = true  （UI 勾选 “真人模式”） → “真人模式”
        //     · 开火方式：等用户按下物理左键的“开火意图”才代发（支持两种手感）：
        //        - 瞬狙：按右键 → Waiting 等镜打开 → 按左键 → 开火（防盲狙闸门，见 QuickScopeController）
        //        - 开镜打：按右键 → 等镜打开 → 任意时刻按左键才开火
        //     · 代码路径：QuickScopeController 只负责“等待开镜防盲狙”，放行后开火决策统一走
        //       ProcessYoloFrame step 7 的 if 分支（有目标瞄准代发/无目标盲射准心，微自瞄过滤全程生效）
        //     · QuickScopeController.Enabled = true
        //
        private QuickScopeController _quickScope;
        // UI chkQuickScopeMode.Checked 的镜像（UI 线程事件修改，变更时 push 给 _quickScope.Enabled）
        // true = 真人模式；false = 瞬狙模式
        private bool _quickScopeMode = false;
        private bool _microAimMode = false;              // 微自瞄：准心在框内才锁定代发，框外按无目标盲射（真人模式 step 7 全局生效）
        private int _microAimExtend = 30;                // 微自瞄锁定框扩展像素（XY方向各扩展此值）
        // 真人模式“等待狙击镜打开”窗口（ms）：右键按下后在此时长内等待 snipeEnabled 为 true，
        // 期间左键意图挂起；超时未开镜则放行盲射。需要测试后可手动调整。仅真人模式生效。
        private int _quickScopeWaitScopeMs = 70;
        // 真人模式盲射帧数（UI txtBlindFireFrames，0~10，默认 1）：
        //   0 = 不执行自瞄：左键意图只原地代发（不看识别结果）
        //   1 = 只看意图帧识别结果：有目标→瞄准代发，无→立即原地盲射
        //   N≥2 = 除意图帧外多等 N-1 帧，期间任意识别到目标转瞄准代发，否则原地盲射
        // 意图帧计为第 1 帧；开镜边沿强制清零挂起防残留开枪。
        private int _blindFireDelayFrames = 1;
        private bool _blindFirePending = false;      // 意图已消费，正等待后续帧识别结果（仅 N≥2 时置位）
        private int _blindFireNoTargetCount = 0;     // 挂起以来的连续未识别帧数

        // === 步枪会话模式 ===
        // 右键按住期间软件按左键，松开右键时释放，按压时长由用户控制点射/连发
        // 状态完全封装在 RifleSessionController，这里仅保留引用
        private RifleSessionController _rifleSession;

        // === 左键屏蔽控制器 ===
        // 屏蔽决策直接基于每帧准心检测结果（无去抖）；狙击模式左键跨帧屏蔽，代发由软件统一发
        private LeftMaskController _leftMask;

        // 常规狙击模式的开镜会话锁：每次开镜边沿（snipeEnabled false→true）重置，代发后置位，确保本次开镜仅代发一枚
        private bool _prevSnipeEnabled = false;
        private bool _snipeFiredInCurrentScope = false;

        #endregion

        #region 初始化

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // 程序关闭时必须解除设备侧仅残留的屏蔽位，避免侧键、左键等物理输入完全失灵
            this.FormClosing += Form1_FormClosing;

            // 从 UI 控件同步初始状态
            _quickScopeMode = chkQuickScopeMode.Checked;
            _microAimMode = chkMicroAim.Checked;
            if (int.TryParse(txtMicroAimExtend.Text, out int extendPx) && extendPx >= 0)
                _microAimExtend = extendPx;
            if (int.TryParse(txtBlindFireFrames.Text, out int blindFrames))
                _blindFireDelayFrames = Math.Clamp(blindFrames, 0, 10);
            _autoSwitchWeapon = chkAutoSwitchWeapon.Checked;
            _rifleLockHead = chkRifleLockHead.Checked;

            InitializeModelSelection();
            InitializeMWCapture();
            StartYoloThread();
            StartStatsThread();
        }

        private void InitializeModelSelection()
        {
            // 扫描 Models 目录，查找所有 .engine 文件
            var engineFiles = Directory.Exists(MODELS_DIR)
                ? Directory.GetFiles(MODELS_DIR, "*-pose.engine")
                    .Select(Path.GetFileName)
                    .OrderBy(f => f)
                    .ToArray()
                : Array.Empty<string>();

            cmbSniperModel.Items.AddRange(engineFiles);
            cmbRifleModel.Items.AddRange(engineFiles);

            // 默认选择：狙击和步枪都用 M（如果存在）
            SelectModelDefault(cmbSniperModel, engineFiles, "m-pose");
            SelectModelDefault(cmbRifleModel, engineFiles, "m-pose");
        }

        private void SelectModelDefault(System.Windows.Forms.ComboBox cmb, string[] files, string keyword)
        {
            for (int i = 0; i < files.Length; i++)
            {
                if (files[i].Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    cmb.SelectedIndex = i;
                    return;
                }
            }
            if (files.Length > 0) cmb.SelectedIndex = 0;
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
        //
        // ===================== YOLO 处理线程概览 =====================
        //
        // 输入： MWCapture 采集卡 → _captureBitmap（BGR24，由 OnFrameCaptured 回调填充）
        // 同步： _frameState(0/1 Interlocked) + _frameEvent(AutoResetEvent)
        //        • 采集回调 CAS 0→1 成功才写帧并 Set 事件
        //        • 本线程 WaitOne 后处理，处理完 Exchange 回 0 释放写位
        //        • 这种索引使采集速率超过处理速率时自动丢帧而不阀堆
        //
        // 双模型策略：狙击和步枪各自加载 TRT engine，相同路径时共用一个实例
        //                （由 WeaponDispatcher.FireMode 决定用哪个 predictor）
        //
        // 关键要点：本线程是 KmBox 指令发送的唯一源头（键鼠统一由此线程发，
        //                硬件监听线程只记录状态/边沿），避免多线程串指令
        //
        // =====================================================================
        
        /// <summary>
        /// 创建并启动后台 YOLO 推理线程。由 Form1_Load 调用一次。
        /// 主循环：等待帧事件 → ProcessYoloFrame → 调试图更新 → 帧计数加 1 → 释放帧位。
        /// </summary>
        private void StartYoloThread()
        {
            // 从 UI 线程读取当前选中的 .engine 模型文件名（还在 UI 线程，可直接访问 ComboBox）
            _sniperModelPath = cmbSniperModel.SelectedItem is string s ? Path.Combine(MODELS_DIR, s) : null;
            _rifleModelPath = cmbRifleModel.SelectedItem is string r ? Path.Combine(MODELS_DIR, r) : null;
        
            // 以任务形式启动后台线程：TRT engine 构造较耗时，放在后台避免阻塞 UI
            Task.Run(() =>
            {
                try
                {
                    // 加载狙击模型（通常高精度 L，狙击需要更稳的远距检测）
                    TrtYoloPoseInferencer sniperPredictor = null;
                    if (_sniperModelPath != null)
                        sniperPredictor = new TrtYoloPoseInferencer(_sniperModelPath, GameConfig.CaptureWidth, GameConfig.CaptureHeight);
        
                    // 加载步枪模型（默认 M，兼顾精度与延迟）；与狙击相同路径时直接共用实例以节省显存
                    TrtYoloPoseInferencer riflePredictor;
                    if (_rifleModelPath != null && _rifleModelPath != _sniperModelPath)
                        riflePredictor = new TrtYoloPoseInferencer(_rifleModelPath, GameConfig.CaptureWidth, GameConfig.CaptureHeight);
                    else
                        riflePredictor = sniperPredictor;
        
                    // 调试绘制复用的 Graphics（只用于 UI 遮挡遮罩 + 调试覆盖，游戏渲染走 MWCapture）
                    var graphics = Graphics.FromImage(_captureBitmap);
        
                    // ===== 主循环不停拉帧 =====
                    while (true)
                    {
                        // 阻塞等待采集回调写一帧。AutoResetEvent 自动复位，无需手动 Reset
                        _frameEvent.WaitOne();
        
                        // 核心处理：准心检测 + 屏蔽驱动 + 武器模式决策 + YOLO 推理 + 开火
                        ProcessYoloFrame(sniperPredictor, riflePredictor, graphics);
        
                        // 调试模式将带标注的截图回展到 pictureBox1
                        UpdateDebugDisplay();
        
                        // 帧计数（统计线程每秒采样一次计算 FPS）
                        _frameCount++;
        
                        // 释放帧位，允许采集回调写下一帧
                        Interlocked.Exchange(ref _frameState, 0);
                    }
                }
                catch (Exception ex)
                {
                    // 后台线程捕获到的异常弹框提示（生产环境建议改为日志）
                    MessageBox.Show(ex.Message);
                }
            });
        }
        
        /// <summary>
        /// 单帧处理主流程：从准心识别到开火执行的完整决策链。
        /// 分为 7 步（包含 2 个早返回点），每步附有编号注释。
        /// </summary>
        private void ProcessYoloFrame(TrtYoloPoseInferencer sniperPredictor, TrtYoloPoseInferencer riflePredictor, Graphics graphics)
        {
            // --------- 1. 准心识别 ---------
            // 从截图固定区域采样像素，判断当前是否存在步枪/狙击准心。
            // 判定为 R=255 && B=0，G 通道区分狙（G=0）/步（G>0，涵盖命中渐变），且两者互斥。
            // 早期因“命中敌人准心变色”需要去抖（帧数去抖器 + 右键 50ms 时间窗），
            // 新判定已从源头消除闪烁，两套去抖均已移除：每帧直接用检测结果，切枪 0 帧延迟。
            var crosshair = ImageHelper.ReadGameCrosshairInfo(_captureBitmap, checkSteady: false);
            bool rifleEnabled = crosshair.RifleEnabled;
            bool snipeEnabled = crosshair.SnipeEnabled;

            // 开镜边沿检测：!snipeEnabled → snipeEnabled 时释放开镜会话锁，允许本次开镜代发一枚
            if (snipeEnabled && !_prevSnipeEnabled)
            {
                _snipeFiredInCurrentScope = false;
                _blindFirePending = false;   // 新开镜会话：清掉上一次的延迟盲射挂起，防止残留开枪
            }
            _prevSnipeEnabled = snipeEnabled;
        
            // 每帧写入当前步枪准心状态：右键硬件事件据此判武器模式（RifleSessionController.IsRifleModeNow）
            _rifleSession?.UpdateRifleCrosshair(rifleEnabled);
        
            // --------- 1.2 屏蔽驱动（每帧）---------
            // 步枪模式（rifleEnabled）：右键预屏蔽，配合 RifleSession 右键会话机制
            // 狙击模式（!rifleEnabled）：左键跨帧全程屏蔽，代发由软件统一发。
            // 屏蔽不可解除：解除后“右键按下再屏蔽”存在时间差，来不及拦截盲狙，
            // 且物理左键与软件代发会互相冲突；菜单态点击走 step 2.5 的快速点击优化。
            if (!_debugMode && _kmBox != null && _kmBox.IsConnected)
            {
                _rifleSession?.ApplyRightPreMask(rifleEnabled);
                _leftMask?.ApplyMask(!rifleEnabled);
            }
        
            // --------- 1.5 防盲狙闸门检查 ---------
            // 真人模式下右键按下后尚未开镜时，QuickScope 闸门独占本帧等待开镜
            // （TryHandle 返回 true 代表“继续等开镜”，Form1 不推理，左键意图保持挂起）
            if (_rifleSession != null && !_rifleSession.IsActive && _quickScope != null
                && _quickScope.TryHandle(snipeEnabled, _debugMode))
                return;
        
            // --------- 2. 武器模式调度 ---------
            // WeaponDispatcher 根据准心状态 + 硬件按键 + 步枪会话状态输出决策：
            //   Skip            ：无需处理（例：步枪备战，等待右键）
            //   EndRifleSession ：右键已释，结束步枪会话
            //   Proceed         ：继续处理，携带 (fireMode, lockHead, renderMask)
            var decision = WeaponDispatcher.Decide(rifleEnabled, _kmBox, _rifleSession, _rifleLockHead, _debugMode);
            switch (decision.Action)
            {
                case FireAction.Skip:
                    return;
                case FireAction.EndRifleSession:
                    _rifleSession?.End();
                    return;
            }
            int fireMode = decision.FireMode;      // Rifle / Sniper
            bool lockHead = decision.LockHead;     // 是否优先锁头
            // UI 遮挡遮罩：在 UiMaskRect 区域画色块遮住游戏内右下角 UI，避免 YOLO 将其误识为身体
            if (decision.RenderMask)
                graphics.FillRectangle(GameConfig.MaskBrush, GameConfig.UiMaskRect);
        
            // --------- 2.5 狙击模式未开镜 fast-path ---------
            // !snipeEnabled 代表用户没开镜，本就不应该有描准行为→绝不自动锁人，
            // 仅在用户实际按下物理左键时盲射一枪；同时跳过 YOLO 节省算力
            if (fireMode == (int)GameConfig.FireMode.Sniper && !snipeEnabled)
            {
                // 瞬狙模式下“等待开镜”已由 QuickScopeController.Waiting 独占本帧（此处不可达）；
                // 运行到这里说明左键意图是“用户未开镜直接盲射” 或 “Waiting 超时未开镜”，
                // 两种场景都应走盲射到准心中心，尊重用户原始开火意图。
                if (_leftMask != null && _leftMask.ConsumeManualFireRequest())
                {
                    if (_kmBox != null && !_kmBox.IsMouseRightDown())
                    {
                        // 无右键：典型为游戏菜单点击（或持狙不按右键的点按）。
                        // 走快速点击：不含拉栓冷却与开火前后固定等待，避免把完整开枪节奏
                        // 套到菜单操作上导致响应迟钝；按压时长随机化贴近人类点击指纹。
                        FireActions.QuickClick(_kmBox, _random);
                    }
                    else
                    {
                        // 右键按下未开镜（闸门等待超时放行）：完整狙击开枪序列
                        ExecuteFireAction(fireMode, GameConfig.CaptureWidth / 2, GameConfig.CaptureHeight / 2);
                    }
                }
                return;
            }
        
            // --------- 3. 选择推理器 ---------
            // 按 fireMode 指向对应 TRT engine（同路径时两者指同一实例）
            var predictor = (fireMode == (int)GameConfig.FireMode.Sniper) ? sniperPredictor : riflePredictor;
        
            // --------- 4. YOLO 推理 ---------
            // ImageHelper.ProcessYoloDetection 负责 letterbox/归一化/engine.Infer，返回检测结果列表
            var result = ImageHelper.ProcessYoloDetection(_captureBitmap, predictor);
        
            // --------- 5. 目标选择 ---------
            // TargetSelector 结合 lockHead + 截图中心距离 + 关键点置信，选出最优目标归一到 lockResult
            var lockResult = TargetSelector.ProcessTargets(result, lockHead, GameConfig.CaptureWidth, GameConfig.CaptureHeight, _debugMode);
        
            // --------- 6. 调试绘制（只在 debugMode 启用时生效）---------
            // debug 模式下不开火，仅用于验证 YOLO 输入/输出和准心检测是否正确
            if (_debugMode)
            {
                if(result.Count > 0)
                    DebugRenderer.DrawDebugInfo(_captureBitmap, result, lockResult);
                return;
            }
        
            // --------- 7. 执行开火 ---------
            if (fireMode == (int)GameConfig.FireMode.Rifle)
            {
                // 步枪会话模式：移交给 RifleSession 完整管理（累积移动/更新轨迹/开或不开）
                _rifleSession.HandleFrame(lockResult, _xSensitivity, _ySensitivity);
            }
            else if (fireMode == (int)GameConfig.FireMode.Sniper)
            {
                // 底层狙击代发分支——由 UI “真人模式”复选框决定具体走哪边
                if (_quickScopeMode)
                {
                    // === 真人模式（chkQuickScopeMode 勾选）===
                    // QuickScopeController 只负责“等待开镜防盲狙”，放行后全部开火决策在此：
                    // 绝不自动锁人，只响应用户左键意图（盲射帧数语义见 _blindFireDelayFrames 字段注释）。

                    // 盲射帧数 = 0 时完全不执行自瞄：不看识别结果，意图帧直接原地代发
                    bool useAim = _blindFireDelayFrames > 0;

                    // 微自瞄：准心需在检测框（扩展后）内才算"有效目标"，否则按无目标处理
                    bool hasValidTarget = useAim && lockResult.HasTarget;
                    if (hasValidTarget && _microAimMode)
                    {
                        int cx = GameConfig.CaptureWidth / 2;
                        int cy = GameConfig.CaptureHeight / 2;
                        var box = lockResult.TargetBounds;
                        int ext = _microAimExtend;
                        hasValidTarget = cx >= box.Left - ext && cx < box.Right + ext
                                      && cy >= box.Top - ext && cy < box.Bottom + ext;
                    }

                    if (_leftMask != null && _leftMask.ConsumeManualFireRequest())
                    {
                        // 新的左键意图帧：有目标→瞄准代发；无目标→立即盲射（0/1帧）或挂起等待（N≥2）
                        if (hasValidTarget)
                        {
                            ExecuteFireAction(fireMode, lockResult.TargetX, lockResult.TargetY);
                        }
                        else if (_blindFireDelayFrames <= 1)
                        {
                            // 0=不自瞄直接代发；1=只看当前帧，无结果即原地开枪
                            ExecuteFireAction(fireMode, GameConfig.CaptureWidth / 2, GameConfig.CaptureHeight / 2);
                        }
                        else
                        {
                            _blindFirePending = true;
                            _blindFireNoTargetCount = 1;   // 意图帧本身计为第 1 个未识别帧
                        }
                    }
                    else if (_blindFirePending)
                    {
                        // 挂起等待帧：识别到目标转瞄准代发；未识别帧数达到 N 则原地盲射
                        if (hasValidTarget)
                        {
                            ExecuteFireAction(fireMode, lockResult.TargetX, lockResult.TargetY);
                            _blindFirePending = false;
                        }
                        else if (++_blindFireNoTargetCount >= _blindFireDelayFrames)
                        {
                            ExecuteFireAction(fireMode, GameConfig.CaptureWidth / 2, GameConfig.CaptureHeight / 2);
                            _blindFirePending = false;
                        }
                    }
                }
                else
                {
                    // === 瞬狙模式（chkQuickScopeMode 未勾选）===
                    // 开镜后自动代发一枚——有目标瞄准代发，无目标消费左键意图盲射到准心。
                    // 用 _snipeFiredInCurrentScope 锁定“本次开镜仅代发一次”：右键开镜后游戏内一发一拉栓 + 自动退镜，
                    // 但准心颜色从红渐变回普通需若干帧，期间 snipeEnabled 仍为 true；若不锁会每帧重复触发（150ms 节拍，自动切枪会连续响）。
                    if (lockResult.HasTarget && !_snipeFiredInCurrentScope)
                    {
                        ExecuteFireAction(fireMode, lockResult.TargetX, lockResult.TargetY);
                        _snipeFiredInCurrentScope = true;
                    }
                    else if (!lockResult.HasTarget && _leftMask != null && _leftMask.ConsumeManualFireRequest())
                    {
                        ExecuteFireAction(fireMode, GameConfig.CaptureWidth / 2, GameConfig.CaptureHeight / 2);
                    }
                }
            }
        }
        
        /// <summary>
        /// 按 fireMode 调用对应的 FireActions 原子操作并累加鼠标动作计数。
        /// FireActions 负责：XY 细粒度屏蔽 → MouseMove → 解除 → 左键按放 → 冷却。
        /// </summary>
        private void ExecuteFireAction(int fireMode, int targetX, int targetY)
        {
            if (fireMode == (int)GameConfig.FireMode.Sniper)
            {
                FireActions.SniperFire(_kmBox, targetX, targetY, _xSensitivity, _ySensitivity, _autoSwitchWeapon, _random);
                _mouseActionCount++;
            }
        }
        
        /// <summary>
        /// 调试模式专用：将带 YOLO 标注的截图克隆后 Invoke 到 UI 线程展示。
        /// 非 debug 模式时直接 return，避免克隆/跨线程开销。
        /// </summary>
        private void UpdateDebugDisplay()
        {
            if (!_debugMode) return;
        
            // Clone 是必须的：_captureBitmap 下一帧要被采集线程覆写，
            // 不能直接把引用交给 UI；旧 Image 有 UI 线程负责 Dispose
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
                    UpdateStatsDisplay();
                }
            });
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

            // 先用局部变量完整构造，最后原子发布到字段，
            // 以免其他线程（YOLO 帧循环 / 硬件事件）读到中间态
            var km = new KmBoxNet();
            if (!km.Connect(ip, port, uuid))
            {
                km.Dispose();
                lblKmBoxStatus.Text = "连接失败";
                lblKmBoxStatus.ForeColor = Color.Red;
                return;
            }

            // 构造 Controllers（还未订阅事件，不会被回调触发）
            var leftMask = new LeftMaskController(km);

            var quickScope = new QuickScopeController(km);
            quickScope.Enabled = _quickScopeMode;
            quickScope.WaitForScopeMs = _quickScopeWaitScopeMs;

            var rifleSession = new RifleSessionController(km, () => _mouseActionCount++, _random);
            rifleSession.ResetOnConnect();

            // 原子发布：必须先 Controllers 后 _kmBox，
            // 这样其他线程见到 _kmBox != null 时 Controllers 一定已就绪
            _leftMask = leftMask;
            _quickScope = quickScope;
            _rifleSession = rifleSession;
            _kmBox = km;

            // 发布完成后才订阅硬件事件 + 开监控（确保回调调起时字段已就绪）
            _kmBox.HwMouseButtonChanged += OnKmBoxMouseButtonChanged;
            _kmBox.HwKeyDown += OnKmBoxKeyDown;
            _kmBox.MonitorEnable(9527);
            _kmBox.UnmaskAll();
            _kmBox.Trace(0, 0);

            // 全程屏蔽侧键 1/2：
            // - 侧键1：“步枪打头”切换热键，屏蔽防止切换时误触发游戏内技能（见 OnKmBoxMouseButtonChanged）
            // - 侧键2：当前无功能（原 Burst 点射已移除），游戏内也不使用此键，
            //   屏蔽防止物理按压透传给游戏被检测，预留给后续辅助功能
            _kmBox.MaskMouseSide1(true);
            _kmBox.MaskMouseSide2(true);

            btnKmBoxConnect.Text = "断开";
            lblKmBoxStatus.Text = "已连接";
            lblKmBoxStatus.ForeColor = Color.Green;
        }

        private void DisconnectKmBox()
        {
            if (_kmBox == null) return;

            // 清理顺序关键：必须先停监听线程，否则监听线程在清屏蔽期间会因用户
            // 手指还压着按键（左/右/侧键）而回写 MaskXXX 指令，将刚清的屏蔽位重新置上。
            //
            // 1. 先停监听线程（MonitorDisable 内部 Join(500ms)）
            try { _kmBox.MonitorDisable(); } catch { /* 设备可能已断 */ }

            // 2. 短等 30ms 消化已投递到 UI 线程/事件队列的残留回调
            System.Threading.Thread.Sleep(30);

            // 3. 软件侧左键屏蔽标志清零（_masked = false），避免下次连接后状态残留
            _leftMask?.ReleaseBeforeDisconnect();

            // 4. 一次性兜底解除所有屏蔽位：左/右/中/侧键1/侧键2/XY/滚轮/键盘
            try { _kmBox.UnmaskAll(); } catch { /* 设备可能已断 */ }

            // 5. 断开物理连接
            try { _kmBox.Disconnect(); } catch { }
            try { _kmBox.Dispose(); } catch { }
            _kmBox = null;
            _quickScope = null;
            _rifleSession = null;
            _leftMask = null;
            _blindFirePending = false;   // 断开后清空延迟盲射挂起，避免重连后残留开枪

            btnKmBoxConnect.Text = "连接";
            lblKmBoxStatus.Text = "未连接";
            lblKmBoxStatus.ForeColor = Color.Gray;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 程序关闭时：若 KmBox 仍连接，走 DisconnectKmBox 路径释放侧键/左键屏蔽
            try
            {
                if (_kmBox != null && _kmBox.IsConnected)
                    DisconnectKmBox();
            }
            catch { /* 关闭阶段忽略异常，以免阻碍退出 */ }
        }

        #endregion

        #region KMBOX硬件事件

        private void OnKmBoxMouseButtonChanged(int button, bool isDown)
        {
            // 监听线程职责：仅记录硬件边沿/状态，不发任何 KmBox 指令（指令集中在 YOLO 主线程）

            // === 右键按下 ===
            if (button == 0x02 && isDown)
            {
                // 武器模式判据：当前帧是否步枪准心（即时判定，无时间窗去抖）→ 步枪
                if (_rifleSession != null && _rifleSession.IsRifleModeNow)
                {
                    _rifleSession.OnRightDownRifleMode();
                }
                else
                {
                    // 非步枪模式（含无准心 / 狙击准心待开镜） → 触发瞬狙等待窗口
                    // 只设置 volatile 标志，不发 KmBox 指令；YOLO 主线程 TryHandle 时消费后切 Waiting
                    _quickScope?.OnHwRightDown();
                }
            }

            // === 右键释放 ===
            if (button == 0x02 && !isDown)
            {
                // 解除XY屏蔽 + 结束步枪会话（已知问题：无左键时长补偿，见 ISSUE-012）
                _rifleSession?.OnRightUp();
            }

            // === 左键状态变化 → 上报 LeftMaskController 记录边沿（按下→设代发意图）===
            if (button == 0x01)
            {
                _leftMask?.OnHwLeftEdge(isDown);
            }

            // === 侧键1按下 → 快速切换“步枪打头/打身体” ===
            // 侧键1 全程被屏蔽（不透传游戏），此处消费其按下边沿作为切换热键。
            // 硬件监控线程只提交到 UI 线程翻转复选框，由 chkRifleLockHead_CheckedChanged
            // 统一同步 _rifleLockHead，避免监听线程与 UI 事件双写产生竞态。
            // MonitorThreadProc 已做边缘检测，isDown=true 即一次干净的按下边沿，无需额外去抖。
            if (button == 0x08 && isDown && chkRifleLockHead.IsHandleCreated)
            {
                chkRifleLockHead.BeginInvoke((MethodInvoker)delegate
                {
                    chkRifleLockHead.Checked = !chkRifleLockHead.Checked;
                });
            }
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

        private void chkQuickScopeMode_CheckedChanged(object sender, EventArgs e)
        {
            // UI “真人模式”复选框：
            //   勾选   → _quickScopeMode=true  → QuickScopeController.Enabled=true  → 真人模式（瞬狙+开镜打）
            //   不勾选 → _quickScopeMode=false → QuickScopeController.Enabled=false → 瞬狙模式（开镜即自动代发）
            _quickScopeMode = chkQuickScopeMode.Checked;

            if (_quickScope != null)
            {
                _quickScope.Enabled = _quickScopeMode;
                if (!_quickScopeMode)
                    _quickScope.Reset();
            }
        }

        private void chkMicroAim_CheckedChanged(object sender, EventArgs e)
        {
            _microAimMode = chkMicroAim.Checked;
        }

        private void txtMicroAimExtend_TextChanged(object sender, EventArgs e)
        {
            if (int.TryParse(txtMicroAimExtend.Text, out int extendPx) && extendPx >= 0)
                _microAimExtend = extendPx;
        }

        private void txtBlindFireFrames_TextChanged(object sender, EventArgs e)
        {
            if (int.TryParse(txtBlindFireFrames.Text, out int frames))
                _blindFireDelayFrames = Math.Clamp(frames, 0, 10);
        }

        private void chkAutoSwitchWeapon_CheckedChanged(object sender, EventArgs e)
        {
            _autoSwitchWeapon = chkAutoSwitchWeapon.Checked;
        }

        private void chkRifleLockHead_CheckedChanged(object sender, EventArgs e)
        {
            _rifleLockHead = chkRifleLockHead.Checked;
        }

        #endregion
    }
}
