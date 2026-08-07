using System;
using gprs.KmBox;

namespace gprs.Firing
{
    /// <summary>
    /// 左键屏蔽控制器 + 物理左键边沿捕获。
    ///
    /// 职责：
    /// - 狙击模式（!RifleEnabled）全程屏蔽左键：软件代替开枪，用户左键仅为"意图信号"
    /// - KmBox 硬件监听线程通过 OnHwLeftEdge 上报物理左键边沿（不触发开火）
    /// - YOLO 主线程通过 ConsumeManualFireRequest 消费一次意图（边沿触发，按住不连发）
    ///
    /// 线程模型：
    /// - Apply/Consume 只由 YOLO 主线程调用
    /// - OnHwLeftEdge 由 KmBox 监听线程调用，只写入 volatile bool
    /// - 不在监听线程中发起任何 KmBox 指令（指令集中在 YOLO 主线程，避免并发串指令）
    /// </summary>
    public sealed class LeftMaskController
    {
        private readonly KmBoxNet _km;

        // 硬件状态与屏蔽状态由 YOLO 主线程维护
        private bool _masked;

        // 硬件左键上一次的按下状态（监听线程写，YOLO 线程读；用 volatile 保证可见性）
        private volatile bool _hwLeftIsDown;

        // 边沿标志：监听线程检测到"抬起→按下"时置 true，YOLO 线程读取后清零
        // 用 volatile 足够（单 writer / 单 reader，无复合读-改-写）
        private volatile bool _fireRequested;

        public LeftMaskController(KmBoxNet km)
        {
            _km = km ?? throw new ArgumentNullException(nameof(km));
        }

        /// <summary>当前左键是否被软件屏蔽。</summary>
        public bool IsMasked => _masked;

        /// <summary>测试观察用：是否存在未消费的物理左键开火意图（只读）。</summary>
        public bool HasPendingFireRequest => _fireRequested;

        /// <summary>
        /// YOLO 主线程：每帧调用。
        /// shouldMask=true 时持续屏蔽左键；=false 时解除（只在状态变化时发指令）。
        /// </summary>
        public void ApplyMask(bool shouldMask)
        {
            if (shouldMask && !_masked)
            {
                _km.MaskMouseLeft(true);
                _masked = true;
            }
            else if (!shouldMask && _masked)
            {
                _km.MaskMouseLeft(false);
                _masked = false;
            }
        }

        /// <summary>
        /// KmBox 硬件监听线程：上报物理左键状态变化。
        /// 仅记录边沿（抬起→按下），不发任何 KmBox 指令。
        /// </summary>
        public void OnHwLeftEdge(bool isDown)
        {
            bool wasDown = _hwLeftIsDown;
            _hwLeftIsDown = isDown;

            // 只记录"按下"边沿；按住不连发、抬起不触发
            if (isDown && !wasDown && _masked)
                _fireRequested = true;
        }

        /// <summary>
        /// YOLO 主线程：消费一次按下意图，返回 true 表示本帧应代发开火。
        /// </summary>
        public bool ConsumeManualFireRequest()
        {
            if (!_fireRequested) return false;
            _fireRequested = false;
            return true;
        }

        /// <summary>连接/断开：重置全部状态（硬件已 UnmaskAll）。</summary>
        public void Reset()
        {
            _masked = false;
            _hwLeftIsDown = false;
            _fireRequested = false;
        }

        /// <summary>Disconnect 前调用：若仍屏蔽则显式释放，防止设备残留屏蔽状态。</summary>
        public void ReleaseBeforeDisconnect()
        {
            if (_masked)
            {
                try { _km?.MaskMouseLeft(false); } catch { /* 设备可能已断 */ }
                _masked = false;
            }
        }
    }
}
