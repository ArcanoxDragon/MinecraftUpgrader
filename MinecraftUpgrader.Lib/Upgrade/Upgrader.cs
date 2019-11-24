using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Humanizer;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Options;
using MinecraftUpgrader.Async;
using MinecraftUpgrader.Config;
using MinecraftUpgrader.Constants;
using MinecraftUpgrader.Extensions;
using MinecraftUpgrader.MultiMC;
using MinecraftUpgrader.Options;
using MinecraftUpgrader.Web;
using MinecraftUpgrader.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Semver;

namespace MinecraftUpgrader.Upgrade
{
	public class Upgrader
	{
		private const string ConfigPath = "modpack/pack-upgrade.json";
		private const string IconPath   = "modpack/icon.png";
		private const string IconName   = "arcanox";

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
			{
				using var str = await http.GetStreamAsync( $"{this.options.UpgradeUrl}/{ConfigPath}" );
				using var sr  = new StreamReader( str );
				using var jr  = new JsonTextReader( sr );

				config = json.Deserialize<PackUpgrade>( jr );
			}

			return config;
		}

		private async Task SetupPack( MmcConfig mmcConfig, PackUpgrade pack, string destinationFolder, bool forceRebuild, bool vrEnabled, CancellationToken token, ProgressReporter progress, int? maxRamMb = null )
		{
			var metadataFile = Path.Combine( destinationFolder, "packMeta.json" );
			var cfgFile      = Path.Combine( destinationFolder, "instance.cfg" );
			var tempDir      = Path.Combine( destinationFolder, "temp" );
			var minecraftDir = Path.Combine( destinationFolder, ".minecraft" );
			var librariesDir = Path.Combine( destinationFolder, "libraries" );
			var patchesDir   = Path.Combine( destinationFolder, "patches" );
			var modsDir      = Path.Combine( minecraftDir, "mods" );

			progress?.ReportProgress( "Updating instance configuration..." );

			using ( var fs = File.Open( cfgFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None ) )
			{
				using var sr  = new StreamReader( fs );
				var       cfg = await ConfigReader.ReadConfig<MmcInstance>( sr );

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
			if ( !forceRebuild && File.Exists( metadataFile ) )
			{
				var currentInstanceMetadata = JsonConvert.DeserializeObject<InstanceMetadata>( File.ReadAllText( metadataFile ) );

				if ( currentInstanceMetadata.FileVersion >= 2 )
				{
					currentVersion    = currentInstanceMetadata.Version;
					currentServerPack = currentInstanceMetadata.BuiltFromServerPack;
				}
			}

			var instanceMetadata = new InstanceMetadata {
				FileVersion         = InstanceMetadata.CurrentFileVersion,
				Version             = pack.CurrentVersion,
				BuiltFromServerPack = pack.ServerPack,
				VrEnabled           = vrEnabled,
			};

			Directory.CreateDirectory( minecraftDir );
			Directory.CreateDirectory( tempDir );

			try
			{
				using var web          = new CefWebClient();
				var       downloadTask = "";

				web.DownloadProgressChanged += ( sender, args ) => {
					// ReSharper disable AccessToModifiedClosure
					var dlSize    = args.BytesReceived.Bytes();
					var totalSize = args.TotalBytesToReceive.Bytes();

					if ( args.TotalBytesToReceive > 0 )
					{
						progress?.ReportProgress( args.BytesReceived / (double) args.TotalBytesToReceive,
												  $"{downloadTask} ({dlSize.ToString( "0.##" )} / {totalSize.ToString( "0.##" )})" );
					}
					else
					{
						progress?.ReportProgress( -1, $"{downloadTask} ({dlSize.ToString( "0.##" )})" );
					}
					// ReSharper restore AccessToModifiedClosure
				};

				token.ThrowIfCancellationRequested();

				// Download MultiMC pack descriptor
				downloadTask = "Downloading MultiMC pack descriptor...";
				var packConfigFileName = Path.Combine( destinationFolder, "mmc-pack.json" );
				await web.DownloadFileTaskAsync( pack.MultiMcPack, packConfigFileName, token );

				// Download icon
				if ( !Directory.Exists( mmcConfig.IconsFolder ) )
					Directory.CreateDirectory( mmcConfig.IconsFolder );

				downloadTask = "Downloading pack icon...";
				var iconFileName = Path.Combine( mmcConfig.IconsFolder, "arcanox.png" );
				await web.DownloadFileTaskAsync( $"{this.options.UpgradeUrl}/{IconPath}", iconFileName, token );

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
					var configDir    = Path.Combine( minecraftDir, "config" );
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

					// Download server pack contents
					downloadTask = "Downloading base pack contents...";
					var serverPackFileName = Path.Combine( tempDir, "server.zip" );
					await web.DownloadFileTaskAsync( pack.ServerPack, serverPackFileName, token );

					token.ThrowIfCancellationRequested();
					progress?.ReportProgress( 0, "Extracting base pack contents..." );

					// Extract server pack contents
					using ( var fs = File.Open( serverPackFileName, FileMode.Open, FileAccess.Read, FileShare.Read ) )
					{
						using var zip = new ZipFile( fs );

						progress?.ReportProgress( "Extracting base pack contents (configs)..." );
						await zip.ExtractAsync( minecraftDir, new ZipExtractOptions( "config", false, token, progress ) );
						progress?.ReportProgress( "Extracting base pack contents (mods)..." );
						await zip.ExtractAsync( minecraftDir, new ZipExtractOptions( "mods", true, token, progress ) );
						progress?.ReportProgress( "Extracting base pack contents (resources)..." );
						await zip.TryExtractAsync( minecraftDir, new ZipExtractOptions( "resources", false, token, progress ) );
						progress?.ReportProgress( "Extracting base pack contents (scripts)..." );
						await zip.TryExtractAsync( minecraftDir, new ZipExtractOptions( "scripts", false, token, progress ) );
					}

					token.ThrowIfCancellationRequested();

					// Download client pack overrides
					downloadTask = "Downloading client overrides...";
					var clientPackFileName = Path.Combine( tempDir, "client.zip" );
					await web.DownloadFileTaskAsync( pack.ClientPack, clientPackFileName, token );

					token.ThrowIfCancellationRequested();
					progress?.ReportProgress( 0, "Extracting client overrides..." );

					// Extract client pack contents
					using ( var fs = File.Open( clientPackFileName, FileMode.Open, FileAccess.Read, FileShare.Read ) )
					{
						using var zip = new ZipFile( fs );

						progress?.ReportProgress( "Extracting client overrides (configs)..." );
						await zip.TryExtractAsync( minecraftDir, new ZipExtractOptions( "overrides/animation", true, token, progress ) );
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
								var archiveUrl = $"https://www.curseforge.com/minecraft/mc-mods/{modId}/download/{mod.FileId}/file";

								downloadTask = $"{modTask}\n" +
											   $"Downloading mod archive: {fileName}";

								await web.DownloadFileTaskAsync( archiveUrl, filePath, token );
							}
						}
					}

					if ( version.ConfigReplacements != null )
					{
						var configsTask = $"{versionTask}\nApplying config replacements...";

						// Apply config replacements
						progress?.ReportProgress( -1, configsTask );
						var configDir     = Path.Combine( minecraftDir, "config" );
						var currentConfig = 0;
						foreach ( var (configPath, replacements) in version.ConfigReplacements )
						{
							var configFilePath = Path.Combine( configDir, configPath );

							if ( File.Exists( configFilePath ) )
							{
								progress?.ReportProgress( ( currentConfig++ / (double) version.ConfigReplacements.Count ),
														  $"{configsTask}\n" +
														  $"Config file {currentConfig} of {version.ConfigReplacements.Count}: " +
														  $"{configPath}" );

								var configFileContents = File.ReadAllText( configFilePath, Encoding.UTF8 );

								foreach ( var replacement in replacements )
									configFileContents = Regex.Replace( configFileContents, replacement.Replace, replacement.With );

								File.WriteAllText( configFilePath, configFileContents, Encoding.UTF8 );
							}
						}
					}

					if ( version.ExtraFiles != null )
					{
						var extraFilesTask = $"{versionTask}\nDownloading extra files...";

						// Download extra files
						progress?.ReportProgress( -1, extraFilesTask );
						var currentFile = 0;
						foreach ( var (filename, url) in version.ExtraFiles )
						{
							var filePath = Path.Combine( minecraftDir, filename );

							downloadTask = $"{extraFilesTask}\n" +
										   $"Downloading extra file {currentFile++} of {version.ExtraFiles.Count}: {filename}";

							try
							{
								if ( File.Exists( filePath ) )
									File.Delete( filePath );

								await web.DownloadFileTaskAsync( url, filePath, token );
							}
							catch
							{
								throw new Exception( $"Could not download extra file \"{filename}\" from {url}" );
							}
						}
					}
				}

				#region VR Stuff

				// Handle VR-specific pack settings
				string vrTask = $"Setting up Vivecraft for {( vrEnabled ? "VR" : "non-VR" )} play...";

				progress?.ReportProgress( -1, vrTask );

				var optifineDownloadRegex = new Regex( @"href=(['""])(downloadx\?f=[^'""]+)\1", RegexOptions.Compiled | RegexOptions.IgnoreCase );

				// Set up temporary directory
				var installDirectory = Path.Combine( Path.GetTempPath(), "MinecraftInstaller" );

				if ( !Directory.Exists( installDirectory ) )
					Directory.CreateDirectory( installDirectory );
				if ( !Directory.Exists( librariesDir ) )
					Directory.CreateDirectory( librariesDir );
				if ( !Directory.Exists( patchesDir ) )
					Directory.CreateDirectory( patchesDir );

				// Download Vivecraft installer
				var vivecraftVersion           = vrEnabled ? VivecraftConstants.VivecraftVersionVr : VivecraftConstants.VivecraftVersionNonVr;
				var vivecraftRevision          = vrEnabled ? VivecraftConstants.VivecraftRevisionVr : VivecraftConstants.VivecraftRevisionNonVr;
				var vivecraftInstallerFilename = Path.Combine( installDirectory, "vivecraft_installer.exe" );

				downloadTask = $"{vrTask}\n" +
							   $"Downloading Vivecraft installer...";

				var vivecraftInstallerUri = VivecraftConstants.GetVivecraftInstallerUri( vrEnabled );

				await web.DownloadFileTaskAsync( vivecraftInstallerUri, vivecraftInstallerFilename, token );

				progress?.ReportProgress( -1, $"{vrTask}\nExtracting Vivecraft installer..." );

				using ( var fs = File.Open( vivecraftInstallerFilename, FileMode.Open, FileAccess.Read ) )
				{
					using var zip             = new ZipFile( fs );
					var       versionJarEntry = zip.GetEntry( "version.jar" );

					if ( versionJarEntry == null || !versionJarEntry.IsFile )
						throw new Exception( "Could not find Vivecraft jar file in the installer" );

					var minecriftFilename = $"minecrift-{vivecraftVersion}-{vivecraftRevision}.jar";

					using var entryStream = zip.GetInputStream( versionJarEntry );
					using var destStream  = File.Open( Path.Combine( librariesDir, minecriftFilename ), FileMode.Create, FileAccess.Write );

					await entryStream.CopyToAsync( destStream );
					await destStream.FlushAsync();
				}

				// Download Optifine
				downloadTask = $"{vrTask}\n" +
							   $"Fetching Optifine download information...";

				var optifineDownloadPage = await web.DownloadStringTaskAsync( VivecraftConstants.OptifineMirrorUri );

				if ( string.IsNullOrEmpty( optifineDownloadPage ) )
					throw new Exception( "Could not download Optifine from the mirror" );

				var mirrorPageMatch = optifineDownloadRegex.Match( optifineDownloadPage );

				if ( !mirrorPageMatch.Success )
					throw new Exception( "Unexpected response from Optifine mirror" );

				var optifineDownloadUrl = VivecraftConstants.OptifineBaseUri + mirrorPageMatch.Groups[ 2 ].Value;

				downloadTask = $"{vrTask}\n" +
							   $"Downloading Optifine archive...";

				await web.DownloadFileTaskAsync( optifineDownloadUrl, Path.Combine( librariesDir, VivecraftConstants.OptifineLibraryFilename ), token );

				// Write patch file
				progress?.ReportProgress( -1, "Writing Vivecraft patch file..." );

				var patchFileJson = JsonConvert.SerializeObject(
					vrEnabled ? MmcPatchDefinitions.VivecraftPatchVr : MmcPatchDefinitions.VivecraftPatchNonVr,
					new JsonSerializerSettings {
						NullValueHandling = NullValueHandling.Ignore,
					}
				);

				File.WriteAllText( Path.Combine( patchesDir, "vivecraft.json" ), patchFileJson );

				// Clean up temporary installation files
				progress?.ReportProgress( -1, "Cleaning up temporary files..." );

				try
				{
					Directory.Delete( installDirectory );
				}
				catch
				{
					/* Ignored */
				}

				#endregion

				// Finally write the current version to the instance version file
				// We do this last so that if a version upgrade fails, the user
				// can resume at the last fully completed version
				File.WriteAllText( metadataFile, JsonConvert.SerializeObject( instanceMetadata ) );
			}
			finally
			{
				Directory.Delete( tempDir, true );
			}
		}

