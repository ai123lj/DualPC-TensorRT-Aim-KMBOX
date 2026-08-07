﻿
namespace gprs
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);

            _mwCapture.Dispose();
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            textBox1 = new System.Windows.Forms.TextBox();
            pictureBox1 = new System.Windows.Forms.PictureBox();
            textBox2 = new System.Windows.Forms.TextBox();
            groupBox4 = new System.Windows.Forms.GroupBox();
            lblKmBoxStatus = new System.Windows.Forms.Label();
            btnKmBoxConnect = new System.Windows.Forms.Button();
            txtKmBoxUUID = new System.Windows.Forms.TextBox();
            txtKmBoxPort = new System.Windows.Forms.TextBox();
            txtKmBoxIP = new System.Windows.Forms.TextBox();
            lblKmBoxUUID = new System.Windows.Forms.Label();
            lblKmBoxPort = new System.Windows.Forms.Label();
            lblKmBoxIP = new System.Windows.Forms.Label();
            folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            folderBrowserDialog2 = new System.Windows.Forms.FolderBrowserDialog();
            chkDebugMode = new System.Windows.Forms.CheckBox();
            lblDebugInfo = new System.Windows.Forms.Label();
            chkQuickScopeMode = new System.Windows.Forms.CheckBox();
            lblSniperModel = new System.Windows.Forms.Label();
            cmbSniperModel = new System.Windows.Forms.ComboBox();
            lblRifleModel = new System.Windows.Forms.Label();
            cmbRifleModel = new System.Windows.Forms.ComboBox();
            chkAutoSwitchWeapon = new System.Windows.Forms.CheckBox();
            chkRifleLockHead = new System.Windows.Forms.CheckBox();
            chkMicroAim = new System.Windows.Forms.CheckBox();
            txtMicroAimExtend = new System.Windows.Forms.TextBox();
            lblBlindFireFrames = new System.Windows.Forms.Label();
            txtBlindFireFrames = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            groupBox4.SuspendLayout();
            SuspendLayout();
            // 
            // textBox1
            // 
            textBox1.Font = new System.Drawing.Font("Microsoft YaHei UI", 16F);
            textBox1.Location = new System.Drawing.Point(97, 712);
            textBox1.Name = "textBox1";
            textBox1.Size = new System.Drawing.Size(544, 35);
            textBox1.TabIndex = 1;
            // 
            // pictureBox1
            // 
            pictureBox1.Location = new System.Drawing.Point(1, 2);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new System.Drawing.Size(640, 640);
            pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 2;
            pictureBox1.TabStop = false;
            // 
            // textBox2
            // 
            textBox2.Font = new System.Drawing.Font("Microsoft YaHei UI", 16F);
            textBox2.Location = new System.Drawing.Point(97, 753);
            textBox2.Name = "textBox2";
            textBox2.Size = new System.Drawing.Size(544, 35);
            textBox2.TabIndex = 3;
            // 
            // groupBox4
            // 
            groupBox4.Controls.Add(lblKmBoxStatus);
            groupBox4.Controls.Add(btnKmBoxConnect);
            groupBox4.Controls.Add(txtKmBoxUUID);
            groupBox4.Controls.Add(txtKmBoxPort);
            groupBox4.Controls.Add(txtKmBoxIP);
            groupBox4.Controls.Add(lblKmBoxUUID);
            groupBox4.Controls.Add(lblKmBoxPort);
            groupBox4.Controls.Add(lblKmBoxIP);
            groupBox4.Location = new System.Drawing.Point(97, 648);
            groupBox4.Name = "groupBox4";
            groupBox4.Size = new System.Drawing.Size(544, 58);
            groupBox4.TabIndex = 12;
            groupBox4.TabStop = false;
            groupBox4.Text = "KMBOX设置";
            // 
            // lblKmBoxStatus
            // 
            lblKmBoxStatus.AutoSize = true;
            lblKmBoxStatus.ForeColor = System.Drawing.Color.Gray;
            lblKmBoxStatus.Location = new System.Drawing.Point(493, 26);
            lblKmBoxStatus.Name = "lblKmBoxStatus";
            lblKmBoxStatus.Size = new System.Drawing.Size(44, 17);
            lblKmBoxStatus.TabIndex = 0;
            lblKmBoxStatus.Text = "未连接";
            // 
            // btnKmBoxConnect
            // 
            btnKmBoxConnect.Location = new System.Drawing.Point(425, 22);
            btnKmBoxConnect.Name = "btnKmBoxConnect";
            btnKmBoxConnect.Size = new System.Drawing.Size(60, 25);
            btnKmBoxConnect.TabIndex = 1;
            btnKmBoxConnect.Text = "连接";
            btnKmBoxConnect.UseVisualStyleBackColor = true;
            btnKmBoxConnect.Click += btnKmBoxConnect_Click;
            // 
            // txtKmBoxUUID
            // 
            txtKmBoxUUID.Location = new System.Drawing.Point(307, 22);
            txtKmBoxUUID.Name = "txtKmBoxUUID";
            txtKmBoxUUID.Size = new System.Drawing.Size(100, 23);
            txtKmBoxUUID.TabIndex = 2;
            txtKmBoxUUID.Text = "12345678";
            // 
            // txtKmBoxPort
            // 
            txtKmBoxPort.Location = new System.Drawing.Point(188, 22);
            txtKmBoxPort.Name = "txtKmBoxPort";
            txtKmBoxPort.Size = new System.Drawing.Size(50, 23);
            txtKmBoxPort.TabIndex = 3;
            txtKmBoxPort.Text = "8888";
            // 
            // txtKmBoxIP
            // 
            txtKmBoxIP.Location = new System.Drawing.Point(36, 22);
            txtKmBoxIP.Name = "txtKmBoxIP";
            txtKmBoxIP.Size = new System.Drawing.Size(100, 23);
            txtKmBoxIP.TabIndex = 4;
            txtKmBoxIP.Text = "192.168.3.188";
            // 
            // lblKmBoxUUID
            // 
            lblKmBoxUUID.AutoSize = true;
            lblKmBoxUUID.Location = new System.Drawing.Point(258, 26);
            lblKmBoxUUID.Name = "lblKmBoxUUID";
            lblKmBoxUUID.Size = new System.Drawing.Size(42, 17);
            lblKmBoxUUID.TabIndex = 5;
            lblKmBoxUUID.Text = "UUID:";
            // 
            // lblKmBoxPort
            // 
            lblKmBoxPort.AutoSize = true;
            lblKmBoxPort.Location = new System.Drawing.Point(147, 26);
            lblKmBoxPort.Name = "lblKmBoxPort";
            lblKmBoxPort.Size = new System.Drawing.Size(35, 17);
            lblKmBoxPort.TabIndex = 6;
            lblKmBoxPort.Text = "Port:";
            // 
            // lblKmBoxIP
            // 
            lblKmBoxIP.AutoSize = true;
            lblKmBoxIP.Location = new System.Drawing.Point(8, 26);
            lblKmBoxIP.Name = "lblKmBoxIP";
            lblKmBoxIP.Size = new System.Drawing.Size(22, 17);
            lblKmBoxIP.TabIndex = 7;
            lblKmBoxIP.Text = "IP:";
            // 
            // chkDebugMode
            // 
            chkDebugMode.AutoSize = true;
            chkDebugMode.Location = new System.Drawing.Point(16, 762);
            chkDebugMode.Name = "chkDebugMode";
            chkDebugMode.Size = new System.Drawing.Size(75, 21);
            chkDebugMode.TabIndex = 14;
            chkDebugMode.Text = "调试模式";
            chkDebugMode.UseVisualStyleBackColor = true;
            chkDebugMode.CheckedChanged += chkDebugMode_CheckedChanged;
            // 
            // lblDebugInfo
            // 
            lblDebugInfo.AutoSize = true;
            lblDebugInfo.Font = new System.Drawing.Font("Consolas", 8F);
            lblDebugInfo.Location = new System.Drawing.Point(12, 222);
            lblDebugInfo.Name = "lblDebugInfo";
            lblDebugInfo.Size = new System.Drawing.Size(0, 13);
            lblDebugInfo.TabIndex = 15;
            // 
            // chkQuickScopeMode
            // 
            chkQuickScopeMode.AutoSize = true;
            chkQuickScopeMode.Checked = true;
            chkQuickScopeMode.CheckState = System.Windows.Forms.CheckState.Checked;
            chkQuickScopeMode.Location = new System.Drawing.Point(97, 820);
            chkQuickScopeMode.Name = "chkQuickScopeMode";
            chkQuickScopeMode.Size = new System.Drawing.Size(75, 21);
            chkQuickScopeMode.TabIndex = 20;
            // UI 文本“真人模式”：勾选=真人操作（瞬狙+开镜打，等用户左键意图才代发），
            //                   不勾=瞬狙模式（开镜即自动代发，视觉上像连续瞬狙）。
            // 字段名 chkQuickScopeMode / _quickScopeMode / QuickScopeController 均是历史命名（原程序“瞬狙干预”），
            // 为避免大规模重构未重命名，语义见相关字段注释。
            chkQuickScopeMode.Text = "真人模式";
            chkQuickScopeMode.UseVisualStyleBackColor = true;
            chkQuickScopeMode.CheckedChanged += chkQuickScopeMode_CheckedChanged;
            // 
            // lblSniperModel
            // 
            lblSniperModel.AutoSize = true;
            lblSniperModel.Location = new System.Drawing.Point(97, 794);
            lblSniperModel.Name = "lblSniperModel";
            lblSniperModel.Size = new System.Drawing.Size(35, 17);
            lblSniperModel.TabIndex = 16;
            lblSniperModel.Text = "狙击:";
            // 
            // cmbSniperModel
            // 
            cmbSniperModel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cmbSniperModel.Location = new System.Drawing.Point(137, 790);
            cmbSniperModel.Name = "cmbSniperModel";
            cmbSniperModel.Size = new System.Drawing.Size(190, 25);
            cmbSniperModel.TabIndex = 17;
            // 
            // lblRifleModel
            // 
            lblRifleModel.AutoSize = true;
            lblRifleModel.Location = new System.Drawing.Point(340, 794);
            lblRifleModel.Name = "lblRifleModel";
            lblRifleModel.Size = new System.Drawing.Size(35, 17);
            lblRifleModel.TabIndex = 18;
            lblRifleModel.Text = "步枪:";
            // 
            // cmbRifleModel
            // 
            cmbRifleModel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cmbRifleModel.Location = new System.Drawing.Point(380, 790);
            cmbRifleModel.Name = "cmbRifleModel";
            cmbRifleModel.Size = new System.Drawing.Size(190, 25);
            cmbRifleModel.TabIndex = 19;
            // 
            // chkAutoSwitchWeapon
            // 
            chkAutoSwitchWeapon.AutoSize = true;
            chkAutoSwitchWeapon.Location = new System.Drawing.Point(340, 820);
            chkAutoSwitchWeapon.Name = "chkAutoSwitchWeapon";
            chkAutoSwitchWeapon.Size = new System.Drawing.Size(75, 21);
            chkAutoSwitchWeapon.TabIndex = 23;
            chkAutoSwitchWeapon.Text = "狙击切枪";
            chkAutoSwitchWeapon.UseVisualStyleBackColor = true;
            chkAutoSwitchWeapon.CheckedChanged += chkAutoSwitchWeapon_CheckedChanged;
            // 
            // chkRifleLockHead
            // 
            chkRifleLockHead.AutoSize = true;
            chkRifleLockHead.Checked = true;
            chkRifleLockHead.CheckState = System.Windows.Forms.CheckState.Checked;
            chkRifleLockHead.Location = new System.Drawing.Point(440, 820);
            chkRifleLockHead.Name = "chkRifleLockHead";
            chkRifleLockHead.Size = new System.Drawing.Size(75, 21);
            chkRifleLockHead.TabIndex = 24;
            chkRifleLockHead.Text = "步枪打头";
            chkRifleLockHead.UseVisualStyleBackColor = true;
            chkRifleLockHead.CheckedChanged += chkRifleLockHead_CheckedChanged;
            // 
            // chkMicroAim
            // 
            chkMicroAim.AutoSize = true;
            chkMicroAim.Location = new System.Drawing.Point(525, 820);
            chkMicroAim.Name = "chkMicroAim";
            chkMicroAim.Size = new System.Drawing.Size(75, 21);
            chkMicroAim.TabIndex = 25;
            chkMicroAim.Text = "微自瞄";
            chkMicroAim.UseVisualStyleBackColor = true;
            chkMicroAim.CheckedChanged += chkMicroAim_CheckedChanged;
            // 
            // txtMicroAimExtend
            // 
            txtMicroAimExtend.Location = new System.Drawing.Point(598, 818);
            txtMicroAimExtend.Name = "txtMicroAimExtend";
            txtMicroAimExtend.Size = new System.Drawing.Size(40, 23);
            txtMicroAimExtend.TabIndex = 26;
            txtMicroAimExtend.Text = "30";
            txtMicroAimExtend.TextChanged += txtMicroAimExtend_TextChanged;
            // 
            // lblBlindFireFrames
            // 
            lblBlindFireFrames.AutoSize = true;
            lblBlindFireFrames.Location = new System.Drawing.Point(6, 794);
            lblBlindFireFrames.Name = "lblBlindFireFrames";
            lblBlindFireFrames.Size = new System.Drawing.Size(56, 17);
            lblBlindFireFrames.TabIndex = 27;
            lblBlindFireFrames.Text = "盲射帧数:"; // 仅真人模式生效：0=不自瞄只原地代发，1=只看当前帧，N≥2 多等 N-1 帧（0~10）
            // 
            // txtBlindFireFrames
            // 
            txtBlindFireFrames.Location = new System.Drawing.Point(6, 818);
            txtBlindFireFrames.Name = "txtBlindFireFrames";
            txtBlindFireFrames.Size = new System.Drawing.Size(50, 23);
            txtBlindFireFrames.TabIndex = 28;
            txtBlindFireFrames.Text = "1";
            txtBlindFireFrames.TextChanged += txtBlindFireFrames_TextChanged;
            // 
            // Form1
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(643, 855);
            Controls.Add(txtBlindFireFrames);
            Controls.Add(lblBlindFireFrames);
            Controls.Add(txtMicroAimExtend);
            Controls.Add(chkMicroAim);
            Controls.Add(chkRifleLockHead);
            Controls.Add(chkAutoSwitchWeapon);
            Controls.Add(chkQuickScopeMode);
            Controls.Add(cmbRifleModel);
            Controls.Add(lblRifleModel);
            Controls.Add(cmbSniperModel);
            Controls.Add(lblSniperModel);
            Controls.Add(lblDebugInfo);
            Controls.Add(chkDebugMode);
            Controls.Add(groupBox4);
            Controls.Add(textBox2);
            Controls.Add(pictureBox1);
            Controls.Add(textBox1);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            groupBox4.ResumeLayout(false);
            groupBox4.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.TextBox txtKmBoxIP;
        private System.Windows.Forms.TextBox txtKmBoxPort;
        private System.Windows.Forms.TextBox txtKmBoxUUID;
        private System.Windows.Forms.Button btnKmBoxConnect;
        private System.Windows.Forms.Label lblKmBoxIP;
        private System.Windows.Forms.Label lblKmBoxPort;
        private System.Windows.Forms.Label lblKmBoxUUID;
        private System.Windows.Forms.Label lblKmBoxStatus;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog2;
        private System.Windows.Forms.CheckBox chkDebugMode;
        private System.Windows.Forms.Label lblDebugInfo;
        private System.Windows.Forms.Label lblSniperModel;
        private System.Windows.Forms.ComboBox cmbSniperModel;
        private System.Windows.Forms.Label lblRifleModel;
        private System.Windows.Forms.ComboBox cmbRifleModel;
                private System.Windows.Forms.CheckBox chkQuickScopeMode;
        private System.Windows.Forms.CheckBox chkAutoSwitchWeapon;
        private System.Windows.Forms.CheckBox chkRifleLockHead;
        private System.Windows.Forms.CheckBox chkMicroAim;
        private System.Windows.Forms.TextBox txtMicroAimExtend;
        private System.Windows.Forms.Label lblBlindFireFrames;
        private System.Windows.Forms.TextBox txtBlindFireFrames;

    }
}

