using System;
using System.IO;
using Newtonsoft.Json;

namespace MinecraftUpgrader.Config
{
	public interface IAppConfig
	{
		string JavaPath      { get; }
		string JavaVersion   { get; }
		string LastPrismPath { get; }
		string LastInstance  { get; }
	}

	// TODO: Move out of Lib project?
	public class AppConfig : IAppConfig
	{
		#region Static

		private static AppConfig CachedInstance;

		public static IAppConfig Get() => GetInternal();

		public static void Update(Action<AppConfig> updateAction)
		{
			var config = GetInternal();

			updateAction(config);

			var configPath = GetConfigPath();
			var configJson = JsonConvert.SerializeObject(config);

			File.WriteAllText(configPath, configJson);
		}

		private static AppConfig GetInternal()
		{
			if (CachedInstance != null)
				return CachedInstance;

			var configPath = GetConfigPath();
			var configFolder = Path.GetDirectoryName(configPath);

			if (!Directory.Exists(configFolder))
				Directory.CreateDirectory(configFolder);

			if (File.Exists(configPath))
			{
				var configJson = File.ReadAllText(configPath);
				var appConfig = JsonConvert.DeserializeObject<AppConfig>(configJson);

				CachedInstance = appConfig;
			}
			else
			{
				File.WriteAllText(configPath, "{}");

				CachedInstance = new AppConfig();
			}

			return CachedInstance;
		}

		private static string GetConfigPath() => Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
			".mcarcanox",
			"config.json"
		);

		#endregion

		public string JavaPath      { get; set; }
		public string JavaVersion   { get; set; }
		public string LastPrismPath { get; set; }
		public string LastInstance  { get; set; }
	}
}