using System.IO;
using System.Text.Json;

namespace MinecraftUpgrader.Modpack;

public static class InstanceMetadataReader
{
	private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

	public static InstanceMetadata ReadInstanceMetadata(string instancePath)
	{
		var instanceFile = Path.Combine(instancePath, "packMeta.json");
		var metadataJson = string.Empty;

		if (File.Exists(instanceFile))
			metadataJson = File.ReadAllText(instanceFile);

		if (string.IsNullOrEmpty(metadataJson))
			return new InstanceMetadata();

		return JsonSerializer.Deserialize<InstanceMetadata>(metadataJson, JsonSerializerOptions);
	}
}