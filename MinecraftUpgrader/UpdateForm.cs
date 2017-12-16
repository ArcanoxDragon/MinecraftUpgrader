using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace MinecraftUpgrader
{
	public partial class UpdateForm : Form
	{
		private const string RemoteFileUrl = "https://mc.angeldragons.com/MinecraftDADInstaller.exe";

		public UpdateForm()
		{
			this.InitializeComponent();
		}

		private async void UpdateForm_Load( object sender, EventArgs args )
		{
			byte[] localHash, remoteHash;
			var    md5                = MD5.Create();
			var    curProcess         = Process.GetCurrentProcess();
			var    processFilePath    = curProcess.MainModule.FileName;
			var    processFileOldPath = $"{processFilePath}.old";
			var    otherProcesses     = Process.GetProcessesByName( curProcess.ProcessName )
											   .Where( p => p.Id != curProcess.Id );

			foreach ( var process in otherProcesses )
			{
				process.WaitForExit( 3000 );
				process.Kill();
			}

			if ( File.Exists( processFileOldPath ) )
				File.Delete( processFileOldPath );

			using ( var fs = new FileStream( processFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite ) )
			{
				localHash = md5.ComputeHash( fs );
			}

			using ( var web = new WebClient() )
			{
				using ( var remoteStream = await web.OpenReadTaskAsync( RemoteFileUrl ) )
				{
					remoteHash = md5.ComputeHash( remoteStream );
				}

				if ( localHash.SequenceEqual( remoteHash ) )
				{
					this.Hide();
					new CheckJavaForm().ShowDialog();
					this.Close();
					return;
				}

				this.lbStatus.Text    = "Updating program...";
				this.pbDownload.Style = ProgressBarStyle.Continuous;

				web.DownloadProgressChanged += ( o, e ) => this.Invoke( new Action( () => {
					var dlSize                 = new FileSize( e.BytesReceived );
					var totalSize              = new FileSize( e.TotalBytesToReceive );

					this.lbProgress.Text  = $"Downloading updates... {dlSize} / {totalSize} ({e.ProgressPercentage}%)";
					this.pbDownload.Value = e.ProgressPercentage;
				} ) );

				var updateFilePath = Path.Combine( Path.GetTempPath(), "MinecraftDADInstaller.exe" );

				if ( File.Exists( updateFilePath ) )
					File.Delete( updateFilePath );

				await web.DownloadFileTaskAsync( RemoteFileUrl, updateFilePath );

				this.lbStatus.Text    = "Installing updates...";
				this.pbDownload.Style = ProgressBarStyle.Marquee;

				File.Move( processFilePath, processFileOldPath );
				File.Move( updateFilePath,  processFilePath );

				byte[] newLocalHash;

				using ( var fs = new FileStream( processFilePath, FileMode.Open, FileAccess.ReadWrite ) )
					newLocalHash  = md5.ComputeHash( fs );

				if ( !newLocalHash.SequenceEqual( remoteHash ) )
				{
					MessageBox.Show( this,
									 "Checksum mismatch! The update file is invalid. Update failed.",
									 "Update Failed",
									 MessageBoxButtons.OK,
									 MessageBoxIcon.Error );

					File.Delete( processFilePath );
					File.Move( processFileOldPath, processFilePath );

					this.Hide();
					new CheckJavaForm().ShowDialog();
					this.Close();
					return;
				}

				MessageBox.Show( this,
								 "Update complete! Click OK to restart the program and apply the update.",
								 "Update Complete",
								 MessageBoxButtons.OK,
								 MessageBoxIcon.Information );

				Process.Start( new ProcessStartInfo {
					FileName        = processFilePath,
					UseShellExecute = true
				} );
				Application.Exit();
			}
		}
	}
}