# 待解决问题记录

本文档记录项目中发现的待解决问题、临时修复方案及其来龙去脉，便于后续彻底解决。

---

## 📋 问题状态一览

> 图例：🔴 阻塞排查中 / 🟡 待实施 / 🔵 待规划 / ⚪ 已知暂不处理 / 📋 规划参考 / ✅ 已完成（折叠归档）

| ID | 标题 | 状态 |
|---|---|---|
| ISSUE-022 | 真人模式瞬狙成功率低（换 RTX5060 后加剧） | 🔴 已有初步数据，待确认泄漏路径 |
| ISSUE-021 | 狙击准心偶发识别失败（1.5s 空洞） | 🔴 新发现，暂存待复现 |
| ISSUE-014 | 瞬狙 / 狙击模式 UnmaskAll 时序（CF 键鼠异常候选） | 🔴 调查中 |
| ISSUE-016 | 屏蔽粒度过粗 MaskAll/UnmaskAll | 🟡 方向已认同 |
| ISSUE-017 | 狙击准心期间左键屏蔽策略 | 🟡 规格已锁定 |
| ISSUE-018 | 鼠标操作缺乏真人特征 | 🟡 方向已定 |
| ISSUE-019 | 键鼠异常其他潜在风险点 | 🟡 逐项待处理 |
| ISSUE-009 | 线性模式 XY 耦合 | 🔵 待解决 |
| ISSUE-010 | Rifle 模式头部命中率低 | 🔵 待解决 |
| ISSUE-011 | 步枪射击期间自瞄移动频率低 | 🔵 待解决 |
| ISSUE-003 | 灵敏度自动获取 | 🔵 待实现 |
| ISSUE-006 | Sticky Aim（粘性瞄准） | 🔵 待实现 |
| ISSUE-007 | 位置预测（EMA 速度预测） | 🔵 待实现 |
| ISSUE-008 | FOV 转换功能移除 | 🔵 待实现 |
| ISSUE-012 | 步枪左键按压时长无补偿 | ⚪ 已知暂不处理 |
| ISSUE-020 | 屏蔽职责归属总览 | 📋 规划参考 |
| ISSUE-001 / 002 / 004 / 005 / 013 / 015 | 共 6 项已完成（折叠归档） | ✅ |

> 关注活跃问题直接跳至 **ISSUE-021 / 014 / 016~019**；已完成条目位于文档后半部分，默认折叠。

---

<details>
<summary>✅ <b>ISSUE-001</b>: TargetSelector 部位选择逻辑耦合过紧 — 已完成重构（点击展开完整记录）</summary>

## ISSUE-001: TargetSelector 部位选择逻辑耦合过紧

**状态**: ✅ 已完成重构  
**发现日期**: 2025-01-XX  
**完成日期**: 2025-01-XX  
**影响范围**: `YoloProcessing/TargetSelector.cs`

### 原问题描述

`SelectBestPart` 函数的部位选择逻辑存在多处隐性耦合，修改任一环节都可能导致锁定位置错误。

### 重构方案

#### 新架构

```
YOLO结果
    │
    ▼
1. 排除不可信目标（姿态点置信度低 / 尸体检测）
    │
    ▼
2. 选择最近目标（框中心离画面中心最近）
    │
    ▼
3. 收集所有 23 个部位，每个部位独立阈值判断可用性
    │
    ▼
4. 按优先级表选择部位（lockHead=true 锁头，lockHead=false 锁身体）
    │
    ▼
LockResult
```

#### 部位定义（23个）

| 索引 | 部位 | 类型 |
|-----|------|------|
| 0-16 | 鼻子、眼、耳、肩、肘、腕、髋、膝、踝 | 原始姿态点 |
| 17 | 额头1 | 组合：双耳X + (鼻子Y+框顶Y)/2 |
| 18 | 额头2 | 组合：双耳X + (双耳Y+框顶Y)/2 偏框顶 |
| 19 | 双肩中点 | 组合 |
| 20 | 胸 | 组合：肩髋之间偏肩 |
| 21 | 双髋中点 | 组合 |
| 22 | 框中心 | 兜底 |

#### API 变更

```csharp
// 旧 API
ProcessTargets(result, int preferredLocation, ...)

// 新 API
ProcessTargets(result, bool lockHead, ...)
// lockHead=true  → 使用 HeadFallbackOrder
// lockHead=false → 使用 BodyFallbackOrder
```

#### 优先级表

```csharp
// 锁头表：从头到脚
HeadFallbackOrder = { 额头1, 额头2, 鼻子, 左眼, 右眼, ... , 框中心 }

// 锁身体表：躯干优先
BodyFallbackOrder = { 胸, 双肩中点, 双髋中点, ... , 框中心 }
```

#### 阈值配置

- 每个部位独立阈值，硬编码在各自的判断代码块开头
- 消除了 PartQuality 质量等级系统，简化为“可用/不可用”
- 优先级完全由表顺序决定，不再有“质量优先于偏好”的问题

### 相关文件
- `YoloProcessing/TargetSelector.cs` - 完全重写
- `YoloProcessing/DebugRenderer.cs` - 适配新的 PartInfo 和 23 部位
- `Form1.cs` - 调用方式改为 `lockHead: true/false`

---

*新问题请在下方按 ISSUE-XXX 格式添加*

---

</details>

<details>
<summary>✅ <b>ISSUE-002</b>: FOV 转换功能 — 已完成（点击展开完整记录）</summary>

## ISSUE-002: FOV转换功能

**状态**: ✅ 已完成  
**发现日期**: 2025-01-XX  
**完成日期**: 2025-01-XX  
**影响范围**: 鼠标移动计算

### 原问题描述

当前鼠标移动计算未考虑 FOV（视场角）转换，导致：
- 不同游戏/分辨率下的准心移动精度不一致
- 需要手动调整灵敏度参数

### 实现方案

#### 算法原理

使用 `atan2` 产生非线性映射：
- 目标接近中心时 → 精细移动（atan2 线性近似）
- 目标远离中心时 → 快速移动（atan2 趋于饱和）

#### 核心公式

```csharp
// 计算转换因子
float R = Sensitivity / 2f / MathF.PI;  // R ≈ 818 (Sensitivity=5140)

// 水平轴：标准 atan2 转换
float mx = MathF.Atan2(dx, R) * R;

// 垂直轴：带球面修正（避免对角线过快）
float my = MathF.Atan2(dy, MathF.Sqrt(dx * dx + R * R)) * R;
```

#### UI 开关

- 新增 `chkFovConvert` CheckBox
- 勾选 → 使用 FOV 非线性转换
- 不勾选 → 使用旧的线性计算

#### 配置参数

```csharp
// GameConfig.cs
public static class FovConfig
{
    public const float Sensitivity = 5140f;  // 需要游戏内校准
}
```

### 相关文件
- `Form1.cs` - `CalculateFovMove()` 函数、`ExecuteFireAction()` 分支逻辑
- `Form1.Designer.cs` - `chkFovConvert` UI 控件
- `Utils/GameConfig.cs` - `FovConfig.Sensitivity` 配置

---

</details>

## ISSUE-003: 灵敏度自动获取

**状态**: 待实现  
**发现日期**: 2025-01-XX  
**影响范围**: 用户配置流程

### 问题描述

当前灵敏度参数 (xSensitivity, ySensitivity) 需要手动配置，不同游戏/分辨率需要重新调整，体验不佳。

### 待实现内容

1. **自动校准流程**
   - 用户将准心对准固定目标
   - 程序发送已知鼠标移动量
   - 检测实际准心移动像素
   - 计算灵敏度系数

2. **配置持久化**
   - 保存不同游戏/分辨率的灵敏度配置
   - 启动时自动加载

### 实现方案设想

```
1. 用户点击"校准"按钮
2. 提示用户将准心移到屏幕中心
3. 发送固定鼠标移动 (e.g. 100, 0)
4. 检测准心实际移动像素
5. 计算: 灵敏度 = 鼠标移动量 / 像素移动量 * 100
```

### 相关文件
- `Form1.cs` - xSensitivity, ySensitivity 变量
- `Utils/GameConfig.cs` - Sensitivity 配置类
- 新增: 校准UI和校准逻辑

---

<details>
<summary>✅ <b>ISSUE-004</b>: 步枪开枪触发方式优化 — 已完成（准心识别方案）（点击展开完整记录）</summary>

## ISSUE-004: 步枪开枪触发方式优化

