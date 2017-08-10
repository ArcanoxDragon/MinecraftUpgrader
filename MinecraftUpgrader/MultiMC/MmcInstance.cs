using MinecraftUpgrader.Config;

namespace MinecraftUpgrader.MultiMC
{
	public class MmcInstance
	{
		[ ConfigProperty ]
		public string InstanceType { get; set; }

		[ ConfigProperty ]
		public string IntendedVersion { get; set; }

		[ ConfigProperty( "name" ) ]
		public string Name { get; set; }

		[ ConfigProperty( "iconKey" ) ]
		public string Icon { get; set; }

		[ ConfigProperty ]
		public bool OverrideMemory { get; set; }

		[ ConfigProperty ]
		public int MinMemAlloc { get; set; }

		[ ConfigProperty ]
		public int MaxMemAlloc { get; set; }
	}
}