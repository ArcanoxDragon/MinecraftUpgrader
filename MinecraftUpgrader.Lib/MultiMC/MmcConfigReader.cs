using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MinecraftUpgrader.Config;

namespace MinecraftUpgrader.MultiMC
{
	public static class MmcConfigReader
	{
		public static async Task<MmcConfig> ReadFromMmcFolder(string mmcPath)
		{
			var configPath = Path.Combine(mmcPath, "multimc.cfg");

			if (!File.Exists(configPath))
				throw new FileNotFoundException($"MultiMC config file not found at {configPath}.\n\nMake sure you have run MultiMC at least once to set it up.");

			await using var fs = File.Open(configPath, FileMode.Open, FileAccess.Read);
			using var sr = new StreamReader(fs);
			var config = await ConfigReader.ReadConfig<MmcConfig>(sr);

			if (string.IsNullOrWhiteSpace(config.InstancesFolder) || !Path.IsPathRooted(config.InstancesFolder))
				config.InstancesFolder = Path.Combine(mmcPath, config.InstancesFolder ?? "instances");

			if (string.IsNullOrWhiteSpace(config.IconsFolder) || !Path.IsPathRooted(config.IconsFolder))
				config.IconsFolder = Path.Combine(mmcPath, config.IconsFolder ?? "icons");

			return config;
		}

		public static async Task UpdateConfig(string mmcPath, MmcConfig config)
		{
			var configPath = Path.Combine(mmcPath, "multimc.cfg");

			// Normalize slashes
			config.IconsFolder = config.IconsFolder.Replace("\\", "/");
			config.InstancesFolder = config.InstancesFolder.Replace("\\", "/");
			config.JavaPath = config.JavaPath.Replace("\\", "/");

			await using var fs = File.Open(configPath, FileMode.Open, FileAccess.ReadWrite);

			await ConfigReader.UpdateConfig(config, fs);
		}

		public static async Task<MmcConfig> CreateConfig(string mmcPath)
		{
			var defaults = new MmcConfig {
				AutoUpdate = false,
				InstancesFolder = "instances",
				IconsFolder = "icons",
				Language = "en",
				// TODO: Uncomment the following line if a development MultiMC version is required
				// UpdateChannel   = "develop",
				MinMemAlloc = 512,
				MaxMemAlloc = 1024,
			};

			await using var stream = File.Open(Path.Combine(mmcPath, "multimc.cfg"), FileMode.Create, FileAccess.Write);
			await using var writer = new StreamWriter(stream);

			await ConfigReader.WriteConfig(defaults, writer);

			return defaults;
		}

		public static async Task<IList<(string name, string path)>> GetInstances(this MmcConfig config)
		{
			if (!Directory.Exists(config.InstancesFolder))
				return Enumerable.Empty<(string, string)>().ToList();

			var instances = new List<(string name, string path)>();

			foreach (var directory in Directory.EnumerateDirectories(config.InstancesFolder))
			{
				var instanceCfgPath = Path.Combine(directory, "instance.cfg");

				if (!File.Exists(instanceCfgPath))
					continue;

				try
				{
					await using var fs = File.Open(instanceCfgPath, FileMode.Open, FileAccess.Read);
					using var sr = new StreamReader(fs);
					var instanceCfg = await ConfigReader.ReadConfig<MmcInstance>(sr);

					instances.Add(( instanceCfg.Name, directory ));
				}
				catch (Exception)
				{
					//ignored
				}
			}

			return instances;
		}
	}
}