**状态**: ✅ 已完成（准心识别方案）  
**发现日期**: 2025-01-XX  
**完成日期**: 2025-01-XX  
**影响范围**: 射击模式触发逻辑

### 实现方案

#### 准心识别双模式切换

通过检测屏幕中心2x2区域的像素颜色来区分武器类型：

| 准心类型 | 检测公式 | 阈值 | 触发方式 |
|---------|---------|------|----------|
| 狙击准心（纯红） | `R - (G+B)/2` | > 253 | 自动触发 |
| 步枪准心（黄色） | `(R+G)/2 - B` | > 253 | 右键/侧键 |

#### 模式优先级

```
狙击准心（红色） > 步枪准心+右键 > 步枪准心+侧键
```

#### 实现逻辑

```csharp
// ImageHelper.CrosshairInfo 结构体
SnipeEnabled   // 狙击准心（纯红）
MaxRedness     // 最大红色度
ReddestX/Y     // 最红点坐标

RifleEnabled   // 步枪准心（黄色）
MaxYellowness  // 最大黄色度
YellowestX/Y   // 最黄点坐标

// Form1.cs 触发逻辑
if (crosshair.SnipeEnabled)           // 狙击模式
else if (crosshair.RifleEnabled && 右键) // 步枪模式
else if (crosshair.RifleEnabled && 侧键) // 点射模式
```

#### 使用方式

- **步枪**: 游戏内设置黄色准心 `(255,255,0)`，按住右键瞄准
- **狙击**: 右键开镜，检测到红色狙击镜准心自动触发
- **点射**: 黄色准心下按侧键

### 相关文件
- `YoloProcessing/ImageHelper.cs` - `CrosshairInfo` 结构体、`ReadGameCrosshairInfo()` 检测逻辑
- `Form1.cs` - `ProcessYoloFrame()` 触发分支
- `KmBox/KmBoxNet.cs` - `MaskAll()` 一键屏蔽功能

---

</details>

<details>
<summary>✅ <b>ISSUE-005</b>: 轨迹测试与步枪使用优化 — 已完成（准心识别方案）（点击展开完整记录）</summary>

## ISSUE-005: 轨迹测试与步枪使用优化

**状态**: ✅ 已完成（准心识别方案）  
**发现日期**: 2025-01-XX  
**完成日期**: 2025-01-XX  
**影响范围**: 鼠标移动轨迹、射击模式触发逻辑

### 原问题描述

| 触发方式 | 优点 | 缺点 |
|---------|------|------|
| 左键开枪 | 操作自然 | 锁定慢，跟不上敌人移动 |
| 侧键开枪 | 锁定又准又快 | 侧键不方便按 |

**核心矛盾**: 狙击和步枪都用右键会冲突

### 解决方案

通过识别游戏内准心颜色来区分武器类型：

- **狙击枪**: 不开镜时无准心，开镜后有纯红准心
- **步枪**: 用户自定义准心（设置为黄色）

```
检测逻辑：
屏幕中心2x2像素
    │
    ├─ 检测到纯红 → 狙击模式（自动触发）
    ├─ 检测到黄色 + 右键 → 步枪模式
    └─ 检测到黄色 + 侧键 → 点射模式
```

### 实现细节

详见 ISSUE-004。

### 待测试内容

- [ ] 轨迹测试：使用软件观察鼠标移动轨迹
- [ ] 黄色准心阈值微调
- [ ] 步枪延时公式优化

### 相关文件
- `YoloProcessing/ImageHelper.cs` - 准心检测
- `Form1.cs` - 触发逻辑
- `KmBox/KmBoxNet.cs` - `MaskAll()` 屏蔽功能

---

</details>

## ISSUE-006: Sticky Aim（粘性瞄准）

**状态**: 待实现  
**发现日期**: 2025-01-28  
**影响范围**: 目标选择逻辑  
**参考来源**: Aimmy 项目

### 问题描述

多人场景下，目标可能在相邻帧之间抖动切换，导致准心来回跳动。

### 解决方案

引入“锁定分数”机制，持续锁定同一目标：

```csharp
// 核心逻辑
1. 记录当前锁定目标 _lastTarget
2. 新帧检测到多个目标时，优先匹配上一帧的目标
3. 匹配成功 → 分数增加，继续锁定
4. 匹配失败 → 分数衰减，分数归零才切换目标
```

### 按需推理适配

我们项目是按需推理（准心+按键才触发），需要额外处理：

```csharp
private static DateTime _lastInferTime;
private const int STICKY_TIMEOUT_MS = 500;  // 超时重置

// 如果两次推理间隔 > 500ms，重置粘性状态
if ((DateTime.Now - _lastInferTime).TotalMilliseconds > STICKY_TIMEOUT_MS)
{
    ResetStickyState();
}
_lastInferTime = DateTime.Now;
```

### 实现要点

| 参数 | 值 | 说明 |
|------|-----|------|
| MAX_FRAMES_WITHOUT_TARGET | 3 | 允许丢失帧数 |
| LOCK_SCORE_DECAY | 0.85 | 每帧衰减系数 |
| LOCK_SCORE_GAIN | 15 | 每帧增益 |
| STICKY_TIMEOUT_MS | 500 | 超时重置时间 |

### 相关文件
- `YoloProcessing/TargetSelector.cs` - 添加粘性逻辑
- `参考项目分析.md` - 详细实现方案

---

## ISSUE-008: FOV转换功能移除

**状态**: 待实现  
**发现日期**: 2025-01-28  
**影响范围**: 鼠标移动计算、UI

### 问题描述

FOV（atan2 球面投影）转换在当前游戏中完全不准，经分析游戏使用的是**透视投影**而非球面投影，数学模型不匹配。

### 解决方案

删除 FOV 相关代码和 UI，保留线性转换模式。

### 待删除内容

| 位置 | 内容 |
|------|------|
| `Form1.cs` | `CalculateFovMove()` 函数 |
| `Form1.cs` | `_fovScale` 字段 |
| `Form1.cs` | FOV 分支判断逻辑 |
| `Form1.Designer.cs` | `chkFovConvert` CheckBox |
| `GameConfig.cs` | `FovConfig` 配置类 |

### 相关文件
- `Form1.cs`
- `Form1.Designer.cs`
- `Utils/GameConfig.cs`

---

## ISSUE-009: 线性模式 XY 耦合问题

**状态**: 待解决  
**发现日期**: 2025-01-31  
**影响范围**: 鼠标移动计算

### 问题描述

线性转换模式下：
- X 单独移动：✅ 准确（近距离准，中距离偏小，远距离准）
- Y 单独移动：✅ 准确（同上）
- **45° 斜向移动**：❌ X 轴偏大，Y 轴正常

### 尝试过的方案

#### 方案 1：透视投影 + FOV

使用 `atan(dx/focalLength)` 计算角度，FOV 控制焦距。
- **结果**：edgeBoost=1.0 时退化为线性，问题依旧存在

#### 方案 2：球面投影

将平面投影改为球面投影，总角度按方向分解：
```csharp
totalAngle = atan(√(dx² + dy²) / focalLength)
angleX = totalAngle * (dx / distance)
```
- **结果**：问题依旧存在

#### 方案 3：X 比例修正

直接乘以系数缩小 X：`mouseX = linearX * xCorrection`
- **问题**：单 X 轴移动也被影响，不符合需求

#### 方案 4：X-Y 耦合修正

只在 Y 有值时缩小 X：
```csharp
yRatio = |dy| / (|dx| + |dy|)
xCorrectionFactor = 1.0 - (1.0 - xCorrection) * yRatio
mouseX = linearX * xCorrectionFactor
```
- **结果**：待充分测试

### 待分析方向

1. **确认问题根源**：是代码问题还是游戏/硬件特性？
   - 使用其他工具（如 mousetester）验证鼠标移动是否准确
   - 确认游戏内 X/Y 灵敏度是否独立

2. **对角线修正**：将"直角边之和"缩放为"斜边长度"
   ```csharp
   linear = |mx| + |my|
   diagonal = √(mx² + my²)
   scale = diagonal / linear
   mx *= scale; my *= scale;
   ```

### 相关文件
- `Form1.cs` - 鼠标移动计算逻辑

---

## ISSUE-007: 位置预测（EMA速度预测）

**状态**: 待实现  
**发现日期**: 2025-01-28  
**影响范围**: 移动目标跟踪  
**参考来源**: Aimmy 项目 (WiseTheFoxPrediction)

### 问题描述

对于移动中的目标，锁定当前位置会有延迟，导致打不准。

### 解决方案

