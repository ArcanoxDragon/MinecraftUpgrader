﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MinecraftLauncher.Config;
using MinecraftLauncher.Extensions;
using MinecraftLauncher.Modpack;
using MinecraftLauncher.Utility;
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
		private bool             isVrEnabled;

		// private bool isVrEnabled;

		public MainForm(PackBuilder packBuilder)
		{
			this.packBuilder = packBuilder;

			InitializeComponent();
		}

		private new void BringToFront()
		{
			WindowState = FormWindowState.Minimized;
			Show();
			WindowState = FormWindowState.Normal;
		}

		private async void OnFormLoad(object sender, EventArgs e)
		{
			var mcProfilePath = PackBuilder.ProfilePath;

			this.lbMinecraftPath.Text = mcProfilePath;
			this.appToolTip.SetToolTip(this.lbMinecraftPath, mcProfilePath);

			if (Directory.Exists(mcProfilePath))
				this.lbMinecraftPath.LinkArea = new LinkArea(0, mcProfilePath.Length);
			else
				this.lbMinecraftPath.LinkArea = new LinkArea(0, 0);

			this.isVrEnabled = AppConfig.Get().VrEnabled;

			UpdateVrUi();
			await LoadPackMetadataAsync();
			await CheckCurrentInstanceAsync();

#if DEBUG
			this.chkUseCanaryVersion.Visible = true;
#endif
		}

		private async Task LoadPackMetadataAsync()
		{
			Enabled = false;

			var progressDialog = new ProgressDialog("Loading Mod Pack Info");

			try
			{
				this.packMetadata = await this.packBuilder.LoadConfigAsync(progressReporter: progressDialog.Reporter);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"An error occurred while fetching the mod pack info:\n\n{ex.Message}",
								"Error Loading Mod Pack",
								MessageBoxButtons.OK,
								MessageBoxIcon.Error);

				Close();
			}
			finally
			{
				progressDialog.Close();
				Enabled = true;
			}
		}

		private async Task CheckCurrentInstanceAsync()
		{
			try
			{
				var instanceMetadata = InstanceMetadataReader.ReadInstanceMetadata(PackBuilder.ProfilePath);
				var useCanaryVersion = this.chkUseCanaryVersion.Checked;
				var currentRemoteVersion = useCanaryVersion
											   ? this.packMetadata.CanaryVersion ?? this.packMetadata.CurrentVersion
											   : this.packMetadata.CurrentVersion;

				// Beeg long list of checks to see if this pack needs to be completely converted or not
				if (instanceMetadata == null ||
					instanceMetadata.Version == "0.0.0" ||
					!SemVersion.TryParse(instanceMetadata.Version, out var instanceSemVersion) ||
					!SemVersion.TryParse(currentRemoteVersion, out var serverSemVersion) ||
					instanceMetadata.CurrentLaunchVersion == null ||
					instanceMetadata.CurrentMinecraftVersion != this.packMetadata.IntendedMinecraftVersion ||
					instanceMetadata.CurrentForgeVersion != this.packMetadata.RequiredForgeVersion ||
					instanceMetadata.BuiltFromServerPack != this.packMetadata.ServerPack)
				{
					// Never converted
					this.currentPackState = PackMode.NeedsInstallation;
				}
				else
				{
					var outOfDate = instanceSemVersion < serverSemVersion;

					// Check server pack MD5 if necessary
					if (this.packMetadata.VerifyServerPackMd5)
					{
						using var web = new WebClient();
						var basePackMd5Url = $"{this.packMetadata.ServerPack}.md5";
						var basePackMd5 = await web.DownloadStringTaskAsync(basePackMd5Url);

						outOfDate |= basePackMd5 != instanceMetadata.BuiltFromServerPackMd5;
					}

					if (outOfDate)
					{
						// Out of date
						this.currentPackState = PackMode.NeedsUpdate;
					}
					else
					{
						// Up-to-date
						this.currentPackState = PackMode.ReadyToPlay;

						UpdateVrUi();
					}
				}

				this.instanceMetadata = instanceMetadata;
				UpdateLayoutFromState();
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					this,
					$"An error occurred while checking if the instance was up-to-date:\n\n{ex}",
					"Unexpected Error",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error
				);
			}
		}

		private void UpdateLayoutFromState()
		{
			if (this.packMetadata?.SupportsVr == true)
			{
				if (!this.panelVr.Visible)
				{
					this.panelVr.Visible =  true;
					Height               += this.panelVr.Height;
				}
			}
			else
			{
				if (this.panelVr.Visible)
				{
					this.panelVr.Visible =  false;
					Height               -= this.panelVr.Height;
				}
			}

			this.buttonLayoutPanel.ColumnStyles[0].Width    = 0;
			this.buttonLayoutPanel.ColumnStyles[0].SizeType = SizeType.Absolute;
			this.buttonLayoutPanel.ColumnStyles[1].Width    = 100;
			this.buttonLayoutPanel.ColumnStyles[1].SizeType = SizeType.Percent;

			this.lbInstanceStatus.Text = "";

			if (this.currentPackState == PackMode.NeedsInstallation)
			{
				this.btnRebuild.Enabled = false;

				this.btnGo.Text            = "Install!";
				this.lbInstanceStatus.Text = "Mod pack must be installed into a new Minecraft profile";
			}
			else
			{
				this.btnRebuild.Enabled                         = true;
				this.buttonLayoutPanel.ColumnStyles[0].Width    = 50;
				this.buttonLayoutPanel.ColumnStyles[0].SizeType = SizeType.Percent;
				this.buttonLayoutPanel.ColumnStyles[1].Width    = 50;
				this.buttonLayoutPanel.ColumnStyles[1].SizeType = SizeType.Percent;

				if (this.currentPackState == PackMode.NeedsUpdate)
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

		private void UpdateVrUi()
		{
			var unselectedButton = this.isVrEnabled ? this.btnNonVr : this.btnVr;
			var selectedButton = this.isVrEnabled ? this.btnVr : this.btnNonVr;

			unselectedButton.BackColor = SystemColors.Control;
			unselectedButton.ForeColor = SystemColors.ControlText;
			selectedButton.BackColor   = Color.DodgerBlue;
			selectedButton.ForeColor   = Color.White;
		}

		private void OnBtnNonVrClick(object sender, EventArgs e)
		{
			this.isVrEnabled = false;
			UpdateVrUi();

			AppConfig.Update(config => config.VrEnabled = this.isVrEnabled);
		}

		private void OnBtnVrClick(object sender, EventArgs e)
		{
			this.isVrEnabled = true;
			UpdateVrUi();

			AppConfig.Update(config => config.VrEnabled = this.isVrEnabled);
		}

		private async void OnBtnRebuildClick(object sender, EventArgs e)
			=> await this.DisableWhile(() => DoConfigurePack(true));

		private async void OnBtnGoClick(object sender, EventArgs e) => await this.DisableWhile(async () => {
			if (this.currentPackState == PackMode.ReadyToPlay)
			{
				// Button says "Play"; user doesn't expect another prompt to start MC

				await StartMinecraftAsync();
				return;
			}

			await DoConfigurePack(false);
		});

		private async void OnButtonRefreshClick(object sender, EventArgs e) => await this.DisableWhile(async () => {
			await LoadPackMetadataAsync();
			await CheckCurrentInstanceAsync();
		});

		private async void OnUseCanaryVersionCheckChanged(object sender, EventArgs e)
		{
			await CheckCurrentInstanceAsync();
		}

		private void OnLbMinecraftPathLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			if (Directory.Exists(this.lbMinecraftPath.Text))
			{
				Process.Start("explorer.exe", this.lbMinecraftPath.Text);
			}
		}

		private async Task DoConfigurePack(bool forceRebuild)
		{
			var updating = this.currentPackState == PackMode.NeedsUpdate;
			var actionName = updating ? "Updating" : forceRebuild ? "Rebuilding" : "Installing";
			var dialog = new ProgressDialog($"{actionName} Mod Pack") { ConfirmCancel = true };
			var cancelSource = new CancellationTokenSource();
			var successful = false;

			// Don't need to do this, necessarily, but to be polite to Mojang, we should check
			// for a valid session *before* downloading all of the Minecraft assets.
			var session = await SessionUtil.GetSessionAsync(this);

			if (session == null)
				return;

			dialog.Cancel += (_, _) => {
				cancelSource.Cancel();
				dialog.Reporter.ReportProgress(-1, "Cancelling...please wait");
			};

			dialog.Show(this);

			try
			{
				var useCanaryVersion = this.chkUseCanaryVersion.Checked;

				await this.packBuilder.SetupPackAsync(this.packMetadata, forceRebuild || !updating, useCanaryVersion, cancelSource.Token, dialog.Reporter);

				successful = true;
			}
			catch (Exception ex)
				when (ex is TaskCanceledException
						  or OperationCanceledException
						  or WebException { Status: WebExceptionStatus.RequestCanceled })
			{
				MessageBox.Show(dialog,
								"Pack setup was cancelled. The instance is most likely not in a runnable state.",
								"Pack Setup Cancelled",
								MessageBoxButtons.OK,
								MessageBoxIcon.Warning);
			}
			catch (Exception ex)
			{
				MessageBox.Show(dialog,
								"An error occurred while setting up the pack:\n\n" +
								ex.Message,
								"Error Setting Up Pack",
								MessageBoxButtons.OK,
								MessageBoxIcon.Error);
			}
			finally
			{
				dialog.Close();
				BringToFront();
				await CheckCurrentInstanceAsync();

				if (successful)
					await OfferStartMinecraftAsync("The mod pack was successfully configured!");
			}
		}

		private async Task OfferStartMinecraftAsync(string initialPrompt)
		{
			var choice = MessageBox.Show(this,
										 initialPrompt +
										 "\n\nWould you like to start Minecraft now?",
										 "Start Minecraft?",
										 MessageBoxButtons.YesNo,
										 MessageBoxIcon.Question);

			if (choice == DialogResult.Yes)
				await StartMinecraftAsync();
		}

		private async Task StartMinecraftAsync()
		{
			var launcher = new McLauncher(this.packMetadata, this.instanceMetadata, this.isVrEnabled);

			if (!await launcher.StartMinecraftAsync(this))
				return;

			var consoleWindow = new ConsoleWindow(launcher);

			consoleWindow.FormClosed += (_, _) => Show();

			Hide();
			consoleWindow.Show();
		}
	}
}