using Newtonsoft.Json;

namespace MinecraftUpgrader.MultiMC
{
	public class MmcLibrary
	{
		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("url")]
		public string Url { get; set; }

		[JsonProperty("MMC-hint")]
		public string MmcHint { get; set; }
	}
}