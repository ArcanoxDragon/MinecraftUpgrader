﻿using System.Diagnostics;
using Humanizer;
using MinecraftUpgrader.Config;
using MinecraftUpgrader.Extensions;
using MinecraftUpgrader.Modpack;
using MinecraftUpgrader.Prism;
using MinecraftUpgrader.Utility;
using NickStrupat;
using Semver;

namespace MinecraftUpgrader;

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

	private bool                              initialized;
	private PrismConfig                       config;
	private IList<(string name, string path)> instances;
	private PackMode                          currentPackState = PackMode.New;
	private PackMetadata                      packMetadata;
	private InstanceMetadata                  currentPackMetadata;
	private bool                              isVrEnabled;

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
		this.lbInstanceStatus.Text = "";

		var prismPath = AppConfig.Get().LastPrismPath;

		if (string.IsNullOrEmpty(prismPath))
		{
			if (!LocateOrConfigurePrism(out prismPath))
			{
				Close();
			}
		}

		await LoadPrismInstances(prismPath);
		await UpdatePackMetadata();

		this.initialized = true;
	}

	private async Task RefreshPrismInstances()
	{
		var prismPath = AppConfig.Get().LastPrismPath;

		if (string.IsNullOrEmpty(prismPath))
			return;

		await LoadPrismInstances(prismPath);
	}

	private async Task LoadPrismInstances(string prismPath)
	{
		this.txtPrismPath.Text = prismPath;
		await RefreshPrismConfig(prismPath);

		var lastInstanceName = AppConfig.Get().LastInstance;

		if (!string.IsNullOrEmpty(lastInstanceName))
		{
			if (this.instances?.Any(i => i.name == lastInstanceName) == true)
			{
				var lastInstance = this.instances.First(i => i.name == lastInstanceName);

				this.rbInstanceNew.Checked = false;
				this.rbInstanceExisting.Checked = true;
				this.cmbInstance.Enabled = true;
				this.cmbInstance.SelectedIndex = this.instances.IndexOf(lastInstance);
				await UpdatePackMetadata();
			}
		}
	}

	private bool LocateOrConfigurePrism(out string prismPath)
	{
		if (PrismUtility.TryAutoLocatePrism(out _, out prismPath))
			return true;

		return ShowConfigurePrism(out prismPath);
	}

	private bool ShowConfigurePrism(out string lastPrismPath)
	{
		Hide();

		var form = new ConfigurePrismForm();

		var formResult = form.ShowDialog(this);

		Show();

		if (formResult == DialogResult.OK)
		{
			lastPrismPath = AppConfig.Get().LastPrismPath;

			return true;
		}
		else
		{
			lastPrismPath = null;

			return false;
		}
	}

	private async Task RefreshPrismConfig(string path)
	{
		try
		{
			this.cmbInstance.Items.Clear();
			this.rbInstanceNew.Checked = true;
			this.rbInstanceExisting.Enabled = false;
			this.txtNewInstanceName.Enabled = false;
			this.txtNewInstanceName.Text = "";
			this.cmbInstance.Enabled = false;
			this.config = await PrismConfigReader.ReadFromPrismFolder(path);
			this.lbInstancesFolder.Text = this.config.InstancesFolder;

			try
			{
				this.txtNewInstanceName.Enabled = true;
				this.instances = await this.config.GetInstances();

				if (this.instances.Count > 0)
				{
					this.rbInstanceExisting.Enabled = true;
					this.cmbInstance.Items.AddRange(this.instances.Select(i => (object) i.name).ToArray());
					this.cmbInstance.SelectedIndex = 0;
				}

				AppConfig.Update(appConfig => appConfig.LastPrismPath = path);
			}
			catch (Exception)
			{
				MessageBox.Show(this,
								"An error occurred loading the instance list for the specified Prism Laucher installation.",
								"Error Loading Instances",
								MessageBoxButtons.OK,
								MessageBoxIcon.Error);
			}
		}
		catch (FileNotFoundException)
		{
			MessageBox.Show(this,
							"Could not find Prism Launcher configuration file.\n\n" +
							"Make sure you selected the folder in which PrismLauncher.exe is located, " +
							"and that you have run Prism Launcher at least once.\n\n" +
							"You can try re-launching the installer to automatically download a new " +
							"version of Prism Launcher.",
							"Config Not Found",
							MessageBoxButtons.OK,
							MessageBoxIcon.Error);

			AppConfig.Update(appConfig => {
				appConfig.LastPrismPath = null;
				appConfig.LastInstance = null;
			});
		}
		catch (Exception)
		{
			this.lbInstancesFolder.Text = "Not Found";
		}
	}

	private async void OnBtnBrowsePrismClick(object sender, EventArgs e)
	{
		if (ShowConfigurePrism(out var prismPath))
		{
			await LoadPrismInstances(prismPath);
		}
	}

	private async void OnRbInstanceChecked(object sender, EventArgs e)
	{
		this.txtNewInstanceName.Enabled = this.rbInstanceNew.Checked;
		this.cmbInstance.Enabled = this.rbInstanceExisting.Checked;

		if (this.initialized)
			await UpdatePackMetadata();
	}

	private void OnTxtNewInstanceNameChanged(object sender, EventArgs e)
	{
		if (!this.initialized)
			return;

		UpdateLayoutFromState();
	}

	private async void OnCbInstanceChanged(object sender, EventArgs e)
	{
		await UpdatePackMetadata();
	}

	private void UpdateLayoutFromState()
	{
		if (this.packMetadata is { SupportsVR: true })
		{
			if (!this.panelVR.Visible)
			{
				this.panelVR.Visible = true;
				Height += this.panelVR.Height;
			}
		}
		else
		{
			if (this.panelVR.Visible)
			{
				this.panelVR.Visible = false;
				Height -= this.panelVR.Height;
			}
		}

		this.buttonLayoutPanel.ColumnStyles[0].Width = 0;
		this.buttonLayoutPanel.ColumnStyles[0].SizeType = SizeType.Absolute;
		this.buttonLayoutPanel.ColumnStyles[1].Width = 100;
		this.buttonLayoutPanel.ColumnStyles[1].SizeType = SizeType.Percent;

		if (this.currentPackState == PackMode.New)
		{
			this.btnGo.Enabled = !string.IsNullOrWhiteSpace(this.txtNewInstanceName.Text);
			this.btnGo.Text = "Create!";
		}
		else
		{
			var instanceSelected = this.cmbInstance.SelectedItem != null;

			this.btnGo.Enabled = instanceSelected;
			this.lbInstanceStatus.Text = "";

			if (instanceSelected)
			{
				if (this.currentPackState == PackMode.NeedsConversion)
				{
					this.btnRebuild.Enabled = false;

					this.btnGo.Text = "Convert!";
					this.lbInstanceStatus.Text = "Instance must be converted to custom pack";
				}
				else
				{
					this.btnRebuild.Enabled = true;
					this.buttonLayoutPanel.ColumnStyles[0].Width = 50;
					this.buttonLayoutPanel.ColumnStyles[0].SizeType = SizeType.Percent;
					this.buttonLayoutPanel.ColumnStyles[1].Width = 50;
					this.buttonLayoutPanel.ColumnStyles[1].SizeType = SizeType.Percent;

					if (this.currentPackState == PackMode.NeedsUpdate)
					{
						this.btnGo.Text = "Update!";
						this.lbInstanceStatus.Text = "Instance is out of date and must be updated";
					}
					else
					{
						// Ready To Play

						this.btnGo.Text = "Play!";
						this.lbInstanceStatus.Text = "This instance is ready to play!";
					}
				}
			}
		}
	}

	private async Task UpdatePackMetadata(bool force = false)
	{
		if (this.packMetadata is null || force)
			await LoadPackMetadataAsync();

		if (this.rbInstanceNew.Checked)
		{
			this.currentPackState = PackMode.New;
		}
		else
		{
			await CheckCurrentInstanceAsync();
		}

		UpdateLayoutFromState();
	}

	private Task LoadPackMetadataAsync()
		=> this.disablePanel.DisableWhile(async () => {
			var progressDialog = new ProgressDialog("Loading Mod Pack Info");

			progressDialog.Show(this);

			try
			{
				this.packMetadata = await this.packBuilder.LoadConfigAsync(progressReporter: progressDialog.Reporter);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"An error occurred while fetching the mod pack info:\n\n{ex.GetFriendlyMessage()}",
								"Error Loading Mod Pack",
								MessageBoxButtons.OK,
								MessageBoxIcon.Error);

				// Close();
			}
			finally
			{
				progressDialog.Close();
			}
		});

	private async Task CheckCurrentInstanceAsync()
	{
		var instanceSelected = this.cmbInstance.SelectedItem != null;

		if (instanceSelected)
		{
			var selectedInstance = this.instances[this.cmbInstance.SelectedIndex];
			var instanceMetadata = InstanceMetadataReader.ReadInstanceMetadata(selectedInstance.path);

			if (instanceMetadata == null
				|| instanceMetadata.Version == "0.0.0"
				|| !SemVersion.TryParse(instanceMetadata.Version, SemVersionStyles.Any, out var currentPackVersion)
				|| !SemVersion.TryParse(this.packMetadata.CurrentVersion, SemVersionStyles.Any, out var serverPackVersion))
			{
				// Never converted
				this.currentPackState = PackMode.NeedsConversion;
			}
			else
			{
				var outOfDate = !currentPackVersion.Satisfies(SemVersionRange.AtLeast(serverPackVersion)) ||
								instanceMetadata.BuiltFromServerPack != this.packMetadata.ServerPack;

				// Check server pack MD5 if necessary
				if (this.packMetadata.VerifyServerPackMd5)
				{
					using var http = WebUtility.CreateHttpClient();
					var basePackMd5Url = $"{this.packMetadata.ServerPack}.md5";
					var basePackMd5 = await http.GetStringAsync(basePackMd5Url);

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
					this.isVrEnabled = instanceMetadata.VrEnabled;

					RefreshVRButtons();
				}
			}

			this.currentPackMetadata = instanceMetadata;
		}
	}

	private async void OnBtnNonVRClick(object sender, EventArgs e)
	{
		this.isVrEnabled = false;
		RefreshVRButtons();

		await CheckVRState();
	}

	private async void OnBtnVRClick(object sender, EventArgs e)
	{
		this.isVrEnabled = true;
		RefreshVRButtons();

		await CheckVRState();
	}

	private async Task CheckVRState()
	{
		if (this.currentPackMetadata != null)
		{
			if (this.currentPackState == PackMode.ReadyToPlay && this.currentPackMetadata.VrEnabled != this.isVrEnabled)
			{
				this.currentPackState = PackMode.NeedsUpdate;
				UpdateLayoutFromState();
			}

			if (this.currentPackState != PackMode.ReadyToPlay && this.currentPackMetadata.VrEnabled == this.isVrEnabled)
			{
				await UpdatePackMetadata();
			}
		}
	}

	private void RefreshVRButtons()
	{
		var buttons = new[] { this.btnNonVR, this.btnVR };

		foreach (var button in buttons)
		{
			button.BackColor = SystemColors.Control;
			button.ForeColor = SystemColors.ControlText;
		}

		var selectedButton = this.isVrEnabled ? this.btnVR : this.btnNonVR;

		selectedButton.BackColor = Color.DodgerBlue;
		selectedButton.ForeColor = Color.White;
	}

	private async void OnBtnRebuildClick(object sender, EventArgs e)
		=> await this.disablePanel.DisableWhile(() => DoConfigurePack(true));

	private async void OnBtnGoClick(object sender, EventArgs e)
		=> await this.disablePanel.DisableWhile(async () => {
			if (this.currentPackState == PackMode.ReadyToPlay)
			{
				// Button says "Play"; user doesn't expect another prompt to start MC

				var selectedInstance = this.instances[this.cmbInstance.SelectedIndex];

				StartMinecraft(selectedInstance.name);
				return;
			}

			await DoConfigurePack(false);
		});

	private void OnOpenPrismClick(object sender, EventArgs e)
	{
		StartPrism();
	}

	private async Task DoConfigurePack(bool forceRebuild)
	{
		var newInstance = this.rbInstanceNew.Checked;
		var updating = this.currentPackState == PackMode.NeedsUpdate;
		var actionName = ( newInstance || forceRebuild ) ? "Building new" : updating ? "Updating" : "Converting";
		var dialog = new ProgressDialog($"{actionName} instance");
		var cancelSource = new CancellationTokenSource();
		var ci = new ComputerInfo();
		var ramSize = ( (long) ci.TotalPhysicalMemory ).Bytes();
		// Set JVM max memory to 2 GB less than the user's total RAM, at most 12 GB but at least 5
		var maxRamGb = (int) Math.Max(Math.Min(ramSize.Gigabytes - 4, 12), 5);
		var instanceName = newInstance ? this.txtNewInstanceName.Text : this.instances[this.cmbInstance.SelectedIndex].name;
		var successful = false;

		dialog.Cancel += (_, _) => {
			cancelSource.Cancel();
			dialog.Reporter.ReportProgress(-1, "Cancelling...please wait");
		};
		dialog.Show(this);

		try
		{
			var prismInstances = Process.GetProcessesByName("prismlauncher");

			if (prismInstances.Any())
			{
				MessageBox.Show(dialog,
								"The installer has detected that there are instances of Prism Launcher still open.\n\n" +
								"Prism Launcher must be closed to install a new pack. Click OK to close Prism Launcher automatically.",
								"Prism Launcher Running",
								MessageBoxButtons.OK,
								MessageBoxIcon.Warning);

				foreach (var process in prismInstances)
					process.Kill();
			}

			await PrismConfigReader.UpdateConfig(this.txtPrismPath.Text, this.config);

			if (newInstance)
			{
				await this.packBuilder.NewInstanceAsync(this.config, this.txtNewInstanceName.Text, this.isVrEnabled, cancelSource.Token, dialog.Reporter, maxRamMb: maxRamGb * 1024);
			}
			else // Convert instance
			{
				await this.packBuilder.ConvertInstance(this.config, this.instances[this.cmbInstance.SelectedIndex].path, forceRebuild, this.isVrEnabled, cancelSource.Token, dialog.Reporter, maxRamMb: maxRamGb * 1024);
			}

			AppConfig.Update(appConfig => {
				appConfig.LastPrismPath = this.txtPrismPath.Text;
				appConfig.LastInstance = instanceName;
			});

			successful = true;
		}
		catch (Exception ex) when (ex is OperationCanceledException
									or TaskCanceledException { InnerException: null or not TimeoutException })
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
							ex.GetFriendlyMessage(),
							"Error Setting Up Pack",
							MessageBoxButtons.OK,
							MessageBoxIcon.Error);
		}
		finally
		{
			dialog.Close();
			BringToFront();
			await UpdatePackMetadata();

			if (successful)
			{
				await RefreshPrismInstances();
				OfferStartMinecraft(instanceName, "The mod pack was successfully configured!");
			}
		}
	}

	private void OfferStartMinecraft(string instanceName, string initialPrompt)
	{
		var choice = MessageBox.Show(this,
									 initialPrompt +
									 "\n\nWould you like to start Minecraft now?\n\n" +
									 "Note: If this is your first time launching Prism Launcher, it will ask you to set up Java " +
									 "as well as configure your Minecraft account. Follow the instructions provided by Prism, and " +
									 "if it asks you which version of Java to use, pick one that starts with \"1.8\".",
									 "Start Prism Launcher?",
									 MessageBoxButtons.YesNo,
									 MessageBoxIcon.Question);

		if (choice == DialogResult.Yes)
		{
			StartMinecraft(instanceName);
		}
	}

	private void StartPrism()
	{
		var startInfo = new ProcessStartInfo {
			UseShellExecute = true,
			FileName = GetPrismExecutablePath(),
			WorkingDirectory = this.txtPrismPath.Text,
		};

		Process.Start(startInfo);
	}

	private void StartMinecraft(string instanceName)
	{
		var startInfo = new ProcessStartInfo {
			UseShellExecute = true,
			FileName = GetPrismExecutablePath(),
			Arguments = $"-l \"{instanceName}\"",
			WorkingDirectory = this.txtPrismPath.Text,
		};

		Process.Start(startInfo);
		Application.Exit();
	}

	private string GetPrismExecutablePath()
	{
		if (PrismUtility.TryAutoLocatePrism(out var programPath, out _))
			return programPath;

		// If we couldn't find the installed version, we assume the user has the portable version.
		// The executable is therefore in the same folder as the config.
		return Path.Combine(this.txtPrismPath.Text, "prismlauncher.exe");
	}
}