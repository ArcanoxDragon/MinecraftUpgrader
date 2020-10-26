using System;
using System.IO;
using System.Net;
using System.Windows.Forms;
using MinecraftLauncher.DI;
using MinecraftLauncher.Modpack;
using MinecraftLauncher.Utility;

namespace MinecraftLauncher
{
	static class Program
	{
		private static readonly string LF      = Environment.NewLine;
		private static readonly object LogLock = new object();

		[ STAThread ]
		private static void Main()
		{
			AppDomain.CurrentDomain.FirstChanceException += ( sender, args ) => {
				try
				{
					lock ( LogLock )
					{
						File.AppendAllText( GetLogPath(), $"=== Exception occurred at {DateTime.Now:yyyy-MM-dd hh:mm:ss tt} ==={LF}" );
						File.AppendAllText( GetLogPath(), $"{args.Exception}{LF}{LF}" );
					}
				}
				catch
				{
					// Ignored
				}
			};

			AppDomain.CurrentDomain.UnhandledException += ( sender, args ) => {
				MessageBox.Show( $"Error: {( args.ExceptionObject as Exception )?.Message ?? "Unknown"}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
			};

			// Configure dependency injection
			Services.Configure( modPackUrl: "https://mc.arcanox.me" );

			// Configure web clients
			ServicePointManager.Expect100Continue = true;
			ServicePointManager.SecurityProtocol  = SecurityProtocolType.Tls12;

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault( false );

			// Hook up a method that will run as soon as the application first goes idle
			Application.Idle += ApplicationOnEnterIdle;

			Application.Run();
		}

		private static async void ApplicationOnEnterIdle( object sender, EventArgs e )
		{
			// Don't want to run more than once
			Application.Idle -= ApplicationOnEnterIdle;

			try
			{
				var updateStatus = await Services.CreateInstance<UpdateForm>().CheckForUpdatesAsync();

				if ( updateStatus != UpdateForm.Result.NoUpdateNeeded )
				{
					Application.Exit();
					return;
				}

				var javaCheckPassed = await JavaChecker.CheckJavaAsync();

				if ( !javaCheckPassed )
				{
					Application.Exit();
					return;
				}

				var mainForm = Services.CreateInstance<MainForm>();

				mainForm.Closing += ( o, args ) => Application.Exit();
				mainForm.Show();
			}
			catch ( Exception ex )
			{
				File.AppendAllText( GetLogPath(), $"An exception occurred during program startup: {ex}{LF}{LF}" );
				Application.Exit();
			}
		}

		private static string GetLogPath() => Path.Combine(
			Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ),
			".mcupgrader",
			"log.txt"
		);
	}
}