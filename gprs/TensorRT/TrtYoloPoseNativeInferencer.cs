/**
 * TensorRT-YOLO Pose 高性能推理模块
 * 
 * 提供与 YoloSharp 兼容的 API，实现无缝替换
 * 性能: GPU ~2ms, 总计 ~4ms (vs YoloSharp ~6ms GPU, ~35ms 总计)
 * 
 * 依赖文件 (需放在运行目录):
 * - TrtYoloPoseNative.dll  (C++ 推理封装)
 * - custom_plugins.dll     (TensorRT 自定义插件)
 * - yolov8*-pose.engine    (TensorRT 引擎文件)
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace gprs;

#region YoloSharp 兼容数据类型

/// <summary>
/// 关键点数据 - 兼容 Compunet.YoloSharp.Data.Keypoint
/// </summary>
public readonly struct Keypoint
{
    /// <summary>关键点坐标</summary>
    public Point Point { get; init; }
    
    /// <summary>置信度 (0-1)</summary>
    public float Confidence { get; init; }
    
    public Keypoint(int x, int y, float confidence)
    {
        Point = new Point(x, y);
        Confidence = confidence;
    }
}

/// <summary>
/// 姿态检测结果 - 兼容 Compunet.YoloSharp.Data.Pose
/// </summary>
public class Pose
{
    private readonly Keypoint[] _keypoints;
    
    /// <summary>边界框</summary>
    public Rectangle Bounds { get; init; }
    
    /// <summary>置信度 (0-1)</summary>
    public float Confidence { get; init; }
    
    /// <summary>
    /// 通过索引访问关键点 (COCO 格式 17 个点)
    /// </summary>
    public Keypoint this[int index] => _keypoints[index];
    
    public Pose(Rectangle bounds, float confidence, Keypoint[] keypoints)
    {
        Bounds = bounds;
        Confidence = confidence;
        _keypoints = keypoints ?? new Keypoint[17];
    }
    
    /// <summary>
    /// COCO 关键点索引
    /// </summary>
    public static class Index
    {
        public const int Nose = 0;
        public const int LeftEye = 1;
        public const int RightEye = 2;
        public const int LeftEar = 3;
        public const int RightEar = 4;
        public const int LeftShoulder = 5;
        public const int RightShoulder = 6;
        public const int LeftElbow = 7;
        public const int RightElbow = 8;
        public const int LeftWrist = 9;
        public const int RightWrist = 10;
        public const int LeftHip = 11;
        public const int RightHip = 12;
        public const int LeftKnee = 13;
        public const int RightKnee = 14;
        public const int LeftAnkle = 15;
        public const int RightAnkle = 16;
    }
}

/// <summary>
/// YOLO 检测结果集合 - 兼容 Compunet.YoloSharp.Data.YoloResult&lt;T&gt;
/// 优化：使用预分配数组，避免 GC 压力
/// </summary>
public class YoloResult<T> : IReadOnlyList<T> where T : class
{
    private readonly T[] _items;
    private int _count;
    
    /// <summary>创建预分配容量的结果集</summary>
    /// <param name="capacity">预分配容量（默认 16，足够大多数场景）</param>
    public YoloResult(int capacity = 16)
    {
        _items = new T[capacity];
        _count = 0;
    }
    
    public T this[int index] => _items[index];
    public int Count => _count;
    
    /// <summary>添加项目（不会触发数组扩容）</summary>
    public void Add(T item)
    {
        if (_count < _items.Length)
            _items[_count++] = item;
    }
    
    /// <summary>清空结果（不释放内存，仅重置计数）</summary>
    public void Clear() => _count = 0;
    
    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < _count; i++)
            yield return _items[i];
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

#endregion

#region Native Library Loader

/// <summary>
/// 原生库加载器 - 确保在加载 engine 之前先加载 TensorRT 插件
/// </summary>
internal static class NativeLibraryLoader
{
    private static bool _initialized;
    private static readonly object _lock = new object();
    
    /// <summary>
    /// TensorRT 相关 DLL 子目录名称
    /// </summary>
    public const string SubDirectory = "TensorRT";
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LoadLibrary(string dllPath);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetDllDirectory(string lpPathName);
    
    /// <summary>
    /// 初始化原生库路径
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;
        
        lock (_lock)
        {
            if (_initialized) return;
            
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var trtSubDir = Path.Combine(appDir, SubDirectory);
            
            // 设置 TensorRT SDK 搜索路径
            var tensorRtPaths = new[]
            {
                @"C:\Program Files\NVIDIA GPU Computing Toolkit\TensorRT-10.10.0.31\lib",
                @"C:\Program Files\NVIDIA GPU Computing Toolkit\TensorRT\lib",
                Environment.GetEnvironmentVariable("TRT_LIB_PATH") ?? "",
            };
            
            foreach (var path in tensorRtPaths)
            {
                if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                {
                    SetDllDirectory(path);
                    break;
                }
            }
            
            // 按依赖顺序预加载 DLL (优先子目录，其次根目录)
            var dllsToLoad = new[] { "trtyolo.dll", "custom_plugins.dll", "TrtYoloPoseNative.dll" };
            
            foreach (var dllName in dllsToLoad)
            {
                var paths = new[]
                {
                    Path.Combine(trtSubDir, dllName),
                    Path.Combine(appDir, dllName),
                };
                
                foreach (var dllPath in paths)
                {
                    if (File.Exists(dllPath))
                    {
                        LoadLibrary(dllPath);
                        break;
                    }
                }
            }
            
            _initialized = true;
        }
    }
}

#endregion

#region Native Structures

[StructLayout(LayoutKind.Sequential)]
internal struct TrtKeyPoint
{
    public float X;
    public float Y;
    public float Confidence;
}

[StructLayout(LayoutKind.Sequential)]
internal struct TrtBox
{
    public float Left;
    public float Top;
    public float Right;
    public float Bottom;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct TrtPoseResult
{
    public TrtBox Box;
    public float Confidence;
    public int ClassId;
    public fixed float Keypoints[17 * 3]; // 17 keypoints * (x, y, confidence)
}

[StructLayout(LayoutKind.Sequential)]
internal struct TrtPoseResults
{
    public int Count;
    public IntPtr Results; // TrtPoseResult*
}

[StructLayout(LayoutKind.Sequential)]
internal struct TrtPerformanceReport
{
    public double GpuLatencyMs;
    public double CpuLatencyMs;
    public double Throughput;
}

#endregion

#region Native API

internal static class TrtYoloPoseNativeApi
{
    private const string DllName = "TrtYoloPoseNative.dll";

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr TrtPoseEngine_Create(
        [MarshalAs(UnmanagedType.LPStr)] string enginePath,
        int swapRB,
        int inputWidth,
        int inputHeight);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TrtPoseEngine_Destroy(IntPtr engine);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int TrtPoseEngine_Predict(
        IntPtr engine,
        IntPtr imageData,
        int width,
        int height,
        int stride,
        ref TrtPoseResults results);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TrtPoseResults_Free(ref TrtPoseResults results);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int TrtPoseEngine_GetPerformance(
        IntPtr engine,
        ref TrtPerformanceReport report);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int TrtPoseEngine_GetBatchSize(IntPtr engine);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr TrtPoseEngine_GetLastError();
}

#endregion

#region 推理性能统计

/// <summary>
/// 推理性能统计
/// </summary>
public class InferencePerformance
{
    /// <summary>GPU 推理延迟 (ms)</summary>
    public double GpuLatencyMs { get; init; }
    
    /// <summary>CPU 延迟 (ms)</summary>
    public double CpuLatencyMs { get; init; }
    
    /// <summary>吞吐量 (FPS)</summary>
    public double Throughput { get; init; }
    
    /// <summary>总延迟 (ms)</summary>
    public double TotalLatencyMs => GpuLatencyMs + CpuLatencyMs;
}

#endregion

/// <summary>
/// TensorRT-YOLO Pose 高性能推理器
/// </summary>
/// <remarks>
/// 提供与 YoloSharp.YoloPredictor 兼容的 API
/// 优化：内部对象复用，避免每帧 GC 分配
/// </remarks>
public class TrtYoloPoseInferencer : IDisposable
{
    private IntPtr _engine;
    private bool _disposed;
    
    #region 对象复用缓存（避免 GC 压力）
    private const int MAX_DETECTIONS = 16;  // 最大检测数（足够大多数场景）
    
    private readonly YoloResult<Pose> _resultCache;
    private readonly Pose[] _poseCache;
    private readonly Keypoint[][] _keypointCache;
    #endregion
    
    /// <summary>
    /// 创建推理器
    /// </summary>
    /// <param name="enginePath">.engine 模型文件路径</param>
    /// <param name="inputWidth">输入图像宽度（固定可提升性能，0表示动态）</param>
    /// <param name="inputHeight">输入图像高度（固定可提升性能，0表示动态）</param>
    /// <param name="swapRB">是否交换 R/B 通道（Bitmap 是 BGR 格式，需要设为 true）</param>
    public TrtYoloPoseInferencer(
        string enginePath, 
        int inputWidth = 0, 
        int inputHeight = 0,
        bool swapRB = true)
    {
        // 确保原生库已加载
        NativeLibraryLoader.Initialize();
        
        if (!File.Exists(enginePath))
            throw new FileNotFoundException($"Engine 文件不存在: {enginePath}");
        
        _engine = TrtYoloPoseNativeApi.TrtPoseEngine_Create(
            enginePath, 
            swapRB ? 1 : 0, 
            inputWidth, 
            inputHeight);
        
        if (_engine == IntPtr.Zero)
        {
            var errorPtr = TrtYoloPoseNativeApi.TrtPoseEngine_GetLastError();
            var error = Marshal.PtrToStringAnsi(errorPtr) ?? "Unknown error";
            throw new Exception($"创建推理引擎失败: {error}");
        }
        
        // 初始化对象复用缓存
        _resultCache = new YoloResult<Pose>(MAX_DETECTIONS);
        _poseCache = new Pose[MAX_DETECTIONS];
        _keypointCache = new Keypoint[MAX_DETECTIONS][];
        for (int i = 0; i < MAX_DETECTIONS; i++)
        {
            _keypointCache[i] = new Keypoint[17];
        }
    }
    
    /// <summary>
    /// 执行姿态检测 - 兼容 YoloSharp.YoloPredictor.Pose() 的返回格式
    /// </summary>
    /// <param name="bitmap">输入图像</param>
    /// <returns>与 YoloSharp 兼容的检测结果</returns>
    public unsafe YoloResult<Pose> Pose(Bitmap bitmap)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(TrtYoloPoseInferencer));
        
        // 锁定位图数据
        var bmpData = bitmap.LockBits(
            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format24bppRgb);
        
        try
        {
            return PoseFromData(bmpData.Scan0, bitmap.Width, bitmap.Height, bmpData.Stride);
        }
        finally
        {
            bitmap.UnlockBits(bmpData);
        }
    }
    
    /// <summary>
    /// 使用原始数据执行推理（避免 Bitmap 转换开销）
    /// </summary>
    public unsafe YoloResult<Pose> PoseFromData(IntPtr imageData, int width, int height, int stride)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(TrtYoloPoseInferencer));
        
        TrtPoseResults nativeResults = default;
        
        int ret = TrtYoloPoseNativeApi.TrtPoseEngine_Predict(
            _engine,
            imageData,
            width,
            height,
            stride,
            ref nativeResults);
        
        if (ret != 0)
        {
            var errorPtr = TrtYoloPoseNativeApi.TrtPoseEngine_GetLastError();
            var error = Marshal.PtrToStringAnsi(errorPtr) ?? "Unknown error";
            throw new Exception($"推理失败: {error}");
        }
        
        try
        {
            return ConvertToYoloResultCached(nativeResults);
        }
        finally
        {
            TrtYoloPoseNativeApi.TrtPoseResults_Free(ref nativeResults);
        }
    }
    
    /// <summary>
    /// 获取性能报告
    /// </summary>
    public InferencePerformance GetPerformance()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(TrtYoloPoseInferencer));
        
        TrtPerformanceReport report = default;
        TrtYoloPoseNativeApi.TrtPoseEngine_GetPerformance(_engine, ref report);
        
        return new InferencePerformance
        {
            GpuLatencyMs = report.GpuLatencyMs,
            CpuLatencyMs = report.CpuLatencyMs,
            Throughput = report.Throughput
        };
    }
    
    /// <summary>
    /// 批处理大小
    /// </summary>
    public int BatchSize => TrtYoloPoseNativeApi.TrtPoseEngine_GetBatchSize(_engine);
    
    /// <summary>
    /// 转换 Native 结果为 YoloSharp 兼容格式（使用缓存复用对象）
    /// </summary>
    private unsafe YoloResult<Pose> ConvertToYoloResultCached(TrtPoseResults nativeResults)
    {
        _resultCache.Clear();
        
        if (nativeResults.Count == 0 || nativeResults.Results == IntPtr.Zero)
            return _resultCache;
        
        var resultPtr = (TrtPoseResult*)nativeResults.Results;
        int count = Math.Min(nativeResults.Count, MAX_DETECTIONS);
        
        for (int i = 0; i < count; i++)
        {
            var native = resultPtr[i];
            
            // 复用缓存的 Keypoint 数组，更新内容
            var keypoints = _keypointCache[i];
            for (int j = 0; j < 17; j++)
            {
                keypoints[j] = new Keypoint(
                    (int)native.Keypoints[j * 3],
                    (int)native.Keypoints[j * 3 + 1],
                    native.Keypoints[j * 3 + 2]);
            }
            
            // 创建 Pose 对象（引用缓存的 keypoints 数组）
            var pose = new Pose(
                new Rectangle(
                    (int)native.Box.Left,
                    (int)native.Box.Top,
                    (int)(native.Box.Right - native.Box.Left),
                    (int)(native.Box.Bottom - native.Box.Top)),
                native.Confidence,
                keypoints);
            
            _resultCache.Add(pose);
        }
        
        return _resultCache;
    }
    
    public void Dispose()
    {
        if (!_disposed && _engine != IntPtr.Zero)
        {
            TrtYoloPoseNativeApi.TrtPoseEngine_Destroy(_engine);
            _engine = IntPtr.Zero;
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
    
    ~TrtYoloPoseInferencer()
    {
        Dispose();
    }
}
