using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Humanizer;
using MinecraftUpgrader.Config;
using MinecraftUpgrader.MultiMC;
using MinecraftUpgrader.Upgrade;
using NickStrupat;
using Semver;

namespace MinecraftUpgrader
{
	public partial class MainForm : Form
	{
		private enum PackMode
		{
			New,
			NeedsConversion,
			NeedsUpdate,
			ReadyToPlay
		}

		private readonly Upgrader upgrader;

		private MmcConfig                         config;
		private IList<(string name, string path)> instances;
		private PackMode                          currentPackState = PackMode.New;
		private PackUpgrade                       packUpgrade;
		private InstanceMetadata                  currentPackMetadata;
		private bool                              isVrEnabled;

		public MainForm( Upgrader upgrader )
		{
			this.upgrader = upgrader;

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
			this.lbInstanceStatus.Text = "";

			var mmcPath = AppConfig.Get().LastMmcPath;

			if ( string.IsNullOrEmpty( mmcPath ) )
			{
				if ( !this.ShowConfigureMultiMC( out mmcPath ) )
				{
					this.Close();
				}
			}

			await this.LoadMultiMCInstances( mmcPath );
		}

		private async Task LoadMultiMCInstances( string multiMcPath )
		{
			this.txtMmcPath.Text = multiMcPath;
			await this.RefreshMmcConfig( multiMcPath );

			var lastInstanceName = AppConfig.Get().LastInstance;

			if ( !string.IsNullOrEmpty( lastInstanceName ) )
			{
				if ( this.instances?.Any( i => i.name == lastInstanceName ) == true )
				{
					var lastInstance = this.instances.First( i => i.name == lastInstanceName );

					this.rbInstanceNew.Checked      = false;
					this.rbInstanceExisting.Checked = true;
					this.cmbInstance.Enabled        = true;
					this.cmbInstance.SelectedIndex  = this.instances.IndexOf( lastInstance );
					await this.UpdatePackMetadata();
				}
			}
		}

		private bool ShowConfigureMultiMC( out string lastMmcPath, string exitText = null )
		{
			this.Hide();

			var form = new ConfigureMultiMcForm();

			if ( !string.IsNullOrEmpty( exitText ) )
				form.ExitText = exitText;

			var formResult = form.ShowDialog( this );

			this.Show();

			if ( formResult == DialogResult.OK )
			{
				lastMmcPath = AppConfig.Get().LastMmcPath;

				return true;
			}
			else
			{
				lastMmcPath = null;

				return false;
			}
		}

