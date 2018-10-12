using System.Collections.Generic;

namespace MinecraftUpgrader.Upgrade
{
	public class PackUpgrade
	{
		public string InstanceType    { get; set; }
		public string IntendedVersion { get; set; }
		public string ServerPack      { get; set; }
		public string ClientPack      { get; set; }
		public string MultiMcPack     { get; set; }
		public string CurrentVersion  { get; set; }

		public Dictionary<string, PackVersion> Versions { get; set; }
	}
}