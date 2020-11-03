using System.IO;
using Newtonsoft.Json;

namespace MinecraftLauncher.Modpack
{
	public static class InstanceMetadataReader
	{
		public static InstanceMetadata ReadInstanceMetadata( string instancePath )
		{
			var instanceFile = Path.Combine( instancePath, "packMeta.json" );

			if ( !File.Exists( instanceFile ) )
				return null;

			var metadataJson = File.ReadAllText( instanceFile );

			return JsonConvert.DeserializeObject<InstanceMetadata>( metadataJson );
		}
	}
}