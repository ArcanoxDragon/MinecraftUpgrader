using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Options;
using MinecraftUpgrader.Async;
using MinecraftUpgrader.Config;
using MinecraftUpgrader.Extensions;
using MinecraftUpgrader.MultiMC;
using MinecraftUpgrader.Options;
using MinecraftUpgrader.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Semver;

namespace MinecraftUpgrader.Upgrade
{
	public class Upgrader
	{
		private const int    BufferSize = 4 * 1024 * 1024; // 4 MB
		private const string ConfigPath = "modpack/pack-upgrade.json";

		private readonly UpgraderOptions options;

		public Upgrader( IOptions<UpgraderOptions> options )
		{
			this.options = options.Value;
		}

		public async Task<PackUpgrade> LoadConfig()
		{
			var json   = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
			var config = default( PackUpgrade );

			using ( var http = new HttpClient() )
			using ( var str = await http.GetStreamAsync( $"{this.options.UpgradeUrl}/{ConfigPath}" ) )
			using ( var sr = new StreamReader( str ) )
			using ( var jr = new JsonTextReader( sr ) )
			{
				config = json.Deserialize<PackUpgrade>( jr );
			}

			return config;
		}

		private async Task SetupPack( MmcConfig mmcConfig, PackUpgrade pack, string destinationFolder, bool forceRebuild, CancellationToken token, ProgressReporter progress, int? maxRamMb = null )
		{
			var versionFile  = Path.Combine( destinationFolder, "packVersion.json" );
			var cfgFile      = Path.Combine( destinationFolder, "instance.cfg" );
			var tempDir      = Path.Combine( destinationFolder, "temp" );
			var minecraftDir = Path.Combine( destinationFolder, ".minecraft" );
			var modsDir      = Path.Combine( minecraftDir,      "mods" );

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

			var currentVersion    = "0.0.0";
			var currentServerPack = default( string );

			// If forceRebuild is set, we don't even bother checking the current version.
			// Just redo everything.
			if ( !forceRebuild && File.Exists( versionFile ) )
			{
				var currentInstanceVersion = JsonConvert.DeserializeObject<InstanceVersion>( File.ReadAllText( versionFile ) );

				if ( currentInstanceVersion.FileVersion == InstanceVersion.CurrentFileVersion )
				{
					currentVersion    = currentInstanceVersion.Version;
					currentServerPack = currentInstanceVersion.BuiltFromServerPack;
				}
			}

			var instanceVersion = new InstanceVersion {
				FileVersion         = InstanceVersion.CurrentFileVersion,
				Version             = pack.CurrentVersion,
				BuiltFromServerPack = pack.ServerPack
			};

			Directory.CreateDirectory( minecraftDir );
			Directory.CreateDirectory( tempDir );

			try
			{
				using ( var web = new WebClient() )
				{
					var downloadTask = "";

					token.Register( web.CancelAsync );
					web.DownloadProgressChanged += ( sender, args ) => {
						var dlSize    = new FileSize( args.BytesReceived );
						var totalSize = new FileSize( args.TotalBytesToReceive );
						// ReSharper disable once AccessToModifiedClosure
						progress?.ReportProgress( args.BytesReceived / (double) args.TotalBytesToReceive,
												  $"{downloadTask} ({dlSize.ToString()} / {totalSize.ToString()})" );
					};

					token.ThrowIfCancellationRequested();

					// Download MultiMC pack descriptor
					downloadTask = "Downloading MultiMC pack descriptor...";
					var packConfigFileName = Path.Combine( destinationFolder, "mmc-pack.json" );
					await web.DownloadFileTaskAsync( pack.MultiMcPack, packConfigFileName );

					// Download icon
					if ( !Directory.Exists( mmcConfig.IconsFolder ) )
						Directory.CreateDirectory( mmcConfig.IconsFolder );

					downloadTask = "Downloading pack icon...";
					var iconFileName = Path.Combine( mmcConfig.IconsFolder, "dutchie.png" );
					await web.DownloadFileTaskAsync( "http://mc.angeldragons.com/favicon.png", iconFileName );

					// Only download base pack and client overrides if the version is 0 (not installed or old file version),
					// or if the forceRebuild flag is set
					if ( currentVersion == "0.0.0" || currentServerPack != pack.ServerPack )
					{
						// Set this again in case we got here from "currentServerPack" not matching
						// This forces all versions to install later on in this method even if the
						// version strings line up, as a different server pack means a different
						// update path regardless.
						currentVersion = "0.0.0";

						// Clear out old files if present since we're downloading the base pack fresh
						var patchesDir   = Path.Combine( destinationFolder, "patches" );
						var configDir    = Path.Combine( minecraftDir,      "config" );
						var resourcesDir = Path.Combine( minecraftDir,      "resources" );
						var scriptsDir   = Path.Combine( minecraftDir,      "scripts" );

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
							await zip.TryExtractAsync( minecraftDir, new ZipExtractOptions( "overrides/config", true, token, progress ) );
							progress?.ReportProgress( "Extracting client overrides (mods)..." );
							await zip.TryExtractAsync( minecraftDir, new ZipExtractOptions( "overrides/mods", true, token, progress ) );
							progress?.ReportProgress( "Extracting client overrides (resources)..." );
							await zip.TryExtractAsync( minecraftDir, new ZipExtractOptions( "overrides/resources", true, token, progress ) );
							progress?.ReportProgress( "Extracting client overrides (scripts)..." );
							await zip.TryExtractAsync( minecraftDir, new ZipExtractOptions( "overrides/scripts", true, token, progress ) );
						}
					}

					// Process version upgrades
					var modFiles = Directory.EnumerateFiles( modsDir, "*", SearchOption.AllDirectories ).ToList();

					foreach ( var versionKey in pack.Versions.Keys )
					{
						if ( !SemVersion.TryParse( versionKey, out _ ) )
							throw new InvalidOperationException( $"Invalid version number in mod pack definition: {versionKey}" );
					}

					foreach ( var (versionKey, version) in pack.Versions.OrderBy( pair => SemVersion.Parse( pair.Key ) ) )
					{
						var curSemVersion  = SemVersion.Parse( currentVersion );
						var thisSemVersion = SemVersion.Parse( versionKey );

						if ( thisSemVersion <= curSemVersion )
							// This version is already installed; don't bother
							continue;

						var versionTask = $"Processing version {versionKey}...";

						progress?.ReportProgress( versionTask );

						if ( version.Mods != null )
						{
							var currentMod = 0;
							foreach ( var (modId, mod) in version.Mods )
							{
								var modTask = $"{versionTask}\nProcessing mod {modId} ({++currentMod} / {version.Mods.Count})...";

								progress?.ReportProgress( modTask );

								// Delete old version if necessary
								if ( mod.RemoveOld || !string.IsNullOrEmpty( mod.RemovePattern ) )
								{
									progress?.ReportProgress( $"{modTask}\n" +
															  $"Deleting old versions..." );

									var deletePattern = mod.RemovePattern ?? $"{modId}.*";
									var toDelete      = modFiles.Where( file => Regex.IsMatch( file, deletePattern ) ).ToList();

									foreach ( var file in toDelete )
										File.Delete( file );
								}

								// Install new version if necessary
								if ( mod.FileId.HasValue )
								{
									var fileName = $"{modId}.jar";
									var filePath = Path.Combine( minecraftDir, "mods", fileName );
									// TODO: Allow non-CurseForge URLs
									var archiveUrl = $"https://minecraft.curseforge.com/projects/{modId}/files/{mod.FileId}/download";

									downloadTask = $"{modTask}\n" +
												   $"Downloading mod archive: {fileName}";

									await web.DownloadFileTaskAsync( archiveUrl, filePath );
								}
							}
						}

						if ( version.ConfigReplacements != null )
						{
							// Apply config replacements
							progress?.ReportProgress( -1, "Applying config replacements..." );
							var configDir = Path.Combine( minecraftDir, "config" );
							foreach ( var (configPath, replacements) in version.ConfigReplacements )
							{
								var configFilePath = Path.Combine( configDir, configPath );

								if ( File.Exists( configFilePath ) )
								{
									var configFileContents = File.ReadAllText( configFilePath, Encoding.UTF8 );

									foreach ( var replacement in replacements )
										configFileContents = Regex.Replace( configFileContents, replacement.Replace, replacement.With );

									File.WriteAllText( configFilePath, configFileContents, Encoding.UTF8 );
								}
							}
						}
					}

					// Finally write the current version to the instance version file
					// We do this last so that if a version upgrade fails, the user
					// can resume at the last fully completed version
					File.WriteAllText( versionFile, JsonConvert.SerializeObject( instanceVersion ) );
				}
			}
			finally
			{
				Directory.Delete( tempDir, true );
			}
		}

