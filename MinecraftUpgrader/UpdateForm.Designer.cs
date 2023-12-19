namespace MinecraftUpgrader
{
	partial class UpdateForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose( bool disposing )
		{
			if ( disposing && ( components != null ) )
			{
				components.Dispose();
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			var resources = new System.ComponentModel.ComponentResourceManager(typeof(UpdateForm));
			this.lbStatus = new Label();
			this.pbDownload = new ProgressBar();
			this.lbProgress = new Label();
			SuspendLayout();
			// 
			// lbStatus
			// 
			this.lbStatus.AutoSize = true;
			this.lbStatus.Location = new Point(12, 9);
			this.lbStatus.Margin = new Padding(2, 0, 2, 0);
			this.lbStatus.Name = "lbStatus";
			this.lbStatus.Size = new Size(129, 15);
			this.lbStatus.TabIndex = 0;
			this.lbStatus.Text = "Checking for updates...";
			// 
			// pbDownload
			// 
			this.pbDownload.Anchor =    AnchorStyles.Bottom  |  AnchorStyles.Left   |  AnchorStyles.Right ;
			this.pbDownload.Location = new Point(12, 93);
			this.pbDownload.Margin = new Padding(3, 4, 3, 4);
			this.pbDownload.Name = "pbDownload";
			this.pbDownload.Size = new Size(538, 26);
			this.pbDownload.Style = ProgressBarStyle.Marquee;
			this.pbDownload.TabIndex = 2;
			// 
			// lbProgress
			// 
			this.lbProgress.Anchor =    AnchorStyles.Bottom  |  AnchorStyles.Left   |  AnchorStyles.Right ;
			this.lbProgress.Location = new Point(12, 64);
			this.lbProgress.Margin = new Padding(2, 0, 2, 0);
			this.lbProgress.Name = "lbProgress";
			this.lbProgress.Size = new Size(538, 25);
			this.lbProgress.TabIndex = 0;
			// 
			// UpdateForm
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(560, 132);
			Controls.Add(this.pbDownload);
			Controls.Add(this.lbProgress);
			Controls.Add(this.lbStatus);
			FormBorderStyle = FormBorderStyle.FixedSingle;
			Icon = (Icon) resources.GetObject("$this.Icon");
			Margin = new Padding(2);
			MaximizeBox = false;
			Name = "UpdateForm";
			StartPosition = FormStartPosition.CenterScreen;
			Text = "Updating Program";
			Load +=  UpdateForm_Load ;
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private System.Windows.Forms.Label lbStatus;
		private System.Windows.Forms.ProgressBar pbDownload;
		private System.Windows.Forms.Label lbProgress;
	}
}