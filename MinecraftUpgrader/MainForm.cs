using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.VisualBasic.Devices;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using MinecraftUpgrader.MultiMC;
using MinecraftUpgrader.Upgrade;
using MinecraftUpgrader.Zip;
using Semver;

namespace MinecraftUpgrader
{
	public partial class MainForm : Form
	{
		private static readonly Guid OpenFolderMmcCookie = new Guid( "76309a12-c3b3-4954-80ac-242685715563" );

		private bool                              mmcFound;
		private MmcConfig                         config;
		private IList<(string name, string path)> instances;

		public MainForm()
		{
			this.InitializeComponent();
		}

		private new void BringToFront()
		{
			this.WindowState = FormWindowState.Minimized;
			this.Show();
			this.WindowState = FormWindowState.Normal;
		}

		private async void OnFormLoad( object sender, EventArgs e )
		{
			var lastMmcPath      = Registry.GetValue( Constants.Registry.RootKey, Constants.Registry.LastMmcPath,  null ) as string;
			var lastInstanceName = Registry.GetValue( Constants.Registry.RootKey, Constants.Registry.LastInstance, null ) as string;

			if ( string.IsNullOrEmpty( lastMmcPath ) )
			{
				Task.Run( () => this.Invoke( new Func<Task>( async () => await this.OfferSetUpMultiMc() ) ) ).ConfigureAwait( false );
			}
			else
			{
				this.txtMmcPath.Text = lastMmcPath;
				await this.RefreshMmcConfig( lastMmcPath );

				if ( !string.IsNullOrEmpty( lastInstanceName ) )
				{
					if ( this.instances?.Any( i => i.name == lastInstanceName ) == true )
					{
						var lastInstance = this.instances.First( i => i.name == lastInstanceName );

						this.rbInstanceNew.Checked     = false;
						this.rbInstanceConvert.Checked = true;
						this.cbInstance.Enabled        = true;
						this.cbInstance.SelectedIndex  = this.instances.IndexOf( lastInstance );
						this.CheckGoButton();

						Task.Run( () => this.Invoke( new Func<Task>( async () => await this.CheckPackNeedsUpdate( lastInstance ) ) ) ).ConfigureAwait( false );
					}
				}
			}
		}

