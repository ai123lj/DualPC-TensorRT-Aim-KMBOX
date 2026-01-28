using System;
using System.Drawing;

namespace gprs
{
    #region 数据结构定义
    /// <summary>
    /// 部位信息（轻量结构体，避免GC）
    /// </summary>
    public struct PartInfo
    {
        public bool IsValid;       // 是否可用
        public int X, Y;           // 坐标
        public float Confidence;   // 置信度

        public static PartInfo Invalid => new() { IsValid = false };
    }

    /// <summary>
    /// 部位索引常量（共23个部位）
    /// </summary>
    public static class PartIndex
    {
        // === 原始姿态点 (0-16) ===
        public const int NOSE = 0;
        public const int LEFT_EYE = 1;
        public const int RIGHT_EYE = 2;
        public const int LEFT_EAR = 3;
        public const int RIGHT_EAR = 4;
        public const int LEFT_SHOULDER = 5;
        public const int RIGHT_SHOULDER = 6;
        public const int LEFT_ELBOW = 7;
        public const int RIGHT_ELBOW = 8;
        public const int LEFT_WRIST = 9;
        public const int RIGHT_WRIST = 10;
        public const int LEFT_HIP = 11;
        public const int RIGHT_HIP = 12;
        public const int LEFT_KNEE = 13;
        public const int RIGHT_KNEE = 14;
        public const int LEFT_ANKLE = 15;
        public const int RIGHT_ANKLE = 16;

        // === 组合部位 (17-21) ===
        public const int FOREHEAD_1 = 17;      // 额头1：X双耳中间，Y=(鼻子+框顶)/2
        public const int FOREHEAD_2 = 18;      // 额头2：X双耳中间，Y=(双耳+框顶)/2 偏框顶
        public const int SHOULDER_CENTER = 19; // 双肩中点
        public const int CHEST = 20;           // 胸（肩髋之间）
        public const int HIP_CENTER = 21;      // 双髋中点

        // === 兜底 (22) ===
        public const int BOX_CENTER = 22;      // 框中心

        public const int COUNT = 23;
    }

    /// <summary>
    /// 锁定结果
    /// </summary>
    public class LockResult
    {
        public bool HasTarget;
        public int TargetX, TargetY;

        // 调试信息
        public int SelectedTargetId;
        public int SelectedPart;
        public PartInfo[] Parts;
        public string[] SkipReasons;
    }
    #endregion

    /// <summary>
    /// 目标锁定选择器
    /// </summary>
    public static class TargetSelector
    {
        #region COCO关键点索引
        private const int KP_NOSE = 0;
        private const int KP_LEFT_EYE = 1;
        private const int KP_RIGHT_EYE = 2;
        private const int KP_LEFT_EAR = 3;
        private const int KP_RIGHT_EAR = 4;
        private const int KP_LEFT_SHOULDER = 5;
        private const int KP_RIGHT_SHOULDER = 6;
        private const int KP_LEFT_ELBOW = 7;
        private const int KP_RIGHT_ELBOW = 8;
        private const int KP_LEFT_WRIST = 9;
        private const int KP_RIGHT_WRIST = 10;
        private const int KP_LEFT_HIP = 11;
        private const int KP_RIGHT_HIP = 12;
        private const int KP_LEFT_KNEE = 13;
        private const int KP_RIGHT_KNEE = 14;
        private const int KP_LEFT_ANKLE = 15;
        private const int KP_RIGHT_ANKLE = 16;
        #endregion

