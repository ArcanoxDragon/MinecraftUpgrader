using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;
using MinecraftLauncher.Extensions;
using MinecraftLauncher.Utility;

namespace MinecraftLauncher
{
	public partial class ConsoleWindow : Form
	{
		private static readonly Guid SaveFileGuid = Guid.Parse( "f9422b8c-b94f-41be-bef4-1104450086ce" );

		private readonly Process process;

		public ConsoleWindow( Process minecraftProcess )
		{
			this.process = minecraftProcess;

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

		private void AppendText( string text )
		{
			var (stripped, foreground, background) = AnsiUtility.ParseFirstFgAnsiColor( text );

			this.richTextBoxLog.AppendText( stripped, foreground, background );
			this.AutoScrollLog();
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

			this.AppendText( e.Data + "\r\n" );
		} );

		private void OnOutputDataReceived( object sender, DataReceivedEventArgs e ) => this.TryBeginInvoke( () => {
			if ( string.IsNullOrEmpty( e.Data ) )
				return;

			this.AppendText( e.Data + "\r\n" );
		} );

		private void OnProcessExited( object sender, EventArgs e ) => this.TryBeginInvoke( () => {
			this.AppendText( $"\r\n\r\nProcess exited with exit code {this.process.ExitCode}\r\n\r\n\r\n\r\n\r\n\r\n" );
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

		private void buttonKillMinecraft_Click( object sender, EventArgs e )
		{
			this.ConfirmKillMinecraft();
		}

		private void buttonSaveToFile_Click( object sender, EventArgs e )
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

		private void buttonCopyToClipboard_Click( object sender, EventArgs e )
		{
			Clipboard.SetText( this.richTextBoxLog.Text );
		}
	}
}