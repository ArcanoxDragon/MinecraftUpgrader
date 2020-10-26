using System.Collections.Generic;

namespace MinecraftLauncher.Modpack
{
	public class PackVersion
	{
		public Dictionary<string, PackMod>       Mods               { get; set; }
		public Dictionary<string, Replacement[]> ConfigReplacements { get; set; }
		public Dictionary<string, string>        ExtraFiles         { get; set; }
	}
}