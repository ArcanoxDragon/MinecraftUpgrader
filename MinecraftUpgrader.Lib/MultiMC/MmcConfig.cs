using MinecraftUpgrader.Config;

namespace MinecraftUpgrader.MultiMC
{
	public class MmcConfig
	{
		[ConfigProperty("InstanceDir")] public string InstancesFolder { get; set; }
		[ConfigProperty("IconsDir")]    public string IconsFolder     { get; set; }
		[ConfigProperty]                public string JavaPath        { get; set; }
		[ConfigProperty]                public string JavaVersion     { get; set; }
		[ConfigProperty]                public string UpdateChannel   { get; set; }
		[ConfigProperty]                public bool   AutoUpdate      { get; set; }
		[ConfigProperty]                public string Language        { get; set; }
		[ConfigProperty]                public int    MaxMemAlloc     { get; set; }
		[ConfigProperty]                public int    MinMemAlloc     { get; set; }
	}
}