		private async Task RefreshMmcConfig( string path )
		{
			try
			{
				this.cmbInstance.Items.Clear();
				this.rbInstanceNew.Checked      = true;
				this.rbInstanceExisting.Enabled = false;
				this.txtNewInstanceName.Enabled = false;
				this.txtNewInstanceName.Text    = "";
				this.cmbInstance.Enabled        = false;
				this.config                     = await MmcConfigReader.ReadFromMmcFolder( path );
				this.lbInstancesFolder.Text     = this.config.InstancesFolder;

				try
				{
					this.txtNewInstanceName.Enabled = true;
					this.instances                  = await this.config.GetInstances();

					if ( this.instances.Count > 0 )
					{
						this.rbInstanceExisting.Enabled = true;
						this.cmbInstance.Items.AddRange( this.instances.Select( i => (object) i.name ).ToArray() );
						this.cmbInstance.SelectedIndex = 0;
					}

					AppConfig.Update( appConfig => appConfig.LastMmcPath = path );
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

				AppConfig.Update( appConfig => {
					appConfig.LastMmcPath  = null;
					appConfig.LastInstance = null;
				} );
			}
			catch ( Exception )
			{
				this.lbInstancesFolder.Text = "Not Found";
			}
		}

		private async void OnBtnBrowseMmcClick( object sender, EventArgs e )
		{
			if ( this.ShowConfigureMultiMC( out var mmcPath ) )
			{
				await this.LoadMultiMCInstances( mmcPath );
			}
		}

		private async void OnRbInstanceChecked( object sender, EventArgs e )
		{
			this.txtNewInstanceName.Enabled = this.rbInstanceNew.Checked;
			this.cmbInstance.Enabled        = this.rbInstanceExisting.Checked;
			await this.UpdatePackMetadata();
		}

		private async void OnTxtNewInstanceNameChanged( object sender, EventArgs e )
		{
			await this.UpdatePackMetadata();
		}

		private async void OnCbInstanceChanged( object sender, EventArgs e )
		{
			await this.UpdatePackMetadata();
		}

		private void UpdateLayoutFromState()
		{
			if ( this.packUpgrade.SupportsVR )
			{
				if ( !this.panelVR.Visible )
				{
					this.panelVR.Visible =  true;
					this.Height          += this.panelVR.Height;
				}
			}
			else
			{
				if ( this.panelVR.Visible )
				{
					this.panelVR.Visible =  false;
					this.Height          -= this.panelVR.Height;
				}
			}

			this.buttonLayoutPanel.ColumnStyles[ 0 ].Width    = 0;
			this.buttonLayoutPanel.ColumnStyles[ 0 ].SizeType = SizeType.Absolute;
			this.buttonLayoutPanel.ColumnStyles[ 1 ].Width    = 100;
			this.buttonLayoutPanel.ColumnStyles[ 1 ].SizeType = SizeType.Percent;

			if ( this.currentPackState == PackMode.New )
			{
				this.btnGo.Enabled = !string.IsNullOrWhiteSpace( this.txtNewInstanceName.Text );
				this.btnGo.Text    = "Create!";
			}
			else
			{
				var instanceSelected = this.cmbInstance.SelectedItem != null;

				this.btnGo.Enabled         = instanceSelected;
				this.lbInstanceStatus.Text = "";

				if ( instanceSelected )
				{
					if ( this.currentPackState == PackMode.NeedsConversion )
					{
						this.btnRebuild.Enabled = false;

						this.btnGo.Text            = "Convert!";
						this.lbInstanceStatus.Text = "Instance must be converted to custom pack";
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
		}

		private async Task UpdatePackMetadata()
		{
			this.packUpgrade = await this.upgrader.LoadConfig();

			if ( this.rbInstanceNew.Checked )
			{
				this.currentPackState = PackMode.New;
			}
			else
			{
				this.CheckCurrentInstance();
			}

			this.UpdateLayoutFromState();
		}

		private void CheckCurrentInstance()
		{
			var instanceSelected = this.cmbInstance.SelectedItem != null;

			if ( instanceSelected )
			{
				var selectedInstance = this.instances[ this.cmbInstance.SelectedIndex ];
				var instanceMetadata = InstanceMetadataReader.ReadInstanceMetadata( selectedInstance.path );

				if ( instanceMetadata == null
					 || instanceMetadata.Version == "0.0.0"
					 || !SemVersion.TryParse( instanceMetadata.Version, out var instanceSemVersion )
					 || !SemVersion.TryParse( this.packUpgrade.CurrentVersion, out var serverSemVersion ) )
				{
					// Never converted
					this.currentPackState = PackMode.NeedsConversion;
				}
				else
				{
					if ( instanceSemVersion < serverSemVersion
						 || instanceMetadata.BuiltFromServerPack != this.packUpgrade.ServerPack )
					{
						// Out of date
						this.currentPackState = PackMode.NeedsUpdate;
					}
					else
					{
						// Up-to-date
						this.currentPackState = PackMode.ReadyToPlay;
						this.isVrEnabled      = instanceMetadata.VrEnabled;

						this.RefreshVRButtons();
					}
				}

				this.currentPackMetadata = instanceMetadata;
			}
		}

		private async void OnBtnNonVRClick( object sender, EventArgs e )
		{
			this.isVrEnabled = false;
			this.RefreshVRButtons();

			await this.CheckVRState();
		}

		private async void OnBtnVRClick( object sender, EventArgs e )
		{
			this.isVrEnabled = true;
			this.RefreshVRButtons();

			await this.CheckVRState();
		}

		private async Task CheckVRState()
		{
			if ( this.currentPackMetadata != null )
			{
				if ( this.currentPackState == PackMode.ReadyToPlay && this.currentPackMetadata.VrEnabled != this.isVrEnabled )
				{
					this.currentPackState = PackMode.NeedsUpdate;
					this.UpdateLayoutFromState();
				}

				if ( this.currentPackState != PackMode.ReadyToPlay && this.currentPackMetadata.VrEnabled == this.isVrEnabled )
				{
					await this.UpdatePackMetadata();
				}
			}
		}

		private void RefreshVRButtons()
		{
			var buttons = new[] { this.btnNonVR, this.btnVR };

			foreach ( var button in buttons )
			{
				button.BackColor = SystemColors.Control;
				button.ForeColor = SystemColors.ControlText;
			}

			var selectedButton = this.isVrEnabled ? this.btnVR : this.btnNonVR;

			selectedButton.BackColor = Color.DodgerBlue;
			selectedButton.ForeColor = Color.White;
		}

		private async void OnBtnRebuildClick( object sender, EventArgs e )
			=> await this.DoConfigurePack( true );

		private async void OnBtnGoClick( object sender, EventArgs e )
		{
			if ( this.currentPackState == PackMode.ReadyToPlay )
			{
				// Button says "Play"; user doesn't expect another prompt to start MC

				var selectedInstance = this.instances[ this.cmbInstance.SelectedIndex ];

				this.StartMinecraft( selectedInstance.name );
				return;
			}

			await this.DoConfigurePack( false );
		}

		private void OnOpenMultiMCClick( object sender, EventArgs e )
		{
			this.StartMultiMC();
		}

		private async Task DoConfigurePack( bool forceRebuild )
		{
			var newInstance  = this.rbInstanceNew.Checked;
			var updating     = this.currentPackState == PackMode.NeedsUpdate;
			var actionName   = ( newInstance || forceRebuild ) ? "Building new" : updating ? "Updating" : "Converting";
			var dialog       = new ProgressDialog( $"{actionName} instance" );
			var cancelSource = new CancellationTokenSource();
			var ci           = new ComputerInfo();
			var ramSize      = ( (long) ci.TotalPhysicalMemory ).Bytes();
			// Set JVM max memory to 2 GB less than the user's total RAM, at most 12 GB but at least 5
			var maxRamGb     = (int) Math.Max( Math.Min( ramSize.Gigabytes - 4, 12 ), 5 );
			var instanceName = newInstance ? this.txtNewInstanceName.Text : this.instances[ this.cmbInstance.SelectedIndex ].name;
			var successful   = false;

			this.Enabled = false;
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

				// TODO: Uncomment the following line if a development branch of MultiMC is required
				// this.config.UpdateChannel = "develop";

				await MmcConfigReader.UpdateConfig( this.txtMmcPath.Text, this.config );

				if ( newInstance )
				{
					await this.upgrader.NewInstance( this.config, this.txtNewInstanceName.Text, this.isVrEnabled, cancelSource.Token, dialog.Reporter, maxRamMb: maxRamGb * 1024 );
				}
				else // Convert instance
				{
					await this.upgrader.ConvertInstance( this.config, this.instances[ this.cmbInstance.SelectedIndex ].path, forceRebuild, this.isVrEnabled, cancelSource.Token, dialog.Reporter, maxRamMb: maxRamGb * 1024 );
				}

				AppConfig.Update( appConfig => {
					appConfig.LastMmcPath  = this.txtMmcPath.Text;
					appConfig.LastInstance = instanceName;
				} );

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
				await this.UpdatePackMetadata();

				if ( successful )
					this.OfferStartMinecraft( instanceName,
											  "The mod pack was successfully configured!" );
			}
		}

		private void OfferStartMinecraft( string instanceName, string initialPrompt )
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
			{
				this.StartMinecraft( instanceName );
			}
		}

		private void StartMultiMC()
		{
			var startInfo = new ProcessStartInfo {
				UseShellExecute  = true,
				FileName         = Path.Combine( this.txtMmcPath.Text, "MultiMC.exe" ),
				WorkingDirectory = this.txtMmcPath.Text
			};

			Process.Start( startInfo );
		}

		private void StartMinecraft( string instanceName )
		{
			var startInfo = new ProcessStartInfo {
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