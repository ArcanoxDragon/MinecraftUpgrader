using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MinecraftUpgrader.Prism;

public class PrismPatchesFile
{
	[JsonPropertyName("uid")]
	public string Uid { get; set; }

	[JsonPropertyName("releaseTime")]
	public DateTime ReleaseTime { get; set; }

	[JsonPropertyName("time")]
	public DateTime Time { get; set; }

	[JsonPropertyName("mainClass")]
	public string MainClass { get; set; }

	[JsonPropertyName("libraries")]
	public List<PrismLibrary> Libraries { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; }

	[JsonPropertyName("version")]
	public string Version { get; set; }

	[JsonPropertyName("+tweakers")]
	public List<string> Tweakers { get; set; }
}