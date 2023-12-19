using System.Diagnostics;
using System.Net;
using MinecraftUpgrader.DI;
using MinecraftUpgrader.Extensions;

namespace MinecraftUpgrader;

internal static class Program
{
	private static readonly string LF      = Environment.NewLine;
	private static readonly object LogLock = new();

	private static bool InFirstChanceExceptionHandler;

	[STAThread]
	private static void Main()
	{
		// Ensure log directory exists
		var logFileDirectory = Path.GetDirectoryName(GetLogPath());

		if (!Directory.Exists(logFileDirectory))
		{
			try
			{
				Directory.CreateDirectory(logFileDirectory);
			}
			catch
			{
				Debug.WriteLine($"Could not create log file directory at \"{logFileDirectory}\"!");
			}
		}

		AppDomain.CurrentDomain.FirstChanceException += (_, args) => {
			lock (LogLock)
			{
				try
				{
					if (InFirstChanceExceptionHandler)
						return;

					InFirstChanceExceptionHandler = true;

					var logPath = GetLogPath();

					File.AppendAllText(logPath, $"=== Exception occurred at {DateTime.Now:yyyy-MM-dd hh:mm:ss tt} ==={LF}");
					File.AppendAllText(logPath, $"{args.Exception}{LF}{LF}");
				}
				finally
				{
					InFirstChanceExceptionHandler = false;
				}
			}
		};

		AppDomain.CurrentDomain.UnhandledException += (_, args) => {
			MessageBox.Show($"Error: {( args.ExceptionObject as Exception )?.GetFriendlyMessage() ?? "Unknown"}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		};

		// Configure dependency injection
		Services.Configure(upgradeUrl: "https://mc.arcanox.me");

		// Configure web clients
		ServicePointManager.Expect100Continue = true;
		ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

		ApplicationConfiguration.Initialize();
		Application.Run(Services.CreateInstance<UpdateForm>());
	}

	private static string GetLogPath() => Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
		".mcupgrader",
		"log.txt"
	);
}