﻿using System.Collections.Generic;

namespace MinecraftUpgrader.Upgrade
{
	public class PackVersion
	{
		public Dictionary<string, PackMod>       Mods               { get; set; }
		public Dictionary<string, Replacement[]> ConfigReplacements { get; set; }
	}
}