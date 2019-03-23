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
			Services.Configure( upgradeUrl: "https://dutchies.arcanox.me" );

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault( false );
			Application.Run( Services.CreateInstance<UpdateForm>() );
		}
	}
}