        #region 优先级表
        /// <summary>
        /// 锁头优先级（从头到脚）
        /// </summary>
        public static readonly int[] HeadFallbackOrder =
        {
            PartIndex.FOREHEAD_1, PartIndex.FOREHEAD_2, PartIndex.NOSE,
            PartIndex.LEFT_EYE, PartIndex.RIGHT_EYE, PartIndex.LEFT_EAR, PartIndex.RIGHT_EAR,
            PartIndex.SHOULDER_CENTER, PartIndex.LEFT_SHOULDER, PartIndex.RIGHT_SHOULDER,
            PartIndex.CHEST, PartIndex.HIP_CENTER, PartIndex.LEFT_HIP, PartIndex.RIGHT_HIP,
            PartIndex.LEFT_KNEE, PartIndex.RIGHT_KNEE, PartIndex.LEFT_ANKLE, PartIndex.RIGHT_ANKLE,
            PartIndex.LEFT_ELBOW, PartIndex.RIGHT_ELBOW, PartIndex.LEFT_WRIST, PartIndex.RIGHT_WRIST,
            PartIndex.BOX_CENTER
        };

        /// <summary>
        /// 锁身体优先级（躯干优先）
        /// </summary>
        public static readonly int[] BodyFallbackOrder =
        {
            PartIndex.CHEST, PartIndex.SHOULDER_CENTER, PartIndex.HIP_CENTER,
            PartIndex.LEFT_SHOULDER, PartIndex.RIGHT_SHOULDER, PartIndex.LEFT_HIP, PartIndex.RIGHT_HIP,
            PartIndex.FOREHEAD_1, PartIndex.FOREHEAD_2, PartIndex.NOSE,
            PartIndex.LEFT_EAR, PartIndex.RIGHT_EAR, PartIndex.LEFT_EYE, PartIndex.RIGHT_EYE,
            PartIndex.LEFT_KNEE, PartIndex.RIGHT_KNEE, PartIndex.LEFT_ANKLE, PartIndex.RIGHT_ANKLE,
            PartIndex.LEFT_ELBOW, PartIndex.RIGHT_ELBOW, PartIndex.LEFT_WRIST, PartIndex.RIGHT_WRIST,
            PartIndex.BOX_CENTER
        };
        #endregion

        #region 对象复用缓存
        private const int MAX_TARGETS = 16;

        private static readonly LockResult _lockResultCache = new()
        {
            Parts = new PartInfo[PartIndex.COUNT]
        };
        private static readonly PartInfo[][] _partsCache;
        private static readonly string[] _skipReasonsCache = new string[MAX_TARGETS];

        static TargetSelector()
        {
            _partsCache = new PartInfo[MAX_TARGETS][];
            for (int i = 0; i < MAX_TARGETS; i++)
                _partsCache[i] = new PartInfo[PartIndex.COUNT];
        }
        #endregion