		public async Task NewInstance( MmcConfig mmcConfig, string instName, bool vrEnabled, CancellationToken token, ProgressReporter progress = null, int? maxRamMb = null )
		{
			var newInstDir = Path.Combine( mmcConfig.InstancesFolder, instName );
			var newInstCfg = Path.Combine( newInstDir, "instance.cfg" );

			if ( Directory.Exists( newInstDir ) )
				throw new InvalidOperationException( "An instance with the specified name already exists" );

			progress?.ReportProgress( "Creating new instance directory..." );

			Directory.CreateDirectory( newInstDir );

			progress?.ReportProgress( "Loading pack metadata from server..." );

			var pack = await this.LoadConfig();
			var instance = new MmcInstance {
				Name             = instName,
				Icon             = IconName,
				InstanceType     = pack.InstanceType,
				IntendedVersion  = pack.IntendedVersion,
				MCLaunchMethod   = "LauncherPart",
				OverrideJavaArgs = true,
				JvmArgs          = VivecraftConstants.VivecraftJvmArgs,
			};

			token.ThrowIfCancellationRequested();
			progress?.ReportProgress( "Creating new instance config file..." );

			using ( var fs = File.Open( newInstCfg, FileMode.Create, FileAccess.Write, FileShare.None ) )
			{
				using var sw = new StreamWriter( fs );

				await ConfigReader.WriteConfig( instance, sw );
			}

			await this.SetupPack( mmcConfig, pack, newInstDir, true, vrEnabled, token, progress, maxRamMb );
		}