		public async Task NewInstance( MmcConfig mmcConfig, string instName, CancellationToken token, ProgressReporter progress = null, int? maxRamMb = null )
		{
			var newInstDir = Path.Combine( mmcConfig.InstancesFolder, instName );
			var newInstCfg = Path.Combine( newInstDir,                "instance.cfg" );

			if ( Directory.Exists( newInstDir ) )
				throw new InvalidOperationException( "An instance with the specified name already exists" );

			progress?.ReportProgress( "Creating new instance directory..." );

			Directory.CreateDirectory( newInstDir );

			progress?.ReportProgress( "Loading pack metadata from server..." );

			var pack = await this.LoadConfig();
			var instance = new MmcInstance {
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

			await this.SetupPack( mmcConfig, pack, newInstDir, true, token, progress, maxRamMb );
		}

		public async Task ConvertInstance( MmcConfig mmcConfig, string instPath, bool forceRebuild, CancellationToken token, ProgressReporter progress = null, int? maxRamMb = null )
		{
			if ( !Directory.Exists( instPath ) )
				throw new InvalidOperationException( "The specified instance folder was not found" );

			if ( Directory.Exists( Path.Combine( instPath, "minecraft" ) ) || !Directory.Exists( Path.Combine( instPath, ".minecraft" ) ) )
				throw new InvalidOperationException( "The existing instance is using the old MultiMC pack format." +
													 "This installer cannot upgrade an instance to the new MultiMC pack format.\n\n" +
													 "Please use the \"New Instance\" option to create a new MultiMC instance." );

			var         instCfg = Path.Combine( instPath, "instance.cfg" );
			var         pack    = await this.LoadConfig();
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

			await this.SetupPack( mmcConfig, pack, instPath, forceRebuild, token, progress, maxRamMb );
		}
	}
}