# DualPC-TensorRT-Aim-KMBOX（gprs）

基于双机架构的 TensorRT 加速 AI 视觉瞄准系统（美乐威采集卡 + KmBox Net 网络版）。

## 项目概述

运算机 + 游戏机分离：采集卡捕获游戏画面 → YOLOv8-Pose 识别人体姿态 → 目标选择 → KmBox Net（UDP）控制游戏机鼠标。
武器模式通过**画面中心准心像素判定**自动区分（狙击/步枪），无需手动切换，详见 [DESIGN.md](DESIGN.md)。

## 系统架构

```
┌─────────────────────────── 运算机 ───────────────────────────┐
│  美乐威 Pro Capture PCIE ──▶ 640×640 截取 ──▶ TensorRT YOLOv8-Pose │
│        (2K 144Hz)          (~5ms)              (10~20ms)     │
│                                   │                          │
│                          目标选择（23 部位系统）                │
└───────────────────────────────────┼──────────────────────────┘
                                    │ UDP (192.168.3.188:8888)
┌─────────────── 游戏机 ────────────┼──────────────────────────┐
│  显卡 HDMI ──▶ 采集卡              ▼                          │
│                          KmBox Net ──▶ USB HID ──▶ 游戏        │
│  物理鼠标 ──▶ KmBox Net ──▶ 游戏（可被软件按位屏蔽/透传）        │
└──────────────────────────────────────────────────────────────┘
```

## 实测性能（2026-01 测试环境）

> 历史文档曾宣称总延迟 ~12ms，**已废弃**。以下为实测口径，完整拆解见 [docs/LATENCY.md](docs/LATENCY.md)。

| 环节 | 实测 | 说明 |
|------|------|------|
| 采集卡（2K 144Hz 低延迟模式） | ~5ms | 符合 1/144Hz ≈ 6.9ms 理论值 |
| 准心检测 | 0.2-0.4ms | 中心 2×2 像素判定 |
| YOLO 推理 | L: 10.8-19.5ms / **M: ~7ms**（RTX5060） | 视模型与显卡而定，是延迟大头 |
| 目标选择 | <1ms | — |
| KmBox Net（UDP + USB HID） | 数 ms | 事件推送式监听，非轮询 |
| **端到端** | **20-30ms** | 反应测试网页实测 |

**注意**：RTX5090 → RTX5060 换卡后 L 模型推理抖动加剧，瞬狙成功率下降，调查见 [ISSUES.md](ISSUES.md) ISSUE-022；
2026-08-06 实测 RTX5060 上 M 模型端到端推理仅 ~7ms，Monitoring 窗口内推理机会从 2~3 次提升到 ~14 次（详见 [docs/LATENCY.md](docs/LATENCY.md)）。
TensorRT engine 与构建显卡绑定，换卡必须重建 engine。

## 硬件要求

### 运算机
- **显卡**：NVIDIA RTX 系列（推理卡，engine 按此卡构建）
- **采集卡**：美乐威 Pro Capture HDMI（PCIE 版）
- **系统**：Windows 10/11 x64

### 游戏机
- **KmBox Net**：网络版键鼠透传盒子（UDP 协议），接在物理鼠标与游戏机之间
- **连接**：游戏机 HDMI 输出到采集卡；运算机与 KmBox 同一局域网

## 软件依赖

- **.NET 9.0**
- **CUDA 12.x** + **cuDNN 9.x** + **TensorRT 10.x**（TensorRT-Solutions/TensorRT-YOLO 运行时 DLL 已随项目附带于 `gprs/TensorRT/`）
- **美乐威 MWCapture SDK**（`gprs/MWCapture/LibMWCapture.dll` 已附带）

## 安装与配置

### 1. 模型准备

> ⚠️ **engine 文件与显卡硬件绑定，更换显卡必须重新导出！**

