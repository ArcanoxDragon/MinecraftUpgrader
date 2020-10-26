using System;
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
using MinecraftLauncher.Modpack;
using NickStrupat;
using Semver;

namespace MinecraftLauncher
{
	public partial class MainForm : Form
	{
		private enum PackMode
		{
			New,
			NeedsConversion,
			NeedsUpdate,
			ReadyToPlay,
		}

		private readonly PackBuilder packBuilder;

		private PackMetadata     packMetadata;
		private PackMode         currentPackState = PackMode.New;
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
			this.lbMinecraftPath.Text = this.packBuilder.ProfilePath;

			await this.LoadPackMetadataAsync();
			this.CheckCurrentInstance();
		}

		private async Task LoadPackMetadataAsync()
		{
			this.Enabled = false;

			var progressDialog = new ProgressDialog( "Loading mod pack info..." );

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

		private void CheckCurrentInstance()
		{
			var instanceMetadata = InstanceMetadataReader.ReadInstanceMetadata( this.packBuilder.ProfilePath );

			// Beeg long list of checks to see if this pack needs to be completely converted or not
			if ( instanceMetadata == null ||
				 instanceMetadata.Version == "0.0.0" ||
				 !SemVersion.TryParse( instanceMetadata.Version, out var instanceSemVersion ) ||
				 !SemVersion.TryParse( this.packMetadata.CurrentVersion, out var serverSemVersion ) ||
				 instanceMetadata.CurrentLaunchVersion == null ||
				 instanceMetadata.CurrentMinecraftVersion != this.packMetadata.IntendedMinecraftVersion ||
				 instanceMetadata.CurrentForgeVersion != this.packMetadata.RequiredForgeVersion )
			{
				// Never converted
				this.currentPackState = PackMode.NeedsConversion;
			}
			else
			{
				if ( instanceSemVersion < serverSemVersion || instanceMetadata.BuiltFromServerPack != this.packMetadata.ServerPack )
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

			if ( this.currentPackState == PackMode.New )
			{
				this.btnGo.Text = "Create!";
			}
			else
			{
				this.lbInstanceStatus.Text = "";

				if ( this.currentPackState == PackMode.NeedsConversion )
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
		}

		private async void OnBtnRebuildClick( object sender, EventArgs e )
			=> await this.DoConfigurePack( true );

		private async void OnBtnGoClick( object sender, EventArgs e )
		{
			if ( this.currentPackState == PackMode.ReadyToPlay )
			{
				// Button says "Play"; user doesn't expect another prompt to start MC

				await this.StartMinecraftAsync();
				return;
			}

			await this.DoConfigurePack( false );
		}

		private async Task DoConfigurePack( bool forceRebuild )
		{
			var newInstance  = this.currentPackState == PackMode.New;
			var updating     = this.currentPackState == PackMode.NeedsUpdate;
			var actionName   = ( newInstance || forceRebuild ) ? "Building new" : updating ? "Updating" : "Converting";
			var dialog       = new ProgressDialog( $"{actionName} instance" ) { ConfirmCancel = true };
			var cancelSource = new CancellationTokenSource();
			var successful   = false;

			this.Enabled = false;

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
				this.Enabled = true;
				this.BringToFront();
				this.CheckCurrentInstance();

				if ( successful )
					await this.OfferStartMinecraftAsync( "The mod pack was successfully configured!" );
			}
		}

		private async Task OfferStartMinecraftAsync( string initialPrompt )
		{
			var choice = MessageBox.Show( this,
										  initialPrompt +
										  "\n\nWould you like to start Minecraft now?\n\n" +
										  "Note: If you haven't set up a Minecraft account in MultiMC yet, " +
										  "you will have to run either this app or MultiMC again after setting " +
										  "up your account to actually start Minecraft.",
										  "Start MultiMC?",
										  MessageBoxButtons.YesNo,
										  MessageBoxIcon.Question );

			if ( choice == DialogResult.Yes )
				await this.StartMinecraftAsync();
		}

		private async Task StartMinecraftAsync()
		{
			if ( this.instanceMetadata == null )
				return;

			var login   = new MLogin();
			var session = login.ReadSessionCache();

			bool CheckSession() => session?.CheckIsValid() == true;

			if ( !CheckSession() /* Try auto-login */ )
			{
				session = await Task.Run( () => {
					var loginResult = login.TryAutoLogin( session );

					return loginResult.IsSuccess ? loginResult.Session : null;
				} );
			}

			if (true|| !CheckSession() /* Try manual login */ )
			{
				var loginForm   = Services.CreateInstance<LoginForm>();
				var loginResult = loginForm.ShowDialog( this );

				if ( loginResult != DialogResult.OK )
					return;

				session = loginForm.Session;
			}

			if ( !CheckSession() /* Should never get here */ )
			{
				MessageBox.Show( this,
								 "Could not start a valid Mojang session for Minecraft.",
								 "Session Failure",
								 MessageBoxButtons.OK,
								 MessageBoxIcon.Error );

				return;
			}

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
			var versions = await Task.Run( () => MVersionLoader.GetVersionMetadatas( new MinecraftPath( this.packBuilder.ProfilePath ) ) );

			return versions.GetVersion( versionString );
		}
	}
}