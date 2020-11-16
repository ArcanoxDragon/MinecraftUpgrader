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
		private readonly        McLauncher launcher;
		private static readonly Guid       SaveFileGuid = Guid.Parse( "f9422b8c-b94f-41be-bef4-1104450086ce" );

		private readonly Mutex         bufferMutex;
		private readonly StringBuilder bufferBuilder;

		public ConsoleWindow( McLauncher launcher )
		{
			this.launcher      = launcher;
			this.bufferMutex   = new Mutex();
			this.bufferBuilder = new StringBuilder();

			this.InitializeComponent();

			if ( this.launcher.CurrentProcess?.HasExited == false )
				this.HookProcess();

			this.launcher.Launched += this.LauncherLaunched;
		}

		private void HookProcess()
		{
			this.launcher.CurrentProcess.ErrorDataReceived  += this.OnErrorDataReceived;
			this.launcher.CurrentProcess.OutputDataReceived += this.OnOutputDataReceived;
			this.launcher.CurrentProcess.Exited             += this.OnProcessExited;

			this.launcher.CurrentProcess.BeginErrorReadLine();
			this.launcher.CurrentProcess.BeginOutputReadLine();
		}

		private void AppendTextToConsole( string text )
		{
			var (stripped, foreground, background) = AnsiUtility.ParseFirstFgAnsiColor( text );

			this.richTextBoxLog.AppendText( stripped, foreground, background );
			this.AutoScrollLog();
		}

		private void AppendTextToBuffer( string text )
		{
			lock ( this.bufferMutex )
			{
				try
				{
					this.bufferMutex.WaitOne();
					this.bufferBuilder.Append( text );
				}
				finally
				{
					this.bufferMutex.ReleaseMutex();
				}
			}
		}

		private void AutoScrollLog()
		{
			if ( !this.checkBoxAutoScroll.Checked )
				return;

			this.richTextBoxLog.SelectionStart  = this.richTextBoxLog.TextLength;
			this.richTextBoxLog.SelectionLength = 0;
			this.richTextBoxLog.ScrollToCaret();
		}

		private bool ConfirmKillMinecraft()
		{
			if ( this.launcher.CurrentProcess == null )
				return true;

			var result = MessageBox.Show( this,
										  "Are you sure you want to kill the Minecraft process? " +
										  "This may cause world corruption in Single Player worlds.",
										  "Confirm Killing Minecraft",
										  MessageBoxButtons.YesNo,
										  MessageBoxIcon.Warning );

			if ( result == DialogResult.Yes )
			{
				this.launcher.CurrentProcess.Kill();
				return true;
			}

			return false;
		}

		private void OnErrorDataReceived( object sender, DataReceivedEventArgs e ) => this.TryBeginInvoke( () => {
			if ( string.IsNullOrEmpty( e.Data ) )
				return;

			this.AppendTextToBuffer( e.Data + "\r\n" );
		} );

		private void OnOutputDataReceived( object sender, DataReceivedEventArgs e ) => this.TryBeginInvoke( () => {
			if ( string.IsNullOrEmpty( e.Data ) )
				return;

			this.AppendTextToBuffer( e.Data + "\r\n" );
		} );

		private void OnProcessExited( object sender, EventArgs e ) => this.TryBeginInvoke( () => {
			if ( !( sender is Process process ) ) return;

			this.AppendTextToBuffer( $"\r\n\r\nProcess exited with exit code {process.ExitCode}\r\n\r\n\r\n\r\n\r\n\r\n" );
			this.buttonKillLaunchMinecraft.Text = "Launch Minecraft";
		} );

		private void LauncherLaunched( object sender, EventArgs e ) => this.TryBeginInvoke( () => {
			this.HookProcess();
			this.richTextBoxLog.Clear();
			this.buttonKillLaunchMinecraft.Text = "Kill Minecraft";
		} );

		private void ConsoleWindow_FormClosing( object sender, FormClosingEventArgs e )
		{
			var process = this.launcher.CurrentProcess;

			if ( process?.HasExited == false && e.CloseReason == CloseReason.UserClosing )
			{
				if ( this.ConfirmKillMinecraft() )
				{
					process.ErrorDataReceived  -= this.OnErrorDataReceived;
					process.OutputDataReceived -= this.OnOutputDataReceived;
				}
				else
				{
					e.Cancel = true;
				}
			}
		}

		private void OnButtonCopyToClipboard_Click( object sender, EventArgs e )
		{
			Clipboard.SetText( this.richTextBoxLog.Text );
		}

		private void OnButtonSaveToFile_Click( object sender, EventArgs e )
		{
			var saveFileDialog = new CommonSaveFileDialog {
				Filters = {
					new CommonFileDialogFilter( "Text Files (*.txt)", "*.txt" ),
					new CommonFileDialogFilter( "All Files", "*" ),
				},
				AddToMostRecentlyUsedList    = true,
				AlwaysAppendDefaultExtension = true,
				EnsurePathExists             = true,
				CookieIdentifier             = SaveFileGuid,
			};

			var saveResult = saveFileDialog.ShowDialog( this.Handle );

			if ( saveResult == CommonFileDialogResult.Ok )
			{
				File.WriteAllText( saveFileDialog.FileName, this.richTextBoxLog.Text );
			}
		}

		private async void OnButtonKillMinecraft_Click( object sender, EventArgs e )
		{
			if ( this.launcher.CurrentProcess == null )
			{
				try
				{
					this.Enabled = false;
					await this.launcher.StartMinecraftAsync( this );
				}
				finally
				{
					this.Enabled = true;
				}
			}
			else
			{
				this.ConfirmKillMinecraft();
			}
		}

		private void OnTimerFlushBuffer_Tick( object sender, EventArgs e )
		{
			lock ( this.bufferMutex )
			{
				if ( this.bufferBuilder.Length > 0 )
				{
					try
					{
						this.bufferMutex.WaitOne();

						this.AppendTextToConsole( this.bufferBuilder.ToString() );
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