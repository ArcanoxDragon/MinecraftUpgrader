using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Version;
using Humanizer;
using MinecraftLauncher.Config;
using MinecraftLauncher.DI;
using MinecraftLauncher.Extensions;
using MinecraftLauncher.Modpack;
using NickStrupat;
using Semver;

namespace MinecraftLauncher
{
	public partial class MainForm : Form
	{
		private enum PackMode
		{
			NeedsInstallation,
			NeedsUpdate,
			ReadyToPlay,
		}

		private readonly PackBuilder packBuilder;

		private PackMetadata     packMetadata;
		private PackMode         currentPackState = PackMode.NeedsInstallation;
		private InstanceMetadata instanceMetadata;

		// private bool isVrEnabled;

		public MainForm( PackBuilder packBuilder )
		{
			this.packBuilder = packBuilder;

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
			var mcProfilePath = this.packBuilder.ProfilePath;

			this.lbMinecraftPath.Text = mcProfilePath;
			this.appToolTip.SetToolTip( this.lbMinecraftPath, mcProfilePath );

			if ( Directory.Exists( mcProfilePath ) )
				this.lbMinecraftPath.LinkArea = new LinkArea( 0, mcProfilePath.Length );
			else
				this.lbMinecraftPath.LinkArea = new LinkArea( 0, 0 );

			await this.LoadPackMetadataAsync();
			await this.CheckCurrentInstanceAsync();
		}

		private async Task LoadPackMetadataAsync()
		{
			this.Enabled = false;

			var progressDialog = new ProgressDialog( "Loading Mod Pack Info" );

			try
			{
				this.packMetadata = await this.packBuilder.LoadConfigAsync( progressReporter: progressDialog.Reporter );
			}
			catch ( Exception ex )
			{
				MessageBox.Show( $"An error occurred while fetching the mod pack info:\n\n{ex.Message}",
								 "Error Loading Mod Pack",
								 MessageBoxButtons.OK,
								 MessageBoxIcon.Error );

				this.Close();
			}
			finally
			{
				progressDialog.Close();
				this.Enabled = true;
			}
		}

		private async Task CheckCurrentInstanceAsync()
		{
			var instanceMetadata = InstanceMetadataReader.ReadInstanceMetadata( this.packBuilder.ProfilePath );

			// Beeg long list of checks to see if this pack needs to be completely converted or not
			if ( instanceMetadata == null ||
				 instanceMetadata.Version == "0.0.0" ||
				 !SemVersion.TryParse( instanceMetadata.Version, out var instanceSemVersion ) ||
				 !SemVersion.TryParse( this.packMetadata.CurrentVersion, out var serverSemVersion ) ||
				 instanceMetadata.CurrentLaunchVersion == null ||
				 instanceMetadata.CurrentMinecraftVersion != this.packMetadata.IntendedMinecraftVersion ||
				 instanceMetadata.CurrentForgeVersion != this.packMetadata.RequiredForgeVersion ||
				 instanceMetadata.BuiltFromServerPack != this.packMetadata.ServerPack )
			{
				// Never converted
				this.currentPackState = PackMode.NeedsInstallation;
			}
			else
			{
				var outOfDate = instanceSemVersion < serverSemVersion;

				// Check server pack MD5 if necessary
				if ( this.packMetadata.VerifyServerPackMd5 )
				{
					using var web            = new WebClient();
					var       basePackMd5Url = $"{this.packMetadata.ServerPack}.md5";
					var       basePackMd5    = await web.DownloadStringTaskAsync( basePackMd5Url );

					outOfDate |= basePackMd5 != instanceMetadata.BuiltFromServerPackMd5;
				}

				if ( outOfDate )
				{
					// Out of date
					this.currentPackState = PackMode.NeedsUpdate;
				}
				else
				{
					// Up-to-date
					this.currentPackState = PackMode.ReadyToPlay;
					// this.isVrEnabled      = instanceMetadata.VrEnabled;
					// TODO: Add back VR support once ViveCraft is ready
				}
			}

			this.instanceMetadata = instanceMetadata;
			this.UpdateLayoutFromState();
		}

		private void UpdateLayoutFromState()
		{
			this.buttonLayoutPanel.ColumnStyles[ 0 ].Width    = 0;
			this.buttonLayoutPanel.ColumnStyles[ 0 ].SizeType = SizeType.Absolute;
			this.buttonLayoutPanel.ColumnStyles[ 1 ].Width    = 100;
			this.buttonLayoutPanel.ColumnStyles[ 1 ].SizeType = SizeType.Percent;

			this.lbInstanceStatus.Text = "";

			if ( this.currentPackState == PackMode.NeedsInstallation )
			{
				this.btnRebuild.Enabled = false;

				this.btnGo.Text            = "Install!";
				this.lbInstanceStatus.Text = "Mod pack must be installed into a new Minecraft profile";
			}
			else
			{
				this.btnRebuild.Enabled                           = true;
				this.buttonLayoutPanel.ColumnStyles[ 0 ].Width    = 50;
				this.buttonLayoutPanel.ColumnStyles[ 0 ].SizeType = SizeType.Percent;
				this.buttonLayoutPanel.ColumnStyles[ 1 ].Width    = 50;
				this.buttonLayoutPanel.ColumnStyles[ 1 ].SizeType = SizeType.Percent;

				if ( this.currentPackState == PackMode.NeedsUpdate )
				{
					this.btnGo.Text            = "Update!";
					this.lbInstanceStatus.Text = "Instance is out of date and must be updated";
				}
				else
				{
					// Ready To Play

					this.btnGo.Text            = "Play!";
					this.lbInstanceStatus.Text = "This instance is ready to play!";
				}
			}
		}

