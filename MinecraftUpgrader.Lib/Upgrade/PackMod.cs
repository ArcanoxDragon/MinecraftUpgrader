namespace MinecraftUpgrader.Upgrade
{
	public class PackMod
	{
		public long?  FileId        { get; set; }
		public bool   RemoveOld     { get; set; }
		public string RemovePattern { get; set; }
	}
}