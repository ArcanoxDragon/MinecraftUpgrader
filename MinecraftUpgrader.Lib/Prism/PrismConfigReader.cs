using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MinecraftUpgrader.Config;

namespace MinecraftUpgrader.Prism;

public static class PrismConfigReader
{
	private const string ConfigFileName = "prismlauncher.cfg";

	public static async Task<PrismConfig> ReadFromPrismFolder(string prismPath)
	{
		var configPath = Path.Combine(prismPath, ConfigFileName);

		if (!File.Exists(configPath))
			throw new FileNotFoundException($"Prism Launcher config file not found at {configPath}.\n\nMake sure you have run Prism Launcher at least once to set it up.");

		await using var fileStream = File.Open(configPath, FileMode.Open, FileAccess.Read);
		using var reader = new StreamReader(fileStream);
		var config = await ConfigReader.ReadConfig<PrismConfig>(reader);

		if (string.IsNullOrWhiteSpace(config.InstancesFolder) || !Path.IsPathRooted(config.InstancesFolder))
			config.InstancesFolder = Path.Combine(prismPath, config.InstancesFolder ?? "instances");

		if (string.IsNullOrWhiteSpace(config.IconsFolder) || !Path.IsPathRooted(config.IconsFolder))
			config.IconsFolder = Path.Combine(prismPath, config.IconsFolder ?? "icons");

		return config;
	}

	public static async Task UpdateConfig(string prismPath, PrismConfig config)
	{
		var configPath = Path.Combine(prismPath, ConfigFileName);

		// Normalize slashes
		config.IconsFolder = config.IconsFolder.Replace("\\", "/");
		config.InstancesFolder = config.InstancesFolder.Replace("\\", "/");
		config.JavaPath = config.JavaPath.Replace("\\", "/");

		await using var fs = File.Open(configPath, FileMode.Open, FileAccess.ReadWrite);

		await ConfigReader.UpdateConfig(config, fs);
	}

	public static async Task<PrismConfig> CreateConfig(string prismPath)
	{
		var defaults = new PrismConfig {
			InstancesFolder = "instances",
			IconsFolder = "icons",
			Language = "en_US",
			MinMemAlloc = 512,
			MaxMemAlloc = 2048,
		};

		await using var stream = File.Open(Path.Combine(prismPath, ConfigFileName), FileMode.Create, FileAccess.Write);
		await using var writer = new StreamWriter(stream);

		await ConfigReader.WriteConfig(defaults, writer);

		return defaults;
	}

	public static async Task<IList<(string name, string path)>> GetInstances(this PrismConfig config)
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
				await using var fileStream = File.Open(instanceCfgPath, FileMode.Open, FileAccess.Read);
				using var reader = new StreamReader(fileStream);
				var instanceCfg = await ConfigReader.ReadConfig<PrismInstance>(reader);

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