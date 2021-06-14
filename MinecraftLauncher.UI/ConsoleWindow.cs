using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;
using MinecraftLauncher.Extensions;
using MinecraftLauncher.Utility;

namespace MinecraftLauncher
{
	public partial class ConsoleWindow : Form
	{
		private const int MaxLogLength = 64 * 1000 * 1000; // 64 mb

		private static readonly Guid SaveFileGuid = Guid.Parse("f9422b8c-b94f-41be-bef4-1104450086ce");

		private readonly McLauncher    launcher;
		private readonly Mutex         bufferMutex;
		private readonly StringBuilder bufferBuilder;

		public ConsoleWindow(McLauncher launcher)
		{
			this.launcher      = launcher;
			this.bufferMutex   = new Mutex();
			this.bufferBuilder = new StringBuilder();

			InitializeComponent();

			if (this.launcher.CurrentProcess?.HasExited == false)
				HookProcess();

			this.launcher.Launched += LauncherLaunched;
		}

		private void HookProcess()
		{
			this.launcher.CurrentProcess.ErrorDataReceived  += OnErrorDataReceived;
			this.launcher.CurrentProcess.OutputDataReceived += OnOutputDataReceived;
			this.launcher.CurrentProcess.Exited             += OnProcessExited;

			this.launcher.CurrentProcess.BeginErrorReadLine();
			this.launcher.CurrentProcess.BeginOutputReadLine();
		}

		private void AppendTextToConsole(string text)
		{
			var (stripped, foreground, background) = AnsiUtility.ParseFirstFgAnsiColor(text);

			if (this.checkBoxAutoClear.Checked)
			{
				var futureLength = this.richTextBoxLog.TextLength + stripped.Length;

				if (futureLength > MaxLogLength)
					this.richTextBoxLog.Clear();
			}

			this.richTextBoxLog.AppendText(stripped, foreground, background);
			AutoScrollLog();
		}

		private void AppendTextToBuffer(string text)
		{
			lock (this.bufferMutex)
			{
				try
				{
					this.bufferMutex.WaitOne();
					this.bufferBuilder.Append(text);
				}
				finally
				{
					this.bufferMutex.ReleaseMutex();
				}
			}
		}

		private void AutoScrollLog()
		{
			if (!this.checkBoxAutoScroll.Checked)
				return;

			this.richTextBoxLog.SelectionStart  = this.richTextBoxLog.TextLength;
			this.richTextBoxLog.SelectionLength = 0;
			this.richTextBoxLog.ScrollToCaret();
		}

		private bool ConfirmKillMinecraft()
		{
			if (this.launcher.CurrentProcess == null)
				return true;

			var result = MessageBox.Show(this,
										 "Are you sure you want to kill the Minecraft process? " +
										 "This may cause world corruption in Single Player worlds.",
										 "Confirm Killing Minecraft",
										 MessageBoxButtons.YesNo,
										 MessageBoxIcon.Warning);

			if (result == DialogResult.Yes)
			{
				this.launcher.CurrentProcess.Kill();
				return true;
			}

			return false;
		}

		private void OnErrorDataReceived(object sender, DataReceivedEventArgs e) => this.TryBeginInvoke(() => {
			if (string.IsNullOrEmpty(e.Data))
				return;

			AppendTextToBuffer(e.Data + "\r\n");
		});

		private void OnOutputDataReceived(object sender, DataReceivedEventArgs e) => this.TryBeginInvoke(() => {
			if (string.IsNullOrEmpty(e.Data))
				return;

			AppendTextToBuffer(e.Data + "\r\n");
		});

		private void OnProcessExited(object sender, EventArgs e) => this.TryBeginInvoke(() => {
			if (sender is not Process process) return;

			AppendTextToBuffer($"\r\n\r\nProcess exited with exit code {process.ExitCode}\r\n\r\n\r\n\r\n\r\n\r\n");
			this.buttonKillLaunchMinecraft.Text = "Launch Minecraft";
		});

		private void LauncherLaunched(object sender, EventArgs e) => this.TryBeginInvoke(() => {
			HookProcess();
			this.richTextBoxLog.Clear();
			this.buttonKillLaunchMinecraft.Text = "Kill Minecraft";
		});

		private void ConsoleWindow_FormClosing(object sender, FormClosingEventArgs e)
		{
			var process = this.launcher.CurrentProcess;

			if (process?.HasExited == false && e.CloseReason == CloseReason.UserClosing)
			{
				if (ConfirmKillMinecraft())
				{
					process.ErrorDataReceived  -= OnErrorDataReceived;
					process.OutputDataReceived -= OnOutputDataReceived;
				}
				else
				{
					e.Cancel = true;
				}
			}
		}

		private void OnBtnClearConsoleClick(object sender, EventArgs e)
		{
			lock (this.bufferMutex)
			{
				try
				{
					this.bufferMutex.WaitOne();
					this.bufferBuilder.Clear();
					this.richTextBoxLog.Clear();
				}
				finally
				{
					this.bufferMutex.ReleaseMutex();
				}
			}
		}

		private void OnButtonCopyToClipboard_Click(object sender, EventArgs e)
		{
			Clipboard.SetText(this.richTextBoxLog.Text);
		}

		private void OnButtonSaveToFile_Click(object sender, EventArgs e)
		{
			var saveFileDialog = new CommonSaveFileDialog {
				Filters = {
					new CommonFileDialogFilter("Text Files (*.txt)", "*.txt"),
					new CommonFileDialogFilter("All Files", "*"),
				},
				AddToMostRecentlyUsedList    = true,
				AlwaysAppendDefaultExtension = true,
				EnsurePathExists             = true,
				CookieIdentifier             = SaveFileGuid,
			};

			var saveResult = saveFileDialog.ShowDialog(Handle);

			if (saveResult == CommonFileDialogResult.Ok)
			{
				File.WriteAllText(saveFileDialog.FileName, this.richTextBoxLog.Text);
			}
		}

		private async void OnButtonKillMinecraft_Click(object sender, EventArgs e)
		{
			if (this.launcher.CurrentProcess == null)
			{
				try
				{
					Enabled = false;
					await this.launcher.StartMinecraftAsync(this);
				}
				finally
				{
					Enabled = true;
				}
			}
			else
			{
				ConfirmKillMinecraft();
			}
		}

		private void OnTimerFlushBuffer_Tick(object sender, EventArgs e)
		{
			lock (this.bufferMutex)
			{
				if (this.bufferBuilder.Length > 0)
				{
					try
					{
						this.bufferMutex.WaitOne();

						AppendTextToConsole(this.bufferBuilder.ToString());
						this.bufferBuilder.Clear();
					}
					finally
					{
						this.bufferMutex.ReleaseMutex();
					}
				}
			}
		}
	}
}