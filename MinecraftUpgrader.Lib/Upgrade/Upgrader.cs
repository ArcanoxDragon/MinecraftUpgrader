using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using MinecraftUpgrader.Async;
using MinecraftUpgrader.Config;
using MinecraftUpgrader.MultiMC;
using MinecraftUpgrader.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MinecraftUpgrader.Upgrade
{
	public static class Upgrader
	{
		private const int    BufferSize = 4 * 1024 * 1024; // 4 MB
		private const string ConfigPath = "http://mc.angeldragons.com/dutchie/pack-upgrade.json";

		public static async Task<PackUpgrade> LoadConfig()
		{
			var json   = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
			var config = default( PackUpgrade );

			using ( var http = new HttpClient() )
			using ( var str = await http.GetStreamAsync( ConfigPath ) )
			using ( var sr = new StreamReader( str ) )
			using ( var jr = new JsonTextReader( sr ) )
			{
				config = json.Deserialize<PackUpgrade>( jr );
			}

			return config;
		}

		private static async Task SetupPack( MmcConfig mmcConfig, PackUpgrade pack, string destinationFolder, CancellationToken token, ProgressReporter progress, int? maxRamMb = null )
		{
			var versionFile  = Path.Combine( destinationFolder, "packVersion" );
			var cfgFile      = Path.Combine( destinationFolder, "instance.cfg" );
			var tempDir      = Path.Combine( destinationFolder, "temp" );
			var minecraftDir = Path.Combine( destinationFolder, ".minecraft" );

			progress?.ReportProgress( "Updating instance configuration..." );

			using ( var fs = File.Open( cfgFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None ) )
			using ( var sr = new StreamReader( fs ) )
			{
				var cfg = await ConfigReader.ReadConfig<MmcInstance>( sr );

				cfg.OverrideMemory = true;
				cfg.MinMemAlloc    = 1024;
				cfg.MaxMemAlloc    = maxRamMb ?? ( 6 * 1024 );

				await ConfigReader.UpdateConfig( cfg, fs );
			}

			progress?.ReportProgress( "Creating instance folder structure..." );

			File.WriteAllText( versionFile, pack.PackVersion );

			Directory.CreateDirectory( minecraftDir );
			Directory.CreateDirectory( tempDir );

			try
			{
				using ( var web = new WebClient() )
				{
					var downloadTask = "";

					token.Register( web.CancelAsync );
					web.DownloadProgressChanged += ( sender, args ) => {
						var dlSize                 = new FileSize( args.BytesReceived );
						var totalSize              = new FileSize( args.TotalBytesToReceive );
						// ReSharper disable once AccessToModifiedClosure
						progress?.ReportProgress( args.BytesReceived / (double) args.TotalBytesToReceive,
												  $"{downloadTask} ({dlSize.ToString()} / {totalSize.ToString()})" );
					};

					token.ThrowIfCancellationRequested();

					// Download MultiMC pack descriptor
					downloadTask           = "Downloading MultiMC pack descriptor...";
					var packConfigFileName = Path.Combine( destinationFolder, "mmc-pack.json" );
					await web.DownloadFileTaskAsync( pack.MultiMcPack, packConfigFileName );

					// Download icon
					if ( !Directory.Exists( mmcConfig.IconsFolder ) )
						Directory.CreateDirectory( mmcConfig.IconsFolder );

					downloadTask     = "Downloading pack icon...";
					var iconFileName = Path.Combine( mmcConfig.IconsFolder, "dutchie.png" );
					await web.DownloadFileTaskAsync( "http://mc.angeldragons.com/favicon.png", iconFileName );

					// Download server pack contents
					downloadTask           = "Downloading base pack contents...";
					var serverPackFileName = Path.Combine( tempDir, "server.zip" );
					await web.DownloadFileTaskAsync( pack.ServerPack, serverPackFileName );

					token.ThrowIfCancellationRequested();
					progress?.ReportProgress( 0, "Extracting base pack contents..." );

					// Extract server pack contents
					using ( var fs = File.Open( serverPackFileName, FileMode.Open, FileAccess.Read, FileShare.Read ) )
					using ( var zip = new ZipFile( fs ) )
					{
						progress?.ReportProgress( "Extracting base pack contents (configs)..." );
						await zip.ExtractAsync( minecraftDir, new ZipExtractOptions( "config", false, token, progress ) );
						progress?.ReportProgress( "Extracting base pack contents (mods)..." );
						await zip.ExtractAsync( minecraftDir, new ZipExtractOptions( "mods", true, token, progress ) );
						progress?.ReportProgress( "Extracting base pack contents (resources)..." );
						await zip.ExtractAsync( minecraftDir, new ZipExtractOptions( "resources", false, token, progress ) );
						progress?.ReportProgress( "Extracting base pack contents (scripts)..." );
						await zip.ExtractAsync( minecraftDir, new ZipExtractOptions( "scripts", false, token, progress ) );
					}

					token.ThrowIfCancellationRequested();

					// Download client pack overrides
					downloadTask           = "Downloading client overrides...";
					var clientPackFileName = Path.Combine( tempDir, "client.zip" );
					await web.DownloadFileTaskAsync( pack.ClientPack, clientPackFileName );

					token.ThrowIfCancellationRequested();
					progress?.ReportProgress( 0, "Extracting client overrides..." );

					// Extract client pack contents
					using ( var fs = File.Open( clientPackFileName, FileMode.Open, FileAccess.Read, FileShare.Read ) )
					using ( var zip = new ZipFile( fs ) )
					{
						progress?.ReportProgress( "Extracting client overrides (configs)..." );
						await zip.ExtractAsync( minecraftDir, new ZipExtractOptions( "overrides/config", true, token, progress ) );
						progress?.ReportProgress( "Extracting client overrides (mods)..." );
						await zip.ExtractAsync( minecraftDir, new ZipExtractOptions( "overrides/mods", true, token, progress ) );
						progress?.ReportProgress( "Extracting client overrides (resources)..." );
						await zip.ExtractAsync( minecraftDir, new ZipExtractOptions( "overrides/resources", true, token, progress ) );
						progress?.ReportProgress( "Extracting client overrides (scripts)..." );
						await zip.ExtractAsync( minecraftDir, new ZipExtractOptions( "overrides/scripts", true, token, progress ) );
					}

					// Download additional mods
					var iMod = 0;
					foreach ( var mod in pack.AdditionalMods )
					{
						token.ThrowIfCancellationRequested();

						string fileName;

						var matchCurse = Regex.Match( mod, @"projects/([^/]+)/",     RegexOptions.IgnoreCase );
						var matchJar   = Regex.Match( mod, @"^(?:.*/)?([^/]+\.jar)", RegexOptions.IgnoreCase );

						if ( matchCurse.Success )
							fileName = Path.GetFileName( $"{matchCurse.Groups[ 1 ].Value}.jar" );
						else if ( matchJar.Success )
							fileName = matchJar.Groups[ 1 ].Value;
						else // No filename match; skip mod
						{
							iMod++;
							continue;
						}

						var filePath = Path.Combine( minecraftDir, "mods", fileName );
						downloadTask = $"Downloading additional mod {++iMod} of {pack.AdditionalMods.Length}:\n\n" +
									   $"{fileName}";
						await web.DownloadFileTaskAsync( mod, filePath );
					}

					// Remove extraneous mods
					var modsDir  = Path.Combine( minecraftDir, "mods" );
					var modFiles = Directory.EnumerateFiles( modsDir, "*", SearchOption.AllDirectories );
					foreach ( var file in from mod in pack.RemoveMods
										  select modFiles.Where( d => Regex.IsMatch( d, mod ) )
										  into matching
										  from file in matching
										  select file )
					{
						File.Delete( file );
					}
				}
			}
			finally
			{
				Directory.Delete( tempDir, true );
			}
		}

		public static async Task NewInstance( MmcConfig mmcConfig, string instName, CancellationToken token, ProgressReporter progress = null, int? maxRamMb = null )
		{
			var newInstDir = Path.Combine( mmcConfig.InstancesFolder, instName );
			var newInstCfg = Path.Combine( newInstDir,                "instance.cfg" );

			if ( Directory.Exists( newInstDir ) )
				throw new InvalidOperationException( "An instance with the specified name already exists" );

			progress?.ReportProgress( "Creating new instance directory..." );

			Directory.CreateDirectory( newInstDir );

			progress?.ReportProgress( "Loading pack metadata from server..." );

			var pack         = await LoadConfig();
			var instance     = new MmcInstance {
				Name            = instName,
				Icon            = "dutchie",
				InstanceType    = pack.InstanceType,
				IntendedVersion = pack.IntendedVersion,
				MCLaunchMethod  = "LauncherPart"
			};

			token.ThrowIfCancellationRequested();
			progress?.ReportProgress( "Creating new instance config file..." );

			using ( var fs = File.Open( newInstCfg, FileMode.Create, FileAccess.Write, FileShare.None ) )
			using ( var sw = new StreamWriter( fs ) )
			{
				await ConfigReader.WriteConfig( instance, sw );
			}

			await SetupPack( mmcConfig, pack, newInstDir, token, progress, maxRamMb );
		}

		public static async Task ConvertInstance( MmcConfig mmcConfig, string instPath, CancellationToken token, ProgressReporter progress = null, int? maxRamMb = null )
		{
			if ( !Directory.Exists( instPath ) )
				throw new InvalidOperationException( "The specified instance folder was not found" );

			if ( Directory.Exists( Path.Combine( instPath, "minecraft" ) ) || !Directory.Exists( Path.Combine( instPath, ".minecraft" ) ) )
				throw new InvalidOperationException( "The existing instance is using the old MultiMC pack format." +
													 "This installer cannot upgrade an instance to the new MultiMC pack format.\n\n" +
													 "Please use the \"New Instance\" option to create a new MultiMC instance." );

			var         instCfg = Path.Combine( instPath, "instance.cfg" );
			var         pack    = await LoadConfig();
			MmcInstance instance;

			using ( var fs = File.Open( instCfg, FileMode.Open, FileAccess.Read, FileShare.Read ) )
			using ( var sr = new StreamReader( fs ) )
			{
				instance = await ConfigReader.ReadConfig<MmcInstance>( sr );
			}

			token.ThrowIfCancellationRequested();
			if ( instance.InstanceType != pack.InstanceType || instance.IntendedVersion != pack.IntendedVersion )
				throw new InvalidOperationException( "The existing instance is set up for a different version of Minecraft " +
													 "than the target instance. The upgrader cannot convert an existing instance " +
													 "to a different Minecraft version.\n\n" +
													 "Please create a new instance, or manually upgrade the existing instance to " +
													 $"Minecraft {pack.IntendedVersion}" );

			var minecraftDir = Path.Combine( instPath,     ".minecraft" );
			var patchesDir   = Path.Combine( instPath,     "patches" );
			var configDir    = Path.Combine( minecraftDir, "config" );
			var modsDir      = Path.Combine( minecraftDir, "mods" );
			var resourcesDir = Path.Combine( minecraftDir, "resources" );
			var scriptsDir   = Path.Combine( minecraftDir, "scripts" );

			if ( Directory.Exists( patchesDir ) )
				Directory.Delete( patchesDir, true );
			if ( Directory.Exists( configDir ) )
				Directory.Delete( configDir, true );
			if ( Directory.Exists( modsDir ) )
				Directory.Delete( modsDir, true );
			if ( Directory.Exists( resourcesDir ) )
				Directory.Delete( resourcesDir, true );
			if ( Directory.Exists( scriptsDir ) )
				Directory.Delete( scriptsDir, true );

			await SetupPack( mmcConfig, pack, instPath, token, progress, maxRamMb );
		}
	}
}