		public async Task ConvertInstance( MmcConfig mmcConfig, string instPath, bool forceRebuild, bool vrEnabled, CancellationToken token, ProgressReporter progress = null, int? maxRamMb = null )
		{
			if ( !Directory.Exists( instPath ) )
				throw new InvalidOperationException( "The specified instance folder was not found" );

			if ( Directory.Exists( Path.Combine( instPath, "minecraft" ) ) || !Directory.Exists( Path.Combine( instPath, ".minecraft" ) ) )
				throw new InvalidOperationException( "The existing instance is using the old MultiMC pack format. " +
													 "This installer cannot upgrade an instance to the new MultiMC pack format.\n\n" +
													 "Please use the \"New Instance\" option to create a new MultiMC instance." );

			var         instCfg = Path.Combine( instPath, "instance.cfg" );
			var         pack    = await this.LoadConfig();
			MmcInstance instance;

			using ( var fs = File.Open( instCfg, FileMode.Open, FileAccess.Read, FileShare.Read ) )
			{
				using var sr = new StreamReader( fs );

				instance = await ConfigReader.ReadConfig<MmcInstance>( sr );
			}

			token.ThrowIfCancellationRequested();
			if ( instance.InstanceType != pack.InstanceType || ( instance.IntendedVersion != null && instance.IntendedVersion != pack.IntendedVersion ) )
				throw new InvalidOperationException( "The existing instance is set up for a different version of Minecraft " +
													 "than the target instance. The upgrader cannot convert an existing instance " +
													 "to a different Minecraft version.\n\n" +
													 "Please create a new instance, or manually upgrade the existing instance to " +
													 $"Minecraft {pack.IntendedVersion}" );

			// Apply Vivecraft JVM arguments
			instance.OverrideJavaArgs = true;
			instance.JvmArgs          = VivecraftConstants.VivecraftJvmArgs;
			instance.Icon             = IconName;

			using ( var fs = File.Open( instCfg, FileMode.Open, FileAccess.Write, FileShare.Read ) )
			{
				using var sr = new StreamWriter( fs );

				await ConfigReader.WriteConfig( instance, sr );
			}

			await this.SetupPack( mmcConfig, pack, instPath, forceRebuild, vrEnabled, token, progress, maxRamMb );
		}
	}
}