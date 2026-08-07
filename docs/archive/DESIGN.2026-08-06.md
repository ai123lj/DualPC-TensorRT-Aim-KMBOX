# 辅助瞄准系统设计文档

## 系统概述

基于采集卡 + KmBox + YOLO 姿态检测的 FPS 辅助瞄准系统。  
采集卡捕获游戏画面 → YOLO 识别人体目标 → KmBox 控制鼠标移动/点击。

### 硬件链路

```
鼠标 → KmBox → 主机
         ↑
      软件控制（UDP）
```

- **KmBox** 接在鼠标和主机之间，可屏蔽物理按键/移动，也可通过软件命令直接控制虚拟鼠标输出
- **Mask（屏蔽）**：阻止物理鼠标的指定消息传递给主机，但 KmBox 仍能检测到物理按键状态
- **软件命令**（MouseMove/MouseLeft）：直接控制虚拟输出，不受 Mask 影响

---

## 武器模式判定机制

### 即时准心方案

系统通过 **准心检测** 区分步枪模式和狙击模式，而非依赖用户手动切换。

```
每帧检测准心 → UpdateRifleCrosshair 写入当前步枪准心状态
右键按下时 → 读 IsRifleModeNow（当前帧是否步枪准心）
  → 是 → 步枪模式行为（XY屏蔽）
  → 否 → 狙击模式行为（瞬狙预屏蔽）
```

### 准心判定规则

由 `ImageHelper.ReadGameCrosshairInfo` 采样中心 2×2 区域得出，同帧内狙/步互斥：

- 准心像素：`R=255 && B=0`（游戏场景极少出现，命中即准心或命中特效）
- `G=0` → 狙击（纯红准心）；`G>0` → 步枪（黄色准心，且涵盖命中渐变红→橙→黄全段）
- `SnipeEnabled = hasSnipePixel`；`RifleEnabled = !hasSnipePixel && hasRiflePixel`

### 为什么不再需要去抖

早期准心判定用 redness/yellowness 阈值，**命中敌人时准心变色**会造成模式误判闪烁，因此先后叠加过两套去抖，现已全部移除：

| 代次 | 机制 | 移除原因 |
|------|------|---------|
| 第一代 | 右键事件用“最近 50ms 内见过步枪准心”时间窗 | 切狙后 50ms 内按右键被误判为步枪 → 右键被屏蔽，**狙击镜打不开** |
| 第二代 | `CrosshairStabilizer` 帧数去抖（狙升 1 帧 / 其余 3 帧） | 阈值不对称使狙/步稳定态重叠 2~3 帧，切狙后仍走步枪分支 → 左键未屏蔽，**首枪辅助不触发**；反向切步则左键残留屏蔽/误代发一枪 |

新判定（`R=255 && B=0` + G 通道分流）从源头消除闪烁、且两态天然互斥，因此每帧直接使用检测结果，**切枪 0 帧延迟**。实战已验证正常。

### 游戏特性利用

- 步枪：准心始终可见（非开镜状态也有），切到步枪后几帧内准心就出现
- 狙击：只有开镜才有红色准心，不开镜时无准心
- 切枪动画期间：两种准心都不会出现，形成天然的"模式隔离窗口"

---

## 步枪会话模式

### 核心设计

用户通过 **右键按压时长** 控制点射/连发，更符合游戏直觉：

```
右键按下 → 软件 MouseLeft(true) 开火
右键保持 → 有目标时辅助瞄准（MouseMove），无目标时用户自控弹道
右键释放 → 软件 MouseLeft(false) 停火
```

### XY 屏蔽机制

**问题**：右键按下到 YOLO 推理完成有 ~5-15ms 延迟，期间用户可能手动移动鼠标造成冲突。

**解决**：右键按下时立即屏蔽物理鼠标 XY 轴移动。

```
右键按下（事件回调，零延迟）
  → MaskMouseX(true) + MaskMouseY(true)
  → 物理鼠标移动被屏蔽，用户能感知"鼠标锁定"
  → 软件 MouseMove（自瞄）不受影响，走独立命令通道

右键释放
  → MaskMouseX(false) + MaskMouseY(false)
  → 物理鼠标恢复自由
```

**为什么只屏蔽 XY 不屏蔽按键**：MaskAll（包含按键屏蔽）会重建 HID 报告，导致软件设置的左键保持状态被清除。只屏蔽 XY 轴（0x20, 0x40 位）不影响按键位（0x01, 0x02），软件 MouseLeft 状态安全。

### 左键时长补偿

**问题**：YOLO 推理有延迟，用户按右键 100ms，但左键可能只被按了 85ms（YOLO 吃掉 15ms）。

**解决**：右键释放时计算差值并补偿。

```
右键按下 → 记录 _rifleRightDownTimestamp
首帧 MouseLeft(true) → 记录 _rifleLeftDownTimestamp
右键释放 → 计算：
  rightDuration = now - _rifleRightDownTimestamp
  leftElapsed   = now - _rifleLeftDownTimestamp
  remaining     = rightDuration - leftElapsed
  if remaining > 0 → Thread.Sleep(remaining) → 再释放左键
```

### 首帧射击顺序

**原则**：必须先瞄准，再开火。

```
首帧有目标 → MouseMove(瞄准) → MouseLeft(true)(开火)
首帧无目标 → MouseLeft(true)(盲射，用户自控弹道)
后续帧     → 仅辅助瞄准，左键已保持
```

