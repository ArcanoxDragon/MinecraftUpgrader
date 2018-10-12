using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using MinecraftUpgrader.MultiMC;
using MinecraftUpgrader.Zip;

namespace MinecraftUpgrader.Util
{
	public static class MultiMcInstaller
	{
		public static async Task<bool> InstallMultiMC( IWin32Window parentWindow )
		{
			var dialog = new ProgressDialog( "Downloading MultiMC" );

			try
			{
				var tempDir      = Path.GetTempPath();
				var tempFile     = Path.Combine( tempDir, "multimc.zip" );
				var cancelSource = new CancellationTokenSource();

				dialog.Cancel += ( o, args ) => {
					cancelSource.Cancel();
					dialog.Reporter.ReportProgress( -1, "Cancelling...please wait" );
				};
				dialog.Show( parentWindow );

				using ( var web = new WebClient() )
				{
					cancelSource.Token.Register( web.CancelAsync );

					web.DownloadProgressChanged += ( sender, args ) => {
						var dlSize    = new FileSize( args.BytesReceived );
						var totalSize = new FileSize( args.TotalBytesToReceive );
						// ReSharper disable once AccessToModifiedClosure
						dialog.Reporter.ReportProgress( args.BytesReceived / (double) args.TotalBytesToReceive,
														$"Downloading MultiMC... ({dlSize.ToString()} / {totalSize.ToString()})" );
					};

					await web.DownloadFileTaskAsync( Constants.Paths.MultiMcDownload, tempFile );
				}

				var mmcInstallPath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments ), "MultiMC" );

				if ( !Directory.Exists( mmcInstallPath ) )
					Directory.CreateDirectory( mmcInstallPath );

				using ( var fs = File.Open( tempFile, FileMode.Open, FileAccess.Read, FileShare.Read ) )
				using ( var zip = new ZipFile( fs ) )
				{
					dialog.Reporter.ReportProgress( -1, "Extracting MultiMC..." );

					await zip.ExtractAsync( mmcInstallPath,
											new ZipExtractOptions {
												DirectoryName           = "MultiMC",
												RecreateFolderStructure = false,
												OverwriteExisting       = true,
												CancellationToken       = cancelSource.Token,
												ProgressReporter        = dialog.Reporter
											} );
				}

				File.Delete( tempFile );

				dialog.Reporter.ReportProgress( -1, "Setting up MultiMC..." );

				var mmcConfig   = await MmcConfigReader.CreateConfig( mmcInstallPath );
				var javaPath    = Registry.GetValue( Constants.Registry.RootKey, Constants.Registry.JavaPath,    null ) as string;
				var javaVersion = Registry.GetValue( Constants.Registry.RootKey, Constants.Registry.JavaVersion, null ) as string;

				if ( javaPath != null && javaVersion != null )
				{
					mmcConfig.JavaPath    = javaPath.Replace( "java.exe", "javaw.exe" );
					mmcConfig.JavaVersion = javaVersion;

					await MmcConfigReader.UpdateConfig( mmcInstallPath, mmcConfig );
				}

				Registry.SetValue( Constants.Registry.RootKey, Constants.Registry.LastMmcPath, mmcInstallPath );

				dialog.Close();

				MessageBox.Show( parentWindow,
								 $"Successfully installed MultiMC to \"{mmcInstallPath}\"!",
								 "MultiMC Setup Complete",
								 MessageBoxButtons.OK,
								 MessageBoxIcon.Information );

				return true;
			}
			catch ( TaskCanceledException )
			{
				MessageBox.Show( parentWindow,
								 "MultiMC setup cancelled. You will need to download it yourself, pick an existing installation," +
								 "or re-run the installer and start the MultiMC setup again.",
								 "MultiMC Setup Cancelled",
								 MessageBoxButtons.OK,
								 MessageBoxIcon.Error );

				return false;
			}
			finally
			{
				dialog.Close();
			}
		}
	}
}