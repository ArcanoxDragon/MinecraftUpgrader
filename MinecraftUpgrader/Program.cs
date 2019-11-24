using System;
using System.Net;
using System.Windows.Forms;
using MinecraftUpgrader.DI;

namespace MinecraftUpgrader
{
	static class Program
	{
		[ STAThread ]
		private static void Main()
		{
			AppDomain.CurrentDomain.UnhandledException += ( sender, args ) => {
				MessageBox.Show( $"Error: {( args.ExceptionObject as Exception )?.Message ?? "Unknown"}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
			};

			// Configure dependency injection
			Services.Configure( upgradeUrl: "https://mc.arcanox.me" );

			// Configure web clients
			ServicePointManager.Expect100Continue = true;
			ServicePointManager.SecurityProtocol  = SecurityProtocolType.Tls12;

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault( false );
			Application.Run( Services.CreateInstance<UpdateForm>() );
		}
	}
}