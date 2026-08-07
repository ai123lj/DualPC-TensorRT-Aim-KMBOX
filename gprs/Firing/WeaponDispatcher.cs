using gprs.KmBox;

namespace gprs.Firing
{
    /// <summary>
    /// 武器调度决策结果。WeaponDispatcher.Decide 的返回值。
    /// </summary>
    public readonly struct FireDecision
    {
        public FireAction Action { get; }
        public int FireMode { get; }
        public bool LockHead { get; }
        public bool RenderMask { get; }

        private FireDecision(FireAction action, int fireMode, bool lockHead, bool renderMask)
        {
            Action = action;
            FireMode = fireMode;
            LockHead = lockHead;
            RenderMask = renderMask;
        }

        public static FireDecision Proceed(int fireMode, bool lockHead, bool renderMask) =>
            new FireDecision(FireAction.Proceed, fireMode, lockHead, renderMask);

        public static FireDecision Skip() => new FireDecision(FireAction.Skip, 0, false, false);

        public static FireDecision EndRifleSession() => new FireDecision(FireAction.EndRifleSession, 0, false, false);
    }

    public enum FireAction
    {
        Skip,
        Proceed,
        EndRifleSession,
    }

    /// <summary>
    /// 无状态武器调度器。
    ///
    /// 规则（基于当前帧准心检测结果，无去抖）：
    /// - 步枪会话活跃（右键仍按）→ 保持 Rifle 模式
    /// - 步枪会话活跃（右键已抬）→ 结束会话
    /// - RifleEnabled + 右键按下 → 启动步枪会话
    /// - RifleEnabled 其他 → Skip（步枪备战，不介入）
    /// - !RifleEnabled（狙击模式，包括开镜与未开镜）→ Sniper 自动 + 代发兜底
    /// </summary>
    public static class WeaponDispatcher
    {
        public static FireDecision Decide(
            bool rifleEnabled,
            KmBoxNet kmBox,
            RifleSessionController rifleSession,
            bool rifleLockHead,
            bool debugMode)
        {
            // Debug：固定 Sniper + 画遮罩（便于观察 YOLO 输入）
            if (debugMode)
                return FireDecision.Proceed((int)GameConfig.FireMode.Sniper, lockHead: false, renderMask: true);

            if (kmBox == null || !kmBox.IsConnected)
                return FireDecision.Skip();

            // 活跃步枪会话优先：右键仍按着 → 保持 Rifle；已释放 → 结束
            if (rifleSession?.IsActive == true)
            {
                if (kmBox.IsMouseRightDown())
                    return FireDecision.Proceed((int)GameConfig.FireMode.Rifle, rifleLockHead, renderMask: true);
                return FireDecision.EndRifleSession();
            }

            // 步枪模式（含手雷/小刀）
            if (rifleEnabled)
            {
                // 右键按下 → 启动步枪会话
                if (kmBox.IsMouseRightDown())
                    return FireDecision.Proceed((int)GameConfig.FireMode.Rifle, rifleLockHead, renderMask: true);

                // 步枪备战：不介入（但左/右键屏蔽由外层驱动保持）
                return FireDecision.Skip();
            }

            // 狙击模式（!RifleEnabled）：统一走 Sniper 路径，Form1 内做"自动 or 代发"细分
            // 锁身体，不画遮罩（狙击画面干净）
            return FireDecision.Proceed((int)GameConfig.FireMode.Sniper, lockHead: false, renderMask: false);
        }
    }
}
