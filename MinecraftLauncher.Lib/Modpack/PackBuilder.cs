using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CmlLib.Core;
using CmlLib.Core.Downloader;
using CmlLib.Core.Version;
using Humanizer;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Options;
using MinecraftLauncher.Async;
using MinecraftLauncher.Config;
using MinecraftLauncher.Extensions;
using MinecraftLauncher.Options;
using MinecraftLauncher.Utility;
using MinecraftLauncher.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Semver;

namespace MinecraftLauncher.Modpack
{
	public class PackBuilder
	{
		private static readonly JsonSerializer JsonSerializer = new JsonSerializer {
			ContractResolver = new CamelCasePropertyNamesContractResolver(),
		};

		private const string ConfigPath = "modpack/pack-info.json";
		public static string ProfilePath => Path.Combine( MinecraftPath.GetOSDefaultPath(), Constants.Minecraft.ProfileSubfolder );

		private readonly PackBuilderOptions options;

		public PackBuilder( IOptions<PackBuilderOptions> options )
		{
			this.options = options.Value;
		}

		public string PackMetadataUrl => $"{this.options.ModPackUrl}/{ConfigPath}";

		public async Task<PackMetadata> LoadConfigAsync( CancellationToken cancellationToken = default, ProgressReporter progressReporter = default )
		{
			using var web = new WebClient();

			cancellationToken.Register( web.CancelAsync );

			if ( progressReporter != null )
				web.DownloadProgressChanged += ( sender, args ) => {
					progressReporter.ReportProgress( args.ProgressPercentage / 100.0, "Loading mod pack info..." );
				};

			using var stream = await web.OpenReadTaskAsync( this.PackMetadataUrl );

			cancellationToken.ThrowIfCancellationRequested();

			using var streamReader   = new StreamReader( stream );
			using var jsonTextReader = new JsonTextReader( streamReader );

			return JsonSerializer.Deserialize<PackMetadata>( jsonTextReader );
		}

		private async Task<string> SetupNewProfileAsync( PackMetadata pack, CancellationToken token = default, ProgressReporter progress = default )
		{
			var mcVersionLoader = new MVersionLoader( new MinecraftPath( ProfilePath ) );
			var mcVersions      = await mcVersionLoader.GetVersionMetadatasAsync( token );
			var mcVersion       = mcVersions.GetVersion( pack.IntendedMinecraftVersion );
			var mcPath          = new MinecraftPath( ProfilePath );

			// Install base MC jar
			var downloader = new MDownloader( mcPath, mcVersion );

			downloader.ChangeFile += args => progress?.ReportProgress( args.ProgressedFileCount / (double) args.TotalFileCount,
																	   $"Downloading Minecraft JAR...\nDownloading file {args.FileName} (file {args.ProgressedFileCount} / {args.TotalFileCount})" );

			downloader.ChangeProgress += ( sender, args ) => progress?.ReportProgress( args.ProgressPercentage / 100.0 );

			await downloader.DownloadMinecraftAsync( token );
			token.ThrowIfCancellationRequested();

			// Install Minecraft Forge
			var forgeInstall = new MForge( mcPath, AppConfig.Get().JavaPath );

			forgeInstall.FileChanged += args => progress?.ReportProgress( args.ProgressedFileCount / (double) args.TotalFileCount,
																		  $"Installing Minecraft Forge...\nDownloading file {args.FileName} (file {args.ProgressedFileCount} / {args.TotalFileCount})" );

			forgeInstall.InstallerOutput += ( sender, message ) => progress?.ReportProgress( $"Installing Minecraft Forge...\n{message}" );

			var forgeVersionInstalled = await forgeInstall.InstallForgeAsync( pack.IntendedMinecraftVersion, pack.RequiredForgeVersion, token );
			token.ThrowIfCancellationRequested();

			// Refresh version info now that Forge is installed
			mcVersions = mcVersionLoader.GetVersionMetadatasFromLocal();
			mcVersion  = mcVersions.GetVersion( forgeVersionInstalled );

			// Install MC assets
			downloader = new MDownloader( mcPath, mcVersion );
			downloader.ChangeFile += args => progress?.ReportProgress( args.ProgressedFileCount / (double) args.TotalFileCount,
																	   $"Downloading Minecraft assets...\nDownloading file {args.FileName} (file {args.ProgressedFileCount} / {args.TotalFileCount})" );

			await downloader.DownloadAllAsync( cancellationToken: token );
			token.ThrowIfCancellationRequested();

			return forgeVersionInstalled;
		}