使用 EMA（指数移动平均）计算目标速度，预测未来位置：

```csharp
// EMA 平滑位置
_emaX = ALPHA * rawX + (1 - ALPHA) * _emaX;

// 计算速度
float newVelX = (_emaX - _prevX) / dt;
_velocityX = ALPHA * newVelX + (1 - ALPHA) * _velocityX;

// 预测未来位置
predictedX = _emaX + _velocityX * leadTime;
```

### 按需推理适配

我们项目不是一直推理，预测只在特定场景有效：

| 场景 | 是否适用 | 原因 |
|------|---------|------|
| 步枪扫射 | ✅ 适用 | 按住右键连续推理，有足够采样点 |
| 点射连发 | ⚠️ 部分 | 采样点少，预测不准 |
| 狙击单发 | ❌ 不适用 | 无历史数据，无法预测 |

```csharp
// 只在步枪模式且连续推理时启用
if (fireMode == FireMode.Rifle && _consecutiveFrames >= 3)
{
    (targetX, targetY) = GetPredictedPosition(rawX, rawY, leadTime);
}
else
{
    (targetX, targetY) = (rawX, rawY);  // 直接使用当前位置
}
```

### 实现要点

| 参数 | 值 | 说明 |
|------|-----|------|
| ALPHA | 0.5 | EMA 平滑系数 |
| leadTime | 0.05~0.1s | 预测提前量，需测试调整 |
| MIN_FRAMES | 3 | 最少连续帧数才启用预测 |

### 相关文件
- `YoloProcessing/TargetSelector.cs` - 添加预测逻辑
- `Form1.cs` - 传入 fireMode 参数
- `参考项目分析.md` - 详细实现方案

---

## ISSUE-010: Rifle 模式头部命中率低

**状态**: 待解决  
**发现日期**: 2025-02-07  
**影响范围**: Rifle 步枪模式射击精度

### 问题描述

使用 L 模型后，Rifle 步枪模式经常打不中头部，Sniper 狙击模式则表现正常。

### 原因分析

#### 1. L 模型推理延迟 + 目标运动 = 帧数据过时（主因）

目标偏移量基于帧采集时刻 T0 计算，但从 T0 到实际开枪存在显著延迟：

| 阶段 | 预估耗时 |
|---|---|
| YOLO L模型推理 | 15~30ms |
| 目标选择+计算 | ~1ms |
| MaskAll + MouseMove | ~2-5ms |
| **总延迟** | **~20-35ms** |

敌人运动中 20-35ms 足以使头部移出数像素。Sniper 锁身体（大目标）容错高，Rifle 锁头（小目标）偏几像素就打空。

#### 2. 推理期间用户移动鼠标导致偏移失效

时序问题：
```
T0: 帧采集 → 准心在位置 A
T1: YOLO推理中 → 用户移动鼠标，准心到位置 B  
T2: 系统计算出 "从位置 A 需移动 X 像素到目标头部"
T3: MaskAll 屏蔽物理鼠标
T4: 系统移动 X 像素 → 但准心在 B 而非 A，打偏
```

MaskAll 仅在射击时屏蔽输入，推理期间（最长阶段）用户鼠标自由移动，计算偏移量与实际准心位置不匹配。

### 改进方向

| 方向 | 思路 |
|---|---|
| 降低推理延迟 | 为 Rifle 使用更小更快的模型（S/M），Sniper 保留 L 模型 |
| 运动预测补偿 | 结合 ISSUE-007 EMA 预测，补偿推理延迟期间的目标位移 |
| 用户移动补偿 | MaskAll 前获取物理鼠标累积位移并补偿到偏移量中 |

### 相关文件
- `Form1.cs` - `StartYoloThread()`、`ProcessYoloFrame()`、`ExecuteFireAction()`
- `YoloProcessing/ImageHelper.cs` - YOLO 推理调用

---

## ISSUE-011: 步枪射击期间自瞄移动频率低于预期

**状态**: 待解决  
**发现日期**: 2025-04-10  
**影响范围**: 步枪模式自瞄移动频率

### 问题描述

`RIFLE_MOVE_INTERVAL_MS = 80`（理论约12次/秒），但实际体感只有2-3次/秒。推理帧率在144Hz以上，排除推理速度瓶颈。

### 原因分析

步枪连发射击时，后坐力动画/枪口火焰导致屏幕中心准心颜色变化，`crosshair.RifleEnabled` 频繁返回 false。

当准心检测失败时，代码走 else 分支：
```csharp
if (_rifleSessionActive && _kmBox.IsMouseRightDown())
    return;  // 直接跳过整帧：不执行 YOLO 推理，不调用 HandleRifleSession
```

**结果**：自瞄移动频率 = 射击期间准心检测成功的频率（远低于帧率），而非 `RIFLE_MOVE_INTERVAL_MS` 控制的间隔。

### 曾尝试的修复

将 else 分支改为不 return，继续执行 YOLO 推理 + HandleRifleSession。编译通过但未充分测试，已回滚。

**回滚原因**：准心检测失败时继续推理可能引入副作用（如遮罩矩形未绘制、推理器选择等），需要更全面的方案。

### 可能的解决方向

1. **优化准心检测**：增大检测区域（如4x4）或降低阈值，提高射击期间的检测通过率
2. **准心状态缓存**：最近N帧内检测到步枪准心则持续视为步枪模式（类似 `_lastRifleCrosshairTimestamp` 的思路）
3. **分离准心检测与YOLO推理**：在 else 分支中仅跳过准心相关逻辑，但仍允许已激活的步枪会话继续推理和移动

### 相关文件
- `Form1.cs` - `ProcessYoloFrame()` else 分支
- `YoloProcessing/ImageHelper.cs` - `ReadGameCrosshairInfo()` 准心检测逻辑

---

## ISSUE-012: 步枪模式左键按压时长无补偿

**状态**: 已知问题（暂不修复）  
**发现日期**: 2025-04-10  
**影响范围**: 步枪模式射击手感

### 问题描述

步枪模式下，右键按压时长由用户控制（点射/连发），软件在YOLO推理完成后按下左键开火。由于推理有延迟（~7ms@144Hz），左键按下时间晚于右键按下时间，导致左键实际按压时长 < 右键按压时长。

```
时间线：
T0: 用户按下右键
T1: YOLO推理完成，软件按下左键（延迟 = T1-T0）
T2: 用户释放右键 → 软件立即释放左键
    左键实际按压 = T2-T1 < 右键按压 T2-T0
```

### 曾实现的补偿方案

记录右键按压时长（`_rifleRightDownTimestamp` → `_rifleRightUpTimestamp`）和左键按下时间戳（`_rifleLeftDownTimestamp`），在释放左键前 Sleep 补偿差值。

**移除原因**：
1. 连续快速点射时，补偿 Sleep 导致开枪时间异常长（级联延迟）
2. 将 Sleep 放在监控线程会阻塞整个 KmBox UDP 监控
3. 将 Sleep 移到 YOLO 线程后仍有时序问题（右键快速释放+按下时，YOLO线程的补偿检查可能跨越多帧）
4. 一帧的偏差（~7ms）在实际体感中几乎不可察觉

### 当前行为

右键释放 → 立即释放左键，不做时长补偿。左键按压时长比右键短约一帧（~7ms），用户基本无感。

### 相关文件
- `Form1.cs` - `OnKmBoxMouseButtonChanged()` 右键释放处理、`EndRifleSession()`

---

<details>
<summary>✅ <b>ISSUE-013</b>: 步枪模式单击右键后持续连发（左键卡住）— 已修复（点击展开完整记录）</summary>

## ISSUE-013: 步枪模式单击右键后持续连发（左键卡住）

**状态**: ✅ 已修复  
**发现日期**: 2025-04-10  
**修复日期**: 2025-04-10  
**影响范围**: 步枪模式射击控制  
**严重程度**: 高（影响正常使用）

### 问题描述

**复现条件**：步枪模式，单击/按住右键开火  

**现象**：
- 单击右键后持续连发（不停连发），如同左键一直按压
- 按住右键连射几发后反而停止射击，即使右键还按着
- 对着没人的地方也能复现

### 调查过程

先后排除了 5 个假设（竞态条件、火焰误触发、MouseMove 重放按键状态、_hwMouseButtons 时序、模式选择优先级），均未解决问题。

最终通过**隔离测试模块** (`TestRifleForm`) 定位根因：
- 创建纯净测试窗体，只有 KmBox 连接 + 右键→左键逻辑，无 YOLO/准心/瞬狙
- 测试结果：同样出现持续连发
- 添加 `MaskMouseRight(true)` 屏蔽右键后：**完全正常**