        #region 核心入口
        /// <summary>
        /// 处理YOLO推理结果，返回最终锁定坐标
        /// </summary>
        /// <param name="result">YOLO推理结果</param>
        /// <param name="lockHead">true=锁头，false=锁身体</param>
        /// <param name="captureWidth">画面宽度</param>
        /// <param name="captureHeight">画面高度</param>
        /// <param name="debugMode">是否收集调试信息</param>
        public static LockResult ProcessTargets(
            YoloResult<Pose> result,
            bool lockHead,
            int captureWidth,
            int captureHeight,
            bool debugMode = false)
        {
            var lockResult = _lockResultCache;
            lockResult.HasTarget = false;
            lockResult.SelectedTargetId = -1;
            lockResult.SelectedPart = -1;
            lockResult.Parts = null;

            if (debugMode)
            {
                int count = Math.Min(result.Count, MAX_TARGETS);
                for (int i = 0; i < count; i++)
                    _skipReasonsCache[i] = null;
                lockResult.SkipReasons = _skipReasonsCache;
            }
            else
            {
                lockResult.SkipReasons = null;
            }

            if (result.Count == 0)
                return lockResult;

            float centerX = captureWidth / 2f;
            float centerY = captureHeight / 2f;
            float minDistanceSq = float.MaxValue;
            int targetCount = Math.Min(result.Count, MAX_TARGETS);

            // === 步骤1+2: 遍历目标，排除不可信，选最近的 ===
            for (int i = 0; i < targetCount; i++)
            {
                var pose = result[i];
                var bounds = pose.Bounds;

                // 排除：整体置信度过低
                string rejectReason = CheckTargetValidity(pose);
                if (rejectReason != null)
                {
                    if (debugMode) lockResult.SkipReasons[i] = rejectReason;
                    continue;
                }

                // 排除：异常姿势（尸体）
                rejectReason = DetectCorpse(pose, bounds);
                if (rejectReason != null)
                {
                    if (debugMode) lockResult.SkipReasons[i] = rejectReason;
                    continue;
                }

                // 计算框中心到画面中心的距离
                float boxCenterX = bounds.X + bounds.Width / 2f;
                float boxCenterY = bounds.Y + bounds.Height / 2f;
                float diffX = boxCenterX - centerX;
                float diffY = boxCenterY - centerY;
                float distSq = diffX * diffX + diffY * diffY;

                if (distSq < minDistanceSq)
                {
                    minDistanceSq = distSq;
                    lockResult.SelectedTargetId = i;
                }
            }

            if (lockResult.SelectedTargetId < 0)
                return lockResult;

            // === 步骤3: 收集选中目标的所有部位 ===
            var selectedPose = result[lockResult.SelectedTargetId];
            var selectedBounds = selectedPose.Bounds;
            var parts = _partsCache[lockResult.SelectedTargetId];

            CalculateAllParts(selectedPose, selectedBounds, parts);
            lockResult.Parts = parts;

            // === 步骤4: 按优先级选择部位 ===
            var fallbackOrder = lockHead ? HeadFallbackOrder : BodyFallbackOrder;

            foreach (int partIndex in fallbackOrder)
            {
                if (parts[partIndex].IsValid)
                {
                    lockResult.SelectedPart = partIndex;
                    lockResult.TargetX = parts[partIndex].X;
                    lockResult.TargetY = parts[partIndex].Y;
                    lockResult.HasTarget = true;
                    break;
                }
            }

            return lockResult;
        }
        #endregion

        #region 目标有效性检查
        /// <summary>
        /// 检查目标整体有效性（排除墙/物体等误识别）
        /// </summary>
        private static string CheckTargetValidity(Pose pose)
        {
            // === 阈值：整体有效性判断（每个姿态点独立阈值） ===
            const float TH_NOSE = 0.6f;             // 鼻子有效性阈值
            const float TH_LEFT_EYE = 0.6f;         // 左眼有效性阈值
            const float TH_RIGHT_EYE = 0.6f;        // 右眼有效性阈值
            const float TH_LEFT_EAR = 0.6f;         // 左耳有效性阈值
            const float TH_RIGHT_EAR = 0.6f;        // 右耳有效性阈值
            const float TH_LEFT_SHOULDER = 0.7f;    // 左肩有效性阈值
            const float TH_RIGHT_SHOULDER = 0.7f;   // 右肩有效性阈值
            const float TH_LEFT_ELBOW = 0.7f;       // 左肘有效性阈值
            const float TH_RIGHT_ELBOW = 0.7f;      // 右肘有效性阈值
            const float TH_LEFT_WRIST = 0.7f;       // 左腕有效性阈值
            const float TH_RIGHT_WRIST = 0.7f;      // 右腕有效性阈值
            const float TH_LEFT_HIP = 0.7f;         // 左髋有效性阈值
            const float TH_RIGHT_HIP = 0.7f;        // 右髋有效性阈值
            const float TH_LEFT_KNEE = 0.7f;        // 左膝有效性阈值
            const float TH_RIGHT_KNEE = 0.7f;       // 右膝有效性阈值
            const float TH_LEFT_ANKLE = 0.7f;       // 左踝有效性阈值
            const float TH_RIGHT_ANKLE = 0.7f;      // 右踝有效性阈值
            const int TH_MIN_VALID_COUNT = 3;       // 至少要有N个关键点超过各自阈值

            // 阈值数组（与 COCO 关键点索引对应）
            // stackalloc 在栈上分配，零 GC 压力，保持阈值在函数内方便修改
            ReadOnlySpan<float> thresholds = stackalloc float[17]
            {
                TH_NOSE, TH_LEFT_EYE, TH_RIGHT_EYE, TH_LEFT_EAR, TH_RIGHT_EAR,
                TH_LEFT_SHOULDER, TH_RIGHT_SHOULDER, TH_LEFT_ELBOW, TH_RIGHT_ELBOW,
                TH_LEFT_WRIST, TH_RIGHT_WRIST, TH_LEFT_HIP, TH_RIGHT_HIP,
                TH_LEFT_KNEE, TH_RIGHT_KNEE, TH_LEFT_ANKLE, TH_RIGHT_ANKLE
            };

            int validCount = 0;
            for (int i = 0; i < 17; i++)
            {
                if (pose[i].Confidence > thresholds[i])
                    validCount++;
            }

            if (validCount < TH_MIN_VALID_COUNT)
                return $"置信点不足:{validCount}";

            return null;
        }
        #endregion