		public async Task SetupPackAsync( PackMetadata pack, bool forceRebuild, CancellationToken token = default, ProgressReporter progress = default )
		{
			var metadataFile = Path.Combine( ProfilePath, "packMeta.json" );
			var tempDir      = Path.Combine( ProfilePath, "temp" );
			var modsDir      = Path.Combine( ProfilePath, "mods" );

			progress?.ReportProgress( "Creating instance folder structure..." );

			var currentVersion          = "0.0.0";
			var currentServerPack       = default(string);
			var currentMinecraftVersion = default(string);
			var currentForgeVersion     = default(string);
			var currentLaunchVersion    = default(string);
			var currentBasePackMd5      = default(string);

			// If forceRebuild is set, we don't even bother checking the current version.
			// Just redo everything.
			if ( !forceRebuild && File.Exists( metadataFile ) )
			{
				var currentInstanceMetadata = JsonConvert.DeserializeObject<InstanceMetadata>( File.ReadAllText( metadataFile ) );

				if ( currentInstanceMetadata.FileVersion >= 2 )
				{
					currentVersion          = currentInstanceMetadata.Version;
					currentServerPack       = currentInstanceMetadata.BuiltFromServerPack;
					currentMinecraftVersion = currentInstanceMetadata.CurrentMinecraftVersion;
					currentForgeVersion     = currentInstanceMetadata.CurrentForgeVersion;
					currentLaunchVersion    = currentInstanceMetadata.CurrentLaunchVersion;
					currentBasePackMd5      = currentInstanceMetadata.BuiltFromServerPackMd5;
				}
			}

			var instanceMetadata = new InstanceMetadata {
				FileVersion             = InstanceMetadata.CurrentFileVersion,
				CurrentMinecraftVersion = pack.IntendedMinecraftVersion,
				CurrentForgeVersion     = pack.RequiredForgeVersion,
				CurrentLaunchVersion    = currentLaunchVersion,
				Version                 = pack.CurrentVersion,
				BuiltFromServerPack     = pack.ServerPack,
				BuiltFromServerPackMd5  = currentBasePackMd5,
			};

			Directory.CreateDirectory( ProfilePath );
			Directory.CreateDirectory( tempDir );

			try
			{
				using var web = new WebClient();

				var downloadTask = "";

				web.DownloadProgressChanged += ( sender, args ) => {
					// ReSharper disable AccessToModifiedClosure
					var dlSize    = args.BytesReceived.Bytes();
					var totalSize = args.TotalBytesToReceive.Bytes();

					if ( args.TotalBytesToReceive > 0 )
					{
						// TODO: When Humanizer update is released, replace ToString calls below with interpolation shorthand
						progress?.ReportProgress( args.BytesReceived / (double) args.TotalBytesToReceive,
												  $"{downloadTask} ({dlSize.ToString( "0.##" )} / {totalSize.ToString( "0.##" )})" );
					}
					else
					{
						// TODO: When Humanizer update is released, replace ToString calls below with interpolation shorthand
						progress?.ReportProgress( -1, $"{downloadTask} ({dlSize.ToString( "0.##" )})" );
					}
					// ReSharper restore AccessToModifiedClosure
				};

				token.ThrowIfCancellationRequested();
				token.Register( web.CancelAsync );

				// Set up as a new profile if necessary
				if ( forceRebuild || currentForgeVersion != pack.RequiredForgeVersion || string.IsNullOrEmpty( currentLaunchVersion ) )
				{
					instanceMetadata.CurrentLaunchVersion = await this.SetupNewProfileAsync( pack, token, progress );
				}

				var rebuildBasePack = forceRebuild ||
									  currentVersion == "0.0.0" ||
									  currentServerPack != pack.ServerPack ||
									  currentMinecraftVersion != pack.IntendedMinecraftVersion;

				// Check server pack MD5 against local built-from to see if the base pack may have changed at all
				if ( pack.VerifyServerPackMd5 )
				{
					var basePackMd5Url = $"{pack.ServerPack}.md5";
					var basePackMd5    = await web.DownloadStringTaskAsync( basePackMd5Url );

					if ( !string.IsNullOrEmpty( basePackMd5 ) )
					{
						rebuildBasePack |= basePackMd5 != currentBasePackMd5;
					}
				}

				// Only download base pack and client overrides if the version is 0 (not installed or old file version),
				// or if the forceRebuild flag is set
				if ( rebuildBasePack )
				{
					// Set this again in case we got here from "currentServerPack" or "currentMinecraftVersion" not matching
					// This forces all versions to install later on in this method even if the version strings line up, as a
					// different server pack or MC version means a different update path regardless.
					currentVersion = "0.0.0";

					// Clear out old files if present since we're downloading the base pack fresh
					var configDir    = Path.Combine( ProfilePath, "config" );
					var resourcesDir = Path.Combine( ProfilePath, "resources" );
					var scriptsDir   = Path.Combine( ProfilePath, "scripts" );

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

					instanceMetadata.BuiltFromServerPackMd5 = CryptoUtility.CalculateFileMd5( serverPackFileName );

					token.ThrowIfCancellationRequested();
					progress?.ReportProgress( 0, "Extracting base pack contents..." );

					// Extract server pack contents
					using ( var fs = File.Open( serverPackFileName, FileMode.Open, FileAccess.Read, FileShare.Read ) )
					{
						using var zip = new ZipFile( fs );

						progress?.ReportProgress( "Extracting base pack contents (configs)..." );
						await zip.ExtractAsync( ProfilePath, new ZipExtractOptions( "config", false, token, progress ) );
						progress?.ReportProgress( "Extracting base pack contents (mods)..." );
						await zip.ExtractAsync( ProfilePath, new ZipExtractOptions( "mods", true, token, progress ) );
						progress?.ReportProgress( "Extracting base pack contents (resources)..." );
						await zip.TryExtractAsync( ProfilePath, new ZipExtractOptions( "resources", false, token, progress ) );
						progress?.ReportProgress( "Extracting base pack contents (scripts)..." );
						await zip.TryExtractAsync( ProfilePath, new ZipExtractOptions( "scripts", false, token, progress ) );
						progress?.ReportProgress( "Extracting base pack contents..." );

						// Individual files that might exist
						foreach ( var filename in pack.AdditionalBasePackFiles )
						{
							await zip.TryExtractAsync( ProfilePath, new ZipExtractOptions( filenamePattern: Regex.Escape( filename ), overwriteExisting: false, cancellationToken: token, progressReporter: progress ) );
						}
					}

					token.ThrowIfCancellationRequested();

					if ( !string.IsNullOrEmpty( pack.ClientPack ) )
					{
						// Download client pack overrides
						downloadTask = "Downloading client overrides...";
						var clientPackFileName = Path.Combine( tempDir, "client.zip" );
						await web.DownloadFileTaskAsync( pack.ClientPack, clientPackFileName );

						token.ThrowIfCancellationRequested();
						progress?.ReportProgress( 0, "Extracting client overrides..." );

						// Extract client pack contents
						using var fs  = File.Open( clientPackFileName, FileMode.Open, FileAccess.Read, FileShare.Read );
						using var zip = new ZipFile( fs );

						progress?.ReportProgress( "Extracting client overrides (configs)..." );
						await zip.TryExtractAsync( ProfilePath, new ZipExtractOptions( "overrides/animation", true, token, progress ) );
						progress?.ReportProgress( "Extracting client overrides (configs)..." );
						await zip.TryExtractAsync( ProfilePath, new ZipExtractOptions( "overrides/config", true, token, progress ) );
						progress?.ReportProgress( "Extracting client overrides (mods)..." );
						await zip.TryExtractAsync( ProfilePath, new ZipExtractOptions( "overrides/mods", true, token, progress ) );
						progress?.ReportProgress( "Extracting client overrides (resources)..." );
						await zip.TryExtractAsync( ProfilePath, new ZipExtractOptions( "overrides/resources", true, token, progress ) );
						progress?.ReportProgress( "Extracting client overrides (scripts)..." );
						await zip.TryExtractAsync( ProfilePath, new ZipExtractOptions( "overrides/scripts", true, token, progress ) );
					}
				}

				// Process version upgrades
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

					var futureVersions = pack.Versions.Keys
											 .Where( vk => SemVersion.Parse( vk ) > thisSemVersion )
											 .Select( vk => pack.Versions[ vk ] )
											 .ToList();

					if ( version.Mods != null )
					{
						var currentModFiles = Directory.EnumerateFiles( modsDir, "*", SearchOption.AllDirectories ).ToList();
						var currentMod      = 0;

						foreach ( var (modId, mod) in version.Mods )
						{
							bool VersionInstallsThisMod( PackVersion v )
								// See if there are any mods in this version with the same mod ID and which download a new JAR
								=> v.Mods?.Any( pair => pair.Key == modId && !string.IsNullOrEmpty( pair.Value.FileUri ) ) == true;

							if ( futureVersions.Any( VersionInstallsThisMod ) )
								// Skip the mod in this version because a later version installs it
								continue;

							var modTask = $"{versionTask}\nProcessing mod {modId} ({++currentMod} / {version.Mods.Count})...";

							progress?.ReportProgress( modTask );

							// Delete old version if necessary
							if ( mod.RemoveOld || !string.IsNullOrEmpty( mod.RemovePattern ) )
							{
								progress?.ReportProgress( $"{modTask}\n" +
														  $"Deleting old versions..." );

								var deletePattern = mod.RemovePattern ?? $"{modId}.*";
								var toDelete      = currentModFiles.Where( file => Regex.IsMatch( file, deletePattern ) ).ToList();

								foreach ( var file in toDelete )
									File.Delete( file );
							}

							// Install new version if necessary
							if ( !string.IsNullOrEmpty( mod.FileUri ) )
							{
								var fileName = Path.GetFileName( mod.FileUri ) ?? $"{modId}.jar";
								var filePath = Path.Combine( ProfilePath, "mods", fileName );

								downloadTask = $"{modTask}\n" +
											   $"Downloading mod archive: {fileName}";

								await web.DownloadFileTaskAsync( mod.FileUri, filePath );
							}
						}
					}

					if ( version.ConfigReplacements != null )
					{
						var configsTask = $"{versionTask}\nApplying config replacements...";

						// Apply config replacements
						progress?.ReportProgress( -1, configsTask );
						var configDir     = Path.Combine( ProfilePath, "config" );
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

								var configFileContents = File.ReadAllText( configFilePath, EncodingUtil.UTF8NoBom );

								foreach ( var replacement in replacements )
									configFileContents = Regex.Replace( configFileContents, replacement.Replace, replacement.With );

								File.WriteAllText( configFilePath, configFileContents, EncodingUtil.UTF8NoBom );
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
							var filePath = Path.Combine( ProfilePath, filename );

							downloadTask = $"{extraFilesTask}\n" +
										   $"Downloading extra file {currentFile++} of {version.ExtraFiles.Count}: {filename}";

							try
							{
								if ( File.Exists( filePath ) )
									File.Delete( filePath );

								await web.DownloadFileTaskAsync( url, filePath );
							}
							catch
							{
								throw new Exception( $"Could not download extra file \"{filename}\" from {url}" );
							}
						}
					}
				}

				// TODO: Add back VR stuff once ViveCraft is ready

				// Finally write the current version to the instance version file
				// We do this last so that if a version upgrade fails, the user
				// can resume at the last fully completed version
				using var packMetaFileStream = File.Open( metadataFile, FileMode.Create, FileAccess.Write, FileShare.None );
				using var packMetaWriter     = new StreamWriter( packMetaFileStream );

				JsonSerializer.Serialize( packMetaWriter, instanceMetadata );
				await packMetaWriter.FlushAsync();
			}
			finally
			{
				Directory.Delete( tempDir, true );
			}
		}
	}
}