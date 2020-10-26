using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using CmlLib.Core.Auth;
using MinecraftLauncher.Config;

namespace MinecraftLauncher
{
	public partial class LoginForm : Form
	{
		public LoginForm()
		{
			this.InitializeComponent();
		}

		public MSession Session { get; private set; }

		private void ValidateForm()
		{
			var usernameValid = !string.IsNullOrWhiteSpace( this.textBoxUsername.Text );
			var passwordValid = !string.IsNullOrWhiteSpace( this.textBoxPassword.Text );

			this.buttonLogin.Enabled = usernameValid && passwordValid;
		}

		private async void LoginForm_Load( object sender, EventArgs e )
		{
			this.textBoxUsername.Text = AppConfig.Get().LastUsername ?? string.Empty;
			this.labelStatus.Text     = string.Empty;

			await Task.Delay( 100 ); // For some reason we need this for the password box focus to work

			if ( !string.IsNullOrWhiteSpace( this.textBoxUsername.Text ) )
				this.textBoxPassword.Focus();
		}

		private void OnTextBoxKeyPressed( object sender, KeyPressEventArgs e )
		{
			this.ValidateForm();
		}

		private async void buttonLogin_Click( object sender, EventArgs e )
		{
			this.Enabled          = false;
			this.labelStatus.Text = "Please wait...";

			try
			{
				var username    = this.textBoxUsername.Text;
				var password    = this.textBoxPassword.Text;
				var loginResult = await Task.Run( () => new MLogin().Authenticate( username, password ) );

				if ( loginResult.IsSuccess )
				{
					this.Session      = loginResult.Session;
					this.DialogResult = DialogResult.OK;
					AppConfig.Update( config => config.LastUsername = username );
					return;
				}

				MessageBox.Show( this,
								 $"Login failed: {loginResult.ErrorMessage}",
								 "Login Failed",
								 MessageBoxButtons.OK,
								 MessageBoxIcon.Error );

				this.textBoxPassword.Text = "";
				this.textBoxPassword.Focus();
			}
			finally
			{
				this.Enabled          = true;
				this.labelStatus.Text = string.Empty;
			}
		}
	}
}