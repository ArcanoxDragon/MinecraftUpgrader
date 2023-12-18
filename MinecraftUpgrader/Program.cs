﻿using System;
using System.IO;
using System.Net;
using System.Windows.Forms;
using MinecraftUpgrader.DI;

namespace MinecraftUpgrader
{
	static class Program
	{
		private static readonly string LF      = Environment.NewLine;
		private static readonly object LogLock = new();

		[STAThread]
		private static void Main()
		{
			AppDomain.CurrentDomain.FirstChanceException += (sender, args) => {
				try
				{
					lock (LogLock)
					{
						File.AppendAllText(GetLogPath(), $"=== Exception occurred at {DateTime.Now:yyyy-MM-dd hh:mm:ss tt} ==={LF}");
						File.AppendAllText(GetLogPath(), $"{args.Exception}{LF}{LF}");
					}
				}
				catch
				{
					// Ignored
				}
			};

			AppDomain.CurrentDomain.UnhandledException += (sender, args) => {
				MessageBox.Show($"Error: {( args.ExceptionObject as Exception )?.Message ?? "Unknown"}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			};

			// Configure dependency injection
			Services.Configure(upgradeUrl: "https://mc.arcanox.me");

			// Configure web clients
			ServicePointManager.Expect100Continue = true;
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(Services.CreateInstance<UpdateForm>());
		}

		private static string GetLogPath() => Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
			".mcupgrader",
			"log.txt"
		);
	}
}