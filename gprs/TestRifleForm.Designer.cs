namespace gprs
{
    partial class TestRifleForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            // === 控件声明 ===
            panelTop = new System.Windows.Forms.Panel();
            lblIP = new System.Windows.Forms.Label();
            txtIP = new System.Windows.Forms.TextBox();
            lblPort = new System.Windows.Forms.Label();
            txtPort = new System.Windows.Forms.TextBox();
            lblUUID = new System.Windows.Forms.Label();
            txtUUID = new System.Windows.Forms.TextBox();
            btnConnect = new System.Windows.Forms.Button();
            lblStatus = new System.Windows.Forms.Label();
            chkEnable = new System.Windows.Forms.CheckBox();
            btnEmergencyStop = new System.Windows.Forms.Button();
            btnClear = new System.Windows.Forms.Button();
            lblState = new System.Windows.Forms.Label();
            txtLog = new System.Windows.Forms.TextBox();

            panelTop.SuspendLayout();
            SuspendLayout();

            // === panelTop ===
            panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            panelTop.Location = new System.Drawing.Point(0, 0);
            panelTop.Size = new System.Drawing.Size(700, 80);
            panelTop.Controls.Add(lblIP);
            panelTop.Controls.Add(txtIP);
            panelTop.Controls.Add(lblPort);
            panelTop.Controls.Add(txtPort);
            panelTop.Controls.Add(lblUUID);
            panelTop.Controls.Add(txtUUID);
            panelTop.Controls.Add(btnConnect);
            panelTop.Controls.Add(lblStatus);
            panelTop.Controls.Add(chkEnable);
            panelTop.Controls.Add(btnEmergencyStop);
            panelTop.Controls.Add(btnClear);

            // === Row 1: 连接参数 ===
            int y1 = 8;

            lblIP.Text = "IP:";
            lblIP.Location = new System.Drawing.Point(10, y1 + 3);
            lblIP.AutoSize = true;

            txtIP.Text = "192.168.3.188";
            txtIP.Location = new System.Drawing.Point(30, y1);
            txtIP.Size = new System.Drawing.Size(110, 23);

            lblPort.Text = "Port:";
            lblPort.Location = new System.Drawing.Point(150, y1 + 3);
            lblPort.AutoSize = true;

            txtPort.Text = "8888";
            txtPort.Location = new System.Drawing.Point(185, y1);
            txtPort.Size = new System.Drawing.Size(55, 23);

            lblUUID.Text = "UUID:";
            lblUUID.Location = new System.Drawing.Point(250, y1 + 3);
            lblUUID.AutoSize = true;

            txtUUID.Text = "12345678";
            txtUUID.Location = new System.Drawing.Point(290, y1);
            txtUUID.Size = new System.Drawing.Size(90, 23);

            btnConnect.Text = "连接";
            btnConnect.Location = new System.Drawing.Point(390, y1);
            btnConnect.Size = new System.Drawing.Size(60, 25);
            btnConnect.Click += btnConnect_Click;

            lblStatus.Text = "未连接";
            lblStatus.ForeColor = System.Drawing.Color.Gray;
            lblStatus.Location = new System.Drawing.Point(460, y1 + 3);
            lblStatus.AutoSize = true;

            // === Row 2: 控制 ===
            int y2 = 42;

            chkEnable.Text = "启用测试（右键→左键）";
            chkEnable.Location = new System.Drawing.Point(10, y2);
            chkEnable.AutoSize = true;
            chkEnable.CheckedChanged += chkEnable_CheckedChanged;

            btnEmergencyStop.Text = "紧急停止";
            btnEmergencyStop.Location = new System.Drawing.Point(220, y2);
            btnEmergencyStop.Size = new System.Drawing.Size(80, 25);
            btnEmergencyStop.BackColor = System.Drawing.Color.OrangeRed;
            btnEmergencyStop.ForeColor = System.Drawing.Color.White;
            btnEmergencyStop.Click += btnEmergencyStop_Click;

            btnClear.Text = "清空日志";
            btnClear.Location = new System.Drawing.Point(310, y2);
            btnClear.Size = new System.Drawing.Size(70, 25);
            btnClear.Click += btnClear_Click;

            // === lblState (状态栏) ===
            lblState.Text = "等待连接...";
            lblState.Dock = System.Windows.Forms.DockStyle.Bottom;
            lblState.Size = new System.Drawing.Size(700, 25);
            lblState.BackColor = System.Drawing.Color.LightGray;
            lblState.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            lblState.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);

            // === txtLog (日志区域) ===
            txtLog.Multiline = true;
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            txtLog.Font = new System.Drawing.Font("Consolas", 9F);
            txtLog.BackColor = System.Drawing.Color.Black;
            txtLog.ForeColor = System.Drawing.Color.LightGreen;

            // === TestRifleForm ===
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(700, 500);
            Controls.Add(txtLog);       // Fill (中间)
            Controls.Add(lblState);     // Bottom
            Controls.Add(panelTop);     // Top
            Text = "步枪模式隔离测试 - ISSUE-013";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Label lblIP;
        private System.Windows.Forms.TextBox txtIP;
        private System.Windows.Forms.Label lblPort;
        private System.Windows.Forms.TextBox txtPort;
        private System.Windows.Forms.Label lblUUID;
        private System.Windows.Forms.TextBox txtUUID;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.CheckBox chkEnable;
        private System.Windows.Forms.Button btnEmergencyStop;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Label lblState;
        private System.Windows.Forms.TextBox txtLog;
    }
}
