using System.Collections.Generic;

namespace MinecraftUpgrader.Modpack;

public class PackVersion
{
	public Dictionary<string, PackMod>       Mods               { get; set; }
	public Dictionary<string, Replacement[]> ConfigReplacements { get; set; }
	public Dictionary<string, string>        ExtraFiles         { get; set; }
}