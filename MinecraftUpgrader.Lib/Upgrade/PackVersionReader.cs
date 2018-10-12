using System.IO;
using Newtonsoft.Json;

namespace MinecraftUpgrader.Upgrade
{
	public static class PackVersionReader
	{
		public static InstanceVersion ReadPackVersion( string instancePath )
		{
			var instanceFile = Path.Combine( instancePath, "packVersion.json" );
			var versionJson = string.Empty;

			if ( File.Exists( instanceFile ) )
				versionJson = File.ReadAllText( instanceFile );

			if ( string.IsNullOrEmpty( instanceFile ) )
				return new InstanceVersion();

			return JsonConvert.DeserializeObject<InstanceVersion>( versionJson );
		}
	}
}