using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;
using MinecraftUpgrader.Config;
using MinecraftUpgrader.MultiMC;
using MinecraftUpgrader.Utility;

namespace MinecraftUpgrader
{
	public partial class ConfigureMultiMcForm : Form
	{
		private static readonly Guid OpenFolderCookie = new("76309a12-c3b3-4954-80ac-242685715563");

		public ConfigureMultiMcForm()
		{
			InitializeComponent();
		}

		public string ExitText
		{
			get => this.btnExit.Text;
			set => this.btnExit.Text = value;
		}

		private async void btnBrowse_Click(object sender, EventArgs e)
		{
			var dialog = new CommonOpenFileDialog {
				IsFolderPicker = true,
				CookieIdentifier = OpenFolderCookie
			};
			var result = dialog.ShowDialog(Handle);

			if (result == CommonFileDialogResult.Ok)
			{
				var mmcPath = dialog.FileName;

				try
				{
					await MmcConfigReader.ReadFromMmcFolder(mmcPath);
					AppConfig.Update(appConfig => appConfig.LastMmcPath = mmcPath);
					DialogResult = DialogResult.OK;
				}
				catch
				{
					MessageBox.Show("MultiMC was not found in the folder you chose, " +
									"or is installed incorrectly.",
									"MultiMC Not Found",
									MessageBoxButtons.OK,
									MessageBoxIcon.Error);
				}
			}
		}

		private async void btnDownload_Click(object sender, EventArgs e)
		{
			Enabled = false;

			if (await InstallMultiMc())
				DialogResult = DialogResult.OK;
			else
				DialogResult = DialogResult.Cancel;
		}

		private async Task<bool> InstallMultiMc()
		{
			var dialog = new ProgressDialog("Downloading MultiMC");

			try
			{
				var cancelSource = new CancellationTokenSource();

				dialog.Cancel += (o, args) => {
					dialog.Reporter.ReportProgress(-1, "Cancelling...please wait");
					cancelSource.Cancel();
				};
				dialog.Show(this);

				var mmcInstallPath = await MultiMcInstaller.InstallMultiMC(dialog.Reporter, cancelSource.Token);

				MessageBox.Show(this,
								$"Successfully installed MultiMC to \"{mmcInstallPath}\"!",
								"MultiMC Setup Complete",
								MessageBoxButtons.OK,
								MessageBoxIcon.Information);

				return true;
			}
			catch (TaskCanceledException)
			{
				MessageBox.Show(this,
								"MultiMC setup cancelled. You will need to download it yourself, pick an existing installation," +
								"or re-run the installer and start the MultiMC setup again.",
								"MultiMC Setup Cancelled",
								MessageBoxButtons.OK,
								MessageBoxIcon.Error);

				return false;
			}
			finally
			{
				dialog.Close();
			}
		}
	}
}