using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MinecraftUpgrader.Prism
{
	public class PrismPatchesFile
	{
		[JsonProperty("uid")]
		public string Uid { get; set; }

		[JsonProperty("releaseTime")]
		public DateTime ReleaseTime { get; set; }

		[JsonProperty("time")]
		public DateTime Time { get; set; }

		[JsonProperty("mainClass")]
		public string MainClass { get; set; }

		[JsonProperty("libraries")]
		public List<PrismLibrary> Libraries { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("version")]
		public string Version { get; set; }

		[JsonProperty("+tweakers")]
		public List<string> Tweakers { get; set; }
	}
}