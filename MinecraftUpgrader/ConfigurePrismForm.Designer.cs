namespace MinecraftUpgrader
{
	partial class ConfigurePrismForm
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
			var resources = new System.ComponentModel.ComponentResourceManager(typeof(ConfigurePrismForm));
			this.label1 = new Label();
			this.btnDownload = new Button();
			this.btnBrowse = new Button();
			this.btnExit = new Button();
			SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Regular, GraphicsUnit.Point,  0);
			this.label1.Location = new Point(20, 17);
			this.label1.Margin = new Padding(5, 0, 5, 0);
			this.label1.Name = "label1";
			this.label1.Size = new Size(586, 200);
			this.label1.TabIndex = 0;
			this.label1.Text = resources.GetString("label1.Text");
			// 
			// btnDownload
			// 
			this.btnDownload.Anchor =    AnchorStyles.Bottom  |  AnchorStyles.Left   |  AnchorStyles.Right ;
			this.btnDownload.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Regular, GraphicsUnit.Point,  0);
			this.btnDownload.Location = new Point(17, 271);
			this.btnDownload.Margin = new Padding(5, 6, 5, 6);
			this.btnDownload.Name = "btnDownload";
			this.btnDownload.Size = new Size(643, 100);
			this.btnDownload.TabIndex = 1;
			this.btnDownload.Text = "&Download it for me";
			this.btnDownload.UseVisualStyleBackColor = true;
			this.btnDownload.Click +=  btnDownload_Click ;
			// 
			// btnBrowse
			// 
			this.btnBrowse.Anchor =    AnchorStyles.Bottom  |  AnchorStyles.Left   |  AnchorStyles.Right ;
			this.btnBrowse.Location = new Point(17, 383);
			this.btnBrowse.Margin = new Padding(5, 6, 5, 6);
			this.btnBrowse.Name = "btnBrowse";
			this.btnBrowse.Size = new Size(643, 44);
			this.btnBrowse.TabIndex = 2;
			this.btnBrowse.Text = "&Browse for it";
			this.btnBrowse.UseVisualStyleBackColor = true;
			this.btnBrowse.Click +=  btnBrowse_Click ;
			// 
			// btnExit
			// 
			this.btnExit.Anchor =    AnchorStyles.Bottom  |  AnchorStyles.Left   |  AnchorStyles.Right ;
			this.btnExit.DialogResult = DialogResult.Cancel;
			this.btnExit.Location = new Point(17, 438);
			this.btnExit.Margin = new Padding(5, 6, 5, 6);
			this.btnExit.Name = "btnExit";
			this.btnExit.Size = new Size(643, 44);
			this.btnExit.TabIndex = 3;
			this.btnExit.Text = "E&xit";
			this.btnExit.UseVisualStyleBackColor = true;
			// 
			// ConfigurePrismForm
			// 
			AcceptButton = this.btnDownload;
			AutoScaleDimensions = new SizeF(10F, 25F);
			AutoScaleMode = AutoScaleMode.Font;
			CancelButton = this.btnExit;
			ClientSize = new Size(677, 506);
			ControlBox = false;
			Controls.Add(this.btnExit);
			Controls.Add(this.btnBrowse);
			Controls.Add(this.btnDownload);
			Controls.Add(this.label1);
			FormBorderStyle = FormBorderStyle.FixedDialog;
			Icon = (Icon) resources.GetObject("$this.Icon");
			Margin = new Padding(5, 6, 5, 6);
			MaximizeBox = false;
			Name = "ConfigurePrismForm";
			StartPosition = FormStartPosition.CenterScreen;
			Text = "Prism Launcher Setup";
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button btnDownload;
		private System.Windows.Forms.Button btnBrowse;
		private System.Windows.Forms.Button btnExit;
	}
}