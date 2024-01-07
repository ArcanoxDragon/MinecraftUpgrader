using System.Text.Json.Serialization;

namespace MinecraftUpgrader.Prism;

public class PrismLibrary
{
	[JsonPropertyName("name")]
	public string Name { get; set; }

	[JsonPropertyName("url")]
	public string Url { get; set; }

	[JsonPropertyName("MMC-hint")]
	public string MmcHint { get; set; }
}