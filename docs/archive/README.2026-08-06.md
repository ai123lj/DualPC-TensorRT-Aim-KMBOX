# DualPC-TensorRT-Aim-KMBOX

基于双机架构的 TensorRT 加速 AI 视觉瞄准系统（KmBox + 美乐威采集卡版）

## 项目概述

这是一个高性能的双机协同视觉瞄准系统，采用 **运算机 + 游戏机** 分离架构，通过 TensorRT 加速 YOLOv8-Pose 模型实现超低延迟的人体姿态检测与自动瞄准。

### 核心特点

- **极致低延迟**：总延迟约 **12ms**（采集 5ms + 推理 5ms + 控制 2ms）
- **TensorRT 加速**：YOLOv8L-Pose 模型推理仅需 **~5ms**
- **双机架构**：运算与游戏分离，游戏机零性能损耗
- **KmBox 硬件控制**：UDP 网络通信 + USB HID 模拟，支持贝塞尔曲线

## 系统架构

```
┌──────────────────────────────────────────────────────────────────┐
│                          运算机                                   │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐          │
│  │ Pro Capture │───▶│  图像处理   │───▶│  TensorRT   │          │
│  │ PCIE采集卡  │    │  640×640    │    │ YOLOv8-Pose │          │
│  │ 2K 144Hz    │    │    ~5ms     │    │    ~5ms     │          │
│  └──────┬──────┘    └─────────────┘    └──────┬──────┘          │
│         │                                      │                 │
│         │ HDMI输入                             ▼                 │
│         │                             ┌─────────────┐           │
│         │                             │  目标选择   │           │
│         │                             │ 23部位系统 │           │
│         │                             └──────┬──────┘           │
│         │                                    │ UDP网络         │
└─────────┼────────────────────────────────────┼───────────────────┘
          │                                    │
          │                                    ▼
┌─────────┼────────────────────────────────────────────────────────┐
│         │                 游戏机                                  │
│         │                                                        │
│  ┌──────┴──────┐                      ┌─────────────┐           │
│  │  显卡 HDMI  │                      │   KmBox    │◀── UDP    │
│  │    输出     │                      │  USB鼠标   │           │
│  └─────────────┘                      └──────┬──────┘           │
│                                              │ USB              │
│                                              ▼                  │
│                                       ┌─────────────┐           │
│                                       │  游戏机USB  │           │
│                                       │   鼠标输入  │           │
│                                       └─────────────┘           │
└──────────────────────────────────────────────────────────────────┘
```

## 延迟分析

| 环节 | 延迟 | 说明 |
|------|------|------|
| 采集卡 | ~5ms | Pro Capture @ 2K 144Hz 低延迟模式 |
| 图像推理 | ~5ms | TensorRT + YOLOv8L-Pose |
| KmBox | ~2ms | UDP 网络 + USB HID |
| **总延迟** | **~12ms** | 端到端延迟 |

## 硬件要求

### 运算机
- **显卡**：NVIDIA RTX 系列（推荐 RTX 4060 及以上）
- **采集卡**：美乐威 Pro Capture HDMI（PCIE 版本）
- **系统**：Windows 10/11 x64

### 游戏机
- **KmBox**：USB HID 鼠标/键盘模拟器
- **连接**：HDMI 输出到采集卡，KmBox USB 连接游戏机
- **通信**：运算机与 KmBox 通过 UDP 网络通信

### 推荐配置参数
- 采集分辨率：2560×1440 @ 144Hz（低延迟约 5ms）
- 备选：1920×1080 @ 60Hz（低延迟约 15ms，采集卡成本更低）

## 软件依赖

- **.NET 9.0** 或更高版本
- **CUDA 12.x** + **cuDNN 9.x**
- **TensorRT 10.x**
- **美乐威 SDK**（MWCapture）

## 安装与配置

### 1. 环境准备

```bash
# 安装 CUDA、cuDNN、TensorRT
# 配置系统环境变量 TRT_LIB_PATH 指向 TensorRT lib 目录
```

### 2. 模型导出

> ⚠️ **重要**：TensorRT Engine 文件与硬件绑定，不同显卡需重新导出！

