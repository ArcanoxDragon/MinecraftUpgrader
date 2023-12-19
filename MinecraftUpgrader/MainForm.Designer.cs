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
			this.components = new System.ComponentModel.Container();
			var resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.lbInstancesFolder = new Label();
			this.lbInstancesFolderStatic = new Label();
			this.btnBrowsePrism = new Button();
			this.txtPrismPath = new TextBox();
			this.cmbInstance = new ComboBox();
			this.txtNewInstanceName = new TextBox();
			this.label3 = new Label();
			this.rbInstanceExisting = new RadioButton();
			this.rbInstanceNew = new RadioButton();
			this.lbInstanceStatus = new Label();
			this.buttonLayoutPanel = new TableLayoutPanel();
			this.btnRebuild = new Button();
			this.btnGo = new Button();
			this.linkLabel1 = new LinkLabel();
			this.appToolTip = new ToolTip(this.components);
			this.btnNonVR = new Button();
			this.btnVR = new Button();
			this.panelVR = new Panel();
			this.lbVrMode = new Label();
			this.vrModeLayoutPanel = new TableLayoutPanel();
			this.buttonLayoutPanel.SuspendLayout();
			this.panelVR.SuspendLayout();
			this.vrModeLayoutPanel.SuspendLayout();
			SuspendLayout();
			// 
			// lbInstancesFolder
			// 
			this.lbInstancesFolder.AutoSize = true;
			this.lbInstancesFolder.Location = new Point(167, 92);
			this.lbInstancesFolder.Margin = new Padding(5, 0, 5, 0);
			this.lbInstancesFolder.Name = "lbInstancesFolder";
			this.lbInstancesFolder.Size = new Size(98, 25);
			this.lbInstancesFolder.TabIndex = 7;
			this.lbInstancesFolder.Text = "Not Found";
			// 
			// lbInstancesFolderStatic
			// 
			this.lbInstancesFolderStatic.AutoSize = true;
			this.lbInstancesFolderStatic.Location = new Point(15, 92);
			this.lbInstancesFolderStatic.Margin = new Padding(5, 0, 5, 0);
			this.lbInstancesFolderStatic.Name = "lbInstancesFolderStatic";
			this.lbInstancesFolderStatic.Size = new Size(141, 25);
			this.lbInstancesFolderStatic.TabIndex = 8;
			this.lbInstancesFolderStatic.Text = "Instances folder:";
			// 
			// btnBrowsePrism
			// 
			this.btnBrowsePrism.Anchor =   AnchorStyles.Top  |  AnchorStyles.Right ;
			this.btnBrowsePrism.Location = new Point(652, 44);
			this.btnBrowsePrism.Margin = new Padding(5, 6, 5, 6);
			this.btnBrowsePrism.Name = "btnBrowsePrism";
			this.btnBrowsePrism.Size = new Size(55, 44);
			this.btnBrowsePrism.TabIndex = 6;
			this.btnBrowsePrism.Text = "...";
			this.appToolTip.SetToolTip(this.btnBrowsePrism, "Switch Prism instance...");
			this.btnBrowsePrism.UseVisualStyleBackColor = true;
			this.btnBrowsePrism.Click +=  OnBtnBrowsePrismClick ;
			// 
			// txtPrismPath
			// 
			this.txtPrismPath.Anchor =    AnchorStyles.Top  |  AnchorStyles.Left   |  AnchorStyles.Right ;
			this.txtPrismPath.Location = new Point(20, 48);
			this.txtPrismPath.Margin = new Padding(5, 6, 5, 6);
			this.txtPrismPath.Name = "txtPrismPath";
			this.txtPrismPath.ReadOnly = true;
			this.txtPrismPath.Size = new Size(619, 31);
			this.txtPrismPath.TabIndex = 5;
			// 
			// cmbInstance
			// 
			this.cmbInstance.Anchor =    AnchorStyles.Top  |  AnchorStyles.Left   |  AnchorStyles.Right ;
			this.cmbInstance.DropDownStyle = ComboBoxStyle.DropDownList;
			this.cmbInstance.Enabled = false;
			this.cmbInstance.FormattingEnabled = true;
			this.cmbInstance.Location = new Point(20, 196);
			this.cmbInstance.Margin = new Padding(5, 6, 5, 6);
			this.cmbInstance.Name = "cmbInstance";
			this.cmbInstance.Size = new Size(684, 33);
			this.cmbInstance.TabIndex = 3;
			this.cmbInstance.SelectedIndexChanged +=  OnCbInstanceChanged ;
			// 
			// txtNewInstanceName
			// 
			this.txtNewInstanceName.Anchor =    AnchorStyles.Top  |  AnchorStyles.Left   |  AnchorStyles.Right ;
			this.txtNewInstanceName.Location = new Point(20, 348);
			this.txtNewInstanceName.Margin = new Padding(5, 6, 5, 6);
			this.txtNewInstanceName.Name = "txtNewInstanceName";
			this.txtNewInstanceName.Size = new Size(684, 31);
			this.txtNewInstanceName.TabIndex = 11;
			this.txtNewInstanceName.TextChanged +=  OnTxtNewInstanceNameChanged ;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new Point(20, 17);
			this.label3.Margin = new Padding(5, 0, 5, 0);
			this.label3.Name = "label3";
			this.label3.Size = new Size(207, 25);
			this.label3.TabIndex = 12;
			this.label3.Text = "Prism Launcher Location:";
			// 
			// rbInstanceExisting
			// 
			this.rbInstanceExisting.AutoSize = true;
			this.rbInstanceExisting.Checked = true;
			this.rbInstanceExisting.Location = new Point(20, 152);
			this.rbInstanceExisting.Margin = new Padding(5, 6, 5, 6);
			this.rbInstanceExisting.Name = "rbInstanceExisting";
			this.rbInstanceExisting.Size = new Size(230, 29);
			this.rbInstanceExisting.TabIndex = 13;
			this.rbInstanceExisting.TabStop = true;
			this.rbInstanceExisting.Text = "Pick an existing instance:";
			this.rbInstanceExisting.UseVisualStyleBackColor = true;
			this.rbInstanceExisting.CheckedChanged +=  OnRbInstanceChecked ;
			// 
			// rbInstanceNew
			// 
			this.rbInstanceNew.AutoSize = true;
			this.rbInstanceNew.Location = new Point(20, 304);
			this.rbInstanceNew.Margin = new Padding(5, 6, 5, 6);
			this.rbInstanceNew.Name = "rbInstanceNew";
			this.rbInstanceNew.Size = new Size(259, 29);
			this.rbInstanceNew.TabIndex = 14;
			this.rbInstanceNew.Text = "Or create a new one named:";
			this.rbInstanceNew.UseVisualStyleBackColor = true;
			this.rbInstanceNew.CheckedChanged +=  OnRbInstanceChecked ;
			// 
			// lbInstanceStatus
			// 
			this.lbInstanceStatus.AutoSize = true;
			this.lbInstanceStatus.Location = new Point(20, 242);
			this.lbInstanceStatus.Margin = new Padding(5, 0, 5, 0);
			this.lbInstanceStatus.Name = "lbInstanceStatus";
			this.lbInstanceStatus.Size = new Size(130, 25);
			this.lbInstanceStatus.TabIndex = 15;
			this.lbInstanceStatus.Text = "Instance Status";
			// 
			// buttonLayoutPanel
			// 
			this.buttonLayoutPanel.Anchor =    AnchorStyles.Bottom  |  AnchorStyles.Left   |  AnchorStyles.Right ;
			this.buttonLayoutPanel.ColumnCount = 2;
			this.buttonLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
			this.buttonLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
			this.buttonLayoutPanel.Controls.Add(this.btnRebuild, 0, 0);
			this.buttonLayoutPanel.Controls.Add(this.btnGo, 1, 0);
			this.buttonLayoutPanel.Location = new Point(20, 550);
			this.buttonLayoutPanel.Margin = new Padding(5, 6, 5, 6);
			this.buttonLayoutPanel.Name = "buttonLayoutPanel";
			this.buttonLayoutPanel.RowCount = 1;
			this.buttonLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			this.buttonLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 115F));
			this.buttonLayoutPanel.Size = new Size(687, 115);
			this.buttonLayoutPanel.TabIndex = 16;
			// 
			// btnRebuild
			// 
			this.btnRebuild.BackColor = SystemColors.Control;
			this.btnRebuild.Dock = DockStyle.Fill;
			this.btnRebuild.Enabled = false;
			this.btnRebuild.FlatAppearance.BorderSize = 0;
			this.btnRebuild.FlatAppearance.MouseDownBackColor = Color.DodgerBlue;
			this.btnRebuild.FlatAppearance.MouseOverBackColor = Color.FromArgb(  72,   175,   255);
			this.btnRebuild.FlatStyle = FlatStyle.Flat;
			this.btnRebuild.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Regular, GraphicsUnit.Point,  0);
			this.btnRebuild.ForeColor = SystemColors.ControlText;
			this.btnRebuild.Location = new Point(5, 6);
			this.btnRebuild.Margin = new Padding(5, 6, 5, 6);
			this.btnRebuild.Name = "btnRebuild";
			this.btnRebuild.Size = new Size(333, 103);
			this.btnRebuild.TabIndex = 2;
			this.btnRebuild.Text = "Rebuild";
			this.btnRebuild.UseVisualStyleBackColor = false;
			this.btnRebuild.Click +=  OnBtnRebuildClick ;
			// 
			// btnGo
			// 
			this.btnGo.BackColor = Color.DodgerBlue;
			this.btnGo.Dock = DockStyle.Fill;
			this.btnGo.Enabled = false;
			this.btnGo.FlatAppearance.BorderSize = 0;
			this.btnGo.FlatAppearance.MouseDownBackColor = Color.DodgerBlue;
			this.btnGo.FlatAppearance.MouseOverBackColor = Color.FromArgb(  72,   175,   255);
			this.btnGo.FlatStyle = FlatStyle.Flat;
			this.btnGo.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Regular, GraphicsUnit.Point,  0);
			this.btnGo.ForeColor = Color.White;
			this.btnGo.Location = new Point(348, 6);
			this.btnGo.Margin = new Padding(5, 6, 5, 6);
			this.btnGo.Name = "btnGo";
			this.btnGo.Size = new Size(334, 103);
			this.btnGo.TabIndex = 1;
			this.btnGo.Text = "Go!";
			this.btnGo.UseVisualStyleBackColor = false;
			this.btnGo.Click +=  OnBtnGoClick ;
			// 
			// linkLabel1
			// 
			this.linkLabel1.Anchor =   AnchorStyles.Bottom  |  AnchorStyles.Right ;
			this.linkLabel1.AutoSize = true;
			this.linkLabel1.Location = new Point(434, 679);
			this.linkLabel1.Margin = new Padding(5, 0, 5, 0);
			this.linkLabel1.Name = "linkLabel1";
			this.linkLabel1.Size = new Size(279, 25);
			this.linkLabel1.TabIndex = 18;
			this.linkLabel1.TabStop = true;
			this.linkLabel1.Text = "Click here to open Prism Launcher";
			this.linkLabel1.Click +=  OnOpenPrismClick ;
			// 
			// appToolTip
			// 
			this.appToolTip.AutomaticDelay = 100;
			this.appToolTip.AutoPopDelay = 10000;
			this.appToolTip.InitialDelay = 100;
			this.appToolTip.ReshowDelay = 100;
			// 
			// btnNonVR
			// 
			this.btnNonVR.BackColor = Color.DodgerBlue;
			this.btnNonVR.Dock = DockStyle.Fill;
			this.btnNonVR.FlatAppearance.BorderSize = 0;
			this.btnNonVR.FlatAppearance.MouseDownBackColor = Color.DodgerBlue;
			this.btnNonVR.FlatAppearance.MouseOverBackColor = Color.FromArgb(  72,   175,   255);
			this.btnNonVR.FlatStyle = FlatStyle.Flat;
			this.btnNonVR.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Regular, GraphicsUnit.Point,  0);
			this.btnNonVR.ForeColor = Color.White;
			this.btnNonVR.Location = new Point(5, 6);
			this.btnNonVR.Margin = new Padding(5, 6, 5, 6);
			this.btnNonVR.Name = "btnNonVR";
			this.btnNonVR.Size = new Size(333, 50);
			this.btnNonVR.TabIndex = 2;
			this.btnNonVR.Text = "Non-VR";
			this.appToolTip.SetToolTip(this.btnNonVR, "If you don't have a VR headset, or don't wish to play Minecraft in VR,\r\nuse this mode. You will be able to see players who are playing in VR,\r\nbut don't have to play in VR yourself.");
			this.btnNonVR.UseVisualStyleBackColor = false;
			// 
			// btnVR
			// 
			this.btnVR.BackColor = SystemColors.Control;
			this.btnVR.Dock = DockStyle.Fill;
			this.btnVR.FlatAppearance.BorderSize = 0;
			this.btnVR.FlatAppearance.MouseDownBackColor = Color.DodgerBlue;
			this.btnVR.FlatAppearance.MouseOverBackColor = Color.FromArgb(  72,   175,   255);
			this.btnVR.FlatStyle = FlatStyle.Flat;
			this.btnVR.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Regular, GraphicsUnit.Point,  0);
			this.btnVR.Location = new Point(348, 6);
			this.btnVR.Margin = new Padding(5, 6, 5, 6);
			this.btnVR.Name = "btnVR";
			this.btnVR.Size = new Size(334, 50);
			this.btnVR.TabIndex = 1;
			this.btnVR.Text = "VR";
			this.appToolTip.SetToolTip(this.btnVR, resources.GetString("btnVR.ToolTip"));
			this.btnVR.UseVisualStyleBackColor = false;
			// 
			// panelVR
			// 
			this.panelVR.Controls.Add(this.lbVrMode);
			this.panelVR.Controls.Add(this.vrModeLayoutPanel);
			this.panelVR.Location = new Point(20, 398);
			this.panelVR.Margin = new Padding(5, 6, 5, 6);
			this.panelVR.Name = "panelVR";
			this.panelVR.Size = new Size(687, 92);
			this.panelVR.TabIndex = 19;
			// 
			// lbVrMode
			// 
			this.lbVrMode.AutoSize = true;
			this.lbVrMode.Location = new Point(-5, 0);
			this.lbVrMode.Margin = new Padding(5, 0, 5, 0);
			this.lbVrMode.Name = "lbVrMode";
			this.lbVrMode.Size = new Size(100, 25);
			this.lbVrMode.TabIndex = 19;
			this.lbVrMode.Text = "Play Mode:";
			// 
			// vrModeLayoutPanel
			// 
			this.vrModeLayoutPanel.Anchor =    AnchorStyles.Top  |  AnchorStyles.Left   |  AnchorStyles.Right ;
			this.vrModeLayoutPanel.ColumnCount = 2;
			this.vrModeLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
			this.vrModeLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
			this.vrModeLayoutPanel.Controls.Add(this.btnNonVR, 0, 0);
			this.vrModeLayoutPanel.Controls.Add(this.btnVR, 1, 0);
			this.vrModeLayoutPanel.Location = new Point(0, 31);
			this.vrModeLayoutPanel.Margin = new Padding(5, 6, 5, 6);
			this.vrModeLayoutPanel.Name = "vrModeLayoutPanel";
			this.vrModeLayoutPanel.RowCount = 1;
			this.vrModeLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			this.vrModeLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
			this.vrModeLayoutPanel.Size = new Size(687, 62);
			this.vrModeLayoutPanel.TabIndex = 18;
			// 
			// MainForm
			// 
			AutoScaleDimensions = new SizeF(10F, 25F);
			AutoScaleMode = AutoScaleMode.Font;
			BackColor = Color.White;
			ClientSize = new Size(727, 713);
			Controls.Add(this.panelVR);
			Controls.Add(this.linkLabel1);
			Controls.Add(this.buttonLayoutPanel);
			Controls.Add(this.lbInstanceStatus);
			Controls.Add(this.rbInstanceNew);
			Controls.Add(this.rbInstanceExisting);
			Controls.Add(this.label3);
			Controls.Add(this.txtNewInstanceName);
			Controls.Add(this.lbInstancesFolder);
			Controls.Add(this.cmbInstance);
			Controls.Add(this.lbInstancesFolderStatic);
			Controls.Add(this.btnBrowsePrism);
			Controls.Add(this.txtPrismPath);
			FormBorderStyle = FormBorderStyle.FixedDialog;
			Icon = (Icon) resources.GetObject("$this.Icon");
			Margin = new Padding(5, 6, 5, 6);
			MaximizeBox = false;
			MinimumSize = new Size(535, 583);
			Name = "MainForm";
			StartPosition = FormStartPosition.CenterScreen;
			Text = "Minecraft Mod Installer";
			Load +=  OnFormLoad ;
			this.buttonLayoutPanel.ResumeLayout(false);
			this.panelVR.ResumeLayout(false);
			this.panelVR.PerformLayout();
			this.vrModeLayoutPanel.ResumeLayout(false);
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private System.Windows.Forms.Label lbInstancesFolder;
		private System.Windows.Forms.Label lbInstancesFolderStatic;
		private System.Windows.Forms.Button btnBrowsePrism;
		private System.Windows.Forms.TextBox txtPrismPath;
		private System.Windows.Forms.ComboBox cmbInstance;
		private System.Windows.Forms.TextBox txtNewInstanceName;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.RadioButton rbInstanceExisting;
		private System.Windows.Forms.RadioButton rbInstanceNew;
		private System.Windows.Forms.Label lbInstanceStatus;
		private System.Windows.Forms.TableLayoutPanel buttonLayoutPanel;
		private System.Windows.Forms.Button btnRebuild;
		private System.Windows.Forms.Button btnGo;
		private System.Windows.Forms.LinkLabel linkLabel1;
		private System.Windows.Forms.ToolTip appToolTip;
		private System.Windows.Forms.Panel panelVR;
		private System.Windows.Forms.Label lbVrMode;
		private System.Windows.Forms.TableLayoutPanel vrModeLayoutPanel;
		private System.Windows.Forms.Button btnNonVR;
		private System.Windows.Forms.Button btnVR;
	}
}

