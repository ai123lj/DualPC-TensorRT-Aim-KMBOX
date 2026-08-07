namespace gprs
{
    /// <summary>
    /// 游戏配置管理 - 集中管理所有配置参数
    /// </summary>
    public static class GameConfig
    {
        #region 图像采集配置
        public const int CaptureWidth = 640;
        public const int CaptureHeight = 640;
        #endregion

        #region YOLO 推理 UI 遮挡区域配置
        /// <summary>
        /// YOLO 推理前需遮挡的游戏内 UI 区域（防止误识别枪械/技能 UI）
        /// 坐标基于采集画面 (CaptureWidth x CaptureHeight)
        /// </summary>
        public static readonly System.Drawing.Rectangle UiMaskRect = new(
            CaptureWidth / 2 + 200,    // X: 屏幕中心偏右100px
            CaptureHeight / 2 + 50,    // Y: 屏幕中心偏下90px
            250,                        // Width
            280                         // Height
        );

        /// <summary>
        /// 遮挡区域填充画刷
        /// </summary>
        public static readonly System.Drawing.SolidBrush MaskBrush = new(System.Drawing.Color.Black);
        #endregion

        #region 射击模式定义
        /// <summary>
        /// 射击模式枚举
        /// </summary>
        public enum FireMode
        {
            Sniper = 1,  // 狙击模式：直接移动+开枪，200ms冷却
            Rifle = 3    // 步枪模式：贝塞尔曲线移动，不开枪
        }

        #endregion

        #region 灵敏度配置
        /// <summary>
        /// 灵敏度配置（实测值；程序已特化为 CF 专用，固定使用 CF 2K）
        /// </summary>
        public static class Sensitivity
        {
            // 穿越火线 2K分辨率
            public const int CF_2K_X = 170;
            public const int CF_2K_Y = 170;
            // 注：CF 其他分辨率参考值
            // 2560P: 165/222, 1080P: 124/157, 1080p旧: 83/102
        }
        #endregion
    }
}
