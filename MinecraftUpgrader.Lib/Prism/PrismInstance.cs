using MinecraftUpgrader.Config;

namespace MinecraftUpgrader.Prism
{
	public class PrismInstance
	{
		public const string CurrentConfigVersion = "1.2";

		[ConfigProperty]
		public string ConfigVersion { get; set; } = CurrentConfigVersion;

		[ConfigProperty]
		public string InstanceType { get; set; }

		[ConfigProperty("name")]
		public string Name { get; set; }

		[ConfigProperty("iconKey")]
		public string Icon { get; set; }

		[ConfigProperty]
		public bool OverrideMemory { get; set; }

		[ConfigProperty]
		public bool OverrideJavaArgs { get; set; }

		[ConfigProperty]
		public int MinMemAlloc { get; set; }

		[ConfigProperty]
		public int MaxMemAlloc { get; set; }

		[ConfigProperty]
		public string JvmArgs { get; set; }
	}
}