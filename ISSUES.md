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

**状态**: 延时公式待调试  
**发现日期**: 2025-01-XX  
**影响范围**: 射击模式触发逻辑

### 已实现内容

**触发方式**: 左键按下触发（红点优先）

**模式优先级**:
```
红点(狙击) > 左键(步枪) > 侧键(点射)
```

**实现逻辑**:
- 步枪模式使用 `Trace(2, 80)` 开启贝塞尔实时曲线
- 狙击/点射模式使用 `Trace(0, 0)` 关闭曲线，直接移动
- 步枪模式只移动不开枪，用户自己开枪

### 待调试: 延时公式

**当前公式** (`GameConfig.RifleConfig`):
```csharp
// 基准: 50ms 对应距离 226 (≈√(160²+160²))
BaseDelayMs = 50
BaseDistance = 226.0

// 计算: delay = 50 * (distance / 226)
// 最小 10ms
```

**待测试确认**:
- [ ] 距离较短时延时是否合理
- [ ] 距离较长时延时是否足够
- [ ] 公式系数是否需要调整

### 相关文件
- `Form1.cs` - `ProcessYoloFrame()`, `ExecuteFireAction()`
- `Utils/GameConfig.cs` - `FireMode.Rifle`, `RifleConfig`
