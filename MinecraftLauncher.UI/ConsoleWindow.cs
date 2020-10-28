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
		private static readonly Guid SaveFileGuid = Guid.Parse( "f9422b8c-b94f-41be-bef4-1104450086ce" );

		private readonly Process       process;
		private readonly Mutex         bufferMutex;
		private readonly StringBuilder bufferBuilder;

		public ConsoleWindow( Process minecraftProcess )
		{
			this.process       = minecraftProcess;
			this.bufferMutex   = new Mutex();
			this.bufferBuilder = new StringBuilder();

			this.InitializeComponent();
		}

		private void ConsoleWindow_Load( object sender, EventArgs e )
		{
			this.process.ErrorDataReceived  += this.OnErrorDataReceived;
			this.process.OutputDataReceived += this.OnOutputDataReceived;
			this.process.Exited             += this.OnProcessExited;

			this.process.BeginErrorReadLine();
			this.process.BeginOutputReadLine();
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
			var result = MessageBox.Show( this,
										  "Are you sure you want to kill the Minecraft process? " +
										  "This may cause world corruption in Single Player worlds.",
										  "Confirm Killing Minecraft",
										  MessageBoxButtons.YesNo,
										  MessageBoxIcon.Warning );

			if ( result == DialogResult.Yes )
			{
				this.process.Kill();
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
			this.AppendTextToBuffer( $"\r\n\r\nProcess exited with exit code {this.process.ExitCode}\r\n\r\n\r\n\r\n\r\n\r\n" );
			this.buttonKillMinecraft.Enabled = false;
		} );

		private void ConsoleWindow_FormClosing( object sender, FormClosingEventArgs e )
		{
			if ( !this.process.HasExited && e.CloseReason == CloseReason.UserClosing )
			{
				if ( this.ConfirmKillMinecraft() )
				{
					this.process.ErrorDataReceived  -= this.OnErrorDataReceived;
					this.process.OutputDataReceived -= this.OnOutputDataReceived;
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

		private void OnButtonKillMinecraft_Click( object sender, EventArgs e )
		{
			this.ConfirmKillMinecraft();
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