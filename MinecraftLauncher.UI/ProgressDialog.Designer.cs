namespace MinecraftLauncher
{
	sealed partial class ProgressDialog
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
			if (disposing && (this.components != null))
			{
				this.components.Dispose();
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProgressDialog));
			this.pbProgress = new System.Windows.Forms.ProgressBar();
			this.panelStatus = new System.Windows.Forms.Panel();
			this.lbStatus = new System.Windows.Forms.Label();
			this.lbPercentage = new System.Windows.Forms.Label();
			this.btnCancel = new System.Windows.Forms.Button();
			this.panelStatus.SuspendLayout();
			this.SuspendLayout();
			// 
			// pbProgress
			// 
			this.pbProgress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.pbProgress.Location = new System.Drawing.Point(12, 91);
			this.pbProgress.Name = "pbProgress";
			this.pbProgress.Size = new System.Drawing.Size(527, 23);
			this.pbProgress.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
			this.pbProgress.TabIndex = 1;
			// 
			// panelStatus
			// 
			this.panelStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.panelStatus.BackColor = System.Drawing.SystemColors.Window;
			this.panelStatus.Controls.Add(this.lbStatus);
			this.panelStatus.Controls.Add(this.lbPercentage);
			this.panelStatus.Controls.Add(this.pbProgress);
			this.panelStatus.Location = new System.Drawing.Point(0, 0);
			this.panelStatus.Name = "panelStatus";
			this.panelStatus.Size = new System.Drawing.Size(551, 135);
			this.panelStatus.TabIndex = 3;
			// 
			// lbStatus
			// 
			this.lbStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.lbStatus.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lbStatus.Location = new System.Drawing.Point(12, 12);
			this.lbStatus.Name = "lbStatus";
			this.lbStatus.Size = new System.Drawing.Size(527, 76);
			this.lbStatus.TabIndex = 1;
			this.lbStatus.Text = "Please wait...";
			// 
			// lbPercentage
			// 
			this.lbPercentage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.lbPercentage.AutoSize = true;
			this.lbPercentage.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lbPercentage.Location = new System.Drawing.Point(12, 118);
			this.lbPercentage.Name = "lbPercentage";
			this.lbPercentage.Size = new System.Drawing.Size(22, 13);
			this.lbPercentage.TabIndex = 2;
			this.lbPercentage.Text = "0%";
			this.lbPercentage.Visible = false;
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.AutoSize = true;
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(453, 141);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(86, 29);
			this.btnCancel.TabIndex = 4;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new System.EventHandler(this.OnBtnCancelClick);
			// 
			// ProgressDialog
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(551, 177);
			this.ControlBox = false;
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.panelStatus);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "ProgressDialog";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Operation in progress";
			this.panelStatus.ResumeLayout(false);
			this.panelStatus.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.ProgressBar pbProgress;
		private System.Windows.Forms.Panel panelStatus;
		private System.Windows.Forms.Label lbStatus;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Label lbPercentage;
	}
}