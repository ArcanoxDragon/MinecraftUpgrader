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
			this.lbInstancesFolder = new System.Windows.Forms.Label();
			this.lbInstancesFolderStatic = new System.Windows.Forms.Label();
			this.btnBrowseMmc = new System.Windows.Forms.Button();
			this.txtMmcPath = new System.Windows.Forms.TextBox();
			this.cmbInstance = new System.Windows.Forms.ComboBox();
			this.txtNewInstanceName = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.rbInstanceExisting = new System.Windows.Forms.RadioButton();
			this.rbInstanceNew = new System.Windows.Forms.RadioButton();
			this.lbInstanceStatus = new System.Windows.Forms.Label();
			this.buttonLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.btnRebuild = new System.Windows.Forms.Button();
			this.btnGo = new System.Windows.Forms.Button();
			this.buttonLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// lbInstancesFolder
			// 
			this.lbInstancesFolder.AutoSize = true;
			this.lbInstancesFolder.Location = new System.Drawing.Point(100, 48);
			this.lbInstancesFolder.Name = "lbInstancesFolder";
			this.lbInstancesFolder.Size = new System.Drawing.Size(57, 13);
			this.lbInstancesFolder.TabIndex = 7;
			this.lbInstancesFolder.Text = "Not Found";
			// 
			// lbInstancesFolderStatic
			// 
			this.lbInstancesFolderStatic.AutoSize = true;
			this.lbInstancesFolderStatic.Location = new System.Drawing.Point(9, 48);
			this.lbInstancesFolderStatic.Name = "lbInstancesFolderStatic";
			this.lbInstancesFolderStatic.Size = new System.Drawing.Size(85, 13);
			this.lbInstancesFolderStatic.TabIndex = 8;
			this.lbInstancesFolderStatic.Text = "Instances folder:";
			// 
			// btnBrowseMmc
			// 
			this.btnBrowseMmc.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnBrowseMmc.Location = new System.Drawing.Point(391, 23);
			this.btnBrowseMmc.Name = "btnBrowseMmc";
			this.btnBrowseMmc.Size = new System.Drawing.Size(33, 23);
			this.btnBrowseMmc.TabIndex = 6;
			this.btnBrowseMmc.Text = "...";
			this.btnBrowseMmc.UseVisualStyleBackColor = true;
			// 
			// txtMmcPath
			// 
			this.txtMmcPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.txtMmcPath.Location = new System.Drawing.Point(12, 25);
			this.txtMmcPath.Name = "txtMmcPath";
			this.txtMmcPath.ReadOnly = true;
			this.txtMmcPath.Size = new System.Drawing.Size(373, 20);
			this.txtMmcPath.TabIndex = 5;
			// 
			// cmbInstance
			// 
			this.cmbInstance.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.cmbInstance.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbInstance.Enabled = false;
			this.cmbInstance.FormattingEnabled = true;
			this.cmbInstance.Location = new System.Drawing.Point(12, 102);
			this.cmbInstance.Name = "cmbInstance";
			this.cmbInstance.Size = new System.Drawing.Size(412, 21);
			this.cmbInstance.TabIndex = 3;
			this.cmbInstance.SelectedIndexChanged += new System.EventHandler(this.OnCbInstanceChanged);
			// 
			// txtNewInstanceName
			// 
			this.txtNewInstanceName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.txtNewInstanceName.Location = new System.Drawing.Point(12, 181);
			this.txtNewInstanceName.Name = "txtNewInstanceName";
			this.txtNewInstanceName.Size = new System.Drawing.Size(412, 20);
			this.txtNewInstanceName.TabIndex = 11;
			this.txtNewInstanceName.TextChanged += new System.EventHandler(this.OnTxtNewInstanceNameChanged);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(12, 9);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(92, 13);
			this.label3.TabIndex = 12;
			this.label3.Text = "MultiMC Location:";
			// 
			// rbInstanceExisting
			// 
			this.rbInstanceExisting.AutoSize = true;
			this.rbInstanceExisting.Checked = true;
			this.rbInstanceExisting.Location = new System.Drawing.Point(12, 79);
			this.rbInstanceExisting.Name = "rbInstanceExisting";
			this.rbInstanceExisting.Size = new System.Drawing.Size(145, 17);
			this.rbInstanceExisting.TabIndex = 13;
			this.rbInstanceExisting.TabStop = true;
			this.rbInstanceExisting.Text = "Pick an existing instance:";
			this.rbInstanceExisting.UseVisualStyleBackColor = true;
			this.rbInstanceExisting.CheckedChanged += new System.EventHandler(this.OnRbInstanceChecked);
			// 
			// rbInstanceNew
			// 
			this.rbInstanceNew.AutoSize = true;
			this.rbInstanceNew.Location = new System.Drawing.Point(12, 158);
			this.rbInstanceNew.Name = "rbInstanceNew";
			this.rbInstanceNew.Size = new System.Drawing.Size(160, 17);
			this.rbInstanceNew.TabIndex = 14;
			this.rbInstanceNew.Text = "Or create a new one named:";
			this.rbInstanceNew.UseVisualStyleBackColor = true;
			this.rbInstanceNew.CheckedChanged += new System.EventHandler(this.OnRbInstanceChecked);
			// 
			// lbInstanceStatus
			// 
			this.lbInstanceStatus.AutoSize = true;
			this.lbInstanceStatus.Location = new System.Drawing.Point(12, 126);
			this.lbInstanceStatus.Name = "lbInstanceStatus";
			this.lbInstanceStatus.Size = new System.Drawing.Size(81, 13);
			this.lbInstanceStatus.TabIndex = 15;
			this.lbInstanceStatus.Text = "Instance Status";
			// 
			// buttonLayoutPanel
			// 
			this.buttonLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonLayoutPanel.ColumnCount = 2;
			this.buttonLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.buttonLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.buttonLayoutPanel.Controls.Add(this.btnRebuild, 0, 0);
			this.buttonLayoutPanel.Controls.Add(this.btnGo, 1, 0);
			this.buttonLayoutPanel.Location = new System.Drawing.Point(15, 224);
			this.buttonLayoutPanel.Name = "buttonLayoutPanel";
			this.buttonLayoutPanel.RowCount = 1;
			this.buttonLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.buttonLayoutPanel.Size = new System.Drawing.Size(409, 60);
			this.buttonLayoutPanel.TabIndex = 16;
			// 
			// btnRebuild
			// 
			this.btnRebuild.BackColor = System.Drawing.SystemColors.Control;
			this.btnRebuild.Dock = System.Windows.Forms.DockStyle.Fill;
			this.btnRebuild.Enabled = false;
			this.btnRebuild.FlatAppearance.BorderSize = 0;
			this.btnRebuild.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
			this.btnRebuild.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(72)))), ((int)(((byte)(175)))), ((int)(((byte)(255)))));
			this.btnRebuild.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnRebuild.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.btnRebuild.ForeColor = System.Drawing.SystemColors.ControlText;
			this.btnRebuild.Location = new System.Drawing.Point(3, 3);
			this.btnRebuild.Name = "btnRebuild";
			this.btnRebuild.Size = new System.Drawing.Size(198, 54);
			this.btnRebuild.TabIndex = 2;
			this.btnRebuild.Text = "Rebuild";
			this.btnRebuild.UseVisualStyleBackColor = false;
			this.btnRebuild.Click += new System.EventHandler(this.OnBtnRebuildClick);
			// 
			// btnGo
			// 
			this.btnGo.BackColor = System.Drawing.Color.DodgerBlue;
			this.btnGo.Dock = System.Windows.Forms.DockStyle.Fill;
			this.btnGo.Enabled = false;
			this.btnGo.FlatAppearance.BorderSize = 0;
			this.btnGo.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
			this.btnGo.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(72)))), ((int)(((byte)(175)))), ((int)(((byte)(255)))));
			this.btnGo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnGo.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.btnGo.ForeColor = System.Drawing.Color.White;
			this.btnGo.Location = new System.Drawing.Point(207, 3);
			this.btnGo.Name = "btnGo";
			this.btnGo.Size = new System.Drawing.Size(199, 54);
			this.btnGo.TabIndex = 1;
			this.btnGo.Text = "Go!";
			this.btnGo.UseVisualStyleBackColor = false;
			this.btnGo.Click += new System.EventHandler(this.OnBtnGoClick);
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.White;
			this.ClientSize = new System.Drawing.Size(436, 296);
			this.Controls.Add(this.buttonLayoutPanel);
			this.Controls.Add(this.lbInstanceStatus);
			this.Controls.Add(this.rbInstanceNew);
			this.Controls.Add(this.rbInstanceExisting);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.txtNewInstanceName);
			this.Controls.Add(this.lbInstancesFolder);
			this.Controls.Add(this.cmbInstance);
			this.Controls.Add(this.lbInstancesFolderStatic);
			this.Controls.Add(this.btnBrowseMmc);
			this.Controls.Add(this.txtMmcPath);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MinimumSize = new System.Drawing.Size(330, 330);
			this.Name = "MainForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Minecraft Upgrader";
			this.Load += new System.EventHandler(this.OnFormLoad);
			this.buttonLayoutPanel.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label lbInstancesFolder;
		private System.Windows.Forms.Label lbInstancesFolderStatic;
		private System.Windows.Forms.Button btnBrowseMmc;
		private System.Windows.Forms.TextBox txtMmcPath;
		private System.Windows.Forms.ComboBox cmbInstance;
		private System.Windows.Forms.TextBox txtNewInstanceName;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.RadioButton rbInstanceExisting;
		private System.Windows.Forms.RadioButton rbInstanceNew;
		private System.Windows.Forms.Label lbInstanceStatus;
		private System.Windows.Forms.TableLayoutPanel buttonLayoutPanel;
		private System.Windows.Forms.Button btnRebuild;
		private System.Windows.Forms.Button btnGo;
	}
}