使用 [TensorRT-YOLO](https://github.com/laugh12321/TensorRT-YOLO) 在**运算机当前显卡**上导出：

```bash
trtyolo export -w yolov8l-pose.pt -v yolov8 -o ./models --fp16
```

将所有 `*-pose.engine` 放入程序目录下 `Models/`（程序启动时自动扫描）。

### 2. KmBox Net 配置

主界面填写（默认值已内置）：

| 项 | 默认值 |
|---|---|
| IP | 192.168.3.188 |
| 端口 | 8888 |
| UUID | 12345678 |

连接后程序自动开启物理键鼠监控（`MonitorEnable(9527)`，UDP 事件推送）。

### 3. 编译运行

```bash
# Visual Studio 2022 打开 gprs.sln，编译 Release x64
# 或命令行：
dotnet build gprs/gprs.csproj -c Release
```

### 4. 游戏内准备

- 步枪准心设置为**黄色 (255,255,0)**（系统靠准心颜色区分武器模式，见 DESIGN.md）
- 狙击开镜后为纯红准心，无需设置

## 测试入口（编译宏）

`Program.cs` 顶部通过 `#define` 切换启动窗体（默认全注释 = 主程序）：

| 宏 | 窗体 | 用途 |
|---|---|---|
| （无） | Form1 | 主程序 |
| `TEST_MODE` | TestMoveForm | HumanLikeMove 鼠标轨迹测试 |
| `TEST_RIFLE` | TestRifleForm | ISSUE-013 步枪模式隔离测试（无 YOLO） |
| `TEST_SNIPER` | TestSniperForm | 狙击反作弊触发测试（瞬移+开枪时序快照） |
| `TEST_CROSSHAIR` | TestCrosshairColorForm | 步枪准心命中颜色采样（持续观察 RGB） |
| `TEST_CROSSHAIR_STATE` | TestCrosshairStateForm | 准心状态监视：复刻真人模式原生链路，四态/RGB/状态机时序记录 + CSV 导出 |

## 项目结构

```
gprs - 采集卡2.3/
├── README.md / DESIGN.md / ISSUES.md      # 三大件：入口 / 设计 / 问题追踪
├── docs/                                  # 次级文档（延迟专题 / 参考调研 / 归档）
└── gprs/                                  # 主程序
    ├── Form1.cs                           # 主窗体 + YOLO 主循环（KmBox 指令唯一源头）
    ├── Program.cs                         # 启动入口（#define 测试宏切换）
    ├── Firing/                            # 射击控制层（阶段 1~2 解耦产物）
    │   ├── WeaponDispatcher.cs            #   武器模式调度
    │   ├── QuickScopeController.cs        #   真人模式瞬狙四态状态机
    │   ├── RifleSessionController.cs      #   步枪会话（右键=开火周期）
    │   ├── LeftMaskController.cs          #   狙击左键屏蔽 + 物理边沿意图捕获
    │   └── FireActions.cs                 #   开火动作（分段移动 + 细粒度 XY 屏蔽）
    ├── YoloProcessing/
    │   ├── ImageHelper.cs                 #   准心检测 + YOLO 推理封装
    │   ├── TargetSelector.cs              #   目标选择（23 部位系统）
    │   └── DebugRenderer.cs               #   调试渲染
    ├── KmBox/KmBoxNet.cs                  # KmBox Net UDP 协议封装
    ├── MWCapture/                         # 美乐威采集卡 SDK 封装
    ├── TensorRT/                          # TensorRT-YOLO C 封装 + 运行时 DLL
    ├── Utils/GameConfig.cs                # 游戏配置（分辨率/灵敏度等）
    └── Test*Form.cs                       # 各测试窗体（见上表）
```

## 文档索引

| 文档 | 内容 |
|---|---|
| [DESIGN.md](DESIGN.md) | 架构与机制设计：准心判定、步枪会话、真人模式状态机、KmBox Mask 机制、模块划分 |
| [ISSUES.md](ISSUES.md) | 问题追踪（ISSUE-001~022），活跃问题见顶部状态表 |
| [docs/LATENCY.md](docs/LATENCY.md) | 端到端延迟专题：拆分实测、采集卡低延迟配置结论、待验证项 |
| [docs/REFERENCE-PROJECTS.md](docs/REFERENCE-PROJECTS.md) | 参考项目调研快照（2025-01-28） |
| [docs/archive/](docs/archive/) | 文档大改版前的原件快照（仅供追溯） |

## 文档维护约定

1. **单一事实来源**：每个主题只在一处文档维护（设计→DESIGN、问题→ISSUES、延迟→LATENCY），其它文档只引用不重复；
2. **以代码为准**：文档与代码矛盾一律按代码修正文档；修改行为类代码时同步检查 DESIGN/ISSUES 相关章节；
3. **新增问题**：在 ISSUES.md 按 `ISSUE-XXX` 模板追加，并同步更新顶部状态表；
4. **大改版前归档**：结构性重写任何文档前，先把原件整份复制到 `docs/archive/` 并登记。

## 注意事项

1. **模型兼容性**：engine 与构建显卡绑定，换卡必须重新导出（ISSUE-022 的教训）
2. **采集卡驱动**：保持最新美乐威驱动
3. **TensorRT 版本**：SDK 版本须与 `gprs/TensorRT/` 下 DLL 匹配
4. **仅供学习研究**：请勿用于破坏游戏公平性

## 相关链接

- [TensorRT-YOLO](https://github.com/laugh12321/TensorRT-YOLO) — 模型导出工具
- [美乐威 SDK](https://www.magewell.com/downloads) — 采集卡 SDK
- [原版仓库 v3.0](https://github.com/ai123lj/DualPC-TensorRT-Aim) — TensorRT 加速版
- [原版仓库 v2.x](https://github.com/ai123lj/Ai-Aim-Dual-Computer) — ONNX 版本

## License

MIT License
