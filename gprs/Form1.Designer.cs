﻿﻿﻿
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
            radioButton1 = new System.Windows.Forms.RadioButton();
            groupBox1 = new System.Windows.Forms.GroupBox();
            radioButton3 = new System.Windows.Forms.RadioButton();
            radioButton2 = new System.Windows.Forms.RadioButton();
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
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            groupBox1.SuspendLayout();
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
            // radioButton1
            // 
            radioButton1.AutoSize = true;
            radioButton1.Location = new System.Drawing.Point(6, 22);
            radioButton1.Name = "radioButton1";
            radioButton1.Size = new System.Drawing.Size(74, 21);
            radioButton1.TabIndex = 6;
            radioButton1.TabStop = true;
            radioButton1.Text = "穿越火线";
            radioButton1.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(radioButton3);
            groupBox1.Controls.Add(radioButton2);
            groupBox1.Controls.Add(radioButton1);
            groupBox1.Location = new System.Drawing.Point(1, 648);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new System.Drawing.Size(90, 108);
            groupBox1.TabIndex = 7;
            groupBox1.TabStop = false;
            groupBox1.Text = "功能选择";
            // 
            // radioButton3
            // 
            radioButton3.AutoSize = true;
            radioButton3.Location = new System.Drawing.Point(6, 78);
            radioButton3.Name = "radioButton3";
            radioButton3.Size = new System.Drawing.Size(38, 21);
            radioButton3.TabIndex = 12;
            radioButton3.Text = "cs";
            radioButton3.UseVisualStyleBackColor = true;
            // 
            // radioButton2
            // 
            radioButton2.AutoSize = true;
            radioButton2.Location = new System.Drawing.Point(6, 51);
            radioButton2.Name = "radioButton2";
            radioButton2.Size = new System.Drawing.Size(74, 21);
            radioButton2.TabIndex = 11;
            radioButton2.Text = "无畏契约";
            radioButton2.UseVisualStyleBackColor = true;
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
            // Form1
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(646, 793);
            Controls.Add(lblDebugInfo);
            Controls.Add(chkDebugMode);
            Controls.Add(groupBox4);
            Controls.Add(groupBox1);
            Controls.Add(textBox2);
            Controls.Add(pictureBox1);
            Controls.Add(textBox1);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox4.ResumeLayout(false);
            groupBox4.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.RadioButton radioButton1;
        private System.Windows.Forms.GroupBox groupBox1;
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
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.RadioButton radioButton3;
        private System.Windows.Forms.CheckBox chkDebugMode;
        private System.Windows.Forms.Label lblDebugInfo;

    }
}

