namespace gprs
{
    partial class TestMoveForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            panelParams = new System.Windows.Forms.Panel();
            _btnClear = new System.Windows.Forms.Button();
            _btnStart = new System.Windows.Forms.Button();
            _chkOvershoot = new System.Windows.Forms.CheckBox();
            _chkJitter = new System.Windows.Forms.CheckBox();
            _chkEasing = new System.Windows.Forms.CheckBox();
            _chkRandomArc = new System.Windows.Forms.CheckBox();
            _numEasingPower = new System.Windows.Forms.NumericUpDown();
            lblPower = new System.Windows.Forms.Label();
            _numSmoothThreshold = new System.Windows.Forms.NumericUpDown();
            lblSmooth = new System.Windows.Forms.Label();
            _numSmoothness = new System.Windows.Forms.NumericUpDown();
            lblSmoothness = new System.Windows.Forms.Label();
            _numTargetY = new System.Windows.Forms.NumericUpDown();
            lblTargetY = new System.Windows.Forms.Label();
            _numTargetX = new System.Windows.Forms.NumericUpDown();
            lblTargetX = new System.Windows.Forms.Label();
            splitContainer = new System.Windows.Forms.SplitContainer();
            _picTrajectory = new System.Windows.Forms.PictureBox();
            _gridPoints = new System.Windows.Forms.DataGridView();
            ColIndex = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ColDeltaX = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ColDeltaY = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ColAccumX = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ColAccumY = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ColElapsedMs = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _lblStats = new System.Windows.Forms.Label();
            panelParams.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_numEasingPower).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_numSmoothThreshold).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_numSmoothness).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_numTargetY).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_numTargetX).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainer).BeginInit();
            splitContainer.Panel1.SuspendLayout();
            splitContainer.Panel2.SuspendLayout();
            splitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_picTrajectory).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_gridPoints).BeginInit();
            SuspendLayout();
            // 
            // panelParams
            // 
            panelParams.Controls.Add(_btnClear);
            panelParams.Controls.Add(_btnStart);
            panelParams.Controls.Add(_chkOvershoot);
            panelParams.Controls.Add(_chkJitter);
            panelParams.Controls.Add(_chkEasing);
            panelParams.Controls.Add(_chkRandomArc);
            panelParams.Controls.Add(_numEasingPower);
            panelParams.Controls.Add(lblPower);
            panelParams.Controls.Add(_numSmoothThreshold);
            panelParams.Controls.Add(lblSmooth);
            panelParams.Controls.Add(_numSmoothness);
            panelParams.Controls.Add(lblSmoothness);
            panelParams.Controls.Add(_numTargetY);
            panelParams.Controls.Add(lblTargetY);
            panelParams.Controls.Add(_numTargetX);
            panelParams.Controls.Add(lblTargetX);
            panelParams.Dock = System.Windows.Forms.DockStyle.Top;
            panelParams.Location = new System.Drawing.Point(0, 0);
            panelParams.Name = "panelParams";
            panelParams.Padding = new System.Windows.Forms.Padding(10);
            panelParams.Size = new System.Drawing.Size(1188, 100);
            panelParams.TabIndex = 0;
            // 
            // _btnClear
            // 
            _btnClear.Location = new System.Drawing.Point(460, 45);
            _btnClear.Name = "_btnClear";
            _btnClear.Size = new System.Drawing.Size(60, 28);
            _btnClear.TabIndex = 15;
            _btnClear.Text = "清空";
            _btnClear.UseVisualStyleBackColor = true;
            _btnClear.Click += BtnClear_Click;
            // 
            // _btnStart
            // 
            _btnStart.Location = new System.Drawing.Point(370, 45);
            _btnStart.Name = "_btnStart";
            _btnStart.Size = new System.Drawing.Size(80, 28);
            _btnStart.TabIndex = 14;
            _btnStart.Text = "开始测试";
            _btnStart.UseVisualStyleBackColor = true;
            _btnStart.Click += BtnStart_Click;
            // 
            // _chkOvershoot
            // 
            _chkOvershoot.AutoSize = true;
            _chkOvershoot.Location = new System.Drawing.Point(270, 50);
            _chkOvershoot.Name = "_chkOvershoot";
            _chkOvershoot.Size = new System.Drawing.Size(75, 21);
            _chkOvershoot.TabIndex = 13;
            _chkOvershoot.Text = "过冲修正";
            _chkOvershoot.UseVisualStyleBackColor = true;
            // 
            // _chkJitter
            // 
            _chkJitter.AutoSize = true;
            _chkJitter.Checked = true;
            _chkJitter.CheckState = System.Windows.Forms.CheckState.Checked;
            _chkJitter.Location = new System.Drawing.Point(190, 50);
            _chkJitter.Name = "_chkJitter";
            _chkJitter.Size = new System.Drawing.Size(63, 21);
            _chkJitter.TabIndex = 12;
            _chkJitter.Text = "微抖动";
            _chkJitter.UseVisualStyleBackColor = true;
            // 
            // _chkEasing
            // 
            _chkEasing.AutoSize = true;
            _chkEasing.Checked = true;
            _chkEasing.CheckState = System.Windows.Forms.CheckState.Checked;
            _chkEasing.Location = new System.Drawing.Point(100, 50);
            _chkEasing.Name = "_chkEasing";
            _chkEasing.Size = new System.Drawing.Size(75, 21);
            _chkEasing.TabIndex = 11;
            _chkEasing.Text = "缓入缓出";
            _chkEasing.UseVisualStyleBackColor = true;
            // 
            // _chkRandomArc
            // 
            _chkRandomArc.AutoSize = true;
            _chkRandomArc.Checked = true;
            _chkRandomArc.CheckState = System.Windows.Forms.CheckState.Checked;
            _chkRandomArc.Location = new System.Drawing.Point(10, 50);
            _chkRandomArc.Name = "_chkRandomArc";
            _chkRandomArc.Size = new System.Drawing.Size(75, 21);
            _chkRandomArc.TabIndex = 10;
            _chkRandomArc.Text = "随机弧度";
            _chkRandomArc.UseVisualStyleBackColor = true;
            // 
            // _numEasingPower
            // 
            _numEasingPower.DecimalPlaces = 1;
            _numEasingPower.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
            _numEasingPower.Location = new System.Drawing.Point(688, 12);
            _numEasingPower.Maximum = new decimal(new int[] { 5, 0, 0, 0 });
            _numEasingPower.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            _numEasingPower.Name = "_numEasingPower";
            _numEasingPower.Size = new System.Drawing.Size(50, 23);
            _numEasingPower.TabIndex = 9;
            _numEasingPower.Value = new decimal(new int[] { 2, 0, 0, 0 });
            // 
            // lblPower
            // 
            lblPower.AutoSize = true;
            lblPower.Location = new System.Drawing.Point(598, 15);
            lblPower.Name = "lblPower";
            lblPower.Size = new System.Drawing.Size(85, 17);
            lblPower.TabIndex = 8;
            lblPower.Text = "EasingPower:";
            // 
            // _numSmoothThreshold
            // 
            _numSmoothThreshold.Location = new System.Drawing.Point(538, 12);
            _numSmoothThreshold.Name = "_numSmoothThreshold";
            _numSmoothThreshold.Size = new System.Drawing.Size(50, 23);
            _numSmoothThreshold.TabIndex = 7;
            _numSmoothThreshold.Value = new decimal(new int[] { 20, 0, 0, 0 });
            // 
            // lblSmooth
            // 
            lblSmooth.AutoSize = true;
            lblSmooth.Location = new System.Drawing.Point(418, 15);
            lblSmooth.Name = "lblSmooth";
            lblSmooth.Size = new System.Drawing.Size(114, 17);
            lblSmooth.TabIndex = 6;
            lblSmooth.Text = "SmoothThreshold:";
            // 
            // _numSmoothness
            // 
            _numSmoothness.DecimalPlaces = 1;
            _numSmoothness.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            _numSmoothness.Location = new System.Drawing.Point(358, 12);
            _numSmoothness.Maximum = new decimal(new int[] { 30, 0, 0, 65536 });
            _numSmoothness.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            _numSmoothness.Name = "_numSmoothness";
            _numSmoothness.Size = new System.Drawing.Size(50, 23);
            _numSmoothness.TabIndex = 5;
            _numSmoothness.Value = new decimal(new int[] { 10, 0, 0, 65536 });
            // 
            // lblSmoothness
            // 
            lblSmoothness.AutoSize = true;
            lblSmoothness.Location = new System.Drawing.Point(270, 15);
            lblSmoothness.Name = "lblSmoothness";
            lblSmoothness.Size = new System.Drawing.Size(82, 17);
            lblSmoothness.TabIndex = 4;
            lblSmoothness.Text = "Smoothness:";
            // 
            // _numTargetY
            // 
            _numTargetY.Location = new System.Drawing.Point(200, 12);
            _numTargetY.Maximum = new decimal(new int[] { 500, 0, 0, 0 });
            _numTargetY.Minimum = new decimal(new int[] { 500, 0, 0, int.MinValue });
            _numTargetY.Name = "_numTargetY";
            _numTargetY.Size = new System.Drawing.Size(60, 23);
            _numTargetY.TabIndex = 3;
            _numTargetY.Value = new decimal(new int[] { 100, 0, 0, 0 });
            // 
            // lblTargetY
            // 
            lblTargetY.AutoSize = true;
            lblTargetY.Location = new System.Drawing.Point(140, 15);
            lblTargetY.Name = "lblTargetY";
            lblTargetY.Size = new System.Drawing.Size(56, 17);
            lblTargetY.TabIndex = 2;
            lblTargetY.Text = "TargetY:";
            // 
            // _numTargetX
            // 
            _numTargetX.Location = new System.Drawing.Point(70, 12);
            _numTargetX.Maximum = new decimal(new int[] { 500, 0, 0, 0 });
            _numTargetX.Minimum = new decimal(new int[] { 500, 0, 0, int.MinValue });
            _numTargetX.Name = "_numTargetX";
            _numTargetX.Size = new System.Drawing.Size(60, 23);
            _numTargetX.TabIndex = 1;
            _numTargetX.Value = new decimal(new int[] { 100, 0, 0, 0 });
            // 
            // lblTargetX
            // 
            lblTargetX.AutoSize = true;
            lblTargetX.Location = new System.Drawing.Point(10, 15);
            lblTargetX.Name = "lblTargetX";
            lblTargetX.Size = new System.Drawing.Size(57, 17);
            lblTargetX.TabIndex = 0;
            lblTargetX.Text = "TargetX:";
            // 
            // splitContainer
            // 
            splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer.Location = new System.Drawing.Point(0, 100);
            splitContainer.Name = "splitContainer";
            // 
            // splitContainer.Panel1
            // 
            splitContainer.Panel1.Controls.Add(_picTrajectory);
            // 
            // splitContainer.Panel2
            // 
            splitContainer.Panel2.Controls.Add(_gridPoints);
            splitContainer.Size = new System.Drawing.Size(1188, 531);
            splitContainer.SplitterDistance = 723;
            splitContainer.TabIndex = 1;
            // 
            // _picTrajectory
            // 
            _picTrajectory.BackColor = System.Drawing.Color.White;
            _picTrajectory.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            _picTrajectory.Dock = System.Windows.Forms.DockStyle.Fill;
            _picTrajectory.Location = new System.Drawing.Point(0, 0);
            _picTrajectory.Name = "_picTrajectory";
            _picTrajectory.Size = new System.Drawing.Size(723, 531);
            _picTrajectory.TabIndex = 0;
            _picTrajectory.TabStop = false;
            _picTrajectory.Paint += PicTrajectory_Paint;
            // 
            // _gridPoints
            // 
            _gridPoints.AllowUserToAddRows = false;
            _gridPoints.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            _gridPoints.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            _gridPoints.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { ColIndex, ColDeltaX, ColDeltaY, ColAccumX, ColAccumY, ColElapsedMs });
            _gridPoints.Dock = System.Windows.Forms.DockStyle.Fill;
            _gridPoints.Location = new System.Drawing.Point(0, 0);
            _gridPoints.Name = "_gridPoints";
            _gridPoints.ReadOnly = true;
            _gridPoints.RowHeadersWidth = 30;
            _gridPoints.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            _gridPoints.Size = new System.Drawing.Size(461, 531);
            _gridPoints.TabIndex = 0;
            // 
            // ColIndex
            // 
            ColIndex.FillWeight = 50F;
            ColIndex.HeaderText = "序号";
            ColIndex.Name = "ColIndex";
            ColIndex.ReadOnly = true;
            // 
            // ColDeltaX
            // 
            ColDeltaX.FillWeight = 50F;
            ColDeltaX.HeaderText = "ΔX";
            ColDeltaX.Name = "ColDeltaX";
            ColDeltaX.ReadOnly = true;
            // 
            // ColDeltaY
            // 
            ColDeltaY.FillWeight = 50F;
            ColDeltaY.HeaderText = "ΔY";
            ColDeltaY.Name = "ColDeltaY";
            ColDeltaY.ReadOnly = true;
            // 
            // ColAccumX
            // 
            ColAccumX.HeaderText = "累计X";
            ColAccumX.Name = "ColAccumX";
            ColAccumX.ReadOnly = true;
            // 
            // ColAccumY
            // 
            ColAccumY.HeaderText = "累计Y";
            ColAccumY.Name = "ColAccumY";
            ColAccumY.ReadOnly = true;
            // 
            // ColElapsedMs
            // 
            ColElapsedMs.HeaderText = "时间(ms)";
            ColElapsedMs.Name = "ColElapsedMs";
            ColElapsedMs.ReadOnly = true;
            // 
            // _lblStats
            // 
            _lblStats.BackColor = System.Drawing.Color.LightGray;
            _lblStats.Dock = System.Windows.Forms.DockStyle.Bottom;
            _lblStats.Location = new System.Drawing.Point(0, 631);
            _lblStats.Name = "_lblStats";
            _lblStats.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            _lblStats.Size = new System.Drawing.Size(1188, 30);
            _lblStats.TabIndex = 2;
            _lblStats.Text = "统计: 等待测试...";
            _lblStats.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // TestMoveForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1188, 661);
            Controls.Add(splitContainer);
            Controls.Add(_lblStats);
            Controls.Add(panelParams);
            Name = "TestMoveForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "HumanLikeMove 测试工具";
            panelParams.ResumeLayout(false);
            panelParams.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_numEasingPower).EndInit();
            ((System.ComponentModel.ISupportInitialize)_numSmoothThreshold).EndInit();
            ((System.ComponentModel.ISupportInitialize)_numSmoothness).EndInit();
            ((System.ComponentModel.ISupportInitialize)_numTargetY).EndInit();
            ((System.ComponentModel.ISupportInitialize)_numTargetX).EndInit();
            splitContainer.Panel1.ResumeLayout(false);
            splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer).EndInit();
            splitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_picTrajectory).EndInit();
            ((System.ComponentModel.ISupportInitialize)_gridPoints).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel panelParams;
        private System.Windows.Forms.Label lblTargetX;
        private System.Windows.Forms.NumericUpDown _numTargetX;
        private System.Windows.Forms.Label lblTargetY;
        private System.Windows.Forms.NumericUpDown _numTargetY;
        private System.Windows.Forms.Label lblSmoothness;
        private System.Windows.Forms.NumericUpDown _numSmoothness;
        private System.Windows.Forms.Label lblSmooth;
        private System.Windows.Forms.NumericUpDown _numSmoothThreshold;
        private System.Windows.Forms.Label lblPower;
        private System.Windows.Forms.NumericUpDown _numEasingPower;
        private System.Windows.Forms.CheckBox _chkRandomArc;
        private System.Windows.Forms.CheckBox _chkEasing;
        private System.Windows.Forms.CheckBox _chkJitter;
        private System.Windows.Forms.CheckBox _chkOvershoot;
        private System.Windows.Forms.Button _btnStart;
        private System.Windows.Forms.Button _btnClear;
        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.PictureBox _picTrajectory;
        private System.Windows.Forms.DataGridView _gridPoints;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColIndex;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColDeltaX;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColDeltaY;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColAccumX;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColAccumY;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColElapsedMs;
        private System.Windows.Forms.Label _lblStats;
    }
}
