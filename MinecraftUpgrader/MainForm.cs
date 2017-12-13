using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic.Devices;
using Microsoft.WindowsAPICodePack.Dialogs;
using MinecraftUpgrader.MultiMC;
using MinecraftUpgrader.Upgrade;

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
								 "and that you have run MultiMC at least once.",
								 "Config Not Found",
								 MessageBoxButtons.OK,
								 MessageBoxIcon.Error );
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
			var maxRamGb = (int) Math.Min( ramSize.GigaBytes - 2, 12 );

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

				MessageBox.Show( dialog,
								 "Successfully set up the mod pack!",
								 "Pack Setup Complete",
								 MessageBoxButtons.OK,
								 MessageBoxIcon.Information );
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
			}
		}
	}
}