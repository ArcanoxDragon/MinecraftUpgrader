using System.Collections.Generic;

namespace MinecraftUpgrader.Modpack;

public class CursePackManifest
{
	public string Name            { get; set; }
	public string Version         { get; set; }
	public string Author          { get; set; }
	public string ManifestType    { get; set; }
	public int    ManifestVersion { get; set; }

	public List<CursePackManifestFile> Files { get; set; }
}

public class CursePackManifestFile
{
	public int  ProjectID { get; set; }
	public int  FileID    { get; set; }
	public bool Required  { get; set; }
}