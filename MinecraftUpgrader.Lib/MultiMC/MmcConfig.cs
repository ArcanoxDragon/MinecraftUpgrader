using MinecraftUpgrader.Config;

namespace MinecraftUpgrader.MultiMC
{
	public class MmcConfig
	{
		[ ConfigProperty( "InstanceDir" ) ]
		public string InstancesFolder { get; set; }

		[ ConfigProperty( "IconsDir" ) ]
		public string IconsFolder { get; set; }

		[ ConfigProperty ]
		public string JavaPath { get; set; }

		[ ConfigProperty ]
		public string JavaVersion { get; set; }

		[ConfigProperty]
		public string UpdateChannel { get; set; }
	}
}