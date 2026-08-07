using System;
using System.Diagnostics;
using gprs.KmBox;

namespace gprs.Firing
{
    /// <summary>
    /// 防盲狙闸门控制器（状态机）。
    ///
    /// │ UI 对应关系 │
    /// - 类名/字段 QuickScopeController / _quickScope 是历史命名（源自早期“瞬狙干预”），
    ///   实际对应 UI 上 chkQuickScopeMode 复选框的 “真人模式”：
    ///     · UI 勾选 → Enabled=true  → 本控制器工作，等开镜后才放行开火链路
    ///     · UI 未勾   → Enabled=false → 本控制器不介入，Form1 走“瞬狙模式”（开镜即自动代发一枪）
    /// - 即：本类仅在 UI “真人模式”勾选时生效。
    ///
    /// │ 职责（Monitoring 冗余移除后）│
    /// - 早期版本含 Monitoring 状态：开镜后由本类独占帧跑 YOLO、消费左键意图代发。
    ///   该逻辑与 Form1 主循环（step 3~7 真人模式分支）逐行等价（同一 predictor、同一帧、
    ///   同一意图消费语义），属于重复实现，已删除；配套的“监控窗口”（WindowMs）与
    ///   UI「监控(ms)」输入框一并移除，微自瞄判定移入 Form1 主路径全局生效。
    /// - 本类现在只做一件事：右键按下后等待狙击镜打开（防盲狙——避免镜未打开就代发，
    ///   导致子弹完全没有准头）。等待期间独占本帧（不跑 YOLO）、挂起左键意图不消费；
    ///   开镜成功（或 WaitForScopeMs 超时）后立即交还控制权，后续所有开火决策
    ///   （有目标瞄准代发 / 无目标盲射准心）统一由 Form1 常规流程处理。
    ///
    /// 返回值语义：
    /// - Waiting 窗口内 return true：本帧已被独占，Form1 跳过常规流程
    /// - 已放行 / 超时 / 未启用：return false，放行给 Form1 常规代发流程
    /// </summary>
    public sealed class QuickScopeController
    {
        // Waiting：用户在非步枪模式下按下右键后的“等待开镜”窗口（默认 100ms）
        // Released：闸门已通过（开镜成功或等待超时），控制权交回 Form1 常规流程
        private enum State { Idle, Waiting, Released }

        // === 依赖 ===
        private readonly KmBoxNet _km;

        // === 外部可配置 ===
        public bool Enabled { get; set; }

        /// <summary>
        /// 右键按下后等待狙击镜打开的最大时长（ms）。
        /// 期间保持独占本帧，左键意图被 LeftMaskController 挂起；
        /// 超时未开镜 → Released，Form1 fast-path 消费意图盲射。
        /// </summary>
        public int WaitForScopeMs { get; set; } = 100;

        // === 内部状态 ===
        private State _state = State.Idle;
        private long _timestamp;

        /// <summary>
        /// 测试观察用：当前状态机状态名（Idle/Waiting/Released）。
        /// 仅供诊断窗体（TestCrosshairStateForm）轮询读取，只读不影响行为。
        /// </summary>
        public string StateName => _state.ToString();

        // 硬件监听线程 → YOLO 主线程 边沿信号：右键按下待消费
        // 只在 用户按右键开镜（非步枪模式）时被设置，由主线程消费后切到 Waiting
        private volatile bool _pendingRightDown;

        public QuickScopeController(KmBoxNet km)
        {
            _km = km ?? throw new ArgumentNullException(nameof(km));
        }

        /// <summary>
        /// 防盲狙闸门主入口。
        /// 返回 true 表示本帧已被等待窗口独占（Form1 跳过常规流程，不跑 YOLO）。
        /// </summary>
        public bool TryHandle(bool snipeEnabled, bool debugMode)
        {
            if (!Enabled || debugMode)
            {
                Reset();
                return false;
            }

            if (_km == null || !_km.IsConnected)
                return false;

            // 先消费硬件线程递来的右键边沿：非 Waiting 状态都重新进入 Waiting（重置计时）
            if (_pendingRightDown)
            {
                _pendingRightDown = false;
                if (_state != State.Waiting)
                    EnterWaiting();
            }

            // Idle / Released：闸门不介入，放行给 Form1 常规流程
            if (_state != State.Waiting)
                return false;

            // 开镜成功 → 闸门放行，控制权交回 Form1
            if (snipeEnabled)
            {
                _state = State.Released;
                return false;
            }

            // 超时未开镜 → 同样放行，Form1 fast-path 消费挂起的左键意图盲射
            long elapsedMs = (Stopwatch.GetTimestamp() - _timestamp) * 1000 / Stopwatch.Frequency;
            if (elapsedMs >= WaitForScopeMs)
            {
                _state = State.Released;
                return false;
            }

            return true; // 继续等，独占本帧（不跑 YOLO 节省算力）
        }

        /// <summary>
        /// 硬件监听线程：非步枪模式下捕获到右键按下时调用。
        /// 只设置 volatile 标志，不发 KmBox 指令；YOLO 主线程下一帧 TryHandle 时消费。
        /// </summary>
        public void OnHwRightDown()
        {
            if (!Enabled) return;
            _pendingRightDown = true;
        }

        /// <summary>
        /// 重置闸门状态（准心消失或模式关闭时调用）。
        /// 左键屏蔽由 LeftMaskController 独立管理，本方法不处理。
        /// </summary>
        public void Reset()
        {
            _state = State.Idle;
            _pendingRightDown = false;
        }

        // ===================== 私有实现 =====================

        private void EnterWaiting()
        {
            _timestamp = Stopwatch.GetTimestamp();
            _state = State.Waiting;
        }
    }
}
