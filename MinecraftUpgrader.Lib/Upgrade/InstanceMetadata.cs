namespace MinecraftUpgrader.Upgrade
{
	public class InstanceMetadata
	{
		public const int CurrentFileVersion = 3;

		public string Version                  { get; set; } = "0.0.0";
		public string BuiltFromServerPack      { get; set; }
		public string IntendedMinecraftVersion { get; set; }
		public int    FileVersion              { get; set; } = CurrentFileVersion;
		public bool   VrEnabled                { get; set; }
	}
}