**为什么不能提前开火**：如果在 YOLO 推理前就按下左键，第一发子弹会打在移动前的位置，自瞄的第一发必然打不到目标。

### 准心闪失保护

**问题**：射击时的枪口动画/后坐力会暂时干扰准心检测，`crosshair.RifleEnabled` 返回 false。如果直接结束会话释放左键，连发就变成了点射。

**解决**：在模式判定的 else 分支中，如果会话仍然活跃且右键仍按着，保持会话不结束。

```csharp
else
{
    // 步枪会话中准心可能被射击动画/后坐力暂时干扰
    // 右键仍按着则保持会话，会话只由右键释放事件结束
    if (_rifleSessionActive && _kmBox.IsMouseRightDown())
        return;
    EndRifleSession();
    return;
}
```

### 自瞄移动节流

**问题**：采集卡有 ~5ms 采集延迟，连续快速移动可能基于移动前的旧画面做决策；移动频率过高也可能被游戏反作弊检测。

**解决**：首次移动立即执行，后续移动限制为 150ms 间隔。

```
RIFLE_MOVE_INTERVAL_MS = 150
首帧：MouseMove 立即执行
后续帧：检查距上次移动是否 >= 150ms，否则跳过
```

---

## 瞬狙干预模式

### 三态状态机

```
Idle → Monitoring → PassThrough
```

- **Idle**：空闲，等待狙击准心出现
- **Monitoring**：监控窗口内，每帧 YOLO 检测。有目标时屏蔽左键，等用户点击时自瞄替代开火
- **PassThrough**：窗口超时或已开火，不再干预

### 右键预屏蔽

**问题**：右键按下（开镜）到狙击准心出现有延迟（开镜动画），这段时间内用户可能已经点击左键开火，但此时系统还未检测到目标，左键直接生效 → 打空。

**解决**：在右键按下时（仅狙击模式判定下）立即屏蔽左键，等准心出现后由状态机接管。

```
右键按下（判定为狙击模式）→ MaskMouseLeft(true)
准心出现 → 预屏蔽平滑过渡到 Monitoring 状态
非狙击准心或超时 → 解除预屏蔽
```

### 条件左键屏蔽

**原则**：只在镜中有人时屏蔽左键，无人时用户可自由开枪（非辅助场景）。

```
Monitoring 中：
  有目标 → MaskMouseLeft(true)，等用户点击 → 自瞄替代开火
  无目标 → MaskMouseLeft(false)，用户自由开枪
```

---

## KmBox Mask 机制详解

### Mask 标志位

| 位 | 含义 | 方法 |
|----|------|------|
| 0x01 | 屏蔽左键 | MaskMouseLeft |
| 0x02 | 屏蔽右键 | MaskMouseRight |
| 0x20 | 屏蔽X轴移动 | MaskMouseX |
| 0x40 | 屏蔽Y轴移动 | MaskMouseY |

### 关键踩坑记录

1. **MaskAll 会清除软件按键状态**：MaskAll 发送包含按键屏蔽位的掩码，KmBox 固件重建 HID 报告时会清除软件设置的左键保持状态。UnmaskAll 后恢复物理状态（用户没按左键→左键释放）。  
   → **解决**：步枪会话中不使用 MaskAll，只使用 MaskMouseX/Y（不影响按键位）。

2. **MouseMove 的 button 参数问题**：原始 `MouseMove` 方法传 `button=0`（`BuildMouseData(0, x, y, 0)`），可能在某些固件版本中重置按键状态。已修复为使用 `_mouseButton`（当前软件按键状态）。

3. **Mask 命令是累积式的**：每次 MaskMouseX/Y/Left 只修改对应位，不影响其他位。多个 Mask 操作可以安全叠加（如瞬狙预屏蔽左键 + 步枪 XY 屏蔽互不干扰）。

---

## UI 开关说明

| 控件 | 默认值 | 作用 |
|------|--------|------|
| 瞬狙干预 | 勾选 | 开启瞬狙干预模式（三态状态机 + 条件左键屏蔽） |
| 窗口(ms) | 100000 | 瞬狙监控窗口时长，超时后不再干预 |
| 狙击切枪 | 未勾选 | 狙击开火后自动滚轮切枪 |
| 步枪打头 | 勾选 | 步枪模式锁定头部（取消勾选锁定身体）。可按**鼠标侧键1**快速切换，无需手动点 UI |

### 初始化注意事项

Designer 中设置的 `Checked = true` / `Text = "..."` 只影响 UI 显示，不会自动触发 `CheckedChanged` 事件。因此在 `Form1_Load` 中统一从 UI 控件读取初始值：

```csharp
_quickScopeMode = chkQuickScopeMode.Checked;
_autoSwitchWeapon = chkAutoSwitchWeapon.Checked;
_rifleLockHead = chkRifleLockHead.Checked;
_quickScopeWindowMs = int.Parse(txtQuickScopeWindow.Text);
```

---

## 双模型架构

狙击和步枪使用不同 YOLO 模型，通过 UI 下拉框选择：

- **狙击模型**（默认 L）：精度高，推理慢，适合单发场景
- **步枪模型**（默认 S）：推理快，适合连发场景需要快速响应

模型在启动时加载，如果两个下拉框选择相同模型则共用实例，避免重复加载。