### 确认根因：游戏无法同时处理右键+左键

游戏引擎在同时检测到右键和左键按下时行为异常，导致左键状态混乱。

之前右键=游戏特殊功能（开镜/消音器）时未发现，是因为游戏自身消费了右键事件，不存在右键+左键同时按下的情况。

### 修复方案

**在步枪模式右键按下时屏蔽硬件右键**（`MaskMouseRight`），游戏只收到软件左键：

**修复1：右键预屏蔽**（`ProcessYoloFrame` 中准心检测后）

准心可见或步枪会话活跃时提前屏蔽右键，避免首次按下时硬件右键 HID 报告先于 UDP 屏蔽命令到达游戏。
仅基于事件回调屏蔽存在时序问题（~5-8ms 延迟），导致游戏特殊功能键≠右键时右+左仍冲突。

```csharp
// ProcessYoloFrame - 准心检测后
bool shouldMaskRight = crosshair.RifleEnabled || _rifleSessionActive;
if (shouldMaskRight && !_rifleRightPreMasked)
    _kmBox.MaskMouseRight(true);
else if (!shouldMaskRight && _rifleRightPreMasked)
    _kmBox.MaskMouseRight(false);
```

**修复2：事件回调屏蔽**（`OnKmBoxMouseButtonChanged` 中作为安全网）

```csharp
// OnKmBoxMouseButtonChanged - 右键按下
if (isRifleMode)
{
    if (!_rifleRightPreMasked)  // 预屏蔽未生效时的安全网
        _kmBox.MaskMouseRight(true);
    _kmBox.MaskMouseX(true);
    _kmBox.MaskMouseY(true);
}

// OnKmBoxMouseButtonChanged - 右键释放（XY屏蔽，右键由预屏蔽管理）
if (_rifleXYMasked)
{
    _kmBox.MaskMouseX(false);
    _kmBox.MaskMouseY(false);
}
```

**修复3：重构模式选择优先级**（步枪会话期间枪口火焰使准心变红→`SnipeEnabled=true`，必须让步枪会话优先于准心检测）：

> **已验证**：曾尝试去掉 `_rifleSessionActive` 优先级分支（假设准心不受火焰影响），结果步枪无法连发。
> 确认：枪口火焰确实会干扰准心检测，`_rifleSessionActive` 保护代码是必要的。

```csharp
// ProcessYoloFrame 模式选择（步枪会话优先于准心检测）：
if (_rifleSessionActive && IsMouseRightDown())    // ① 步枪会话继续
else if (_rifleSessionActive)                     // ② 右键已释放→EndRifleSession
else if (crosshair.SnipeEnabled)                  // ③ 狙击模式
else if (crosshair.RifleEnabled && RightDown)      // ④ 新步枪会话
else if (crosshair.RifleEnabled && Side2Down)       // ⑤ 点射模式
else return;                                       // ⑥ 无操作
```

性能影响：零。

### 诊断工具

`TestRifleForm` 隔离测试窗体已保留，启动方式：`Program.cs` 中取消注释 `#define TEST_RIFLE`。

### 相关文件
- `Form1.cs` - ProcessYoloFrame 模式选择 / OnKmBoxMouseButtonChanged 右键屏蔽
- `KmBox/KmBoxNet.cs` - MaskMouseRight / HwMouseButtonState 调试属性
- `TestRifleForm.cs` / `TestRifleForm.Designer.cs` - 隔离测试模块

---

</details>

## ISSUE-014: 瞬狙 / 狙击模式 UnmaskAll 时序导致凭空鼠标事件（CF 报键鼠异常根因候选）

**状态**: 🔍 调查中（已观察到异常时序，尚未100%证实与 CF 弹窗相关）  
**发现日期**: 2026-04-10  
**影响范围**: 狙击模式（`ExecuteFireAction` Sniper 分支 / 瞬狙预屏蔽超时 / Monitoring 窗口超时）  
**严重程度**: 高（疑似触发 CF 反作弊弹出“键鼠异常”警告）

### 问题描述

**现象**：
- 在 CF **枪王排位**（团队/练枪不触发）使用狙击枪时，频繁弹“键鼠异常”
- 之前怀疑自动切枪（MouseWheel），关闭自动切枪后仍出现
- 关闭本程序连续打狙 1 小时不报 → 确认是程序注入触发
- 步枪模式不报 → 排除 MaskAll 本身、MouseMove、点击时长等共性因素

### 根因分析：双重屏蔽 + UnmaskAll 的时序破绽

狙击流程存在**两层屏蔽嵌套**：

| 阶段 | 动作 | `_maskFlag` |
|---|---|---|
| 右键按下（硬件事件） | `MaskMouseLeft(true)` 预屏蔽 | `0x01` |
| 准心识别 → Monitoring | 无变化 | `0x01` |
| `ExecuteFireAction` 开头 | `MaskAll()` 叠加 | `0x63` (左+右+X+Y) |
| `ExecuteFireAction` 结尾 | `UnmaskAll()` 一次性全清 | `0x00` |

**异常场景**（用户点击时长 > 瞬狙执行时长 80-150ms）：

```
T+0     用户按左键（硬件）→ 被屏蔽
T+7     软件触发 MouseLeft(true) → 游戏看到“软件按下”
T+130   软件 MouseLeft(false)    → 游戏看到“软件抬起”
T+131   UnmaskAll() → 此时硬件左键仍按下 (IsMouseLeftDown=true)
        → 屏蔽解除瞬间，硬件状态从“被过滤=0”突然透传为“按下=1”
        → 游戏收到凭空的“按下”★
T+200   用户真抬手 → 游戏收到“抬起”
```

**游戏实际看到的事件序列**：
```
[软件] 按下 → [软件] 抬起 → [硬件] 按下(凭空) → [硬件] 抬起
```
一次物理点击，游戏看到 **2 次按下 + 2 次抬起**，其中一对时序异常。这种模式是反作弊典型嗅探目标。

### 为什么步枪模式不报

