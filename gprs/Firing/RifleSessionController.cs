using System;
using System.Diagnostics;
using gprs.KmBox;

namespace gprs.Firing
{
    /// <summary>
    /// 步枪会话控制器。
    ///
    /// 行为：
    /// - 右键按下触发会话（首帧先瞄准后开火，后续帧仅辅助瞄准）
    /// - 右键抬起结束会话
    /// - 期间屏蔽 XY 以免手动移动和自瞄冲突
    /// - 步枪准心可见时右键预屏蔽（避免首次按下泄漏到游戏）
    ///
    /// 职责边界：
    /// - 独占步枪会话相关状态字段与常量
    /// - 不知道瞬狙/狙击/点射（那是 Form1 中央调度分支的职责）
    /// - 调用方每帧通过 UpdateRifleCrosshair / ApplyRightPreMask / HandleFrame 调度
    /// </summary>
    public sealed class RifleSessionController
    {
        private const int RIFLE_MOVE_INTERVAL_MS = 150;  // 会话内自瞄移动间隔

        // === 依赖 ===
        private readonly KmBoxNet _km;
        private readonly Action _onFire;   // 每次开火/瞄准移动触发一次，用于 Form1 统计
        private readonly Random _rnd;      // 供 FireActions.MoveInSegments 随机步长使用

        // === 内部状态（外部无法直接读写）===
        private volatile bool _sessionActive;
        private volatile bool _xyMasked;          // XY 是否被屏蔽
        private volatile bool _rightPreMasked;    // 右键预屏蔽
        private long _lastMoveTimestamp;

        // 即时步枪模式标志：由 YOLO 主线程每帧写入当前准心检测结果，供右键硬件事件（监听线程）读取。
        private volatile bool _rifleCrosshairNow;

        /// <summary>会话是否处于活跃状态。</summary>
        public bool IsActive => _sessionActive;

        /// <summary>
        /// 步枪模式判据：当前帧是否检测到步枪准心。
        /// 早期用“最近 50ms 内见过步枪准心”的时间窗做去抖，会导致切狙后 50ms 内按右键
        /// 被误判为步枪而屏蔽掉（狙击镜打不开）；准心检测改进后无需时间兑底，已改为即时判定。
        /// </summary>
        public bool IsRifleModeNow => _rifleCrosshairNow;

        public RifleSessionController(KmBoxNet km, Action onFire, Random rnd)
        {
            _km = km;
            _onFire = onFire;
            _rnd = rnd ?? new Random();
        }

        /// <summary>
        /// ProcessYoloFrame：每帧调用，写入当前步枪准心是否可见。
        /// 右键硬件事件通过 IsRifleModeNow 读取本值判定武器模式。
        /// </summary>
        public void UpdateRifleCrosshair(bool visible)
        {
            _rifleCrosshairNow = visible;
        }

        /// <summary>
        /// ProcessYoloFrame：每帧更新右键预屏蔽。
        /// 步枪准心可见 或 会话活跃 时持续屏蔽，否则解除。
        /// </summary>
        public void ApplyRightPreMask(bool rifleCrosshairVisible)
        {
            bool shouldMask = rifleCrosshairVisible || _sessionActive;
            if (shouldMask && !_rightPreMasked)
            {
                _km.MaskMouseRight(true);
                _rightPreMasked = true;
            }
            else if (!shouldMask && _rightPreMasked)
            {
                _km.MaskMouseRight(false);
                _rightPreMasked = false;
            }
        }

        /// <summary>
        /// ProcessYoloFrame：步枪会话每帧处理（首帧启动 + 后续帧辅助瞄准）。
        /// XY 屏蔽已在右键按下时由 OnRightDownRifleMode 完成，此处不重复处理。
        /// </summary>
        public void HandleFrame(LockResult lockResult, int xSensitivity, int ySensitivity)
        {
            // === 首帧：启动会话 ===
            if (!_sessionActive)
            {
                // 防止孤儿会话：YOLO 推理期间右键可能已释放，此时不应启动新会话
                // 否则左键按下后无人释放，直到下一帧 YOLO 清理，导致连续点射时多余开火
                if (!_km.IsMouseRightDown())
                    return;

                // 有目标：先移动瞄准，再开火（确保第一发命中）
                if (lockResult.HasTarget)
                {
                    int mx = (lockResult.TargetX - GameConfig.CaptureWidth / 2) * xSensitivity / 100;
                    int my = (lockResult.TargetY - GameConfig.CaptureHeight / 2) * ySensitivity / 100;
                    // 最大移动限制（误识别拦截）：步枪背景模糊、识别度差时容易乱锁，大位移直接放弃本帧。
                    // 通过后走 FireActions.MoveInSegments：随机化 + 轻量加速，单次 ≤120，减少异常指纹。
                    if (Math.Abs(mx) < 200 && Math.Abs(my) < 200)
                        FireActions.MoveInSegments(_km, mx, my, _rnd);
                }
                // 瞄准完成（或无目标），软件代发开火
                _km.MouseLeft(true);
                _lastMoveTimestamp = Stopwatch.GetTimestamp();
                _sessionActive = true;
                _onFire?.Invoke();
                return;
            }

            // === 后续帧：仅辅助瞄准，左键已保持 ===
            if (!lockResult.HasTarget)
                return;

            int mouseX = (lockResult.TargetX - GameConfig.CaptureWidth / 2) * xSensitivity / 100;
            int mouseY = (lockResult.TargetY - GameConfig.CaptureHeight / 2) * ySensitivity / 100;
            // 最大移动限制（误识别拦截）：超过则放弃本帧辅助瞄准，避免“图像模糊、识别乱锁 → 鼠标乱飞”的显异常行为。
            if (Math.Abs(mouseX) >= 200 || Math.Abs(mouseY) >= 200)
                return;

            bool shouldMove = (Stopwatch.GetTimestamp() - _lastMoveTimestamp) * 1000 / Stopwatch.Frequency >= RIFLE_MOVE_INTERVAL_MS;
            if (!shouldMove)
                return;

            // 分段移动：与狙击一致，随机化 + 轻量加速，单次 ≤120。
            FireActions.MoveInSegments(_km, mouseX, mouseY, _rnd);
            _lastMoveTimestamp = Stopwatch.GetTimestamp();
            _onFire?.Invoke();
        }

        /// <summary>结束会话：释放软件左键。</summary>
        public void End()
        {
            if (_sessionActive)
            {
                _km?.MouseLeft(false);
                _sessionActive = false;
            }
        }

        /// <summary>硬件事件：右键按下 + 当前为步枪模式 → 屏蔽 XY（作为预屏蔽的安全网）。</summary>
        public void OnRightDownRifleMode()
        {
            if (!_rightPreMasked)
            {
                _km.MaskMouseRight(true);
                _rightPreMasked = true;
            }
            _km.MaskMouseX(true);
            _km.MaskMouseY(true);
            _xyMasked = true;
        }

        /// <summary>硬件事件：右键抬起 → 解除 XY 屏蔽 + 结束会话。</summary>
        public void OnRightUp()
        {
            if (_xyMasked)
            {
                _km.MaskMouseX(false);
                _km.MaskMouseY(false);
                _xyMasked = false;
            }
            End();
        }

        /// <summary>连接成功：重置预屏蔽标志（此时底层 UnmaskAll 已执行）。</summary>
        public void ResetOnConnect()
        {
            _rightPreMasked = false;
        }
    }
}
