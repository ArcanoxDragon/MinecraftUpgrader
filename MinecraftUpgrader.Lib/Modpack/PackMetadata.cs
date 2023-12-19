using System.Collections.Generic;

namespace MinecraftUpgrader.Modpack;

public class PackMetadata
{
	public string InstanceType             { get; set; }
	public string IntendedMinecraftVersion { get; set; }
	public string ServerPack               { get; set; }
	public string ClientPack               { get; set; }
	public string PrismPack                { get; set; }
	public string CurrentVersion           { get; set; }
	public bool   VerifyServerPackMd5      { get; set; }
	public bool   SupportsVR               { get; set; }

	public List<string> ClientOverrideFolders   { get; set; }
	public List<string> AdditionalBasePackFiles { get; set; }

	public Dictionary<string, PackVersion> Versions { get; set; }
}