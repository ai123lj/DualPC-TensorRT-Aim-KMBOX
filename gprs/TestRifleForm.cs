using gprs.KmBox;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace gprs
{
    /// <summary>
    /// 步枪模式隔离测试窗体
    /// 纯 KmBox 通信测试：右键按住 → 软件左键按下，右键松开 → 软件左键释放
    /// 排除所有 YOLO/准心/瞬狙逻辑，用于定位 ISSUE-013 根因
    /// </summary>
    public partial class TestRifleForm : Form
    {
        private KmBoxNet _kmBox;
        private readonly Stopwatch _sw = Stopwatch.StartNew();
        private volatile bool _testEnabled;
        private volatile bool _sessionActive;
        private int _eventCount;

        public TestRifleForm()
        {
            InitializeComponent();
        }

        #region KmBox 连接

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (_kmBox != null && _kmBox.IsConnected)
            {
                DisconnectKmBox();
                return;
            }

            try
            {
                string ip = txtIP.Text.Trim();
                int port = int.Parse(txtPort.Text.Trim());
                string uuid = txtUUID.Text.Trim();

                _kmBox = new KmBoxNet();
                if (_kmBox.Connect(ip, port, uuid))
                {
                    _kmBox.HwMouseButtonChanged += OnHwMouseButtonChanged;
                    _kmBox.MonitorEnable(9527);
                    _kmBox.UnmaskAll();
                    _kmBox.Trace(0, 0);

                    btnConnect.Text = "断开";
                    lblStatus.Text = "已连接";
                    lblStatus.ForeColor = Color.Green;
                    Log("KmBox 连接成功，监控已启动 (port 9527)");
                }
                else
                {
                    _kmBox.Dispose();
                    _kmBox = null;
                    lblStatus.Text = "连接失败";
                    lblStatus.ForeColor = Color.Red;
                    Log("KmBox 连接失败");
                }
            }
            catch (Exception ex)
            {
                Log($"连接异常: {ex.Message}");
                lblStatus.Text = "异常";
                lblStatus.ForeColor = Color.Red;
            }
        }

        private void DisconnectKmBox()
        {
            // 安全释放：先结束会话
            if (_sessionActive)
            {
                _kmBox.MouseLeft(false);
                _sessionActive = false;
                Log("断开前释放左键");
            }

            _kmBox.MonitorDisable();
            _kmBox.Disconnect();
            _kmBox.Dispose();
            _kmBox = null;

            btnConnect.Text = "连接";
            lblStatus.Text = "未连接";
            lblStatus.ForeColor = Color.Gray;
            Log("KmBox 已断开");
        }

        #endregion

        #region 核心逻辑：右键 → 左键

        private void OnHwMouseButtonChanged(int button, bool isDown)
        {
            double ms = _sw.Elapsed.TotalMilliseconds;
            _eventCount++;

            string btnName = button switch
            {
                0x01 => "左键",
                0x02 => "右键",
                0x04 => "中键",
                0x08 => "侧键1",
                0x10 => "侧键2",
                _ => $"0x{button:X2}"
            };

            Log($"[HW] {btnName} {(isDown ? "按下" : "释放")}  (hwButtons=0x{_kmBox.HwMouseButtonState:X2})");

            // 只处理右键
            if (button != 0x02)
                return;

            if (!_testEnabled)
            {
                Log($"  → 测试未启用，跳过");
                return;
            }

            if (isDown)
            {
                // 右键按下 → 左键按下
                int ret = _kmBox.MouseLeft(true);
                _sessionActive = true;
                Log($"  → MouseLeft(true) 发送, 返回={ret}, session=ON");
            }
            else
            {
                // 右键释放 → 左键释放
                int ret = _kmBox.MouseLeft(false);
                _sessionActive = false;
                Log($"  → MouseLeft(false) 发送, 返回={ret}, session=OFF");
            }
        }

        #endregion

        #region 日志

        private void Log(string message)
        {
            double ms = _sw.Elapsed.TotalMilliseconds;
            string line = $"[{ms:F1}ms] #{_eventCount} {message}";

            if (txtLog.InvokeRequired)
            {
                txtLog.BeginInvoke(new Action(() => AppendLog(line)));
            }
            else
            {
                AppendLog(line);
            }
        }

        private void AppendLog(string line)
        {
            txtLog.AppendText(line + Environment.NewLine);

            // 更新状态显示
            bool rightDown = _kmBox?.IsMouseRightDown() ?? false;
            lblState.Text = $"硬件右键: {(rightDown ? "按下" : "释放")}  |  " +
                           $"会话: {(_sessionActive ? "ON" : "OFF")}  |  " +
                           $"事件数: {_eventCount}";
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtLog.Clear();
            _eventCount = 0;
            lblState.Text = "等待...";
        }

        #endregion

        #region UI 事件

        private void chkEnable_CheckedChanged(object sender, EventArgs e)
        {
            _testEnabled = chkEnable.Checked;

            if (_kmBox == null || !_kmBox.IsConnected)
            {
                Log($"测试 {(_testEnabled ? "启用" : "禁用")}（未连接，屏蔽无效）");
                return;
            }

            if (_testEnabled)
            {
                // 屏蔽硬件右键：游戏收不到右键，但 KmBox 监控仍能检测到
                _kmBox.MaskMouseRight(true);
                Log("测试启用 → 硬件右键已屏蔽（游戏不可见，监控可见）");
            }
            else
            {
                // 先释放左键，再解除右键屏蔽
                if (_sessionActive)
                {
                    _kmBox.MouseLeft(false);
                    _sessionActive = false;
                    Log("禁用测试 → 释放左键");
                }
                _kmBox.MaskMouseRight(false);
                Log("测试禁用 → 硬件右键已恢复");
            }
        }

        /// <summary>紧急停止：释放所有按键 + 解除所有屏蔽</summary>
        private void btnEmergencyStop_Click(object sender, EventArgs e)
        {
            if (_kmBox != null && _kmBox.IsConnected)
            {
                _kmBox.MouseLeft(false);
                _kmBox.UnmaskAll();  // 解除所有屏蔽（包括右键屏蔽）
                _sessionActive = false;
                _testEnabled = false;
                chkEnable.Checked = false;
                Log("=== 紧急停止：左键释放 + 解除所有屏蔽（含右键）===");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 关闭前安全清理
            if (_kmBox != null && _kmBox.IsConnected)
            {
                if (_sessionActive)
                {
                    _kmBox.MouseLeft(false);
                    _sessionActive = false;
                }
                _kmBox.UnmaskAll();  // 确保解除所有屏蔽
                _kmBox.MonitorDisable();
                _kmBox.Disconnect();
                _kmBox.Dispose();
            }
            base.OnFormClosing(e);
        }

        #endregion
    }
}
