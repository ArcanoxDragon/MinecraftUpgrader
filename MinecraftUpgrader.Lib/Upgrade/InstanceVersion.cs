namespace MinecraftUpgrader.Upgrade
{
	public class InstanceVersion
	{
		public const int CurrentFileVersion = 2;

		public string Version             { get; set; } = "0.0.0";
		public string BuiltFromServerPack { get; set; }
		public int    FileVersion         { get; set; } = CurrentFileVersion;
	}
}