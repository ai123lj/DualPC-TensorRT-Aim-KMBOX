using System;
using System.Threading;
using gprs.KmBox;

namespace gprs.Firing
{
    /// <summary>
    /// 开火动作集合（无状态，纯动作执行）。
    ///
    /// 设计原则（阶段 3 重构后）：
    /// - 左键屏蔽由 LeftMaskController 跨帧持有，本类不管
    /// - XY 屏蔽由本类内部细粒度管理（只封住 MouseMove 瞬间）
    /// - 不再使用 MaskAll / UnmaskAll，避免粗粒度清零破坏其他模块的屏蔽状态
    /// - 每个方法是一次完整的"开枪 + 清理"原子序列，对外不依赖任何跨帧状态
    /// </summary>
    public static class FireActions
    {
        /// <summary>
        /// 狙击模式开枪：
        /// 屏蔽 XY → 移动到目标 → 解除 XY → 左键按下 → (可选)滚轮切枪 → 左键抬起 → 150ms 拉栓冷却
        ///
        /// 注意：
        /// - 调用方需保证左键已被 LeftMaskController 屏蔽（狙击模式准心期间本就屏蔽）
        /// - 右键在狙击模式不屏蔽，用户按右键换弹/投雷仍生效
        /// - 单次 MouseMove 轴向上限 ±120像素（高速 USB2.0 / KmBox 的经验限制，超过易导致丢帧/键鼠异常）：
        ///   超出按轴向主距分成 N 段，首段 90~115 像素（轻量加速）、中间段 110~120 随机、末段补余；
        ///   段间曾加过 3~8ms Sleep 加强“真人连续移动”指纹，但延迟对性能影响较大，暂时关闭。
        /// </summary>
        public static void SniperFire(KmBoxNet km, int targetX, int targetY,
                                       int xSensitivity, int ySensitivity,
                                       bool autoSwitchWeapon, Random rnd)
        {
            if (km == null || !km.IsConnected) return;
        
            int mouseX = (targetX - GameConfig.CaptureWidth / 2) * xSensitivity / 100;
            int mouseY = (targetY - GameConfig.CaptureHeight / 2) * ySensitivity / 100;
        
            // XY 细粒度屏蔽：只在整个移动期间屏蔽，防止用户手部抖动污染自瞄向量
            km.MaskMouseX(true);
            km.MaskMouseY(true);
        
            // 分段移动：统一走 MoveInSegments（单次 ≤120、随机化 + 轻量加速）
            MoveInSegments(km, mouseX, mouseY, rnd);
        
            km.MaskMouseX(false);
            km.MaskMouseY(false);

            // 开火序列
            km.MouseLeft(true);
            Thread.Sleep(rnd.Next(30, 51));
            if (autoSwitchWeapon)
                km.MouseWheel(-1);
            Thread.Sleep(rnd.Next(50, 100));
            km.MouseLeft(false);

            // 拉栓冷却（狙击单发节奏）
            Thread.Sleep(150);
        }

        /// <summary>
        /// 快速点击（菜单/未持镜点按场景）：
        /// 左键按下 → 随机 30~50ms 按压 → 左键抬起，无移动、无 XY 屏蔽、无拉栓冷却。
        ///
        /// 用途：无准心且无右键时的左键意图代发（典型为游戏菜单点击）。
        /// 与 SniperFire 的区别：不带开火前后的固定等待和 150ms 拉栓冷却，
        /// 避免把完整开枪节奏套到菜单操作上导致响应迟钝；
        /// 按压时长随机化贴近人类点击指纹（游戏会检测键鼠异常操作时间）。
        /// 调用方需保证左键已被 LeftMaskController 屏蔽。
        /// </summary>
        public static void QuickClick(KmBoxNet km, Random rnd)
        {
            if (km == null || !km.IsConnected) return;

            km.MouseLeft(true);
            Thread.Sleep(rnd.Next(80, 151));   // 人类单击按压时长量级，随机避免固定指纹
            km.MouseLeft(false);
        }

        /// <summary>
        /// 分段鼠标移动（公共工具）：单次 MouseMove 主轴绝对值 ≤ 120，
        /// 预防 USB2.0 HID report 个别字节崩、KmBox 固件分包丢帧，也减少“大跳瞬移”的异常指纹。
        /// 首段 90~115 轻量加速起手，后续段 110~120 随机避免固定步长，末段补原始余数保证落点。
        /// 调用者职责：预先完成 XY 屏蔽（如需）和最大移动量拦截（防误识别）；本方法只负责“怎么动”，不负责“要不要动”。
        /// </summary>
        public static void MoveInSegments(KmBoxNet km, int mouseX, int mouseY, Random rnd)
        {
            if (km == null || !km.IsConnected) return;
            if (mouseX == 0 && mouseY == 0) return;

            const int MAX_SEG = 120;
            int maxAbs = Math.Max(Math.Abs(mouseX), Math.Abs(mouseY));

            if (maxAbs <= MAX_SEG)
            {
                km.MouseMove(mouseX, mouseY);
                return;
            }

            int appliedX = 0;
            int appliedY = 0;
            int remainMain = maxAbs;   // 主轴剩余绝对值
            bool firstSeg = true;

            while (remainMain > 0)
            {
                // 首段轻量加速曲线：90~115；后续段满速 110~120 随机。
                // 上限严格 ≤120，rnd.Next(a, b) 不包含 b。
                int segMain = firstSeg ? rnd.Next(90, 116) : rnd.Next(110, 121);
                firstSeg = false;

                if (segMain >= remainMain)
                {
                    // 末段：直接补齐剩余，避免整除累计误差；remainMain < segMain ≤ 120 所以主轴也 ≤120
                    km.MouseMove(mouseX - appliedX, mouseY - appliedY);
                    break;
                }

                // 按比例把 segMain 映射为 XY 增量（主轴绝对值恰为 segMain，从轴 ≤ segMain）
                int newAppliedMain = (maxAbs - remainMain) + segMain;
                int targetX2 = (int)((long)mouseX * newAppliedMain / maxAbs);
                int targetY2 = (int)((long)mouseY * newAppliedMain / maxAbs);
                km.MouseMove(targetX2 - appliedX, targetY2 - appliedY);
                appliedX = targetX2;
                appliedY = targetY2;
                remainMain -= segMain;

                // 段间延时：曾用 Thread.Sleep(rnd.Next(3, 8)) 模拟真人连续指纹，
                // 目前无条件关闭（延迟对性能影响太大，后续若再现异常再评估恢复）
                // if (remainMain > 0) Thread.Sleep(rnd.Next(3, 8));
            }
        }
    }
}
