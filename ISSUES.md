# 待解决问题记录

本文档记录项目中发现的待解决问题、临时修复方案及其来龙去脉，便于后续彻底解决。

---

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