		private async void OnBtnRebuildClick( object sender, EventArgs e )
			=> await this.DisableWhile( () => this.DoConfigurePack( true ) );

		private async void OnBtnGoClick( object sender, EventArgs e ) => await this.DisableWhile( async () => {
			if ( this.currentPackState == PackMode.ReadyToPlay )
			{
				// Button says "Play"; user doesn't expect another prompt to start MC

				await this.StartMinecraftAsync();
				return;
			}

			await this.DoConfigurePack( false );
		} );

		private void lbMinecraftPath_LinkClicked( object sender, LinkLabelLinkClickedEventArgs e )
		{
			if ( Directory.Exists( this.lbMinecraftPath.Text ) )
			{
				Process.Start( "explorer.exe", this.lbMinecraftPath.Text );
			}
		}

		private async Task DoConfigurePack( bool forceRebuild )
		{
			var updating     = this.currentPackState == PackMode.NeedsUpdate;
			var actionName   = updating ? "Updating" : forceRebuild ? "Rebuilding" : "Installing";
			var dialog       = new ProgressDialog( $"{actionName} Mod Pack" ) { ConfirmCancel = true };
			var cancelSource = new CancellationTokenSource();
			var successful   = false;

			// Don't need to do this, necessarily, but to be polite to Mojang, we should check
			// for a valid session *before* downloading all of the Minecraft assets.
			var session = await this.GetSessionAsync();

			if ( session == null )
				return;

			dialog.Cancel += ( o, args ) => {
				cancelSource.Cancel();
				dialog.Reporter.ReportProgress( -1, "Cancelling...please wait" );
			};

			dialog.Show( this );

			try
			{
				await this.packBuilder.SetupPackAsync( this.packMetadata, forceRebuild || !updating, cancelSource.Token, dialog.Reporter );

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
				this.BringToFront();
				await this.CheckCurrentInstanceAsync();

				if ( successful )
					await this.OfferStartMinecraftAsync( "The mod pack was successfully configured!" );
			}
		}

		private async Task OfferStartMinecraftAsync( string initialPrompt )
		{
			var choice = MessageBox.Show( this,
										  initialPrompt +
										  "\n\nWould you like to start Minecraft now?",
										  "Start Minecraft?",
										  MessageBoxButtons.YesNo,
										  MessageBoxIcon.Question );

			if ( choice == DialogResult.Yes )
				await this.StartMinecraftAsync();
		}

		private async Task StartMinecraftAsync()
		{
			if ( this.instanceMetadata == null )
				return;

			var session = await this.GetSessionAsync();

			if ( session == null )
				return;

			var launchOptions = this.CreateLaunchOptions( session );

			launchOptions.StartVersion = await this.GetVersionMetadataAsync( this.instanceMetadata.CurrentLaunchVersion );

			var launch  = new MLaunch( launchOptions );
			var process = launch.GetProcess();

			process.StartInfo.UseShellExecute        = false;
			process.StartInfo.RedirectStandardError  = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.EnableRaisingEvents              = true;

			this.Hide();
			process.Start();

			var consoleWindow = new ConsoleWindow( process );

			consoleWindow.FormClosed += ( sender, args ) => this.Show();
			consoleWindow.Show();
		}

		private async Task<MSession> GetSessionAsync()
		{
			var login   = new MLogin();
			var session = login.ReadSessionCache();

			bool CheckSession() => session?.CheckIsValid() == true;

			if ( CheckSession() /* Try auto-login if a valid session existed in the cache */ )
			{
				var loginResult = await login.TryAutoLoginAsync( session );

				session = loginResult.IsSuccess ? loginResult.Session : null;
			}

			if ( !CheckSession() /* Try manual login */ )
			{
				var loginForm   = Services.CreateInstance<LoginForm>();
				var loginResult = loginForm.ShowDialog( this );

				if ( loginResult != DialogResult.OK )
					return null;

				session = loginForm.Session;
			}

			if ( !CheckSession() /* Should never get here */ )
			{
				MessageBox.Show( this,
								 "Could not start a valid Mojang user session for Minecraft.",
								 "Session Failure",
								 MessageBoxButtons.OK,
								 MessageBoxIcon.Error );

				return null;
			}

			return session;
		}

		private MLaunchOption CreateLaunchOptions( MSession session )
		{
			var computerInfo = new ComputerInfo();
			var ramSize      = ( (long) computerInfo.TotalPhysicalMemory ).Bytes();
			// Set JVM max memory to 2 GB less than the user's total RAM, at most 12 GB but at least 5
			var maxRamGb = (int) Math.Max( Math.Min( ramSize.Gigabytes - 4, 12 ), 5 );

			return new MLaunchOption {
				Path             = new MinecraftPath( this.packBuilder.ProfilePath ),
				JavaPath         = AppConfig.Get().JavaPath,
				MinimumRamMb     = (int) 1.Gigabytes().Megabytes,
				MaximumRamMb     = (int) maxRamGb.Gigabytes().Megabytes,
				GameLauncherName = this.packMetadata.DisplayName,
				Session          = session,
			};
		}

		private async Task<MVersion> GetVersionMetadataAsync( string versionString )
		{
			var versionLoader = new MVersionLoader( new MinecraftPath( this.packBuilder.ProfilePath ) );
			var versions      = await versionLoader.GetVersionMetadatasAsync();

			return versions.GetVersion( versionString );
		}
	}
}