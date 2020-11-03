using System;
using System.IO;
using System.Net;
using System.Windows.Forms;
using MinecraftLauncher.DI;
using MinecraftLauncher.Utility;

namespace MinecraftLauncher
{
	static class Program
	{
		private static readonly string LF = Environment.NewLine;

		[ STAThread ]
		private static void Main()
		{
			var consoleLogWriter = new ConsoleLogWriter( GetLogPath(), Console.Out );

			Console.SetOut( consoleLogWriter );

			AppDomain.CurrentDomain.FirstChanceException += ( sender, args ) => {
				Console.WriteLine( $"=== Exception occurred at {DateTime.Now:yyyy-MM-dd hh:mm:ss tt} ==={LF}" );
				Console.WriteLine( $"{args.Exception}{LF}{LF}" );
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
				Console.WriteLine( $"An exception occurred during program startup: {ex}{LF}{LF}" );
				Application.Exit();
			}
		}

		private static string GetLogPath() => Path.Combine(
			Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ),
			".mcarcanox",
			"log.txt"
		);
	}
}