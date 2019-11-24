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