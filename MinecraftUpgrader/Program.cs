using System;
using System.Windows.Forms;
using MinecraftUpgrader.DI;

namespace MinecraftUpgrader
{
	static class Program
	{
		[ STAThread ]
		private static void Main()
		{
			Services.Configure( upgradeUrl: "https://mc.angeldragons.com" );

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault( false );
			Application.Run( Services.CreateInstance<UpdateForm>() );
		}
	}
}