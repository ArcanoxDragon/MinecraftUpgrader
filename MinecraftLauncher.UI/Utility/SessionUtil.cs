using System.Threading.Tasks;
using System.Windows.Forms;
using CmlLib.Core.Auth;
using MinecraftLauncher.DI;

namespace MinecraftLauncher.Utility
{
	public static class SessionUtil
	{
		public static async Task<MSession> GetSessionAsync( IWin32Window fromWindow )
		{
			var login   = new MLogin();
			var session = login.ReadSessionCache();

			bool CheckSession() => session?.CheckIsValid() == true;

			if ( CheckSession() /* Try auto-login if a valid session existed in the cache */ )
			{
				var loginResult = await login.TryAutoLoginAsync( session );

				session = loginResult.IsSuccess ? loginResult.Session : null;
			}

			if ( !CheckSession() /* Try manual login */ )
			{
				var loginForm   = Services.CreateInstance<LoginForm>();
				var loginResult = loginForm.ShowDialog( fromWindow );

				if ( loginResult != DialogResult.OK )
					return null;

				session = loginForm.Session;
			}

			if ( !CheckSession() /* Should never get here */ )
			{
				MessageBox.Show( fromWindow,
								 "Could not start a valid Mojang user session for Minecraft.",
								 "Session Failure",
								 MessageBoxButtons.OK,
								 MessageBoxIcon.Error );

				return null;
			}

			return session;
		}
	}
}