使用 [TensorRT-YOLO](https://github.com/laugh12321/TensorRT-YOLO) 导出模型：

```bash
# 从官方 YOLO PT 模型导出
trtyolo export -w yolov8l-pose.pt -v yolov8 -o ./models --fp16
```

### 3. 编译运行

```bash
# 使用 Visual Studio 2022 打开 gprs.sln
# 编译 Release x64 版本
# 将 Engine 模型文件放置到程序目录
```

### 4. KmBox 配置

将 KmBox 硬件单独配置：
- 波特率：115200
- USB HID 鼠标/键盘模式

## 项目结构

```
DualPC-TensorRT-Aim-KMBOX/
├── gprs/                       # 主程序
│   ├── Form1.cs                # 主窗口逻辑
│   ├── MWCapture/              # 美乐威采集卡 SDK 封装
│   ├── TensorRT/               # TensorRT 推理封装
│   │   └── TrtYoloPoseNativeInferencer.cs
│   ├── YoloProcessing/         # YOLO 数据处理
│   │   ├── TargetSelector.cs   # 目标选择算法（23部位系统）
│   │   ├── ImageHelper.cs      # 图像处理
│   │   └── DebugRenderer.cs    # 调试渲染
│   ├── KmBox/                  # KmBox 硬件控制封装
│   │   └── KmBoxNet.cs         # 网络通信封装
│   └── Utils/                  # 工具类
│       └── GameConfig.cs       # 游戏配置
├── ISSUES.md                   # 问题追踪文档
└── gprs.sln                    # 解决方案文件
```

## 版本更新

### v4.0 (KmBox + 美乐威采集卡版)

#### 目标选择架构重构
- 🎯 **23 部位系统**：17 原始姿态点 + 5 组合部位 + 1 兜底
  - 新增组合部位：额头1、额头2、双肩中点、胸、双髋中点
- 📊 **独立阈值配置**：每个部位单独设置置信度阈值，方便游戏内调试
- 🔄 **优先级表系统**：锁头/锁身体两套独立优先级表
- ✨ **简化 API**：`bool lockHead` 参数替代复杂的部位枚举
- 🚨 **尸体检测**：基于宽高比和姿态点位置过滤倒地目标

#### 硬件控制优化
- 🔌 **KmBox 集成**：新增 KmBox 硬件控制模块
- 📡 **网络通信**：支持 UDP 网络控制协议
- 🎮 **贝塞尔曲线**：支持平滑鼠标轨迹控制

#### 性能优化
- ♻️ **对象复用**：LockResult、PartInfo 缓存复用，零 GC 压力
- 🚀 **栈分配优化**：阈值数组使用 stackalloc，避免堆分配
- 📉 **代码精简**：移除冗余代码，减少 500+ 行

#### 鼠标移动优化
- 🎯 **FOV 转换**：新增 atan2 非线性转换算法，接近目标时精细移动，远离时快速追踪
- 🔀 **双模式切换**：UI 开关实时切换线性/FOV转换，方便对比测试

### v3.0 (TensorRT 加速版)
- 🚀 **TensorRT 加速**：推理延迟从 ~15ms 降至 ~5ms
- 👁️ **小目标识别**：改进小目标和部分遮挡目标的识别能力
- 🏗️ **代码重构**：优化项目结构，模块化设计

### v2.2
- 基础 ONNX 推理版本
- 详见原仓库 [Ai-Aim-Dual-Computer](https://github.com/ai123lj/Ai-Aim-Dual-Computer)

## 使用说明

1. 连接硬件：HDMI 线连接游戏机显卡到采集卡，KmBox USB 连接游戏机
2. 配置网络：确保运算机与 KmBox 在同一局域网，配置 IP 和端口
3. 启动程序：运行编译后的 gprs.exe
4. 调整参数：根据游戏调整灵敏度和瞄准点

## 注意事项

1. **模型兼容性**：Engine 文件与显卡硬件绑定，更换显卡需重新导出
2. **采集卡驱动**：确保安装最新美乐威驱动
3. **TensorRT 版本**：确保 TensorRT SDK 版本与 DLL 匹配
4. **仅供学习研究**：请勿用于破坏游戏公平性

## 相关链接

- [TensorRT-YOLO](https://github.com/laugh12321/TensorRT-YOLO) - 模型导出工具
- [美乐威 SDK](https://www.magewell.com/downloads) - 采集卡 SDK
- [原版仓库 v3.0](https://github.com/ai123lj/DualPC-TensorRT-Aim) - TensorRT 加速版
- [原版仓库 v2.x](https://github.com/ai123lj/Ai-Aim-Dual-Computer) - ONNX 版本

## License

MIT License
