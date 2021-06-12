using System;
using System.IO;
using Newtonsoft.Json;

namespace MinecraftLauncher.Config
{
	public interface IAppConfig
	{
		string  JavaPath     { get; }
		string  LastUsername { get; }
		double? MaxRamMb     { get; }
		bool    VrEnabled    { get; }
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

		private static AppConfig GetInternal(bool tryOld = true)
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
				var oldConfigPath = GetOldConfigPath();

				if (tryOld && File.Exists(oldConfigPath))
				{
					File.Copy(oldConfigPath, configPath);

					return GetInternal(tryOld: false);
				}

				File.WriteAllText(configPath, "{}");

				CachedInstance = new AppConfig();
			}

			return CachedInstance;
		}

		// TODO: Remove
		private static string GetOldConfigPath() => Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
			".mcupgrader",
			"config.json"
		);

		private static string GetConfigPath() => Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
			".mcarcanox",
			"config.json"
		);

		#endregion

		public string  JavaPath     { get; set; }
		public string  LastUsername { get; set; }
		public double? MaxRamMb     { get; set; }
		public bool    VrEnabled    { get; set; }
	}
}