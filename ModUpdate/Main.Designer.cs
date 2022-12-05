namespace ModUpdater
{
    partial class Main
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            this.TheProgressBar = new System.Windows.Forms.ProgressBar();
            this.Tips = new System.Windows.Forms.Label();
            this.ButtonOk = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // TheProgressBar
            // 
            this.TheProgressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TheProgressBar.Location = new System.Drawing.Point(12, 12);
            this.TheProgressBar.Name = "TheProgressBar";
            this.TheProgressBar.Size = new System.Drawing.Size(260, 23);
            this.TheProgressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.TheProgressBar.TabIndex = 0;
            // 
            // Tips
            // 
            this.Tips.AutoSize = true;
            this.Tips.Location = new System.Drawing.Point(12, 47);
            this.Tips.Name = "Tips";
            this.Tips.Size = new System.Drawing.Size(92, 17);
            this.Tips.TabIndex = 1;
            this.Tips.Text = "正在连接服务器";
            // 
            // ButtonOk
            // 
            this.ButtonOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonOk.Location = new System.Drawing.Point(197, 47);
            this.ButtonOk.Name = "ButtonOk";
            this.ButtonOk.Size = new System.Drawing.Size(75, 23);
            this.ButtonOk.TabIndex = 2;
            this.ButtonOk.Text = "好的";
            this.ButtonOk.UseVisualStyleBackColor = true;
            this.ButtonOk.Visible = false;
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(284, 81);
            this.Controls.Add(this.ButtonOk);
            this.Controls.Add(this.Tips);
            this.Controls.Add(this.TheProgressBar);
            this.Font = new System.Drawing.Font("微软雅黑 Light", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Main";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Mod更新器";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.Main_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ProgressBar TheProgressBar;
        private System.Windows.Forms.Label Tips;
        private System.Windows.Forms.Button ButtonOk;
    }
}

