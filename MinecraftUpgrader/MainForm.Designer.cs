namespace MinecraftUpgrader
{
	partial class MainForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.gbStepOne = new System.Windows.Forms.GroupBox();
			this.lbInstancesFolder = new System.Windows.Forms.Label();
			this.lbInstancesFolderStatic = new System.Windows.Forms.Label();
			this.btnBrowseMmc = new System.Windows.Forms.Button();
			this.txtMmcPath = new System.Windows.Forms.TextBox();
			this.lbDownloadMmc = new System.Windows.Forms.LinkLabel();
			this.lbMmcPath = new System.Windows.Forms.Label();
			this.gbStep2 = new System.Windows.Forms.GroupBox();
			this.txtNewInstanceName = new System.Windows.Forms.TextBox();
			this.cbInstance = new System.Windows.Forms.ComboBox();
			this.rbInstanceConvert = new System.Windows.Forms.RadioButton();
			this.rbInstanceNew = new System.Windows.Forms.RadioButton();
			this.label1 = new System.Windows.Forms.Label();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.btnGo = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.gbStepOne.SuspendLayout();
			this.gbStep2.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// gbStepOne
			// 
			this.gbStepOne.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.gbStepOne.Controls.Add(this.lbInstancesFolder);
			this.gbStepOne.Controls.Add(this.lbInstancesFolderStatic);
			this.gbStepOne.Controls.Add(this.btnBrowseMmc);
			this.gbStepOne.Controls.Add(this.txtMmcPath);
			this.gbStepOne.Controls.Add(this.lbDownloadMmc);
			this.gbStepOne.Controls.Add(this.lbMmcPath);
			this.gbStepOne.Location = new System.Drawing.Point(18, 18);
			this.gbStepOne.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.gbStepOne.Name = "gbStepOne";
			this.gbStepOne.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.gbStepOne.Size = new System.Drawing.Size(994, 120);
			this.gbStepOne.TabIndex = 0;
			this.gbStepOne.TabStop = false;
			this.gbStepOne.Text = "Step 1: MultiMC";
			// 
			// lbInstancesFolder
			// 
			this.lbInstancesFolder.AutoSize = true;
			this.lbInstancesFolder.Location = new System.Drawing.Point(146, 88);
			this.lbInstancesFolder.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.lbInstancesFolder.Name = "lbInstancesFolder";
			this.lbInstancesFolder.Size = new System.Drawing.Size(84, 20);
			this.lbInstancesFolder.TabIndex = 4;
			this.lbInstancesFolder.Text = "Not Found";
			// 
			// lbInstancesFolderStatic
			// 
			this.lbInstancesFolderStatic.AutoSize = true;
			this.lbInstancesFolderStatic.Location = new System.Drawing.Point(9, 88);
			this.lbInstancesFolderStatic.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.lbInstancesFolderStatic.Name = "lbInstancesFolderStatic";
			this.lbInstancesFolderStatic.Size = new System.Drawing.Size(127, 20);
			this.lbInstancesFolderStatic.TabIndex = 4;
			this.lbInstancesFolderStatic.Text = "Instances folder:";
			// 
			// btnBrowseMmc
			// 
			this.btnBrowseMmc.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnBrowseMmc.Location = new System.Drawing.Point(936, 49);
			this.btnBrowseMmc.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.btnBrowseMmc.Name = "btnBrowseMmc";
			this.btnBrowseMmc.Size = new System.Drawing.Size(50, 35);
			this.btnBrowseMmc.TabIndex = 2;
			this.btnBrowseMmc.Text = "...";
			this.btnBrowseMmc.UseVisualStyleBackColor = true;
			this.btnBrowseMmc.Click += new System.EventHandler(this.OnBtnBrowseMmcClick);
			// 
			// txtMmcPath
			// 
			this.txtMmcPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.txtMmcPath.Location = new System.Drawing.Point(14, 52);
			this.txtMmcPath.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.txtMmcPath.Name = "txtMmcPath";
			this.txtMmcPath.Size = new System.Drawing.Size(912, 26);
			this.txtMmcPath.TabIndex = 1;
			this.txtMmcPath.Leave += new System.EventHandler(this.OnTxtMmcPathLeave);
			// 
			// lbDownloadMmc
			// 
			this.lbDownloadMmc.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.lbDownloadMmc.AutoSize = true;
			this.lbDownloadMmc.Location = new System.Drawing.Point(802, 25);
			this.lbDownloadMmc.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.lbDownloadMmc.Name = "lbDownloadMmc";
			this.lbDownloadMmc.Size = new System.Drawing.Size(180, 20);
			this.lbDownloadMmc.TabIndex = 0;
			this.lbDownloadMmc.TabStop = true;
			this.lbDownloadMmc.Text = "Download MultiMC Here";
			this.lbDownloadMmc.TextAlign = System.Drawing.ContentAlignment.TopRight;
			this.lbDownloadMmc.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnLbDownloadMmcClick);
			// 
			// lbMmcPath
			// 
			this.lbMmcPath.AutoSize = true;
			this.lbMmcPath.Location = new System.Drawing.Point(9, 25);
			this.lbMmcPath.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.lbMmcPath.Name = "lbMmcPath";
			this.lbMmcPath.Size = new System.Drawing.Size(476, 20);
			this.lbMmcPath.TabIndex = 0;
			this.lbMmcPath.Text = "You need MultiMC to run the pack. Where do you have it installed?";
			// 
			// gbStep2
			// 
			this.gbStep2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.gbStep2.Controls.Add(this.txtNewInstanceName);
			this.gbStep2.Controls.Add(this.cbInstance);
			this.gbStep2.Controls.Add(this.rbInstanceConvert);
			this.gbStep2.Controls.Add(this.rbInstanceNew);
			this.gbStep2.Controls.Add(this.label1);
			this.gbStep2.Location = new System.Drawing.Point(18, 148);
			this.gbStep2.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.gbStep2.Name = "gbStep2";
			this.gbStep2.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.gbStep2.Size = new System.Drawing.Size(994, 229);
			this.gbStep2.TabIndex = 1;
			this.gbStep2.TabStop = false;
			this.gbStep2.Text = "Step 2: Pick Instance";
			// 
			// txtNewInstanceName
			// 
			this.txtNewInstanceName.Enabled = false;
			this.txtNewInstanceName.Location = new System.Drawing.Point(262, 108);
			this.txtNewInstanceName.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.txtNewInstanceName.Name = "txtNewInstanceName";
			this.txtNewInstanceName.Size = new System.Drawing.Size(721, 26);
			this.txtNewInstanceName.TabIndex = 1;
			this.txtNewInstanceName.TextChanged += new System.EventHandler(this.OnTxtNewInstanceNameChanged);
			// 
			// cbInstance
			// 
			this.cbInstance.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.cbInstance.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbInstance.Enabled = false;
			this.cbInstance.FormattingEnabled = true;
			this.cbInstance.Location = new System.Drawing.Point(38, 180);
			this.cbInstance.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.cbInstance.Name = "cbInstance";
			this.cbInstance.Size = new System.Drawing.Size(946, 28);
			this.cbInstance.TabIndex = 3;
			this.cbInstance.SelectedIndexChanged += new System.EventHandler(this.OnCbInstanceChanged);
			// 
			// rbInstanceConvert
			// 
			this.rbInstanceConvert.AutoSize = true;
			this.rbInstanceConvert.Enabled = false;
			this.rbInstanceConvert.Location = new System.Drawing.Point(14, 145);
			this.rbInstanceConvert.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.rbInstanceConvert.Name = "rbInstanceConvert";
			this.rbInstanceConvert.Size = new System.Drawing.Size(214, 24);
			this.rbInstanceConvert.TabIndex = 2;
			this.rbInstanceConvert.Text = "Convert existing instance:";
			this.rbInstanceConvert.UseVisualStyleBackColor = true;
			this.rbInstanceConvert.CheckedChanged += new System.EventHandler(this.OnRbInstanceChecked);
			// 
			// rbInstanceNew
			// 
			this.rbInstanceNew.AutoSize = true;
			this.rbInstanceNew.Checked = true;
			this.rbInstanceNew.Location = new System.Drawing.Point(14, 109);
			this.rbInstanceNew.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.rbInstanceNew.Name = "rbInstanceNew";
			this.rbInstanceNew.Size = new System.Drawing.Size(236, 24);
			this.rbInstanceNew.TabIndex = 0;
			this.rbInstanceNew.TabStop = true;
			this.rbInstanceNew.Text = "Create new instance named:";
			this.rbInstanceNew.UseVisualStyleBackColor = true;
			this.rbInstanceNew.CheckedChanged += new System.EventHandler(this.OnRbInstanceChecked);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(9, 25);
			this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(749, 60);
			this.label1.TabIndex = 0;
			this.label1.Text = resources.GetString("label1.Text");
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.btnGo);
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Location = new System.Drawing.Point(18, 386);
			this.groupBox1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.groupBox1.Size = new System.Drawing.Size(994, 228);
			this.groupBox1.TabIndex = 2;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Step 3: Go!";
			// 
			// btnGo
			// 
			this.btnGo.Enabled = false;
			this.btnGo.Location = new System.Drawing.Point(408, 142);
			this.btnGo.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.btnGo.Name = "btnGo";
			this.btnGo.Size = new System.Drawing.Size(178, 63);
			this.btnGo.TabIndex = 0;
			this.btnGo.Text = "Go!";
			this.btnGo.UseVisualStyleBackColor = true;
			this.btnGo.Click += new System.EventHandler(this.OnBtnGoClick);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(9, 25);
			this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(943, 80);
			this.label2.TabIndex = 0;
			this.label2.Text = resources.GetString("label2.Text");
			// 
			// MainForm
			// 
			this.AcceptButton = this.btnGo;
			this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1030, 634);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.gbStep2);
			this.Controls.Add(this.gbStepOne);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.Name = "MainForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Minecraft Upgrader";
			this.gbStepOne.ResumeLayout(false);
			this.gbStepOne.PerformLayout();
			this.gbStep2.ResumeLayout(false);
			this.gbStep2.PerformLayout();
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox gbStepOne;
		private System.Windows.Forms.Button btnBrowseMmc;
		private System.Windows.Forms.TextBox txtMmcPath;
		private System.Windows.Forms.LinkLabel lbDownloadMmc;
		private System.Windows.Forms.Label lbMmcPath;
		private System.Windows.Forms.Label lbInstancesFolderStatic;
		private System.Windows.Forms.Label lbInstancesFolder;
		private System.Windows.Forms.GroupBox gbStep2;
		private System.Windows.Forms.TextBox txtNewInstanceName;
		private System.Windows.Forms.ComboBox cbInstance;
		private System.Windows.Forms.RadioButton rbInstanceConvert;
		private System.Windows.Forms.RadioButton rbInstanceNew;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Button btnGo;
		private System.Windows.Forms.Label label2;
	}
}