        #region 尸体/异常姿态检测
        /// <summary>
        /// 检测是否为尸体或异常姿态
        /// </summary>
        private static string DetectCorpse(Pose pose, Rectangle bounds)
        {
            // === 阈值：尸体检测 ===
            const float TH_SHOULDER = 0.7f;         // 肩膀置信度阈值
            const float TH_EAR = 0.6f;              // 耳朵置信度阈值
            const float TH_HIP = 0.7f;              // 髋部置信度阈值
            const float TH_ANKLE = 0.7f;            // 脚踝置信度阈值
            const float TH_ASPECT_RATIO = 0.7f;     // 检测框宽高比阈值（低于此值判定为躺倒）

            float aspectRatio = (float)bounds.Height / bounds.Width;

            // 宽高比检测：躺倒的尸体宽高比接近1或更小
            if (aspectRatio < TH_ASPECT_RATIO)
            {
                bool hasUpper = pose[KP_LEFT_SHOULDER].Confidence > TH_SHOULDER ||
                               pose[KP_RIGHT_SHOULDER].Confidence > TH_SHOULDER;
                bool hasLower = pose[KP_LEFT_HIP].Confidence > TH_HIP ||
                               pose[KP_RIGHT_HIP].Confidence > TH_HIP;
                if (hasUpper && hasLower)
                    return $"尸体:宽高比{aspectRatio:F2}";
            }

            // 头在肩下检测（倒立/趴着）
            if (pose[KP_LEFT_SHOULDER].Confidence > TH_SHOULDER &&
                pose[KP_RIGHT_SHOULDER].Confidence > TH_SHOULDER &&
                pose[KP_LEFT_EAR].Confidence > TH_EAR &&
                pose[KP_RIGHT_EAR].Confidence > TH_EAR)
            {
                float shouldersY = (pose[KP_LEFT_SHOULDER].Point.Y + pose[KP_RIGHT_SHOULDER].Point.Y) / 2f;
                float earsY = (pose[KP_LEFT_EAR].Point.Y + pose[KP_RIGHT_EAR].Point.Y) / 2f;
                if (earsY > shouldersY)
                    return "尸体:头在肩下";
            }

            // 髋在脚上检测（倒立）
            if (pose[KP_LEFT_ANKLE].Confidence > TH_ANKLE &&
                pose[KP_RIGHT_ANKLE].Confidence > TH_ANKLE &&
                pose[KP_LEFT_HIP].Confidence > TH_HIP &&
                pose[KP_RIGHT_HIP].Confidence > TH_HIP)
            {
                float hipsY = (pose[KP_LEFT_HIP].Point.Y + pose[KP_RIGHT_HIP].Point.Y) / 2f;
                float anklesY = (pose[KP_LEFT_ANKLE].Point.Y + pose[KP_RIGHT_ANKLE].Point.Y) / 2f;
                if (hipsY > anklesY)
                    return "尸体:髋在脚下";
            }

            return null;
        }
        #endregion

