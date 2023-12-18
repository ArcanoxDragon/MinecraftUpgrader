#pragma warning disable 162
// ReSharper disable HeuristicUnreachableCode

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Humanizer;
using Microsoft.Extensions.Options;
using MinecraftUpgrader.Options;

namespace MinecraftUpgrader
{
	public partial class UpdateForm : Form
	{
		private readonly string remoteFileUrl;
		private readonly string remoteMd5Url;

		public UpdateForm( IOptions<PackBuilderOptions> options )
		{
			this.remoteFileUrl = $"{options.Value.ModPackUrl}/MinecraftInstaller.exe";
			this.remoteMd5Url  = $"{options.Value.ModPackUrl}/installercheck.php";

			this.InitializeComponent();
		}

		private async void UpdateForm_Load( object sender, EventArgs args )
		{
#if DEBUG
			const bool SkipUpdate = true;
#else
			const bool SkipUpdate = false;
#endif

			var md5                = MD5.Create();
			var curProcess         = Process.GetCurrentProcess();
			var processFilePath    = curProcess.MainModule?.FileName;
			var processFileOldPath = $"{processFilePath}.old";

			if ( string.IsNullOrEmpty( processFilePath ) )
			{
				MessageBox.Show( this,
								 "Fatal error...could not locate the running program on the file system.",
								 "Update Failed",
								 MessageBoxButtons.OK,
								 MessageBoxIcon.Error );

				return;
			}

			byte[] localHash, remoteHash;
			var otherProcesses = Process.GetProcessesByName( curProcess.ProcessName )
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
				using ( var remoteStream = await web.OpenReadTaskAsync( this.remoteMd5Url ) )
				using ( var reader = new StreamReader( remoteStream ) )
				{
					var remoteHashString = await reader.ReadToEndAsync();
					var remoteHashSplit  = Regex.Split( remoteHashString, @"([a-f0-9]{2})", RegexOptions.IgnoreCase ).Where( s => !string.IsNullOrEmpty( s ) );

					remoteHash = remoteHashSplit.Select( s => byte.Parse( s, NumberStyles.HexNumber ) ).ToArray();
				}

				// ReSharper disable once RedundantLogicalConditionalExpressionOperand
				// ReSharper disable once ConditionIsAlwaysTrueOrFalse
				if ( SkipUpdate || localHash.SequenceEqual( remoteHash ) )
				{
					this.Hide();
					new CheckJavaForm().ShowDialog();
					this.Close();
					return;
				}

				this.lbStatus.Text    = "Updating program...";
				this.pbDownload.Style = ProgressBarStyle.Continuous;

				web.DownloadProgressChanged += ( o, e ) => this.Invoke( new Action( () => {
					var dlSize    = e.BytesReceived.Bytes();
					var totalSize = e.TotalBytesToReceive.Bytes();

					this.lbProgress.Text  = $"Downloading updates... {dlSize.ToString( "0.##" )} / {totalSize.ToString( "0.##" )} ({e.ProgressPercentage}%)";
					this.pbDownload.Value = e.ProgressPercentage;
				} ) );

				var updateFilePath = Path.Combine( Path.GetTempPath(), "MinecraftInstaller.exe" );

				if ( File.Exists( updateFilePath ) )
					File.Delete( updateFilePath );

				await web.DownloadFileTaskAsync( this.remoteFileUrl, updateFilePath );

				this.lbStatus.Text    = "Installing updates...";
				this.pbDownload.Style = ProgressBarStyle.Marquee;

				File.Move( processFilePath, processFileOldPath );
				File.Move( updateFilePath, processFilePath );

				byte[] newLocalHash;

				using ( var fs = new FileStream( processFilePath, FileMode.Open, FileAccess.ReadWrite ) )
					newLocalHash = md5.ComputeHash( fs );

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