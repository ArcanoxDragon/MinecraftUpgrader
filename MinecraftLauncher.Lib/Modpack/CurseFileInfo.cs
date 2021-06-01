using Newtonsoft.Json;

namespace MinecraftLauncher.Modpack
{
	public class CurseFileInfo
	{
		public int    Id             { get; set; }
		public string DisplayName    { get; set; }
		public string FileName       { get; set; }
		public string FileNameOnDisk { get; set; }
		public long   FileLength     { get; set; }
		public int    ReleaseType    { get; set; }
		public int    FileStatus     { get; set; }
		public string DownloadURL    { get; set; }

		[JsonProperty("_Project")]
		public CurseFileProject Project { get; set; }
	}

	public class CurseFileProject
	{
		public string Path        { get; set; }
		public string PackageType { get; set; }
	}
}