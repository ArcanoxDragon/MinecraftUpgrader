using System.Collections.Generic;

namespace MinecraftLauncher.Modpack
{
	public class PackMetadata
	{
		public string DisplayName              { get; set; }
		public string IntendedMinecraftVersion { get; set; }
		public string RequiredForgeVersion     { get; set; }
		public string ServerPack               { get; set; }
		public string ClientPack               { get; set; }
		public string CurrentVersion           { get; set; }
		public bool   VerifyServerPackMd5      { get; set; }

		public List<string> ClientOverrideFolders   { get; set; }
		public List<string> AdditionalBasePackFiles { get; set; }

		public Dictionary<string, PackVersion> Versions { get; set; }
	}
}