namespace MinecraftLauncher
{
	partial class ConsoleWindow
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConsoleWindow));
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.richTextBoxLog = new System.Windows.Forms.RichTextBox();
			this.panel1 = new System.Windows.Forms.Panel();
			this.checkBoxAutoScroll = new System.Windows.Forms.CheckBox();
			this.btnClearConsole = new System.Windows.Forms.Button();
			this.buttonCopyToClipboard = new System.Windows.Forms.Button();
			this.buttonSaveToFile = new System.Windows.Forms.Button();
			this.buttonKillLaunchMinecraft = new System.Windows.Forms.Button();
			this.timerFlushBuffer = new System.Windows.Forms.Timer(this.components);
			this.checkBoxAutoClear = new System.Windows.Forms.CheckBox();
			this.tableLayoutPanel1.SuspendLayout();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.ColumnCount = 1;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Controls.Add(this.richTextBoxLog, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 1);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 2;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(1058, 604);
			this.tableLayoutPanel1.TabIndex = 0;
			// 
			// richTextBoxLog
			// 
			this.richTextBoxLog.BackColor = System.Drawing.SystemColors.Window;
			this.richTextBoxLog.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.richTextBoxLog.Dock = System.Windows.Forms.DockStyle.Fill;
			this.richTextBoxLog.Font = new System.Drawing.Font("Consolas", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.richTextBoxLog.Location = new System.Drawing.Point(3, 3);
			this.richTextBoxLog.Name = "richTextBoxLog";
			this.richTextBoxLog.ReadOnly = true;
			this.richTextBoxLog.Size = new System.Drawing.Size(1052, 550);
			this.richTextBoxLog.TabIndex = 0;
			this.richTextBoxLog.Text = "";
			this.richTextBoxLog.WordWrap = false;
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.checkBoxAutoClear);
			this.panel1.Controls.Add(this.checkBoxAutoScroll);
			this.panel1.Controls.Add(this.btnClearConsole);
			this.panel1.Controls.Add(this.buttonCopyToClipboard);
			this.panel1.Controls.Add(this.buttonSaveToFile);
			this.panel1.Controls.Add(this.buttonKillLaunchMinecraft);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel1.Location = new System.Drawing.Point(0, 556);
			this.panel1.Margin = new System.Windows.Forms.Padding(0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(1058, 48);
			this.panel1.TabIndex = 1;
			// 
			// checkBoxAutoScroll
			// 
			this.checkBoxAutoScroll.AutoSize = true;
			this.checkBoxAutoScroll.Checked = true;
			this.checkBoxAutoScroll.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxAutoScroll.Location = new System.Drawing.Point(12, 17);
			this.checkBoxAutoScroll.Name = "checkBoxAutoScroll";
			this.checkBoxAutoScroll.Size = new System.Drawing.Size(115, 17);
			this.checkBoxAutoScroll.TabIndex = 1;
			this.checkBoxAutoScroll.Text = "Auto-scroll console";
			this.checkBoxAutoScroll.UseVisualStyleBackColor = true;
			// 
			// btnClearConsole
			// 
			this.btnClearConsole.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnClearConsole.Location = new System.Drawing.Point(628, 12);
			this.btnClearConsole.Name = "btnClearConsole";
			this.btnClearConsole.Size = new System.Drawing.Size(100, 24);
			this.btnClearConsole.TabIndex = 0;
			this.btnClearConsole.Text = "Clear Console";
			this.btnClearConsole.UseVisualStyleBackColor = true;
			this.btnClearConsole.Click += new System.EventHandler(this.OnBtnClearConsoleClick);
			// 
			// buttonCopyToClipboard
			// 
			this.buttonCopyToClipboard.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCopyToClipboard.Location = new System.Drawing.Point(734, 12);
			this.buttonCopyToClipboard.Name = "buttonCopyToClipboard";
			this.buttonCopyToClipboard.Size = new System.Drawing.Size(100, 24);
			this.buttonCopyToClipboard.TabIndex = 0;
			this.buttonCopyToClipboard.Text = "Copy to Clipboard";
			this.buttonCopyToClipboard.UseVisualStyleBackColor = true;
			this.buttonCopyToClipboard.Click += new System.EventHandler(this.OnButtonCopyToClipboard_Click);
			// 
			// buttonSaveToFile
			// 
			this.buttonSaveToFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonSaveToFile.Location = new System.Drawing.Point(840, 12);
			this.buttonSaveToFile.Name = "buttonSaveToFile";
			this.buttonSaveToFile.Size = new System.Drawing.Size(100, 24);
			this.buttonSaveToFile.TabIndex = 0;
			this.buttonSaveToFile.Text = "Save To File...";
			this.buttonSaveToFile.UseVisualStyleBackColor = true;
			this.buttonSaveToFile.Click += new System.EventHandler(this.OnButtonSaveToFile_Click);
			// 
			// buttonKillLaunchMinecraft
			// 
			this.buttonKillLaunchMinecraft.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonKillLaunchMinecraft.Location = new System.Drawing.Point(946, 12);
			this.buttonKillLaunchMinecraft.Name = "buttonKillLaunchMinecraft";
			this.buttonKillLaunchMinecraft.Size = new System.Drawing.Size(100, 24);
			this.buttonKillLaunchMinecraft.TabIndex = 0;
			this.buttonKillLaunchMinecraft.Text = "Kill Minecraft";
			this.buttonKillLaunchMinecraft.UseVisualStyleBackColor = true;
			this.buttonKillLaunchMinecraft.Click += new System.EventHandler(this.OnButtonKillMinecraft_Click);
			// 
			// timerFlushBuffer
			// 
			this.timerFlushBuffer.Enabled = true;
			this.timerFlushBuffer.Tick += new System.EventHandler(this.OnTimerFlushBuffer_Tick);
			// 
			// checkBoxAutoClear
			// 
			this.checkBoxAutoClear.AutoSize = true;
			this.checkBoxAutoClear.Checked = true;
			this.checkBoxAutoClear.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxAutoClear.Location = new System.Drawing.Point(133, 17);
			this.checkBoxAutoClear.Name = "checkBoxAutoClear";
			this.checkBoxAutoClear.Size = new System.Drawing.Size(114, 17);
			this.checkBoxAutoClear.TabIndex = 1;
			this.checkBoxAutoClear.Text = "Auto-clear console";
			this.checkBoxAutoClear.UseVisualStyleBackColor = true;
			// 
			// ConsoleWindow
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1058, 604);
			this.Controls.Add(this.tableLayoutPanel1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "ConsoleWindow";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Minecraft Console";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ConsoleWindow_FormClosing);
			this.tableLayoutPanel1.ResumeLayout(false);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.RichTextBox richTextBoxLog;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.CheckBox checkBoxAutoScroll;
		private System.Windows.Forms.Button buttonKillLaunchMinecraft;
		private System.Windows.Forms.Button buttonSaveToFile;
		private System.Windows.Forms.Button buttonCopyToClipboard;
		private System.Windows.Forms.Timer timerFlushBuffer;
		private System.Windows.Forms.Button btnClearConsole;
		private System.Windows.Forms.CheckBox checkBoxAutoClear;
	}
}