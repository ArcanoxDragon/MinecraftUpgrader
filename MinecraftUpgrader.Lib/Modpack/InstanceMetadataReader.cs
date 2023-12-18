using System.IO;
using Newtonsoft.Json;

namespace MinecraftUpgrader.Modpack
{
	public static class InstanceMetadataReader
	{
		public static InstanceMetadata ReadInstanceMetadata( string instancePath )
		{
			var instanceFile = Path.Combine( instancePath, "packMeta.json" );
			var metadataJson  = string.Empty;

			if ( File.Exists( instanceFile ) )
				metadataJson = File.ReadAllText( instanceFile );

			if ( string.IsNullOrEmpty( instanceFile ) )
				return new InstanceMetadata();

			return JsonConvert.DeserializeObject<InstanceMetadata>( metadataJson );
		}
	}
}