        #region 部位计算
        /// <summary>
        /// 计算所有部位坐标和可用性
        /// </summary>
        private static void CalculateAllParts(Pose pose, Rectangle bounds, PartInfo[] parts)
        {
            // 清空所有部位
            for (int i = 0; i < PartIndex.COUNT; i++)
                parts[i] = default;

            // ==================== 原始姿态点 ====================

            // === 鼻子 ===
            {
                const float TH = 0.6f;  // 鼻子可用阈值
                if (pose[KP_NOSE].Confidence > TH)
                    parts[PartIndex.NOSE] = CreatePart(pose[KP_NOSE]);
            }

            // === 左眼 ===
            {
                const float TH = 0.6f;  // 左眼可用阈值
                if (pose[KP_LEFT_EYE].Confidence > TH)
                    parts[PartIndex.LEFT_EYE] = CreatePart(pose[KP_LEFT_EYE]);
            }

            // === 右眼 ===
            {
                const float TH = 0.6f;  // 右眼可用阈值
                if (pose[KP_RIGHT_EYE].Confidence > TH)
                    parts[PartIndex.RIGHT_EYE] = CreatePart(pose[KP_RIGHT_EYE]);
            }

            // === 左耳 ===
            {
                const float TH = 0.6f;  // 左耳可用阈值
                if (pose[KP_LEFT_EAR].Confidence > TH)
                    parts[PartIndex.LEFT_EAR] = CreatePart(pose[KP_LEFT_EAR]);
            }

            // === 右耳 ===
            {
                const float TH = 0.6f;  // 右耳可用阈值
                if (pose[KP_RIGHT_EAR].Confidence > TH)
                    parts[PartIndex.RIGHT_EAR] = CreatePart(pose[KP_RIGHT_EAR]);
            }

            // === 左肩 ===
            {
                const float TH = 0.7f;  // 左肩可用阈值
                if (pose[KP_LEFT_SHOULDER].Confidence > TH)
                    parts[PartIndex.LEFT_SHOULDER] = CreatePart(pose[KP_LEFT_SHOULDER]);
            }

            // === 右肩 ===
            {
                const float TH = 0.7f;  // 右肩可用阈值
                if (pose[KP_RIGHT_SHOULDER].Confidence > TH)
                    parts[PartIndex.RIGHT_SHOULDER] = CreatePart(pose[KP_RIGHT_SHOULDER]);
            }

            // === 左手肘 ===
            {
                const float TH = 0.7f;  // 左肘可用阈值
                if (pose[KP_LEFT_ELBOW].Confidence > TH)
                    parts[PartIndex.LEFT_ELBOW] = CreatePart(pose[KP_LEFT_ELBOW]);
            }

            // === 右肘 ===
            {
                const float TH = 0.7f;  // 右肘可用阈值
                if (pose[KP_RIGHT_ELBOW].Confidence > TH)
                    parts[PartIndex.RIGHT_ELBOW] = CreatePart(pose[KP_RIGHT_ELBOW]);
            }

            // === 左手腕 ===
            {
                const float TH = 0.7f;  // 左腕可用阈值
                if (pose[KP_LEFT_WRIST].Confidence > TH)
                    parts[PartIndex.LEFT_WRIST] = CreatePart(pose[KP_LEFT_WRIST]);
            }

            // === 右腕 ===
            {
                const float TH = 0.7f;  // 右腕可用阈值
                if (pose[KP_RIGHT_WRIST].Confidence > TH)
                    parts[PartIndex.RIGHT_WRIST] = CreatePart(pose[KP_RIGHT_WRIST]);
            }

            // === 左髋 ===
            {
                const float TH = 0.7f;  // 左髋可用阈值
                if (pose[KP_LEFT_HIP].Confidence > TH)
                    parts[PartIndex.LEFT_HIP] = CreatePart(pose[KP_LEFT_HIP]);
            }

            // === 右髋 ===
            {
                const float TH = 0.7f;  // 右髋可用阈值
                if (pose[KP_RIGHT_HIP].Confidence > TH)
                    parts[PartIndex.RIGHT_HIP] = CreatePart(pose[KP_RIGHT_HIP]);
            }

            // === 左膝 ===
            {
                const float TH = 0.7f;  // 左膝可用阈值
                if (pose[KP_LEFT_KNEE].Confidence > TH)
                    parts[PartIndex.LEFT_KNEE] = CreatePart(pose[KP_LEFT_KNEE]);
            }

            // === 右膝 ===
            {
                const float TH = 0.7f;  // 右膝可用阈值
                if (pose[KP_RIGHT_KNEE].Confidence > TH)
                    parts[PartIndex.RIGHT_KNEE] = CreatePart(pose[KP_RIGHT_KNEE]);
            }

            // === 左脚踝 ===
            {
                const float TH = 0.7f;  // 左踝可用阈值
                if (pose[KP_LEFT_ANKLE].Confidence > TH)
                    parts[PartIndex.LEFT_ANKLE] = CreatePart(pose[KP_LEFT_ANKLE]);
            }

            // === 右脚踝 ===
            {
                const float TH = 0.7f;  // 右踝可用阈值
                if (pose[KP_RIGHT_ANKLE].Confidence > TH)
                    parts[PartIndex.RIGHT_ANKLE] = CreatePart(pose[KP_RIGHT_ANKLE]);
            }

            // ==================== 组合部位 ====================

            // === 额头1：X=双耳中间，Y=(鼻子Y + 框顶Y)/2 ===
            {
                const float TH_LEFT_EAR = 0.6f;   // 额头1 - 左耳阈值
                const float TH_RIGHT_EAR = 0.6f;  // 额头1 - 右耳阈值
                const float TH_NOSE = 0.6f;       // 额头1 - 鼻子阈值

                if (pose[KP_LEFT_EAR].Confidence > TH_LEFT_EAR &&
                    pose[KP_RIGHT_EAR].Confidence > TH_RIGHT_EAR &&
                    pose[KP_NOSE].Confidence > TH_NOSE)
                {
                    int x = (pose[KP_LEFT_EAR].Point.X + pose[KP_RIGHT_EAR].Point.X) / 2;
                    int y = (pose[KP_NOSE].Point.Y + bounds.Y) / 2;
                    float conf = (pose[KP_LEFT_EAR].Confidence + pose[KP_RIGHT_EAR].Confidence + pose[KP_NOSE].Confidence) / 3;
                    parts[PartIndex.FOREHEAD_1] = new PartInfo { IsValid = true, X = x, Y = y, Confidence = conf };
                }
            }

            // === 额头2：X=双耳中间，Y=(双耳Y + 框顶Y)/2，偏向框顶 ===
            {
                const float TH_LEFT_EAR = 0.6f;   // 额头2 - 左耳阈值
                const float TH_RIGHT_EAR = 0.6f;  // 额头2 - 右耳阈值

                if (pose[KP_LEFT_EAR].Confidence > TH_LEFT_EAR &&
                    pose[KP_RIGHT_EAR].Confidence > TH_RIGHT_EAR)
                {
                    int x = (pose[KP_LEFT_EAR].Point.X + pose[KP_RIGHT_EAR].Point.X) / 2;
                    int earsY = (pose[KP_LEFT_EAR].Point.Y + pose[KP_RIGHT_EAR].Point.Y) / 2;
                    int y = (earsY + bounds.Y * 2) / 3;  // 偏向框顶
                    float conf = (pose[KP_LEFT_EAR].Confidence + pose[KP_RIGHT_EAR].Confidence) / 2;
                    parts[PartIndex.FOREHEAD_2] = new PartInfo { IsValid = true, X = x, Y = y, Confidence = conf };
                }
            }

            // === 双肩中点 ===
            {
                const float TH_LEFT_SHOULDER = 0.7f;   // 双肩中点 - 左肩阈值
                const float TH_RIGHT_SHOULDER = 0.7f;  // 双肩中点 - 右肩阈值

                if (pose[KP_LEFT_SHOULDER].Confidence > TH_LEFT_SHOULDER &&
                    pose[KP_RIGHT_SHOULDER].Confidence > TH_RIGHT_SHOULDER)
                {
                    int x = (pose[KP_LEFT_SHOULDER].Point.X + pose[KP_RIGHT_SHOULDER].Point.X) / 2;
                    int y = (pose[KP_LEFT_SHOULDER].Point.Y + pose[KP_RIGHT_SHOULDER].Point.Y) / 2;
                    float conf = (pose[KP_LEFT_SHOULDER].Confidence + pose[KP_RIGHT_SHOULDER].Confidence) / 2;
                    parts[PartIndex.SHOULDER_CENTER] = new PartInfo { IsValid = true, X = x, Y = y, Confidence = conf };
                }
            }

            // === 胸（肩髋之间，偏向肩） ===
            {
                const float TH_LEFT_SHOULDER = 0.7f;   // 胸 - 左肩阈值
                const float TH_RIGHT_SHOULDER = 0.7f;  // 胸 - 右肩阈值
                const float TH_LEFT_HIP = 0.7f;        // 胸 - 左髋阈值
                const float TH_RIGHT_HIP = 0.7f;       // 胸 - 右髋阈值

                bool hasLS = pose[KP_LEFT_SHOULDER].Confidence > TH_LEFT_SHOULDER;
                bool hasRS = pose[KP_RIGHT_SHOULDER].Confidence > TH_RIGHT_SHOULDER;
                bool hasLH = pose[KP_LEFT_HIP].Confidence > TH_LEFT_HIP;
                bool hasRH = pose[KP_RIGHT_HIP].Confidence > TH_RIGHT_HIP;

                if ((hasLS || hasRS) && (hasLH || hasRH))
                {
                    // 计算肩中心
                    float sx, sy, sc;
                    if (hasLS && hasRS)
                    {
                        sx = (pose[KP_LEFT_SHOULDER].Point.X + pose[KP_RIGHT_SHOULDER].Point.X) / 2f;
                        sy = (pose[KP_LEFT_SHOULDER].Point.Y + pose[KP_RIGHT_SHOULDER].Point.Y) / 2f;
                        sc = (pose[KP_LEFT_SHOULDER].Confidence + pose[KP_RIGHT_SHOULDER].Confidence) / 2;
                    }
                    else if (hasLS)
                    {
                        sx = pose[KP_LEFT_SHOULDER].Point.X;
                        sy = pose[KP_LEFT_SHOULDER].Point.Y;
                        sc = pose[KP_LEFT_SHOULDER].Confidence;
                    }
                    else
                    {
                        sx = pose[KP_RIGHT_SHOULDER].Point.X;
                        sy = pose[KP_RIGHT_SHOULDER].Point.Y;
                        sc = pose[KP_RIGHT_SHOULDER].Confidence;
                    }

                    // 计算髋中心
                    float hx, hy, hc;
                    if (hasLH && hasRH)
                    {
                        hx = (pose[KP_LEFT_HIP].Point.X + pose[KP_RIGHT_HIP].Point.X) / 2f;
                        hy = (pose[KP_LEFT_HIP].Point.Y + pose[KP_RIGHT_HIP].Point.Y) / 2f;
                        hc = (pose[KP_LEFT_HIP].Confidence + pose[KP_RIGHT_HIP].Confidence) / 2;
                    }
                    else if (hasLH)
                    {
                        hx = pose[KP_LEFT_HIP].Point.X;
                        hy = pose[KP_LEFT_HIP].Point.Y;
                        hc = pose[KP_LEFT_HIP].Confidence;
                    }
                    else
                    {
                        hx = pose[KP_RIGHT_HIP].Point.X;
                        hy = pose[KP_RIGHT_HIP].Point.Y;
                        hc = pose[KP_RIGHT_HIP].Confidence;
                    }

                    // 胸在肩髋之间，偏向肩 (2:1)
                    int x = (int)((sx * 2 + hx) / 3);
                    int y = (int)((sy * 2 + hy) / 3);
                    parts[PartIndex.CHEST] = new PartInfo { IsValid = true, X = x, Y = y, Confidence = Math.Min(sc, hc) };
                }
            }

            // === 双髋中点 ===
            {
                const float TH_LEFT_HIP = 0.7f;   // 双髋中点 - 左髋阈值
                const float TH_RIGHT_HIP = 0.7f;  // 双髋中点 - 右髋阈值

                if (pose[KP_LEFT_HIP].Confidence > TH_LEFT_HIP &&
                    pose[KP_RIGHT_HIP].Confidence > TH_RIGHT_HIP)
                {
                    int x = (pose[KP_LEFT_HIP].Point.X + pose[KP_RIGHT_HIP].Point.X) / 2;
                    int y = (pose[KP_LEFT_HIP].Point.Y + pose[KP_RIGHT_HIP].Point.Y) / 2;
                    float conf = (pose[KP_LEFT_HIP].Confidence + pose[KP_RIGHT_HIP].Confidence) / 2;
                    parts[PartIndex.HIP_CENTER] = new PartInfo { IsValid = true, X = x, Y = y, Confidence = conf };
                }
            }

            // === 框中心（兜底，始终有效） ===
            {
                parts[PartIndex.BOX_CENTER] = new PartInfo
                {
                    IsValid = true,
                    X = bounds.X + bounds.Width / 2,
                    Y = bounds.Y + bounds.Height / 2,
                    Confidence = 0.1f
                };
            }
        }

