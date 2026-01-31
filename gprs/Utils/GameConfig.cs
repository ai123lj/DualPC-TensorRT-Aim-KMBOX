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

        #region 点射模式屏蔽区域配置
        /// <summary>
        /// 点射模式下的屏蔽区域（防止误识别枪械UI）
        /// 坐标基于采集画面 (CaptureWidth x CaptureHeight)
        /// </summary>
        public static readonly System.Drawing.Rectangle BurstMaskRect = new(
            CaptureWidth / 2 + 100,    // X: 屏幕中心偏右100px
            CaptureHeight / 2 + 90,    // Y: 屏幕中心偏下90px
            250,                        // Width
            250                         // Height
        );

        /// <summary>
        /// 屏蔽区域填充画刷
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
            Burst = 2,   // 点射模式：移动+点击
            Rifle = 3    // 步枪模式：贝塞尔曲线移动，不开枪
        }


        #endregion

        #region 灵敏度配置
        /// <summary>
        /// 灵敏度配置（实测值，不同游戏/分辨率需要不同配置）
        /// </summary>
        public static class Sensitivity
        {
            // 穿越火线 2K分辨率
            public const int CF_2K_X = 166;
            public const int CF_2K_Y = 166;
            // 注：其他分辨率参考值
            // 2560P: 165/222, 1080P: 124/157, 1080p旧: 83/102

            // 游戏2 (待确认具体游戏名)
            public const int GAME2_X = 215;
            public const int GAME2_Y = 215;
            // 注：2554p: 160/158, 1080p: 215/207

            // 游戏3 (待确认具体游戏名)
            public const int GAME3_X = 120;
            public const int GAME3_Y = 125;
        }
        #endregion
    }
}
