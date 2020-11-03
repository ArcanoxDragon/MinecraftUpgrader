namespace MinecraftLauncher.Modpack
{
	public class InstanceMetadata
	{
		public const int CurrentFileVersion = 3;

		public string Version                 { get; set; } = "0.0.0";
		public string BuiltFromServerPack     { get; set; }
		public string BuiltFromServerPackMd5  { get; set; }
		public string CurrentMinecraftVersion { get; set; }
		public string CurrentForgeVersion     { get; set; }
		public string CurrentLaunchVersion    { get; set; }
		public int    FileVersion             { get; set; } = CurrentFileVersion;
		public bool   VrEnabled               { get; set; }
	}
}