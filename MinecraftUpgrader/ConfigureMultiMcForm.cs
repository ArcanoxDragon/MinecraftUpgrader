using System;
using System.Windows.Forms;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using MinecraftUpgrader.MultiMC;
using MinecraftUpgrader.Util;

namespace MinecraftUpgrader
{
	public partial class ConfigureMultiMcForm : Form
	{
		private static readonly Guid OpenFolderCookie = new Guid( "76309a12-c3b3-4954-80ac-242685715563" );

		public ConfigureMultiMcForm()
		{
			this.InitializeComponent();
		}

		public string ExitText
		{
			get => this.btnExit.Text;
			set => this.btnExit.Text = value;
		}

		private async void btnBrowse_Click( object sender, EventArgs e )
		{
			var dialog = new CommonOpenFileDialog {
				IsFolderPicker   = true,
				CookieIdentifier = OpenFolderCookie
			};
			var result = dialog.ShowDialog( this.Handle );

			if ( result == CommonFileDialogResult.Ok )
			{
				var mmcPath = dialog.FileName;

				try
				{
					await MmcConfigReader.ReadFromMmcFolder( mmcPath );
					Registry.SetValue( Constants.Registry.RootKey, Constants.Registry.LastMmcPath, mmcPath, RegistryValueKind.String );
					this.DialogResult = DialogResult.OK;
				}
				catch
				{
					MessageBox.Show( "MultiMC was not found in the folder you chose, " +
									 "or is installed incorrectly.",
									 "MultiMC Not Found",
									 MessageBoxButtons.OK,
									 MessageBoxIcon.Error );
				}
			}
		}

		private async void btnDownload_Click( object sender, EventArgs e )
		{
			this.Enabled = false;

			if ( await MultiMcInstaller.InstallMultiMC( this ) )
				this.DialogResult = DialogResult.OK;
			else
				this.DialogResult = DialogResult.Cancel;
		}
	}
}