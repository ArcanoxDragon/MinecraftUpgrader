using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.VisualBasic.Devices;
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
		private const int BufferSize = 4 * 1024 * 1024; // 4 MB
		private const string ConfigPath = "http://mc.angeldragons.com/dutchie/pack-upgrade.json";

		private static async Task<PackUpgrade> LoadConfig()
		{
			var json = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
			PackUpgrade config;

			using ( var http = new HttpClient() )
			using ( var str = await http.GetStreamAsync( ConfigPath ) )
			using ( var sr = new StreamReader( str ) )
			using ( var jr = new JsonTextReader( sr ) )
			{
				config = json.Deserialize<PackUpgrade>( jr );
			}

			return config;
		}

		private static async Task SetupPack( MmcConfig mmcConfig, PackUpgrade pack, string destinationFolder, CancellationToken token, ProgressReporter progress )
		{
			string cfgFile = Path.Combine( destinationFolder, "instance.cfg" );
			string patchesDir = Path.Combine( destinationFolder, "patches" );
			string tempDir = Path.Combine( destinationFolder, "temp" );
			string minecraftDir = Path.Combine( destinationFolder, "minecraft" );

			progress?.ReportProgress( "Updating instance configuration..." );

			using ( var fs = File.Open( cfgFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None ) )
			using ( var sr = new StreamReader( fs ) )
			{
				var ci = new ComputerInfo();
				var ramSize = new FileSize( (long) ci.TotalPhysicalMemory );
				var cfg = await ConfigReader.ReadConfig<MmcInstance>( sr );

				cfg.OverrideMemory = true;
				cfg.MinMemAlloc = 1024;
				// Set JVM max memory to 2 GB less than the user's total RAM, at most 12 GB
				cfg.MaxMemAlloc = (int) ( Math.Min( ramSize.GigaBytes - 2, 12 ) * 1024 );

				await ConfigReader.UpdateConfig( cfg, fs );
			}

			progress?.ReportProgress( "Creating instance folder structure..." );

			Directory.CreateDirectory( minecraftDir );
			Directory.CreateDirectory( patchesDir );
			Directory.CreateDirectory( tempDir );

			try
			{
				using ( var web = new WebClient() )
				{
					string downloadTask = "";

					token.Register( web.CancelAsync );
					web.DownloadProgressChanged += ( sender, args ) => {
						var dlSize = new FileSize( args.BytesReceived );
						var totalSize = new FileSize( args.TotalBytesToReceive );
						// ReSharper disable once AccessToModifiedClosure
						progress?.ReportProgress( args.BytesReceived / (double) args.TotalBytesToReceive,
												  $"{downloadTask} ({dlSize.ToString()} / {totalSize.ToString()})" );
					};

					token.ThrowIfCancellationRequested();

					// Download icon
					if ( !Directory.Exists( mmcConfig.IconsFolder ) )
						Directory.CreateDirectory( mmcConfig.IconsFolder );

					downloadTask = "Downloading pack icon...";
					var iconFileName = Path.Combine( mmcConfig.IconsFolder, "dutchie.png" );
					await web.DownloadFileTaskAsync( "http://mc.angeldragons.com/favicon.png", iconFileName );

					// Download server pack contents
					downloadTask = "Downloading base pack contents...";
					var serverPackFileName = Path.Combine( tempDir, "server.zip" );
					await web.DownloadFileTaskAsync( pack.ServerPack, serverPackFileName );

					token.ThrowIfCancellationRequested();
					progress?.ReportProgress( 0, "Extracting base pack contents..." );

					// Extract server pack contents
					using ( var fs = File.Open( serverPackFileName, FileMode.Open, FileAccess.Read, FileShare.Read ) )
					using ( var zip = new ZipFile( fs ) )
					{
						progress?.ReportProgress( "Extracting base pack contents (configs)..." );
						await zip.ExtractDirectory( "config", minecraftDir, false, token, progress );
						progress?.ReportProgress( "Extracting base pack contents (mods)..." );
						await zip.ExtractDirectory( "mods", minecraftDir, false, token, progress );
						progress?.ReportProgress( "Extracting base pack contents (resources)..." );
						await zip.ExtractDirectory( "resources", minecraftDir, false, token, progress );
						progress?.ReportProgress( "Extracting base pack contents (scripts)..." );
						await zip.ExtractDirectory( "scripts", minecraftDir, false, token, progress );
					}

					token.ThrowIfCancellationRequested();

					// Download client pack overrides
					downloadTask = "Downloading client overrides...";
					var clientPackFileName = Path.Combine( tempDir, "client.zip" );
					await web.DownloadFileTaskAsync( pack.ClientPack, clientPackFileName );

					token.ThrowIfCancellationRequested();
					progress?.ReportProgress( 0, "Extracting client overrides..." );

					// Extract client pack contents
					using ( var fs = File.Open( clientPackFileName, FileMode.Open, FileAccess.Read, FileShare.Read ) )
					using ( var zip = new ZipFile( fs ) )
					{
						progress?.ReportProgress( "Extracting client overrides (configs)..." );
						await zip.ExtractDirectory( "overrides/config", minecraftDir, true, token, progress );
						progress?.ReportProgress( "Extracting client overrides (mods)..." );
						await zip.ExtractDirectory( "overrides/mods", minecraftDir, true, token, progress );
						progress?.ReportProgress( "Extracting client overrides (resources)..." );
						await zip.ExtractDirectory( "overrides/resources", minecraftDir, true, token, progress );
						progress?.ReportProgress( "Extracting client overrides (scripts)..." );
						await zip.ExtractDirectory( "overrides/scripts", minecraftDir, true, token, progress );
					}

					// Download patches
					var iPatch = 0;
					foreach ( string patch in pack.Patches )
					{
						token.ThrowIfCancellationRequested();
						downloadTask = $"Downloading patches {++iPatch} of {pack.Patches.Length}...";
						string fileName = Path.Combine( patchesDir, Path.GetFileName( patch ) );
						await web.DownloadFileTaskAsync( patch, fileName );
					}

					// Download additional mods
					var iMod = 0;
					foreach ( string mod in pack.AdditionalMods )
					{
						token.ThrowIfCancellationRequested();

						string fileName;

						var matchCurse = Regex.Match( mod, @"projects/([^/]+)/", RegexOptions.IgnoreCase );
						var matchJar = Regex.Match( mod, @"^(?:.*/)?([^/]+\.jar)", RegexOptions.IgnoreCase );

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
					var modsDir = Path.Combine( minecraftDir, "mods" );
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

		public static async Task NewInstance( MmcConfig mmcConfig, string instName, CancellationToken token, ProgressReporter progress = null )
		{
			var newInstDir = Path.Combine( mmcConfig.InstancesFolder, instName );
			var newInstCfg = Path.Combine( newInstDir, "instance.cfg" );

			if ( Directory.Exists( newInstDir ) )
				throw new InvalidOperationException( "An instance with the specified name already exists" );

			progress?.ReportProgress( "Creating new instance directory..." );

			Directory.CreateDirectory( newInstDir );

			progress?.ReportProgress( "Loading pack metadata from server..." );

			var pack = await LoadConfig();
			var instance = new MmcInstance {
				Name = instName,
				Icon = "dutchie",
				InstanceType = pack.InstanceType,
				IntendedVersion = pack.IntendedVersion
			};

			token.ThrowIfCancellationRequested();
			progress?.ReportProgress( "Creating new instance config file..." );

			using ( var fs = File.Open( newInstCfg, FileMode.Create, FileAccess.Write, FileShare.None ) )
			using ( var sw = new StreamWriter( fs ) )
			{
				await ConfigReader.WriteConfig( instance, sw );
			}

			await SetupPack( mmcConfig, pack, newInstDir, token, progress );
		}

		public static async Task ConvertInstance( MmcConfig mmcConfig, string instPath, CancellationToken token, ProgressReporter progress = null )
		{
			if ( !Directory.Exists( instPath ) )
				throw new InvalidOperationException( "The specified instance folder was not found" );

			var instCfg = Path.Combine( instPath, "instance.cfg" );
			var pack = await LoadConfig();
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

			var minecraftDir = Path.Combine( instPath, "minecraft" );
			var patchesDir = Path.Combine( instPath, "patches" );
			var configDir = Path.Combine( minecraftDir, "config" );
			var modsDir = Path.Combine( minecraftDir, "mods" );
			var resourcesDir = Path.Combine( minecraftDir, "resources" );
			var scriptsDir = Path.Combine( minecraftDir, "scripts" );

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

			await SetupPack( mmcConfig, pack, instPath, token, progress );
		}
	}
}