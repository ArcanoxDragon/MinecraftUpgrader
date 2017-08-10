namespace MinecraftUpgrader.Upgrade
{
	public class PackUpgrade
	{
		public string InstanceType { get; set; }
		public string IntendedVersion { get; set; }
		public string ServerPack { get; set; }
		public string ClientPack { get; set; }
		public string[] Patches { get; set; }
		public string[] AdditionalMods { get; set; }
		public string[] RemoveMods { get; set; }
	}
}