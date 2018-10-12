﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic.Devices;
using Microsoft.Win32;
using MinecraftUpgrader.MultiMC;
using MinecraftUpgrader.Upgrade;
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
		private PackMode                          currentPackMode = PackMode.New;

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

			var lastMmcPath      = Registry.GetValue( Constants.Registry.RootKey, Constants.Registry.LastMmcPath,  null ) as string;
			var lastInstanceName = Registry.GetValue( Constants.Registry.RootKey, Constants.Registry.LastInstance, null ) as string;

			if ( string.IsNullOrEmpty( lastMmcPath ) )
			{
				this.Hide();

				var findMultiMcResult = new ConfigureMultiMcForm().ShowDialog( this );

				if ( findMultiMcResult == DialogResult.OK )
				{
					this.Show();
					lastMmcPath = Registry.GetValue( Constants.Registry.RootKey, Constants.Registry.LastMmcPath, null ) as string;
				}
				else
				{
					this.Close();
				}
			}

			this.txtMmcPath.Text = lastMmcPath;
			await this.RefreshMmcConfig( lastMmcPath );

			if ( !string.IsNullOrEmpty( lastInstanceName ) )
			{
				if ( this.instances?.Any( i => i.name == lastInstanceName ) == true )
				{
					var lastInstance = this.instances.First( i => i.name == lastInstanceName );

					this.rbInstanceNew.Checked      = false;
					this.rbInstanceExisting.Checked = true;
					this.cmbInstance.Enabled        = true;
					this.cmbInstance.SelectedIndex  = this.instances.IndexOf( lastInstance );
					await this.CheckInstance();
				}
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
				this.lbInstancesFolder.Text = "Not Found";
			}
		}

		private async void OnRbInstanceChecked( object sender, EventArgs e )
		{
			this.txtNewInstanceName.Enabled = this.rbInstanceNew.Checked;
			this.cmbInstance.Enabled        = this.rbInstanceExisting.Checked;
			await this.CheckInstance();
		}

		private async void OnTxtNewInstanceNameChanged( object sender, EventArgs e )
		{
			await this.CheckInstance();
		}

		private async void OnCbInstanceChanged( object sender, EventArgs e )
		{
			await this.CheckInstance();
		}

		private async Task CheckInstance()
		{
			this.buttonLayoutPanel.ColumnStyles[ 0 ].Width    = 0;
			this.buttonLayoutPanel.ColumnStyles[ 0 ].SizeType = SizeType.Absolute;
			this.buttonLayoutPanel.ColumnStyles[ 1 ].Width    = 100;
			this.buttonLayoutPanel.ColumnStyles[ 1 ].SizeType = SizeType.Percent;

			if ( this.rbInstanceNew.Checked )
			{
				this.btnGo.Enabled   = !string.IsNullOrWhiteSpace( this.txtNewInstanceName.Text );
				this.btnGo.Text      = "Create!";
				this.currentPackMode = PackMode.New;
			}
			else
			{
				var instanceSelected = this.cmbInstance.SelectedItem != null;

				this.btnGo.Enabled         = instanceSelected;
				this.lbInstanceStatus.Text = "";

				if ( instanceSelected )
				{
					var selectedInstance = this.instances[ this.cmbInstance.SelectedIndex ];
					var instanceVersion  = PackVersionReader.ReadPackVersion( selectedInstance.path );
					var packUpgrade      = await this.upgrader.LoadConfig();

					if ( instanceVersion == null
						 || instanceVersion.Version == "0.0.0"
						 || !SemVersion.TryParse( instanceVersion.Version,    out var instanceSemVersion )
						 || !SemVersion.TryParse( packUpgrade.CurrentVersion, out var serverSemVersion ) )
					{
						// Never converted
						this.btnRebuild.Enabled = false;

						this.btnGo.Text            = "Convert!";
						this.lbInstanceStatus.Text = "Instance must be converted to custom pack";
						this.currentPackMode       = PackMode.NeedsConversion;
					}
					else
					{
						this.btnRebuild.Enabled                           = true;
						this.buttonLayoutPanel.ColumnStyles[ 0 ].Width    = 50;
						this.buttonLayoutPanel.ColumnStyles[ 0 ].SizeType = SizeType.Percent;
						this.buttonLayoutPanel.ColumnStyles[ 1 ].Width    = 50;
						this.buttonLayoutPanel.ColumnStyles[ 1 ].SizeType = SizeType.Percent;

						if ( instanceSemVersion < serverSemVersion || instanceVersion.BuiltFromServerPack != packUpgrade.ServerPack )
						{
							// Out of date
							this.btnGo.Text            = "Update!";
							this.lbInstanceStatus.Text = "Instance is out of date and must be updated";
							this.currentPackMode       = PackMode.NeedsUpdate;
						}
						else
						{
							// Up-to-date
							this.btnGo.Text            = "Play!";
							this.lbInstanceStatus.Text = "This instance is ready to play!";
							this.currentPackMode       = PackMode.ReadyToPlay;
						}
					}
				}
			}
		}

		private async void OnBtnRebuildClick( object sender, EventArgs e )
			=> await this.DoConfigurePack( true );

		private async void OnBtnGoClick( object sender, EventArgs e )
		{
			if ( this.currentPackMode == PackMode.ReadyToPlay )
			{
				// Button says "Play"; user doesn't expect another prompt to start MC

				var selectedInstance = this.instances[ this.cmbInstance.SelectedIndex ];

				this.StartMinecraft( selectedInstance.name );
				return;
			}

			await this.DoConfigurePack( false );
		}

		private async Task DoConfigurePack( bool forceRebuild )
		{
			var newInstance  = this.rbInstanceNew.Checked;
			var updating     = this.currentPackMode == PackMode.NeedsUpdate;
			var actionName   = ( newInstance || forceRebuild ) ? "Building new" : updating ? "Updating" : "Converting";
			var dialog       = new ProgressDialog( $"{actionName} instance" );
			var cancelSource = new CancellationTokenSource();
			var ci           = new ComputerInfo();
			var ramSize      = new FileSize( (long) ci.TotalPhysicalMemory );
			// Set JVM max memory to 2 GB less than the user's total RAM, at most 12 GB but at least 3
			var maxRamGb     = (int) Math.Max( Math.Min( ramSize.GigaBytes - 4, 12 ), 3 );
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

				this.config.UpdateChannel = "develop";
				await MmcConfigReader.UpdateConfig( this.txtMmcPath.Text, this.config );

				if ( newInstance )
				{
					await this.upgrader.NewInstance( this.config, this.txtNewInstanceName.Text, cancelSource.Token, dialog.Reporter, maxRamMb: maxRamGb * 1024 );
				}
				else // Convert instance
				{
					await this.upgrader.ConvertInstance( this.config, this.instances[ this.cmbInstance.SelectedIndex ].path, forceRebuild, cancelSource.Token, dialog.Reporter, maxRamMb: maxRamGb * 1024 );
				}

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