		private async Task OfferSetUpMultiMc()
		{
			var choice = MessageBox.Show( this,
										  "You have not set the MultiMC path for the installer yet.\n\n" +
										  "Would you like the installer to automatically download and install MultiMC?\n\n" +
										  "If you choose \"No\", you will need to locate an existing installation of MultiMC on your PC.",
										  "MultiMC Not Found",
										  MessageBoxButtons.YesNo,
										  MessageBoxIcon.Question );

			if ( choice == DialogResult.Yes )
			{
				var dialog = new ProgressDialog( "Downloading MultiMC" );

				try
				{
					var tempDir      = Path.GetTempPath();
					var tempFile     = Path.Combine( tempDir, "multimc.zip" );
					var cancelSource = new CancellationTokenSource();

					this.Enabled  =  false;
					dialog.Cancel += ( o, args ) => {
						cancelSource.Cancel();
						dialog.Reporter.ReportProgress( -1, "Cancelling...please wait" );
					};
					dialog.Show( this );

					using ( var web = new WebClient() )
					{
						cancelSource.Token.Register( web.CancelAsync );

						web.DownloadProgressChanged += ( sender, args ) => {
							var dlSize                 = new FileSize( args.BytesReceived );
							var totalSize              = new FileSize( args.TotalBytesToReceive );
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
					this.txtMmcPath.Text = mmcInstallPath;
					await this.RefreshMmcConfig( mmcInstallPath );

					dialog.Close();

					string message = $"Successfully installed MultiMC to \"{mmcInstallPath}\"! " +
									 $"You can now set up a new instance with the correctly " +
									 $"configured mod pack using this installer.\n\n" +
									 $"The first time you start MultiMC, it will ask you to configure " +
									 $"a few things. You can click Next on all of these screens until " +
									 $"you go to start Minecraft, at which point it will ask you to add " +
									 $"a Minecraft account. If you run MultiMC through this installer, " +
									 $"you may have to re-start it after adding an account for Minecraft " +
									 $"to actually launch.";

					MessageBox.Show( this,
									 message,
									 "MultiMC Setup Complete",
									 MessageBoxButtons.OK,
									 MessageBoxIcon.Information );
				}
				catch ( TaskCanceledException )
				{
					MessageBox.Show( this,
									 "MultiMC setup cancelled. You will need to download it yourself, pick an existing installation," +
									 "or re-run the installer and start the MultiMC setup again.",
									 "MultiMC Setup Cancelled",
									 MessageBoxButtons.OK,
									 MessageBoxIcon.Error );
				}
				finally
				{
					dialog.Close();
					this.Enabled = true;
					this.BringToFront();
				}
			}
		}

		private async Task CheckPackNeedsUpdate( (string name, string path) instance )
		{
			var versionPath = Path.Combine( instance.path, "packVersion" );
			var needsUpdate = false;

			if ( File.Exists( versionPath ) )
			{
				if ( SemVersion.TryParse( File.ReadAllText( versionPath ), out var curVersion ) )
				{
					var serverPack = await Upgrader.LoadConfig();

					if ( serverPack.PackVersion == null || curVersion < serverPack.PackVersion )
						needsUpdate = true;
				}
				else
				{
					needsUpdate = true;
				}
			}
			else
			{
				needsUpdate = true;
			}

			if ( needsUpdate )
			{
				MessageBox.Show( this,
								 "The last pack you configured is out of date.\n\n" +
								 "Click the \"Go!\" button to update it.",
								 "Pack Out-Of-Date",
								 MessageBoxButtons.OK,
								 MessageBoxIcon.Warning );
			}
			else
			{
				this.OfferStartMinecraft( instance.name,
										  "The last pack you configured is already up-to-date.",
										  "Click \"No\" if you wish to configure another pack." );
			}
		}

		private async Task RefreshMmcConfig( string path )
		{
			try
			{
				this.cbInstance.Items.Clear();
				this.rbInstanceNew.Checked      = true;
				this.rbInstanceConvert.Enabled  = false;
				this.txtNewInstanceName.Enabled = false;
				this.txtNewInstanceName.Text    = "";
				this.cbInstance.Enabled         = false;
				this.config                     = await MmcConfigReader.ReadFromMmcFolder( path );
				this.lbInstancesFolder.Text     = this.config.InstancesFolder;
				this.mmcFound                   = true;

				try
				{
					this.txtNewInstanceName.Enabled = true;
					this.instances                  = await this.config.GetInstances();

					if ( this.instances.Count > 0 )
					{
						this.rbInstanceConvert.Enabled = true;
						this.cbInstance.Items.AddRange( this.instances.Select( i => (object) i.name ).ToArray() );
						this.cbInstance.SelectedIndex = 0;
					}

					Registry.SetValue( Constants.Registry.RootKey, Constants.Registry.LastMmcPath, path );
				}
				catch ( Exception )
				{
					MessageBox.Show( this,
									 "An error occurred loading the instance list for the specified MultiMC instance.",
									 "Error Loading Instances",
									 MessageBoxButtons.OK,
									 MessageBoxIcon.Error );
				}
			}
			catch ( FileNotFoundException )
			{
				MessageBox.Show( this,
								 "Could not find MultiMC configuration file.\n\n" +
								 "Make sure you selected the folder in which MultiMC.exe is located, " +
								 "and that you have run MultiMC at least once.\n\n" +
								 "You can try re-launching the installer to automatically download a new " +
								 "version of MultiMC.",
								 "Config Not Found",
								 MessageBoxButtons.OK,
								 MessageBoxIcon.Error );

				using ( var subKey = Registry.CurrentUser.OpenSubKey( Constants.Registry.SubKey, true ) )
				{
					if ( subKey != null )
					{
						subKey.DeleteValue( Constants.Registry.LastMmcPath );
						subKey.DeleteValue( Constants.Registry.LastInstance );
					}
				}
			}
			catch ( Exception )
			{
				this.mmcFound               = false;
				this.lbInstancesFolder.Text = "Not Found";
			}
		}

		private async void OnTxtMmcPathLeave( object sender, EventArgs e )
		{
			if ( this.txtMmcPath.Text.Length > 0 )
				await this.RefreshMmcConfig( this.txtMmcPath.Text );
		}

		private void OnLbDownloadMmcClick( object sender, LinkLabelLinkClickedEventArgs e )
		{
			Process.Start( "https://multimc.org/#Download%20&%20Install" );
		}

		private async void OnBtnBrowseMmcClick( object sender, EventArgs e )
		{
			var dialog        = new CommonOpenFileDialog {
				IsFolderPicker   = true,
				CookieIdentifier = OpenFolderMmcCookie
			};
			var result = dialog.ShowDialog( this.Handle );

			if ( result == CommonFileDialogResult.Ok )
			{
				var mmcPath = dialog.FileName;
				await this.RefreshMmcConfig( mmcPath );

				if ( this.mmcFound )
					this.txtMmcPath.Text = mmcPath;
			}
		}

		private void OnRbInstanceChecked( object sender, EventArgs e )
		{
			this.txtNewInstanceName.Enabled = this.rbInstanceNew.Checked;
			this.cbInstance.Enabled         = this.rbInstanceConvert.Checked;
			this.CheckGoButton();
		}

		private void OnTxtNewInstanceNameChanged( object sender, EventArgs e )
		{
			this.CheckGoButton();
		}

		private void OnCbInstanceChanged( object sender, EventArgs e )
		{
			this.CheckGoButton();
		}

		private void CheckGoButton()
		{
			if ( this.rbInstanceNew.Checked )
			{
				this.btnGo.Enabled = !string.IsNullOrWhiteSpace( this.txtNewInstanceName.Text );
			}
			else
			{
				this.btnGo.Enabled = this.cbInstance.SelectedItem != null;
			}
		}

		private async void OnBtnGoClick( object sender, EventArgs e )
		{
			var newInstance  = this.rbInstanceNew.Checked;
			var actionName   = newInstance ? "Building new" : "Converting";
			var dialog       = new ProgressDialog( $"{actionName} instance" );
			var cancelSource = new CancellationTokenSource();
			var ci           = new ComputerInfo();
			var ramSize      = new FileSize( (long) ci.TotalPhysicalMemory );
			// Set JVM max memory to 2 GB less than the user's total RAM, at most 12 GB
			var maxRamGb     = (int) Math.Min( ramSize.GigaBytes - 2, 12 );
			var instanceName = default( string );
			var successful   = false;

			this.Enabled  =  false;
			dialog.Cancel += ( o, args ) => {
				cancelSource.Cancel();
				dialog.Reporter.ReportProgress( -1, "Cancelling...please wait" );
			};
			dialog.Show( this );

			try
			{
				var multiMcInstances = Process.GetProcessesByName( "multimc" );

				if ( multiMcInstances.Any() )
				{
					MessageBox.Show( dialog,
									 "The installer has detected that there are instances of MultiMC still open.\n\n" +
									 "MultiMC must be closed to install a new pack. Click OK to close MultiMC automatically.",
									 "MultiMC Running",
									 MessageBoxButtons.OK,
									 MessageBoxIcon.Warning );

					foreach ( var process in multiMcInstances )
						process.Kill();
				}

				this.config.UpdateChannel = "develop";
				await MmcConfigReader.UpdateConfig( this.txtMmcPath.Text, this.config );

				if ( newInstance )
				{
					await Upgrader.NewInstance( this.config, this.txtNewInstanceName.Text, cancelSource.Token, dialog.Reporter, maxRamMb: maxRamGb * 1024 );
				}
				else // Convert instance
				{
					await Upgrader.ConvertInstance( this.config, this.instances[ this.cbInstance.SelectedIndex ].path, cancelSource.Token, dialog.Reporter, maxRamMb: maxRamGb * 1024 );
				}

				instanceName = newInstance ? this.txtNewInstanceName.Text : this.instances[ this.cbInstance.SelectedIndex ].name;

				Registry.SetValue( Constants.Registry.RootKey, Constants.Registry.LastMmcPath,  this.txtMmcPath.Text );
				Registry.SetValue( Constants.Registry.RootKey, Constants.Registry.LastInstance, instanceName );

				successful = true;
			}
			catch ( Exception ex ) when ( ex is TaskCanceledException ||
										  ex is OperationCanceledException ||
										  ex is WebException we && we.Status == WebExceptionStatus.RequestCanceled )
			{
				MessageBox.Show( dialog,
								 "Pack setup was cancelled. The instance is most likely not in a runnable state.",
								 "Pack Setup Cancelled",
								 MessageBoxButtons.OK,
								 MessageBoxIcon.Warning );
			}
			catch ( Exception ex )
			{
				MessageBox.Show( dialog,
								 "An error occurred while setting up the pack:\n\n" +
								 ex.Message,
								 "Error Setting Up Pack",
								 MessageBoxButtons.OK,
								 MessageBoxIcon.Error );
			}
			finally
			{
				dialog.Close();
				this.Enabled = true;
				this.BringToFront();

				if ( successful )
					this.OfferStartMinecraft( instanceName,
											  "The mod pack was successfully configured!" );
			}
		}

		private void OfferStartMinecraft( string instanceName, string initialPrompt, string postPrompt = null )
		{
			var choice = MessageBox.Show( this,
										  initialPrompt +
										  "\n\nWould you like to start Minecraft now?" +
										  ( postPrompt == null ? "" : $"\n\n{postPrompt}" ),
										  "Start MultiMC?",
										  MessageBoxButtons.YesNo,
										  MessageBoxIcon.Question );

			if ( choice == DialogResult.Yes )
			{
				var startInfo     = new ProcessStartInfo {
					UseShellExecute  = true,
					FileName         = Path.Combine( this.txtMmcPath.Text, "MultiMC.exe" ),
					Arguments        = $"-l \"{instanceName}\"",
					WorkingDirectory = this.txtMmcPath.Text
				};

				Process.Start( startInfo );
				Application.Exit();
			}
		}
	}
}