        /// <summary>
        /// 从关键点创建 PartInfo
        /// </summary>
        private static PartInfo CreatePart(Keypoint kp)
        {
            return new PartInfo
            {
                IsValid = true,
                X = kp.Point.X,
                Y = kp.Point.Y,
                Confidence = kp.Confidence
            };
        }
        #endregion

        #region 调试辅助
        /// <summary>
        /// 获取部位名称（用于调试）
        /// </summary>
        public static string GetPartName(int partIndex)
        {
            return partIndex switch
            {
                PartIndex.NOSE => "鼻子",
                PartIndex.LEFT_EYE => "左眼",
                PartIndex.RIGHT_EYE => "右眼",
                PartIndex.LEFT_EAR => "左耳",
                PartIndex.RIGHT_EAR => "右耳",
                PartIndex.LEFT_SHOULDER => "左肩",
                PartIndex.RIGHT_SHOULDER => "右肩",
                PartIndex.LEFT_ELBOW => "左肘",
                PartIndex.RIGHT_ELBOW => "右肘",
                PartIndex.LEFT_WRIST => "左腕",
                PartIndex.RIGHT_WRIST => "右腕",
                PartIndex.LEFT_HIP => "左髋",
                PartIndex.RIGHT_HIP => "右髋",
                PartIndex.LEFT_KNEE => "左膝",
                PartIndex.RIGHT_KNEE => "右膝",
                PartIndex.LEFT_ANKLE => "左踝",
                PartIndex.RIGHT_ANKLE => "右踝",
                PartIndex.FOREHEAD_1 => "额头1",
                PartIndex.FOREHEAD_2 => "额头2",
                PartIndex.SHOULDER_CENTER => "双肩中点",
                PartIndex.CHEST => "胸",
                PartIndex.HIP_CENTER => "双髋中点",
                PartIndex.BOX_CENTER => "框中心",
                _ => "未知"
            };
        }
        #endregion
    }
}
