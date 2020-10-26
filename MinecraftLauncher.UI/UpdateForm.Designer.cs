namespace MinecraftLauncher
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
			if ( disposing && ( this.components != null ) )
			{
				this.components.Dispose();
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UpdateForm));
			this.lbStatus = new System.Windows.Forms.Label();
			this.pbDownload = new System.Windows.Forms.ProgressBar();
			this.lbProgress = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// lbStatus
			// 
			this.lbStatus.AutoSize = true;
			this.lbStatus.Location = new System.Drawing.Point(12, 9);
			this.lbStatus.Name = "lbStatus";
			this.lbStatus.Size = new System.Drawing.Size(172, 20);
			this.lbStatus.TabIndex = 0;
			this.lbStatus.Text = "Checking for updates...";
			// 
			// pbDownload
			// 
			this.pbDownload.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.pbDownload.Location = new System.Drawing.Point(13, 81);
			this.pbDownload.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.pbDownload.Name = "pbDownload";
			this.pbDownload.Size = new System.Drawing.Size(694, 35);
			this.pbDownload.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
			this.pbDownload.TabIndex = 2;
			// 
			// lbProgress
			// 
			this.lbProgress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.lbProgress.Location = new System.Drawing.Point(9, 121);
			this.lbProgress.Name = "lbProgress";
			this.lbProgress.Size = new System.Drawing.Size(698, 39);
			this.lbProgress.TabIndex = 0;
			// 
			// UpdateForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(720, 169);
			this.Controls.Add(this.pbDownload);
			this.Controls.Add(this.lbProgress);
			this.Controls.Add(this.lbStatus);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "UpdateForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Updating Program";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label lbStatus;
		private System.Windows.Forms.ProgressBar pbDownload;
		private System.Windows.Forms.Label lbProgress;
	}
}