步枪是“按住右键扫射”模式，[HandleRifleSession](file:///Form1.cs) 仅屏蔽右键 + XY，**左键始终是软件按住**；用户的物理左键不参与、不存在“屏蔽/解除屏蔽”包裹真实按键的情况 → 无凭空事件。

### 三个同源触发路径（全部会产生凭空事件）

| # | 触发点 | 条件 |
|---|---|---|
| 1 | `ExecuteFireAction` 的 `UnmaskAll` | 用户点击时长 > 80-150ms（常见） |
| 2 | 瞬狙预屏蔽超时（200ms）解除 | 用户开镜后准心未识别，且左键仍按着 |
| 3 | Monitoring 窗口超时（100ms）解除 | 用户开镜后未按左键，屏蔽到期解除时硬件=按下 |

**共同点**：`_maskFlag` 左键位从 1 变 0，而硬件左键 = 1 → 游戏看到凭空按下。

### 诊断工具：TestSniperForm

独立隔离测试窗体，通过 `Program.cs` 中 `#define TEST_SNIPER` 启动。

**关键日志字段**（`TriggerSniperShot` 四段快照 + 所有硬件事件）：
```
[HW] 左键 按下 / 抬起
[HW] 右键 按下 / 抬起
① 触发 #N 开始 | 硬件左键=按下/抬起
② #N 软件左键按下
③ #N 软件左键抬起完成 | UnmaskAll 前 硬件左键=按下/抬起
   ⚠⚠ #N 异常风险：UnmaskAll 时硬件仍按下 → 游戏将看到凭空的“按下+抬起”
④ #N UnmaskAll 完成
[预屏蔽] 超时 Nms 解除 | 硬件左键=按下⚠/抬起
[Monitoring] 窗口超时 Nms 解除屏蔽 | 硬件左键=按下⚠/抬起
```

**实测样本（触发 #44）**：
```
T+0     右键按下 → 预屏蔽
T+33    预屏蔽命中 → Monitoring
T+883   左键按下（硬件）
T+890   ① 硬件左键=按下 | 位移(-9, -1)
T+892   ② 软件左键按下
T+1014  ③ UnmaskAll 前 硬件左键=按下  ⚠⚠
T+1015  ④ UnmaskAll 完成
```
证实了根因假设，游戏确实会看到凭空的硬件事件。

### 修复方向（待验证）

**方案 A - 自旋等待**：UnmaskAll 前轮询 `IsMouseLeftDown()`，等到硬件抬起或超时（500ms 兜底）再解除。

**方案 B - 分阶段解除**（推荐）：软件左键抬起后，若硬件仍按下：
```csharp
_kmBox.MouseLeft(false);
if (_kmBox.IsMouseLeftDown())
{
    // 保留左键屏蔽位，解除 XY + 右键
    _kmBox.MaskMouseLeft(true);   // _maskFlag 回到 0x01
    // 另起轮询：检测到硬件抬起后再 MaskMouseLeft(false)
}
else
{
    _kmBox.UnmaskAll();
}
```
优点：不改开火时序、不卡游戏输入后续按键；缺点：实现稍复杂，需要异步等待硬件抬起。

**方案 C - 统一应用到所有超时路径**：预屏蔽超时 / Monitoring 窗口超时也必须遵循“硬件按下就继续屏蔽，等抬起”的原则。

### 历史回溯：为什么之前也报异常

用户反馈“没有瞬狙模式时狙击就会报异常，原以为是自动切枪”：
- 早期版本：狙击走 `ExecuteFireAction` Sniper 分支，**同样的 MaskAll → UnmaskAll 模板** → 同样的凭空事件风险
- 自动切枪 `MouseWheel(-1)` 插在两段 Sleep 之间，使窗口变长 → `UnmaskAll 时硬件仍按下` 概率升高 → 异常触发率升高
- **根因一直都在，自动切枪只是加剧因子，不是源头**

### 相关文件
- `Form1.cs` - `ExecuteFireAction` Sniper 分支 / 瞬狙状态机 / `ClearQuickScopePreMask`
- `TestSniperForm.cs` / `TestSniperForm.Designer.cs` - 隔离测试模块
- `KmBox/KmBoxNet.cs` - `MaskAll` / `UnmaskAll` / `IsMouseLeftDown`

---

<details>
<summary>✅ <b>ISSUE-015</b>: 微自瞄边界抖动导致双重触发（原地开枪 + 自瞄飞走）— 已修复（点击展开完整记录）</summary>

## ISSUE-015: 微自瞄边界抖动导致双重触发（原地开枪 + 自瞄飞走）

**状态**: ✅ 已修复  
**发现日期**: 2026-04-10  
**修复日期**: 2026-04-10  
**影响范围**: 瞬狙 + 微自瞄模式（`ProcessQuickScopeMonitoring`）  
**严重程度**: 中（功能异常，非安全问题）

### 现象

微自瞄模式下，准心靠近扩展框边界（识别框 + `_microAimExtend`）开枪时：
- 弹着点在原地（开枪瞬间准心未移动）
- 开枪后鼠标突然移动，且不在敌人身上

### 根因：三个放大因子叠加

| # | 因子 | 作用 |
|---|---|---|
| 1 | **YOLO 每帧检测框微抖 ±3-5px** | 准心在扩展框边界时 `crosshairInBox` true/false 跳变 |
| 2 | **采集卡延迟 55-80ms**（采集+推理+渲染） | 开完枪后下一帧程序看到的还是“开枪前”的画面，敌人还在原位 |
| 3 | **`_quickScopeLeftClickDetected` 标志残留 + `IsMouseLeftDown()` 持续为 true** | 程序无法区分“用户已原生开枪”和“用户刚按下还未开枪” |

### 异常事件链

```
开镜 → Monitoring
↓
帧 N-1: 准心在扩展框外侧 → MaskMouseLeft(false) 解除屏蔽（标志残留）
↓
用户按左键 → 硬件直通 → 游戏开枪，弹着点 = 准心位置（原地★）
↓
采集卡延迟 55-80ms，程序看不到开枪效果
↓
帧 N: YOLO 微抖 → crosshairInBox=true → 屏蔽左键
      IsMouseLeftDown=true 或 标志位残留
      → ExecuteFireAction → MouseMove 飞到敌人位置★
      但枪已冷却，软件左键无效 → 视觉上只有鼠标移动
```

### 修复方案

**新增两个字段**（`Form1.cs`）：
```csharp
private bool _quickScopeLeftConsumed;      // 本次 Monitoring 周期内左键是否已处理
private bool _quickScopeEnterLeftDown;     // 进入 Monitoring 时左键是否已按下
```

**修复 A —— 框外路径清除残留标志 + 消费锁死**：
```csharp
if (!crosshairInBox)
{
    _quickScopeLeftClickDetected = false;   // 清除预屏蔽残留
    if (_kmBox.IsMouseLeftDown())
        _quickScopeLeftConsumed = true;     // 用户原生开枪 → 锁死本周期
    ...
}
```

**修复 E —— 框内路径双重防护**：
```csharp
// 1. 已消费直接返回（防采集卡延迟 + 边界抖动二次触发）
if (_quickScopeLeftConsumed) return true;

// 2. 边沿检测：仅响应进入 Monitoring 后的新鲜按下
bool leftFreshEdge = _kmBox.IsMouseLeftDown() && !_quickScopeEnterLeftDown;
if (_quickScopeLeftClickDetected || leftFreshEdge)
{
    ExecuteFireAction(...);
    _quickScopeLeftConsumed = true;         // 触发后锁死
    ...
}
```

**统一初始化基线**：`EnterQuickScopeMonitoring` / 预屏蔽平滑过渡 / 窗口超时 / `ResetQuickScopeState` 四个入口都重置新字段。

### 预期效果

| 场景 | 修复前 | 修复后 |
|---|---|---|
| 边界抖动（原地开枪 + 鼠标飞走） | 必现 | 消失 |
| 预屏蔽标志残留 → 回框内误触发 | 重复触发 | 标志被清，只放行一次 |
| 用户持续按住左键 | 每帧可重触 | 只响应新鲜边沿 |
| 一次右键开镜触发次数 | N（不确定） | **≤ 1 次** |

### 相关文件
- `Form1.cs` - `ProcessQuickScopeMonitoring` / `EnterQuickScopeMonitoring` / `ResetQuickScopeState`

---

</details>

## 键鼠异常相关问题汇总（供后续重构参考）

本阶段集中调查 CF 键鼠异常和相关衍生问题时发现的多个独立 Bug，根因都指向同一类问题：
**全局状态 + 多入口改写同一状态 + 硬件/软件状态不一致**。

### 已定位的 Bug 清单

| # | 场景 | 根因类别 | 状态 |
|---|---|---|---|
| ISSUE-013 | 步枪模式右键+左键冲突 → 持续连发 | 游戏引擎不兼容同时两键 | ✅ 已修 |
| ISSUE-014 | `UnmaskAll` 时硬件左键仍按下 → 凭空硬件事件 | 屏蔽生命周期 vs 物理按键生命周期没对齐 | 🔍 调查中 |
| ISSUE-015 | 微自瞄边界抖动双重触发 | 状态标志跨轮次残留 + 多轨道改写 | ✅ 已修 |

### 共同模式

1. **状态字段多起源**：例如 `_quickScopeLeftClickDetected` 由硬件事件设置，被 `ProcessQuickScopeMonitoring` 消费，被 `ClearQuickScopePreMask`/`ResetQuickScopeState` 清理，分散在 4 个函数里——添一条支路就容易漏清。
2. **屏蔽状态 vs 硬件状态没对齐**：`UnmaskAll` 只看软件 `_maskFlag`，不看用户硬件按键仍然为 1。
3. **模式互相精妙干扰**：瞬狙状态机 / 步枪会话 / 瞬狙预屏蔽 / 点射 / 微自瞄 —— 共用几个全局字段和 `ProcessYoloFrame` 主分支，一处改动很容易误伤其他模式。

## 还在怀疑、未定位的风险点

- 大位移 MouseMove（瞬狙场景）是否还有特征可疑 — 需实战证据
- HID 消息细粒度时序（Connect/Disconnect/MonitorEnable 的顺序）在边界情况下是否会遇见调用顺序风险
- 步枪模式 `EndRifleSession` 的 Unmask 时序是否也有类似 ISSUE-014 的风险

---

## 键鼠屏蔽策略重构（2026-04 阶段 3~6 规划）

前序工作（阶段 1~2）已完成：`FireActions` / `QuickScopeController` / `RifleSessionController` / `WeaponDispatcher` 四层解耦，Form1.cs 从 796 行 → 526 行。

本阶段目标：**消除粗粒度 `MaskAll/UnmaskAll`，改为各模块按需屏蔽**，减少屏蔽时序引发的键鼠异常。

下列每个 ISSUE 都需用户校正「问题认定 / 需求定义 / 方案选择」后再动工。

---

## ISSUE-016: 屏蔽粒度过粗 —— FireActions 的 MaskAll/UnmaskAll 是核弹级清零

**状态**: 🔍 待校正（用户已初步认同方向）
**发现日期**: 2026-04-10
**影响范围**: `Firing/FireActions.cs`（SniperFire / BurstFire）

### 问题描述

`FireActions.SniperFire` 与 `BurstFire` 在开火前调用 `km.MaskAll()`，开火后调用 `km.UnmaskAll()`。`MaskAll` 屏蔽**所有**按键（左键/右键/中键/侧键/XY/滚轮/键盘），`UnmaskAll` 解除**所有**屏蔽。

### 当前风险

1. `UnmaskAll` 会**顺带解除**其他模块维护的细粒度屏蔽（QuickScope 的左键屏蔽 / RifleSession 的右键预屏蔽 / XY 屏蔽），但其他模块的 bool 标志（`_targetMasked` / `_rightPreMasked` / `_xyMasked`）对此**完全无感**，硬件状态与 bool 脱同步
2. 目前靠 `WeaponDispatcher` 分支互斥（Sniper/Rifle/Burst 不会同帧并发）勉强规避，但任何分支顺序调整都可能撕开伤口
3. MaskAll 屏蔽范围远超实际需要：狙击开火只需防用户"多按一下左键/右键"，不需要屏蔽 XY / 滚轮 / 键盘

### 需求

- 保留开火瞬间"防用户物理输入干扰"的效果
- 开火结束后**不影响**其他模块已有的屏蔽状态
- 屏蔽粒度精确到"本次开火实际需要屏蔽的那几个键"

### 待校正

- [ ] 确认 SniperFire 实际只需屏蔽**左键**即可？还是左键+右键都要屏蔽？
- [ ] BurstFire（点射）是否同样处理？
- [ ] 开火期间用户主动按 ESC / 切枪键盘是否允许透传？

### 建议方案

改为细粒度：
```csharp
// SniperFire 改造前
km.MaskAll();
km.MouseMove(...); km.MouseLeft(true); ...; km.MouseLeft(false);
km.UnmaskAll();

// 改造后（初步提案）
km.MaskMouseLeft(true);  // 只屏蔽左键，防用户多按
km.MouseMove(...); km.MouseLeft(true); ...; km.MouseLeft(false);
km.MaskMouseLeft(false); // 只解除左键
```

---

## ISSUE-017: 狙击准心期间左键屏蔽策略不明确

**状态**: ✅ 规格已锁定（2026-04-10），待实施
**发现日期**: 2026-04-10
**规格确定日期**: 2026-04-10
**影响范围**: 狙击模式 / 瞬狙模式 / `FireActions` / `WeaponDispatcher` / 新开 `LeftMaskController` + `CrosshairStabilizer`

### 问题描述

用户提议："狙击模式目前不需要手动，全程屏蔽左键由软件负责开枪"，但"全程"需要精确定义 —— 否则用户切枪后无法手动开枪 / 近战刀无法挥出。

经过校正，CF 游戏的准心语义特性让规则特别简洁：

- **狙击枪未开镜 = 无任何准心**（仍属狙击模式）
- **狙击枪开镜 = SnipeEnabled**
- **步枪 / 手雷 / 小刀 = RifleEnabled**

所以设计基本线就是以 `RifleEnabled` 做二分：

```
RifleEnabled == true  → 步枪模式（包括手雷/刀）
RifleEnabled == false → 狙击模式（包括狙击开镜与未开镜）
```

### 最终规格（用户确认）

#### 屏蔽规则表

| 准心状态 | 武器场景 | 左键 | 右键 | XY | 软件动作 |
|---|---|---|---|---|---|
| **RifleEnabled** + 右键未按 | 步枪/手雷/小刀 备战 | 不屏蔽 | 屏蔽 | 不屏蔽 | 不介入 |
| **RifleEnabled** + 右键按下 | 步枪开镜瞄准 | 屏蔽 | 屏蔽 | 屏蔽 | 首帧瞄准+开火，后续辅助瞄准 |
| **!RifleEnabled** 无开镜 | 狙击枪平时 | 屏蔽 | 不屏蔽 | 不屏蔽 | 监听物理左键边沿→代发 SniperFire |
| **!RifleEnabled** + SnipeEnabled | 狙击开镜（常规） | 屏蔽 | 不屏蔽 | 开火瞬间屏蔽 | 识别到人自动 SniperFire；无目标时监听左键边沿代发 |
| **!RifleEnabled** + SnipeEnabled | 狙击开镜（瞬狙） | 屏蔽 | 不屏蔽 | 开火瞬间屏蔽 | Monitoring 窗口内：有目标+左键边沿→自瞄代发；无目标+左键边沿→代发；超时→保留代发 |

#### 核心哲学

1. **用户按键 = 意图信号**，所有实际开火动作由软件统一发出
2. **屏蔽驱动源 = 准心**（不是状态机内部决策）：准心可见 1:1 对应屏蔽开关
3. **准心去抖**：状态切换需连续 N 帧一致才生效（防闪烁导致 Mask/Unmask 高频 toggle）

#### 边界条件确认（2026-04-10）

| # | 确认项 | 结论 |
|---|---|---|
| 1 | 常规狙击代发语义 | **边沿触发**（按下一次发一枚），按住不连发 |
| 2 | 瞬狙边沿意图信号 | **保留**，用户没按不乱开火 |
| 3 | 准心去抖实现 | 状态切换需连续 2~3 帧（约 15~20ms @144Hz） |
| 4 | 瞬狙开火瞬间 XY 屏蔽 | `SniperFire` 内部 `MaskMouseX/Y(true)` → `MouseMove` → `MaskMouseX/Y(false)` |
| 5 | 瞬狙 Monitoring 超时行为 | 超时不再自瞄，但**保留**代发（同常规狙击），不解除左键屏蔽 |

### 实现架构

#### 新增组件

##### `CrosshairStabilizer`（准心去抖器）
独立小工具，负责把 `ImageHelper.CrosshairInfo` 的原始每帧状态 → 稳定态。
- 输入：每帧 raw `SnipeEnabled` / `RifleEnabled`
- 输出：稳定 `StableSnipe` / `StableRifle`（需连续 2~3 帧一致才切换）
- 只影响屏蔽决策；YOLO 推理入口仍用 raw（保护自瞄精度）

##### `LeftMaskController`（左键屏蔽 + 物理边沿标志）
专责管狙击模式左键屏蔽与用户意图捕获：
- `ApplyMask(bool stableSnipeOrUnscoped)` — `!stableRifle` 时屏蔽左键并维持
- `OnHwLeftEdge(isDown)` — KmBox 监听线程调用，记录边沿
- `ConsumeManualFireRequest() -> bool` — YOLO 主线程调用，消费一次按下意图
- **KmBox 监听线程只做边沿标记，不发起开火**

#### FireActions 精简（阶段 3）

```csharp
// 改造前的 SniperFire
km.MaskAll();
km.MouseMove(dx, dy);
km.MouseLeft(true); … km.MouseLeft(false);
km.UnmaskAll();  // ← 靠它额外解除了左键屏蔽，危险

// 改造后的 SniperFire
// 左键屏蔽由 LeftMaskController 跨帧持有（本函数不管）
km.MaskMouseX(true); km.MaskMouseY(true);
km.MouseMove(dx, dy);
km.MaskMouseX(false); km.MaskMouseY(false);
km.MouseLeft(true); Thread.Sleep(…); km.MouseLeft(false);
if (autoSwitchWeapon) km.MouseWheel(-1);
Thread.Sleep(150);
```

注意：
- 左键在整个执行期间被 `LeftMaskController` 持续屏蔽（不需重复）
- 右键在狙击模式本来就不屏蔽，用户按右键切换武器/用力投雷仍正常生效
- XY 只封住 `MouseMove` 瞬间

#### 代发消费流程（YOLO 主线程）

```
ProcessYoloFrame 开头：
    stable = CrosshairStabilizer.Update(raw)
    LeftMaskController.ApplyMask(!stable.Rifle)   // 狙击模式屏蔽左键
    RifleSession.ApplyRightPreMask(stable.Rifle) // 步枪模式屏蔽右键

    … 瞬狙干预判断 …

    decision = WeaponDispatcher.Decide(stable, kmBox, rifleSession, …)

    // 狙击模式：用户左键意图消费（优先级高于自瞄）
    if !stable.Rifle and LeftMaskController.ConsumeManualFireRequest():
        if 目标有效:
            SniperFire(到目标)
        else:
            SniperFire(到准心处)  // 盲射，尊重用户意图
        return

    … 正常推理 → 自动开火 …
```

### 与其他 ISSUE 的关联

- **ISSUE-016**：必须同步完成（`FireActions` 删掉 MaskAll/UnmaskAll 才能让 `LeftMaskController` 的跨帧屏蔽不被破坏）
- **ISSUE-019.4**（准心闪烁去抖）：从"风险点"提升为**必修项**（`CrosshairStabilizer` 是本方案的前置依赖）
- **ISSUE-019.3**（线程安全）：KmBox 监听线程只写 `LeftMaskController` 内部 bool，不发 KmBox 指令，本方案不依赖其线程安全
- **ISSUE-013/014**：根源消除——左键屏蔽生命周期与用户物理按键脱耦

### 待实施清单

- [ ] 创建 `CrosshairStabilizer.cs`
- [ ] 创建 `LeftMaskController.cs`
- [ ] `FireActions.SniperFire`/`BurstFire` 改为细粒度屏蔽
- [ ] `QuickScopeController` 去掉自持的左键屏蔽逻辑（移交 `LeftMaskController`）
- [ ] `WeaponDispatcher.Decide` 改用稳定态准心 + `RifleEnabled` 二分
- [ ] `Form1.cs` 每帧驱动 `CrosshairStabilizer.Update` + `LeftMaskController.ApplyMask`
- [ ] `Form1.OnKmBoxMouseButtonChanged` 左键分支改为 `LeftMaskController.OnHwLeftEdge`
- [ ] 四模式回归测试：狙击自动 / 狙击代发 / 瞬狙 / 步枪会话 / 侧键点射

---

## ISSUE-018: 鼠标操作缺乏真人特征（行为层反作弊风险）

**状态**: 🔍 待校正（优先级需用户决定）
**发现日期**: 2026-04-10
**影响范围**: `FireActions` / `RifleSessionController` / 所有 `MouseMove` 调用点

### 问题描述

当前鼠标操作相比真人存在 5 个可疑点（硬件层合法，但行为层可被内容检测识别）：

| # | 位置 | 问题 | 真人表现 |
|---|---|---|---|
| 1 | `SniperFire.MouseMove(dx,dy)` | 瞬移一步到位 | 5~20ms 多点渐进移动 |
| 2 | `SniperFire`: MouseMove→MouseLeft(true) 间隔 0ms | 瞄准到按下 0ms | 视觉确认→按下 20~80ms 反应延迟 |
| 3 | `SniperFire`: 按下→切枪→释放 顺序 | 按着左键切枪 | 真人：松左键→切枪 |
| 4 | `RifleSession` 后续帧固定 150ms 间隔 | 周期化跟枪 | 真人连续非周期修正 |
| 5 | `Thread.Sleep(rnd.Next(30,51))` 范围窄 | 按键时长方差太小 | 人类 30~150ms 方差更大 |

### 待校正

- [ ] 优先级：是否需要本版本就处理？（vs 只在被检测后再优化）
- [ ] 是否接受引入"虚假运动方差"来模拟人类特征？（可能牺牲自瞄精度）
- [ ] MouseMove 分段是否会显著增加开火总时长（影响实战响应）？

### 建议方案

最小改动版：
- MouseMove 拆分 2~3 段，中间 `Thread.Sleep(rnd.Next(3, 8))`
- MouseMove 与 MouseLeft(true) 之间加 `Thread.Sleep(rnd.Next(15, 40))`
- SniperFire 顺序改为：MouseMove → MouseLeft(true) → MouseLeft(false) → 切枪 → Sleep
- RifleSession 后续帧间隔从定值改为 `rnd.Next(110, 190)`

---

## ISSUE-019: 键鼠异常的其他潜在风险点（非屏蔽相关）

**状态**: 🔍 待校正（按风险等级逐项确认是否处理）
**发现日期**: 2026-04-10
**影响范围**: 多处

### 风险清单

#### 19.1 Thread.Sleep 阻塞 YOLO 主线程
- **位置**: `FireActions.SniperFire/BurstFire` 末尾 `Thread.Sleep(150)`
- **风险**: Sleep 期间用户 Disconnect → 回来时 `_kmBox` 已 Dispose → NullRef
- **建议**: Sleep 改为 `Thread.Sleep(150)` 前快照 `_kmBox` 引用，或 Sleep 结束后 `?.` 保护后续调用

#### 19.2 MouseLeft(true) 无兜底释放
- **位置**: `RifleSessionController.HandleFrame` 首帧 `MouseLeft(true)`
- **风险**: YOLO 线程崩溃 / KmBox 中途断线 → **左键永远按着**（游戏里持续射击）
- **建议**: 加看门狗：`_sessionActive=true` 超 3 秒未 End → 强制 `MouseLeft(false)`

#### 19.3 硬件回调线程 vs YOLO 线程并发写 Mask
- **位置**: `OnKmBoxMouseButtonChanged`（硬件回调线程）调 `OnRightDownRifleMode` 会 `MaskMouseX/Y`；同时 YOLO 主线程可能在调 `MouseMove` / `MaskMouseLeft`
- **风险**: KmBox 串口通信若无内部锁 → 指令交错、状态错乱
- **建议**: 确认 `KmBoxNet` 通信是否线程安全；若否，加 `lock(_km)` 包裹所有 Mask/Move 调用

#### 19.4 准心闪烁时 ApplyRightPreMask 高频 toggle
- **位置**: `RifleSessionController.ApplyRightPreMask`
- **风险**: `rifleCrosshairVisible` 因光照/遮挡在 true/false 间快速抖 → 右键 Mask/Unmask 每帧切换 → 硬件队列堆积
- **建议**: 加去抖：切为 false 需连续 N 帧（例如 3 帧 ≈ 20ms）才真正解除

#### 19.5 Disconnect 未主动解除屏蔽
- **位置**: `DisconnectKmBox`
- **风险**: 若断开时 RifleSession XY 屏蔽中 → 设备残留屏蔽状态，下次连接前物理 XY 被设备硬件吞掉
- **建议**: Disconnect 前显式 `_kmBox.UnmaskAll()`（唯一保留的 UnmaskAll 用途）

#### 19.6 ConnectKmBox UnmaskAll 时机
- **位置**: `ConnectKmBox` 事件订阅之后才 UnmaskAll
- **风险**: 订阅→Unmask 窗口内硬件回调拿到的是"还没 Unmask"的中间态
- **建议**: 调整顺序为 Connect → UnmaskAll → 订阅事件 → MonitorEnable（此项影响小，可放最后）

### 待校正

- [ ] 19.1 Sleep 期间 null 防护：本阶段是否处理？
- [ ] 19.2 左键看门狗：3 秒阈值是否合理？
- [ ] 19.3 KmBoxNet 线程安全：是否已有测试证据？需要排查
- [ ] 19.4 准心闪烁去抖：帧数阈值取多少？
- [ ] 19.5 / 19.6 顺序调整：本阶段处理还是后推？

---

## ISSUE-020: 屏蔽职责归属总览（重构后目标态）

**状态**: 📋 规划参考
**发现日期**: 2026-04-10

### 重构后屏蔽职责表（目标态）

| 屏蔽对象 | 持有者 | 触发条件 | 解除条件 | 现状 |
|---|---|---|---|---|
| `MaskMouseLeft` | QuickScope（扩展后） | 狙击准心可见 **或** 瞬狙 Monitoring+有目标 **或** SniperFire 开火瞬间 | 无准心 / 瞬狙窗口结束 / SniperFire 结束 | 🚧 需合并职责 |
| `MaskMouseRight` | RifleSession | 步枪准心可见 或 会话活跃 | 准心消失且会话结束 | ✅ 已实现 |
| `MaskMouseX/Y` | RifleSession | 右键按下（步枪模式） | 右键抬起 | ✅ 已实现 |
| `MaskAll` | ❌ 禁用 | — | — | 🚧 阶段 3 删除 |
| `UnmaskAll` | 仅 Connect/Disconnect | 初始化 / 清理 | — | 🚧 保留 2 处 |

### 核心原则

1. **一键一主**：同一个按键的屏蔽只有一个模块负责，禁止跨模块调用 `MaskMouseXXX`
2. **bool 与硬件 1:1 对齐**：模块的 `_xxxMasked` bool 必须严格跟随自己发出的 `MaskMouseXXX` 指令，没有第三方能颠覆
3. **解除 = 对称操作**：谁开启谁负责解除，禁止 `UnmaskAll` 作为"兜底清理"使用（除 Connect/Disconnect）

### 校正后推进顺序（建议）

1. **阶段 3**（ISSUE-016）：去掉 `FireActions.MaskAll/UnmaskAll` → 改左键细粒度
2. **阶段 4**（ISSUE-017）：引入狙击准心期间左键屏蔽，和瞬狙 QuickScope 合并或挂靠
3. **阶段 5**（ISSUE-019.2）：加 RifleSession 左键看门狗
4. **阶段 6**（ISSUE-018）：真人化鼠标操作（可选）
. **并行**（ISSUE-019.3/4/5/6）：线程安全排查、准心去抖、Disconnect 清屏蔽

---

## ISSUE-021: 狙击准心偶发识别失败（1.5s 空洞窗口）

**状态**: 🔴 新发现，暂存待复现  
**发现日期**: 2026-04-10  
**影响范围**: `YoloProcessing/ImageHelper.cs` 狙击准心判定 / `Form1.cs` 狙击分支  
**当前可复现性**: ❌ 暂时无法稳定复现（文档记录，等下次触发再查）

### 现象

用户报告两个疑似相关症状（重启程序后仍在）：

1. **常规狙击模式需要左键确认才开枪**（按理开镜 + 有目标应自动代发一枚）
2. **瞬狙模式下出现未按左键的自动开枪**

用 `TestCrosshairColorForm` 的「持续观察RGB」功能采到一段关键数据：同一把狙击枪，开镜期间连续帧中出现 **约 1.5 秒判定空洞**：

```
87608ms  R=136 G= 63 B= 67  redness=72   判定=---   （识别失败）
...（中间持续失败）
89018ms  R=136 G= 63 B= 67  redness=72   判定=---   （识别失败）
89220ms  R=175 G= 19 B= 20  redness=255  判定=狙击  （恢复）
```

同时发现同一款狙击在不同状态下 RGB 完全不同：
- **稳态纯红**：`R=255 G=0 B=0` → 判定 OK
- **稳态带光晕**：中心 2×2 平均 `R=175 G=19 B=20`，但 `MaxRedness=255`（2×2 内至少 1 个像素为严格 `(255,0,0)`）→ 判定 OK
- **1.5 秒空洞**：2×2 内**没有任何**纯红像素，最高 redness 仅 72 → 判定失败

用户反馈「我记得以前是纯红色」—— 游戏可能更新过狙击准心样式（带暗红光晕）。

### 推测原因

1. **拉栓/开枪动画窗口期**：狙击开一枪后镜内准心可能短暂消失/变暗（当前无法断定是否该 1.5s 对应拉栓）
2. **采样位置落空**：2×2 扫描正好落在准心图案的空洞处
3. **判定规则 `R==255 && B==0` 过严**：无法容忍轻微色差（但稳态数据显示 2×2 内通常仍有纯红像素，真正失败的是 RGB 都跌到低值的窗口）

### 待用户复现后的测试步骤

再次遇到同症状时，按下列步骤采数据：

- **A**：按住右键开镜 10 秒，**不开枪、不瞄人**，看日志是否全程 `判定=狙击`
- **B**：开镜 → 开一枪 → 继续按住右键看镜内 → 再开一枪，观察每段的识别情况和失败段时长
- **C**：反复 `右键开镜 → 松右键退镜 → 右键开镜`，看开镜瞬间的识别延迟

三段数据到手后可判定根因：判定阈值问题 / 去抖参数问题 / 根本与准心无关。

### 可行修复方向（待数据决定，暂不实施）

| 方案 | 优点 | 缺点 |
|---|---|---|
| 扩大扫描窗口 2×2 → 5×5 / 7×7 | 空间冗余，对图案空洞健壮 | 背景复杂时误判风险上升 |
| 加容差 `R>=250 && G<=10 && B<=5` | 兼容轻微色差 | 解决不了"根本没有纯红像素"的帧 |
| 时间去抖：最近 N 帧曾识别过仍判红 | 抑制瞬时空洞 | 退镜时熄灭延迟，可能与 `_snipeFiredInCurrentScope` 边沿锁冲突 |

### 相关文件

- `YoloProcessing/ImageHelper.cs` — `ReadGameCrosshairInfo` 的 2×2 扫描、`hasSnipePixel` / `hasRiflePixel` 判定
- `Firing/CrosshairStabilizer.cs` — 跨帧去抖
- `Form1.cs` — 主循环狙击分支 / `_prevStableSnipe` / `_snipeFiredInCurrentScope` 边沿锁
- `TestCrosshairColorForm.cs` — 已加「持续观察RGB」独立通道，供用户采样
- `Program.cs` — `#define TEST_CROSSHAIR` 开关

### 暂存决定（2026-04-10）

本次会话结束时**未修改代码**。等再次触发时按「待用户复现后的测试步骤」补齐数据后，再决定修复方向。

---

## 🔴 ISSUE-022：真人模式瞬狙成功率低（换 RTX5060 后加剧）

> 背景：真人模式下狙击枪右键后马上左键，计划应等待开镜→单帧 YOLO→有人自瞄/无人盲射，实测成功率很低。
> 为诊断新建了 `TestCrosshairStateForm`（`TEST_CROSSHAIR_STATE` 宏），复刻主模块真人模式原生链路并全程记录边沿事件。

### 首轮测试数据（2026-08，RTX5060）

1. **开镜延迟 20~40ms，最高 65ms** ≪ WaitForScopeMs(100ms) → **排除 Waiting 超时主因**
2. 零星出现「先右键后左键」却走了 **未开镜左键意图→盲射**（而非 Monitoring 代发）
3. 偶有敌人但未触发自瞄，变成盲射准心中心
4. YOLO 推理时间偶尔变长；从 RTX5090 换到 RTX5060 后全面恶化

### 根因分析

**现象 2 的两条泄漏路径（可用 CSV 前导事件区分）：**

- **路径 A（首选疑）**：Monitoring 中 `snipeEnabled` 闪丢 1 帧（开镜动画中纯红像素瞬间不满足 R=255&&B=0）→
  `TryHandle` 直接放行该帧（无宽容）→ `!snipeEnabled` 落入 step 2.5 fast-path → 挂起意图被消费盲射。
  识别特征：Fast-path 事件前紧邻「狙击已开镜→其它」准心边沿。
- **路径 B**：Waiting 期间始终未检测到开镜 → 100ms 超时 PassThrough → 同帧 fast-path 消费意图。
  识别特征：Fast-path 前有「Waiting→PassThrough」且右键后无「开镜延迟」诊断事件。
- 设计缺口：Monitoring 期间的第二次右键按下被直接丢弃（只在非 Monitoring 态才进 Waiting），连狙双右键时序失控。

**现象 3**：开火决策只用消费意图那一帧的单帧 YOLO 结果，无目标记忆/重试，漏检一帧即盲射。

**现象 4**：Monitoring 窗口 100ms 在 5060 上只有 2~3 次 YOLO 机会，一次推理抖动即吃掉窗口；
engine 在 5090 上构建，5060（同为 sm_120 可运行）未必走最优 tactic。

### 修复方向（待数据确认后实施）

| 方案 | 解决 | 代价 |
|---|---|---|
| 测试窗体埋点推理耗时（`GetPerformance()`） | 验证现象 3/4 | 极小 |
| 纯调参：监控窗口 100→150/200ms 实测 | 现象 4 | 0 |
| 目标记忆：意图帧无目标时用 K ms（~120ms）内缓存目标 | 现象 3 | 小 |
| 意图等目标：无目标不立即盲射，窗口内多等 ≤M ms | 现象 3（对慢卡最有效） | 小 |
| 状态机防闪丢：snipeEnabled 需连续 2 帧丢失才放行 | 现象 2 路径 A | 小 |
| 5060 上重建 engine / 评估 M 模型 | 现象 4 根本性能 | 需在目标机执行 |
| Monitoring 中第二次右键重新进 Waiting | 连狙双右键 | 需语义确认 |

### 相关文件

- `Firing/QuickScopeController.cs` — 状态机（泄漏点：`TryHandle` 中 `!snipeEnabled` 放行分支、Monitoring 丢右键）
- `TestCrosshairStateForm.cs` — 诊断窗体（事件列表/CSV 可区分路径 A/B）
- `Program.cs` — `#define TEST_CROSSHAIR_STATE` 开关

---
