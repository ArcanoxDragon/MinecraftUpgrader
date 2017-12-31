namespace MinecraftUpgrader
{
	public static class Constants
	{
		public static class Registry
		{
			public const string SubKey       = "Software\\ArcanoxDragon\\MinecraftUpgrader\\";
			public const string RootKey      = "HKEY_CURRENT_USER\\" + SubKey;
			public const string LastMmcPath  = "LastMmcPath";
			public const string LastInstance = "LastInstName";
			public const string JavaPath     = "DetectedJavaPath";
			public const string JavaVersion  = "DetectedJavaVersion";
		}

		public static class Paths
		{
			public const string MultiMcDownload = "https://files.multimc.org/downloads/mmc-develop-win32.zip";
		}
	}
}