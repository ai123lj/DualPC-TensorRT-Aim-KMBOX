using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace gprs.KmBox
{
    /// <summary>
    /// KMBox 网络版控制类 - 方便移植的独立封装
    /// 通信协议：UDP
    /// 官网：www.kmbox.top
    /// </summary>
    public class KmBoxNet : IDisposable
    {
        #region 命令码定义
        private const uint CMD_CONNECT = 0xaf3c2828;      // 连接
        private const uint CMD_MOUSE_MOVE = 0xaede7345;   // 鼠标移动
        private const uint CMD_MOUSE_LEFT = 0x9823AE8D;   // 鼠标左键
        private const uint CMD_MOUSE_MIDDLE = 0x97a3AE8D; // 鼠标中键
        private const uint CMD_MOUSE_RIGHT = 0x238d8212;  // 鼠标右键
        private const uint CMD_MOUSE_WHEEL = 0xffeead38;  // 鼠标滚轮
        private const uint CMD_MOUSE_AUTOMOVE = 0xaede7346; // 自动移动
        private const uint CMD_KEYBOARD_ALL = 0x123c2c2f; // 键盘操作
        private const uint CMD_REBOOT = 0xaa8855aa;       // 重启盒子
        private const uint CMD_MONITOR = 0x27388020;      // 监控开关
        private const uint CMD_MASK_MOUSE = 0x23234343;   // 屏蔽鼠标
        private const uint CMD_UNMASK_ALL = 0x23344343;   // 解除屏蔽
        private const uint CMD_BAZER_MOVE = 0xa238455a;   // 贝塞尔移动
        private const uint CMD_SETCONFIG = 0x1d3d3323;    // 设置IP配置
        private const uint CMD_SETVIDPID = 0xffed3232;    // 设置VID/PID
        private const uint CMD_SHOWPIC = 0x12334883;      // LCD显示图片
        private const uint CMD_TRACE_ENABLE = 0xbbcdddac; // 硬件轨迹算法
        #endregion

        #region 键盘修饰键定义
        public const byte KEY_LEFTCONTROL = 224;
        public const byte KEY_LEFTSHIFT = 225;
        public const byte KEY_LEFTALT = 226;
        public const byte KEY_LEFT_GUI = 227;
        public const byte KEY_RIGHTCONTROL = 228;
        public const byte KEY_RIGHTSHIFT = 229;
        public const byte KEY_RIGHTALT = 230;
        public const byte KEY_RIGHT_GUI = 231;
        #endregion

        #region 私有字段
        private UdpClient? _client;
        private IPEndPoint? _endpoint;
        private uint _mac;
        private uint _indexpts = 0;
        private readonly Random _rand = new();
        private readonly object _lock = new();

        // 软件状态
        private int _mouseButton = 0;
        private byte _keyboardCtrl = 0;
        private readonly byte[] _keyboardButtons = new byte[10];
        private int _maskFlag = 0;

        // 监控相关
        private UdpClient? _monitorClient;
        private Thread? _monitorThread;
        private volatile bool _monitorRunning;
        private int _hwMouseButtons;
        private short _hwMouseX, _hwMouseY, _hwMouseWheel;
        private byte _hwKeyboardCtrl;
        private readonly byte[] _hwKeyboardData = new byte[10];

        // 上一帧状态（用于边缘检测）
        private int _lastHwMouseButtons;
        private readonly byte[] _lastHwKeyboardData = new byte[10];
        #endregion

        #region 监控事件
        /// <summary>鼠标按键状态变化事件 (button: 0x01=左, 0x02=右, 0x04=中, isDown: 按下/释放)</summary>
        public event Action<int, bool>? HwMouseButtonChanged;

        /// <summary>硬件键盘按键按下事件 (hidKey: HID键码)</summary>
        public event Action<byte>? HwKeyDown;

        /// <summary>硬件键盘按键释放事件 (hidKey: HID键码)</summary>
        public event Action<byte>? HwKeyUp;
        #endregion

        #region 属性
        /// <summary>
        /// 是否已连接
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// 接收超时时间（毫秒）
        /// </summary>
        public int ReceiveTimeout { get; set; } = 3000;
        #endregion

        #region 连接/断开
        /// <summary>
        /// 连接 KMBox 盒子
        /// </summary>
        /// <param name="ip">盒子IP地址</param>
        /// <param name="port">通信端口</param>
        /// <param name="uuid">盒子UUID（16进制字符串）</param>
        /// <returns>连接是否成功</returns>
        public bool Connect(string ip, int port, string uuid)
        {
            try
            {
                _mac = Convert.ToUInt32(uuid, 16);
                _endpoint = new IPEndPoint(IPAddress.Parse(ip), port);
                _client = new UdpClient();
                _client.Client.ReceiveTimeout = ReceiveTimeout;

                // 发送连接命令
                var packet = BuildPacket(CMD_CONNECT, null);
                _client.Send(packet, packet.Length, _endpoint);

                // 等待响应
                Thread.Sleep(20);
                var response = _client.Receive(ref _endpoint);
                IsConnected = response.Length > 0;

                // 重置状态
                _mouseButton = 0;
                _keyboardCtrl = 0;
                Array.Clear(_keyboardButtons);
                _indexpts = 0;

                return IsConnected;
            }
            catch
            {
                IsConnected = false;
                return false;
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            _client?.Close();
            _client = null;
            IsConnected = false;
        }
        #endregion

        #region 鼠标操作
        /// <summary>
        /// 鼠标相对移动
        /// </summary>
        public int MouseMove(int x, int y)
        {
            var data = BuildMouseData(0, x, y, 0);
            return SendCommand(CMD_MOUSE_MOVE, data);
        }

        /// <summary>
        /// 鼠标左键操作
        /// </summary>
        /// <param name="isDown">true=按下, false=释放</param>
        public int MouseLeft(bool isDown)
        {
            _mouseButton = isDown ? (_mouseButton | 0x01) : (_mouseButton & ~0x01);
            var data = BuildMouseData(_mouseButton, 0, 0, 0);
            return SendCommand(CMD_MOUSE_LEFT, data);
        }

        /// <summary>
        /// 鼠标右键操作
        /// </summary>
        public int MouseRight(bool isDown)
        {
            _mouseButton = isDown ? (_mouseButton | 0x02) : (_mouseButton & ~0x02);
            var data = BuildMouseData(_mouseButton, 0, 0, 0);
            return SendCommand(CMD_MOUSE_RIGHT, data);
        }

        /// <summary>
        /// 鼠标中键操作
        /// </summary>
        public int MouseMiddle(bool isDown)
        {
            _mouseButton = isDown ? (_mouseButton | 0x04) : (_mouseButton & ~0x04);
            var data = BuildMouseData(_mouseButton, 0, 0, 0);
            return SendCommand(CMD_MOUSE_MIDDLE, data);
        }

        /// <summary>
        /// 鼠标侧键1操作
        /// </summary>
        public int MouseSide1(bool isDown)
        {
            _mouseButton = isDown ? (_mouseButton | 0x08) : (_mouseButton & ~0x08);
            var data = BuildMouseData(_mouseButton, 0, 0, 0);
            return SendCommand(CMD_MOUSE_RIGHT, data);
        }

        /// <summary>
        /// 鼠标侧键2操作
        /// </summary>
        public int MouseSide2(bool isDown)
        {
            _mouseButton = isDown ? (_mouseButton | 0x10) : (_mouseButton & ~0x10);
            var data = BuildMouseData(_mouseButton, 0, 0, 0);
            return SendCommand(CMD_MOUSE_RIGHT, data);
        }

        /// <summary>
        /// 鼠标滚轮操作
        /// </summary>
        /// <param name="delta">滚动量，正值向上，负值向下</param>
        public int MouseWheel(int delta)
        {
            var data = BuildMouseData(0, 0, 0, delta);
            return SendCommand(CMD_MOUSE_WHEEL, data);
        }

        /// <summary>
        /// 鼠标全功能操作（一次性设置所有参数）
        /// </summary>
        public int MouseAll(int button, int x, int y, int wheel)
        {
            _mouseButton = button;
            var data = BuildMouseData(button, x, y, wheel);
            return SendCommand(CMD_MOUSE_WHEEL, data);
        }

        /// <summary>
        /// 鼠标自动移动（带拟人化）
        /// </summary>
        /// <param name="x">目标X偏移</param>
        /// <param name="y">目标Y偏移</param>
        /// <param name="ms">移动耗时（毫秒）</param>
        public int MouseMoveAuto(int x, int y, int ms)
        {
            var data = BuildMouseData(0, x, y, 0);
            return SendCommandWithRand(CMD_MOUSE_AUTOMOVE, data, (uint)ms);
        }

        /// <summary>
        /// 鼠标点击（按下+释放）
        /// </summary>
        public void MouseClick(int delayMs = 50)
        {
            MouseLeft(true);
            Thread.Sleep(delayMs);
            MouseLeft(false);
        }

        /// <summary>
        /// 鼠标右键点击
        /// </summary>
        public void MouseRightClick(int delayMs = 50)
        {
            MouseRight(true);
            Thread.Sleep(delayMs);
            MouseRight(false);
        }
        #endregion

        #region 键盘操作
        /// <summary>
        /// 键盘按键按下
        /// </summary>
        /// <param name="vkKey">HID 键码</param>
        public int KeyDown(byte vkKey)
        {
            if (vkKey >= KEY_LEFTCONTROL && vkKey <= KEY_RIGHT_GUI)
            {
                // 修饰键
                int bit = vkKey - KEY_LEFTCONTROL;
                _keyboardCtrl |= (byte)(1 << bit);
            }
            else
            {
                // 普通键 - 添加到按键数组
                for (int i = 0; i < 10; i++)
                {
                    if (_keyboardButtons[i] == vkKey) break; // 已存在
                    if (_keyboardButtons[i] == 0)
                    {
                        _keyboardButtons[i] = vkKey;
                        break;
                    }
                }
            }

            var data = BuildKeyboardData();
            return SendCommand(CMD_KEYBOARD_ALL, data);
        }

        /// <summary>
        /// 键盘按键释放
        /// </summary>
        public int KeyUp(byte vkKey)
        {
            if (vkKey >= KEY_LEFTCONTROL && vkKey <= KEY_RIGHT_GUI)
            {
                int bit = vkKey - KEY_LEFTCONTROL;
                _keyboardCtrl &= (byte)~(1 << bit);
            }
            else
            {
                // 从按键数组移除
                for (int i = 0; i < 10; i++)
                {
                    if (_keyboardButtons[i] == vkKey)
                    {
                        // 后面的元素前移
                        for (int j = i; j < 9; j++)
                            _keyboardButtons[j] = _keyboardButtons[j + 1];
                        _keyboardButtons[9] = 0;
                        break;
                    }
                }
            }

            var data = BuildKeyboardData();
            return SendCommand(CMD_KEYBOARD_ALL, data);
        }

        /// <summary>
        /// 键盘按键单击（按下+释放）
        /// </summary>
        public void KeyPress(byte vkKey, int delayMs = 50)
        {
            KeyDown(vkKey);
            Thread.Sleep(delayMs / 2);
            KeyUp(vkKey);
            Thread.Sleep(delayMs / 2);
        }
        #endregion

        #region 屏蔽功能
        /// <summary>
        /// 屏蔽鼠标左键
        /// </summary>
        public int MaskMouseLeft(bool enable)
        {
            _maskFlag = enable ? (_maskFlag | 0x01) : (_maskFlag & ~0x01);
            return SendMaskCommand();
        }

        /// <summary>
        /// 屏蔽鼠标右键
        /// </summary>
        public int MaskMouseRight(bool enable)
        {
            _maskFlag = enable ? (_maskFlag | 0x02) : (_maskFlag & ~0x02);
            return SendMaskCommand();
        }

        /// <summary>
        /// 屏蔽鼠标X轴移动
        /// </summary>
        public int MaskMouseX(bool enable)
        {
            _maskFlag = enable ? (_maskFlag | 0x20) : (_maskFlag & ~0x20);
            return SendMaskCommand();
        }

        /// <summary>
        /// 屏蔽鼠标Y轴移动
        /// </summary>
        public int MaskMouseY(bool enable)
        {
            _maskFlag = enable ? (_maskFlag | 0x40) : (_maskFlag & ~0x40);
            return SendMaskCommand();
        }

        /// <summary>
        /// 一次性屏蔽所有（左键+右键+X轴+Y轴）
        /// </summary>
        public int MaskAll()
        {
            _maskFlag = 0x01 | 0x02 | 0x20 | 0x40;  // 左键 | 右键 | X轴 | Y轴
            return SendMaskCommand();
        }

        /// <summary>
        /// 解除所有屏蔽
        /// </summary>
        public int UnmaskAll()
        {
            _maskFlag = 0;
            return SendCommandHeaderOnly(CMD_UNMASK_ALL, 0);
        }

        private int SendMaskCommand()
        {
            return SendCommandHeaderOnly(CMD_MASK_MOUSE, (uint)_maskFlag);
        }
        #endregion

        #region 贝塞尔曲线移动
        /// <summary>
        /// 贝塞尔曲线移动（硬件实现，轨迹平滑）
        /// </summary>
        /// <param name="x">目标X偏移</param>
        /// <param name="y">目标Y偏移</param>
        /// <param name="ms">移动耗时（毫秒）</param>
        /// <param name="x1">控制点1的X</param>
        /// <param name="y1">控制点1的Y</param>
        /// <param name="x2">控制点2的X</param>
        /// <param name="y2">控制点2的Y</param>
        public int MouseMoveBeizer(int x, int y, int ms, int x1, int y1, int x2, int y2)
        {
            var data = new byte[56];
            BitConverter.GetBytes(0).CopyTo(data, 0);   // button
            BitConverter.GetBytes(x).CopyTo(data, 4);   // x
            BitConverter.GetBytes(y).CopyTo(data, 8);   // y
            BitConverter.GetBytes(0).CopyTo(data, 12);  // wheel
            BitConverter.GetBytes(x1).CopyTo(data, 16); // point[0]
            BitConverter.GetBytes(y1).CopyTo(data, 20); // point[1]
            BitConverter.GetBytes(x2).CopyTo(data, 24); // point[2]
            BitConverter.GetBytes(y2).CopyTo(data, 28); // point[3]

            return SendCommandWithRand(CMD_BAZER_MOVE, data, (uint)ms);
        }
        #endregion

        #region 硬件轨迹算法
        /// <summary>
        /// 启用/禁用硬件轨迹修正算法
        /// </summary>
        /// <param name="type">算法类型: 0=贝塞尔非实时, 1=追踪非实时, 2=贝塞尔实时, 3=追踪实时</param>
        /// <param name="value">平滑度(0=禁用, 16-100推荐, 值越大越平滑但延迟越高)</param>
        public int Trace(int type, int value)
        {
            uint randValue = unchecked((uint)((type << 24) | (value & 0xFFFFFF)));
            return SendCommandHeaderOnly(CMD_TRACE_ENABLE, randValue);
        }

        /// <summary>
        /// 禁用硬件轨迹算法
        /// </summary>
        public int TraceDisable() => Trace(0, 0);
        #endregion

        #region 物理键鼠监控
        /// <summary>
        /// 开启物理键鼠监控
        /// </summary>
        /// <param name="port">本地监听端口(1024-49151)</param>
        public int MonitorEnable(int port)
        {
            try
            {
                // 先关闭旧的监控
                MonitorDisable();

                // 发送开启监控命令
                uint randValue = unchecked((uint)(port | (0xaa55 << 16)));
                var result = SendCommandHeaderOnly(CMD_MONITOR, randValue);
                if (result != 0) return result;

                // 启动监听线程
                _monitorClient = new UdpClient(port);
                _monitorRunning = true;
                _monitorThread = new Thread(MonitorThreadProc) { IsBackground = true };
                _monitorThread.Start();

                return 0;
            }
            catch (Exception)
            {
                MonitorDisable();
                return -1;
            }
        }

        /// <summary>
        /// 关闭物理键鼠监控
        /// </summary>
        public int MonitorDisable()
        {
            _monitorRunning = false;
            _monitorClient?.Close();
            _monitorThread?.Join(500);
            _monitorClient = null;
            _monitorThread = null;

            return SendCommandHeaderOnly(CMD_MONITOR, 0);
        }

        private void MonitorThreadProc()
        {
            var remoteEp = new IPEndPoint(IPAddress.Any, 0);
            while (_monitorRunning && _monitorClient != null)
            {
                try
                {
                    var data = _monitorClient.Receive(ref remoteEp);
                    if (data.Length >= 19) // 鼠标报告(8字节) + 键盘报告(12字节)
                    {
                        // 解析鼠标报告
                        int newMouseButtons = data[1];
                        _hwMouseX = BitConverter.ToInt16(data, 2);
                        _hwMouseY = BitConverter.ToInt16(data, 4);
                        _hwMouseWheel = BitConverter.ToInt16(data, 6);

                        // 鼠标按键边缘检测
                        if (newMouseButtons != _lastHwMouseButtons)
                        {
                            // 检测各按键变化
                            for (int bit = 0; bit < 5; bit++)
                            {
                                int mask = 1 << bit;
                                bool wasDown = (_lastHwMouseButtons & mask) != 0;
                                bool isDown = (newMouseButtons & mask) != 0;
                                if (wasDown != isDown)
                                    HwMouseButtonChanged?.Invoke(mask, isDown);
                            }
                            _lastHwMouseButtons = newMouseButtons;
                        }
                        _hwMouseButtons = newMouseButtons;

                        // 解析键盘报告
                        _hwKeyboardCtrl = data[9];
                        
                        // 键盘边缘检测
                        for (int i = 0; i < 10 && (10 + i) < data.Length; i++)
                        {
                            byte newKey = data[10 + i];
                            byte oldKey = _lastHwKeyboardData[i];
                            
                            // 新按下的键
                            if (newKey != 0 && !IsKeyInArray(_lastHwKeyboardData, newKey))
                                HwKeyDown?.Invoke(newKey);
                            
                            // 释放的键
                            if (oldKey != 0 && !IsKeyInArray(data, oldKey, 10, 10))
                                HwKeyUp?.Invoke(oldKey);
                            
                            _hwKeyboardData[i] = newKey;
                            _lastHwKeyboardData[i] = newKey;
                        }
                    }
                }
                catch (System.Net.Sockets.SocketException)
                {
                    // 关闭时 UDP 接收被中断，正常退出
                    break;
                }
                catch
                {
                    break;
                }
            }
        }

        private static bool IsKeyInArray(byte[] arr, byte key, int start = 0, int count = 10)
        {
            for (int i = start; i < start + count && i < arr.Length; i++)
                if (arr[i] == key) return true;
            return false;
        }

        /// <summary>检测物理鼠标左键是否按下</summary>
        public bool IsMouseLeftDown() => (_hwMouseButtons & 0x01) != 0;

        /// <summary>检测物理鼠标右键是否按下</summary>
        public bool IsMouseRightDown() => (_hwMouseButtons & 0x02) != 0;

        /// <summary>检测物理鼠标中键是否按下</summary>
        public bool IsMouseMiddleDown() => (_hwMouseButtons & 0x04) != 0;

        /// <summary>检测物理鼠标侧键1是否按下</summary>
        public bool IsMouseSide1Down() => (_hwMouseButtons & 0x08) != 0;

        /// <summary>检测物理鼠标侧键2是否按下</summary>
        public bool IsMouseSide2Down() => (_hwMouseButtons & 0x10) != 0;

        /// <summary>获取物理鼠标移动量</summary>
        public (int x, int y) GetMouseXY() => (_hwMouseX, _hwMouseY);

        /// <summary>获取物理鼠标滚轮值</summary>
        public int GetMouseWheel() => _hwMouseWheel;

        /// <summary>检测物理键盘按键是否按下</summary>
        public bool IsKeyboardDown(byte vkKey)
        {
            if (vkKey >= KEY_LEFTCONTROL && vkKey <= KEY_RIGHT_GUI)
            {
                int bit = vkKey - KEY_LEFTCONTROL;
                return (_hwKeyboardCtrl & (1 << bit)) != 0;
            }
            for (int i = 0; i < 10; i++)
            {
                if (_hwKeyboardData[i] == vkKey) return true;
            }
            return false;
        }
        #endregion

        #region LCD屏幕显示
        /// <summary>
        /// 用指定颜色填充LCD屏幕
        /// </summary>
        /// <param name="rgb565">RGB565格式颜色值</param>
        public int LcdFillColor(ushort rgb565)
        {
            if (!IsConnected || _client == null || _endpoint == null)
                return -1;

            lock (_lock)
            {
                try
                {
                    for (int y = 0; y < 40; y++)
                    {
                        var packet = new byte[16 + 1024];
                        BitConverter.GetBytes(_mac).CopyTo(packet, 0);
                        BitConverter.GetBytes((uint)(y * 4)).CopyTo(packet, 4); // rand = y坐标
                        BitConverter.GetBytes(++_indexpts).CopyTo(packet, 8);
                        BitConverter.GetBytes(CMD_SHOWPIC).CopyTo(packet, 12);

                        // 填充颜色数据
                        for (int c = 0; c < 512; c++)
                        {
                            BitConverter.GetBytes(rgb565).CopyTo(packet, 16 + c * 2);
                        }

                        _client.Send(packet, packet.Length, _endpoint);
                        _client.Receive(ref _endpoint);
                    }
                    return 0;
                }
                catch { return -1; }
            }
        }

        /// <summary>
        /// 将图片显示到LCD屏幕（128x160分辨率，RGB565格式）
        /// </summary>
        public int LcdShowPicture(byte[] rgb565Data)
        {
            if (!IsConnected || _client == null || _endpoint == null)
                return -1;
            if (rgb565Data.Length < 128 * 160 * 2)
                return -2;

            lock (_lock)
            {
                try
                {
                    for (int y = 0; y < 40; y++)
                    {
                        var packet = new byte[16 + 1024];
                        BitConverter.GetBytes(_mac).CopyTo(packet, 0);
                        BitConverter.GetBytes((uint)(y * 4)).CopyTo(packet, 4);
                        BitConverter.GetBytes(++_indexpts).CopyTo(packet, 8);
                        BitConverter.GetBytes(CMD_SHOWPIC).CopyTo(packet, 12);

                        Array.Copy(rgb565Data, y * 1024, packet, 16, 1024);

                        _client.Send(packet, packet.Length, _endpoint);
                        _client.Receive(ref _endpoint);
                    }
                    return 0;
                }
                catch { return -1; }
            }
        }

        /// <summary>
        /// RGB888转RGB565
        /// </summary>
        public static ushort ToRgb565(byte r, byte g, byte b)
        {
            return (ushort)(((r >> 3) << 11) | ((g >> 2) << 5) | (b >> 3));
        }
        #endregion

        #region 盒子配置
        /// <summary>
        /// 设置盒子IP和端口（需要重新连接生效）
        /// </summary>
        public int SetConfig(string newIp, int newPort)
        {
            if (!IsConnected || _client == null || _endpoint == null)
                return -1;

            lock (_lock)
            {
                try
                {
                    var packet = new byte[18];
                    BitConverter.GetBytes(_mac).CopyTo(packet, 0);
                    var ipBytes = IPAddress.Parse(newIp).GetAddressBytes();
                    uint ipValue = BitConverter.ToUInt32(ipBytes, 0);
                    BitConverter.GetBytes(ipValue).CopyTo(packet, 4); // rand = new IP
                    BitConverter.GetBytes(++_indexpts).CopyTo(packet, 8);
                    BitConverter.GetBytes(CMD_SETCONFIG).CopyTo(packet, 12);
                    packet[16] = (byte)(newPort >> 8);
                    packet[17] = (byte)(newPort & 0xFF);

                    _client.Send(packet, packet.Length, _endpoint);
                    _client.Receive(ref _endpoint);
                    return 0;
                }
                catch { return -1; }
            }
        }

        /// <summary>
        /// 设置盒子的USB VID/PID（需要重新上电生效）
        /// </summary>
        public int SetVidPid(ushort vid, ushort pid)
        {
            uint randValue = unchecked((uint)(vid | (pid << 16)));
            return SendCommandHeaderOnly(CMD_SETVIDPID, randValue);
        }

        /// <summary>
        /// 重启盒子
        /// </summary>
        public int Reboot()
        {
            var result = SendCommandHeaderOnly(CMD_REBOOT, 0);
            Disconnect();
            return result;
        }
        #endregion

        #region 私有方法 - 数据包构建
        private byte[] BuildPacket(uint cmd, byte[]? data)
        {
            int dataLen = data?.Length ?? 0;
            var packet = new byte[16 + dataLen];

            // 包头 16字节
            BitConverter.GetBytes(_mac).CopyTo(packet, 0);           // mac
            BitConverter.GetBytes((uint)_rand.Next()).CopyTo(packet, 4); // rand
            BitConverter.GetBytes(++_indexpts).CopyTo(packet, 8);    // indexpts
            BitConverter.GetBytes(cmd).CopyTo(packet, 12);           // cmd

            // 数据
            if (data != null)
                data.CopyTo(packet, 16);

            return packet;
        }

        private byte[] BuildPacketWithRand(uint cmd, byte[]? data, uint randValue)
        {
            int dataLen = data?.Length ?? 0;
            var packet = new byte[16 + dataLen];

            BitConverter.GetBytes(_mac).CopyTo(packet, 0);
            BitConverter.GetBytes(randValue).CopyTo(packet, 4);      // 自定义rand值
            BitConverter.GetBytes(++_indexpts).CopyTo(packet, 8);
            BitConverter.GetBytes(cmd).CopyTo(packet, 12);

            if (data != null)
                data.CopyTo(packet, 16);

            return packet;
        }

        private byte[] BuildMouseData(int button, int x, int y, int wheel)
        {
            var data = new byte[56]; // soft_mouse_t 完整大小
            BitConverter.GetBytes(button).CopyTo(data, 0);
            BitConverter.GetBytes(x).CopyTo(data, 4);
            BitConverter.GetBytes(y).CopyTo(data, 8);
            BitConverter.GetBytes(wheel).CopyTo(data, 12);
            return data;
        }

        private byte[] BuildKeyboardData()
        {
            var data = new byte[12]; // soft_keyboard_t
            data[0] = _keyboardCtrl;
            data[1] = 0; // reserved
            Array.Copy(_keyboardButtons, 0, data, 2, 10);
            return data;
        }

        private int SendCommand(uint cmd, byte[] data)
        {
            if (!IsConnected || _client == null || _endpoint == null)
                return -1;

            lock (_lock)
            {
                try
                {
                    var packet = BuildPacket(cmd, data);
                    _client.Send(packet, packet.Length, _endpoint);
                    _client.Receive(ref _endpoint);
                    return 0;
                }
                catch
                {
                    return -1;
                }
            }
        }

        private int SendCommandWithRand(uint cmd, byte[] data, uint randValue)
        {
            if (!IsConnected || _client == null || _endpoint == null)
                return -1;

            lock (_lock)
            {
                try
                {
                    var packet = BuildPacketWithRand(cmd, data, randValue);
                    _client.Send(packet, packet.Length, _endpoint);
                    _client.Receive(ref _endpoint);
                    return 0;
                }
                catch
                {
                    return -1;
                }
            }
        }

        private int SendCommandHeaderOnly(uint cmd, uint randValue)
        {
            if (!IsConnected || _client == null || _endpoint == null)
                return -1;

            lock (_lock)
            {
                try
                {
                    var packet = new byte[16];
                    BitConverter.GetBytes(_mac).CopyTo(packet, 0);
                    BitConverter.GetBytes(randValue).CopyTo(packet, 4);
                    BitConverter.GetBytes(++_indexpts).CopyTo(packet, 8);
                    BitConverter.GetBytes(cmd).CopyTo(packet, 12);

                    _client.Send(packet, packet.Length, _endpoint);
                    _client.Receive(ref _endpoint);
                    return 0;
                }
                catch
                {
                    return -1;
                }
            }
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            MonitorDisable();
            Disconnect();
            GC.SuppressFinalize(this);
        }
        #endregion
    }

    #region HID 键码表（常用）
    /// <summary>
    /// HID 键盘键码定义
    /// </summary>
    public static class HidKeys
    {
        public const byte A = 4;
        public const byte B = 5;
        public const byte C = 6;
        public const byte D = 7;
        public const byte E = 8;
        public const byte F = 9;
        public const byte G = 10;
        public const byte H = 11;
        public const byte I = 12;
        public const byte J = 13;
        public const byte K = 14;
        public const byte L = 15;
        public const byte M = 16;
        public const byte N = 17;
        public const byte O = 18;
        public const byte P = 19;
        public const byte Q = 20;
        public const byte R = 21;
        public const byte S = 22;
        public const byte T = 23;
        public const byte U = 24;
        public const byte V = 25;
        public const byte W = 26;
        public const byte X = 27;
        public const byte Y = 28;
        public const byte Z = 29;

        public const byte Num1 = 30;
        public const byte Num2 = 31;
        public const byte Num3 = 32;
        public const byte Num4 = 33;
        public const byte Num5 = 34;
        public const byte Num6 = 35;
        public const byte Num7 = 36;
        public const byte Num8 = 37;
        public const byte Num9 = 38;
        public const byte Num0 = 39;

        public const byte Enter = 40;
        public const byte Escape = 41;
        public const byte Backspace = 42;
        public const byte Tab = 43;
        public const byte Space = 44;

        public const byte F1 = 58;
        public const byte F2 = 59;
        public const byte F3 = 60;
        public const byte F4 = 61;
        public const byte F5 = 62;
        public const byte F6 = 63;
        public const byte F7 = 64;
        public const byte F8 = 65;
        public const byte F9 = 66;
        public const byte F10 = 67;
        public const byte F11 = 68;
        public const byte F12 = 69;

        public const byte LeftCtrl = 224;
        public const byte LeftShift = 225;
        public const byte LeftAlt = 226;
        public const byte LeftGui = 227;  // Windows键
        public const byte RightCtrl = 228;
        public const byte RightShift = 229;
        public const byte RightAlt = 230;
        public const byte RightGui = 231;
    }
    #endregion
}
