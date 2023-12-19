using MinecraftUpgrader.Config;
using MinecraftUpgrader.Prism;
using MinecraftUpgrader.Utility;

namespace MinecraftUpgrader;

public partial class ConfigurePrismForm : Form
{
	private static readonly Guid OpenFolderCookie = new("76309a12-c3b3-4954-80ac-242685715563");

	public ConfigurePrismForm()
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
		var dialog = new FolderBrowserDialog {
			AutoUpgradeEnabled = true,
			ClientGuid = OpenFolderCookie,
		};
		var result = dialog.ShowDialog(this);

		if (result == DialogResult.OK)
		{
			var prismPath = dialog.SelectedPath;

			try
			{
				await PrismConfigReader.ReadFromPrismFolder(prismPath);
				AppConfig.Update(appConfig => appConfig.LastPrismPath = prismPath);
				DialogResult = DialogResult.OK;
			}
			catch
			{
				MessageBox.Show("Prism Launcher was not found in the folder you chose, " +
								"or is installed incorrectly.",
								"Prism Launcher Not Found",
								MessageBoxButtons.OK,
								MessageBoxIcon.Error);
			}
		}
	}

	private async void btnDownload_Click(object sender, EventArgs e)
	{
		Enabled = false;

		if (await InstallPrism())
			DialogResult = DialogResult.OK;
		else
			DialogResult = DialogResult.Cancel;
	}

	private async Task<bool> InstallPrism()
	{
		var dialog = new ProgressDialog("Downloading Prism Launcher");

		try
		{
			var cancelSource = new CancellationTokenSource();

			dialog.Cancel += (_, _) => {
				dialog.Reporter.ReportProgress(-1, "Cancelling...please wait");
				cancelSource.Cancel();
			};
			dialog.Show(this);

			var installPath = await PrismInstaller.InstallPrism(dialog.Reporter, cancelSource.Token);

			MessageBox.Show(this,
							$"Successfully installed Prism Launcher to \"{installPath}\"!",
							"Prism Launcher Setup Complete",
							MessageBoxButtons.OK,
							MessageBoxIcon.Information);

			return true;
		}
		catch (TaskCanceledException)
		{
			MessageBox.Show(this,
							"Prism Launcher setup cancelled. You will need to download it yourself, pick an existing installation," +
							"or re-run the installer and start the Prism Launcher setup again.",
							"Prism Launcher Setup Cancelled",
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