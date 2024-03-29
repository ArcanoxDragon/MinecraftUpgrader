﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Humanizer;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Options;
using MinecraftUpgrader.Async;
using MinecraftUpgrader.Config;
using MinecraftUpgrader.Options;
using MinecraftUpgrader.Prism;
using MinecraftUpgrader.Utility;
using MinecraftUpgrader.Zip;
using Semver;

namespace MinecraftUpgrader.Modpack;

public class PackBuilder(IOptions<PackBuilderOptions> options)
{
	private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web) {
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true,
	};

	private const string ConfigPath = "modpack/pack-info.json";
	private const string IconPath   = "modpack/icon.png";
	private const string IconName   = "arcanox";

	private static readonly string[] DefaultBasePackFolders = [
		".minecraft/config",
		".minecraft/coremods",
		".minecraft/defaultconfigs",
		".minecraft/mods",
	];

	private static readonly string[] DefaultClientOverrideFolders = [
		"animation",
		"configs",
		"defaultconfigs",
		"mods",
		"paintings",
		"resources",
		"scripts",
	];

	private static readonly string[] ProfileFilePatternsToPreserve = [
		@"options\.txt$",
		@"servers\.dat$",
		@"assets[/\\].*$",
		@"(?:local|resourcepacks|saves|schematics|screenshots|shaderpacks|texturepacks)[/\\].*$",
	];

	private readonly PackBuilderOptions options = options.Value;

	public string PackMetadataUrl => $"{this.options.ModPackUrl}/{ConfigPath}";

	public async Task<PackMetadata> LoadConfigAsync(CancellationToken cancellationToken = default, ProgressReporter progressReporter = default)
	{
		using var http = WebUtility.CreateHttpClient();
		var downloader = new FileDownloader(http);

		if (progressReporter != null)
		{
			downloader.ProgressChanged += (_, args) => {
				progressReporter.ReportProgress(args.ProgressPercentage / 100.0, "Loading mod pack info...");
			};
		}

		await using var stream = await downloader.DownloadToMemoryAsync(PackMetadataUrl, cancellationToken);

		cancellationToken.ThrowIfCancellationRequested();

		var packMetadata = JsonSerializer.Deserialize<PackMetadata>(stream, JsonSerializerOptions);

		if (packMetadata is null)
			throw new JsonException("Could not parse mod pack configuration file");

		return packMetadata;
	}

	private async Task SetupPackAsync(PrismConfig prismConfig, PackMetadata pack, string destinationFolder, bool forceRebuild, bool vrEnabled, CancellationToken token, ProgressReporter progress, int? maxRamMb = null)
	{
		var metadataFile = Path.Combine(destinationFolder, "packMeta.json");
		var cfgFile = Path.Combine(destinationFolder, "instance.cfg");
		var tempDir = Path.Combine(destinationFolder, "temp");
		var minecraftDir = Path.Combine(destinationFolder, ".minecraft");
		var librariesDir = Path.Combine(destinationFolder, "libraries");
		var patchesDir = Path.Combine(destinationFolder, "patches");
		var modsDir = Path.Combine(minecraftDir, "mods");

		progress?.ReportProgress("Updating instance configuration...");

		await using (var fs = File.Open(cfgFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
		{
			using var sr = new StreamReader(fs);
			var cfg = await ConfigReader.ReadConfig<PrismInstance>(sr);

			cfg.OverrideMemory = true;
			cfg.MinMemAlloc = 1024;
			cfg.MaxMemAlloc = maxRamMb ?? ( 6 * 1024 );

			await ConfigReader.UpdateConfig(cfg, fs);
		}

		progress?.ReportProgress("Creating instance folder structure...");

		var currentVersion = "0.0.0";
		var currentServerPack = default(string);
		var currentMinecraftVersion = default(string);
		var currentBasePackMd5 = default(string);

		// If forceRebuild is set, we don't even bother checking the current version.
		// Just redo everything.
		if (!forceRebuild && File.Exists(metadataFile))
		{
			var instanceMetadataJson = await File.ReadAllTextAsync(metadataFile, token).ConfigureAwait(false);
			var currentInstanceMetadata = JsonSerializer.Deserialize<InstanceMetadata>(instanceMetadataJson, JsonSerializerOptions);

			if (currentInstanceMetadata.FileVersion >= 2)
			{
				currentVersion = currentInstanceMetadata.Version;
				currentServerPack = currentInstanceMetadata.BuiltFromServerPack;
				currentMinecraftVersion = currentInstanceMetadata.IntendedMinecraftVersion;
				currentBasePackMd5 = currentInstanceMetadata.BuiltFromServerPackMd5;
			}
		}

		var instanceMetadata = new InstanceMetadata {
			FileVersion = InstanceMetadata.CurrentFileVersion,
			Version = pack.CurrentVersion,
			IntendedMinecraftVersion = pack.IntendedMinecraftVersion,
			BuiltFromServerPack = pack.ServerPack,
			BuiltFromServerPackMd5 = currentBasePackMd5,
			VrEnabled = vrEnabled,
		};

		Directory.CreateDirectory(minecraftDir);

		try
		{
			using var http = WebUtility.CreateHttpClient();
			var downloader = new FileDownloader(http);

			var downloadTask = "";

			downloader.ProgressChanged += (_, args) => {
				// ReSharper disable AccessToModifiedClosure
				var dlSize = args.BytesReceived.Bytes();
				var totalSize = args.TotalBytesToReceive.Bytes();

				if (args.TotalBytesToReceive > 0)
				{
					// TODO: When Humanizer update is released, replace ToString calls below with interpolation shorthand
					progress?.ReportProgress(args.BytesReceived / (double) args.TotalBytesToReceive,
											 $"{downloadTask} ({dlSize.ToString("0.##")} / {totalSize.ToString("0.##")})");
				}
				else
				{
					// TODO: When Humanizer update is released, replace ToString calls below with interpolation shorthand
					progress?.ReportProgress(-1, $"{downloadTask} ({dlSize.ToString("0.##")})");
				}
				// ReSharper restore AccessToModifiedClosure
			};

			token.ThrowIfCancellationRequested();

			// Download Prism pack descriptor
			downloadTask = "Downloading Prism pack descriptor...";
			var packConfigFileName = Path.Combine(destinationFolder, "mmc-pack.json");
			await downloader.DownloadFileAsync(pack.PrismPack, packConfigFileName, token);

			// Download icon
			if (!Directory.Exists(prismConfig.IconsFolder))
				Directory.CreateDirectory(prismConfig.IconsFolder);

			downloadTask = "Downloading pack icon...";
			var iconFileName = Path.Combine(prismConfig.IconsFolder, "arcanox.png");
			await downloader.DownloadFileAsync($"{this.options.ModPackUrl}/{IconPath}", iconFileName, token);

			var rebuildBasePack = forceRebuild ||
								  currentVersion == "0.0.0" ||
								  currentServerPack != pack.ServerPack ||
								  currentMinecraftVersion != pack.IntendedMinecraftVersion;

			// Check server pack MD5 against local built-from to see if the base pack may have changed at all
			if (pack.VerifyServerPackMd5)
			{
				var basePackMd5Url = $"{pack.ServerPack}.md5";
				var basePackMd5 = await http.GetStringAsync(basePackMd5Url, token);

				if (!string.IsNullOrEmpty(basePackMd5))
					rebuildBasePack |= basePackMd5 != currentBasePackMd5;
			}

			// Now create the temp dir for the following tasks
			Directory.CreateDirectory(tempDir);

			// Only download base pack and client overrides if the version is 0 (not installed or old file version),
			// or if the forceRebuild flag is set
			if (rebuildBasePack)
			{
				// Clear out old profile files
				progress?.ReportProgress("Cleaning up old files...");

				foreach (var file in Directory.EnumerateFiles(minecraftDir, "*", SearchOption.AllDirectories))
				{
					if (ProfileFilePatternsToPreserve.Any(toPreserve => Regex.IsMatch(file, toPreserve)))
						continue;

					File.Delete(file);
				}

				// Clear out old profile directories
				foreach (var directory in Directory.EnumerateDirectories(minecraftDir))
				{
					var directoryContents = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).ToList();
					var isEmpty = directoryContents.Count == 0;

					if (isEmpty)
						Directory.Delete(directory, true);
				}

				// Set this again in case we got here from "currentServerPack" not matching
				// This forces all versions to install later on in this method even if the
				// version strings line up, as a different server pack means a different
				// update path regardless.
				currentVersion = "0.0.0";

				// Clear out old files if present since we're downloading the base pack fresh
				var configDir = Path.Combine(minecraftDir, "config");
				var resourcesDir = Path.Combine(minecraftDir, "resources");
				var scriptsDir = Path.Combine(minecraftDir, "scripts");

				if (Directory.Exists(patchesDir))
					Directory.Delete(patchesDir, true);

				if (Directory.Exists(configDir))
					Directory.Delete(configDir, true);

				if (Directory.Exists(modsDir))
					Directory.Delete(modsDir, true);

				if (Directory.Exists(resourcesDir))
					Directory.Delete(resourcesDir, true);

				if (Directory.Exists(scriptsDir))
					Directory.Delete(scriptsDir, true);

				// Download server pack contents
				downloadTask = "Downloading base pack contents...";
				var serverPackFileName = Path.Combine(tempDir, "server.zip");
				await downloader.DownloadFileAsync(pack.ServerPack, serverPackFileName, token);

				instanceMetadata.BuiltFromServerPackMd5 = CryptoUtility.CalculateFileMd5(serverPackFileName);

				token.ThrowIfCancellationRequested();
				progress?.ReportProgress(0, "Extracting base pack contents...");

				// Extract server pack contents
				await using (var fs = File.Open(serverPackFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					using var zip = new ZipFile(fs);

					foreach (var folder in pack.BasePackFolders ?? DefaultBasePackFolders.ToList())
					{
						progress?.ReportProgress($"Extracting base pack contents ({folder})...");
						await zip.ExtractAsync(minecraftDir, new ZipExtractOptions(folder, false, token, progress));
					}

					progress?.ReportProgress("Extracting base pack contents...");

					// Individual files that might exist
					if (pack.AdditionalBasePackFiles is not null)
					{
						foreach (var filename in pack.AdditionalBasePackFiles)
						{
							await zip.TryExtractAsync(minecraftDir, new ZipExtractOptions(filenamePattern: Regex.Escape(filename), overwriteExisting: false, cancellationToken: token, progressReporter: progress));
						}
					}
				}

				token.ThrowIfCancellationRequested();

				if (!string.IsNullOrEmpty(pack.ClientPack))
				{
					// Download client pack overrides
					downloadTask = "Downloading client overrides...";
					var clientPackFileName = Path.Combine(tempDir, "client.zip");
					await downloader.DownloadFileAsync(pack.ClientPack, clientPackFileName, token);

					token.ThrowIfCancellationRequested();
					progress?.ReportProgress(0, "Extracting client overrides...");

					// Extract client pack contents
					await using var fs = File.Open(clientPackFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
					using var zip = new ZipFile(fs);

					async Task ExtractOptionalOverrideFolder(ZipFile zipFile, string folder)
					{
						progress?.ReportProgress($"Extracting client overrides ({folder})...");
						await zipFile.TryExtractAsync(minecraftDir, new ZipExtractOptions($"overrides/{folder}", true, token, progress));
					}

					foreach (var folder in pack.ClientOverrideFolders ?? DefaultClientOverrideFolders.ToList())
					{
						await ExtractOptionalOverrideFolder(zip, folder);
					}

					// Fulfuill the client pack manifest if present
					if (await zip.TryExtractAsync(tempDir, new ZipExtractOptions(filenamePattern: Regex.Escape("manifest.json"), overwriteExisting: true)))
					{
						var clientManifestFile = Path.Combine(tempDir, "manifest.json");
						var clientManifestJson = await File.ReadAllTextAsync(clientManifestFile, token).ConfigureAwait(false);
						var clientManifest = JsonSerializer.Deserialize<CursePackManifest>(clientManifestJson, JsonSerializerOptions);

						if (clientManifest.Files?.Count > 0)
						{
							progress?.ReportProgress(0, "Downloading client-only mods...");

							var currentFile = 0;

							foreach (var file in clientManifest.Files)
							{
								var currentProgress = currentFile / (double) clientManifest.Files.Count;

								progress?.ReportProgress(currentProgress);

								var fileInfo = await CurseUtility.GetFileInfoAsync(file.ProjectID, file.FileID, token);

								downloadTask = $"Downloading client-only mods...\n{fileInfo.DisplayName}";
								progress?.ReportProgress(currentProgress, downloadTask);

								var fileDestinationFolder = Path.Combine(minecraftDir, fileInfo.Project.Path);

								if (!Directory.Exists(fileDestinationFolder))
									Directory.CreateDirectory(fileDestinationFolder);

								var fullFilePath = Path.Combine(fileDestinationFolder, fileInfo.FileNameOnDisk);

								if (!File.Exists(fullFilePath))
									await downloader.DownloadFileAsync(fileInfo.DownloadURL, fullFilePath, token);

								currentFile++;
							}
						}
					}
				}
			}

			// Process version upgrades
			foreach (var versionKey in pack.Versions.Keys)
			{
				if (!SemVersion.TryParse(versionKey, SemVersionStyles.Any, out _))
					throw new InvalidOperationException($"Invalid version number in mod pack definition: {versionKey}");
			}

			var currentPackVersion = SemVersion.Parse(currentVersion, SemVersionStyles.Any);
			var desiredPackVersion = SemVersion.Parse(pack.CurrentVersion, SemVersionStyles.Any);
			var installedVersionRange = SemVersionRange.AtMost(currentPackVersion);
			var futureVersionRange = SemVersionRange.GreaterThan(desiredPackVersion);

			foreach (var (versionKey, version) in pack.Versions.OrderBy(pair => SemVersion.Parse(pair.Key, SemVersionStyles.Any)))
			{
				var thisPackVersion = SemVersion.Parse(versionKey, SemVersionStyles.Any);

				if (thisPackVersion.Satisfies(installedVersionRange))
					// This version is already installed; don't bother
					continue;

				if (thisPackVersion.Satisfies(futureVersionRange))
					// We aren't supposed to install this version yet; it's newer than the "intended" pack version
					continue;

				var versionTask = $"Processing version {versionKey}...";

				progress?.ReportProgress(versionTask);

				// Find all versions AFTER this one that will be installed (but not future versions that aren't to be installed yet)
				var remainingToInstallRange = SemVersionRange.InclusiveOfEnd(thisPackVersion, desiredPackVersion);
				var remainingVersionsToInstall = pack.Versions.Keys
					.Where(vk => SemVersion.TryParse(vk, SemVersionStyles.Any, out var parsed) && parsed.Satisfies(remainingToInstallRange))
					.Select(vk => pack.Versions[vk])
					.ToList();

				if (version.Mods != null)
				{
					var currentModFiles = Directory.EnumerateFiles(modsDir, "*", SearchOption.AllDirectories).ToList();
					var currentMod = 0;

					foreach (var (modId, mod) in version.Mods)
					{
						bool VersionInstallsThisMod(PackVersion v)
							// See if there are any mods in this version with the same mod ID and which download a new JAR
							=> v.Mods?.Any(pair => pair.Key == modId && !string.IsNullOrEmpty(pair.Value.FileUri)) is true;

						if (remainingVersionsToInstall.Any(VersionInstallsThisMod))
							// Skip the mod in this version because a later version installs it
							continue;

						var modTask = $"{versionTask}\nProcessing mod {modId} ({++currentMod} / {version.Mods.Count})...";

						progress?.ReportProgress(modTask);

						// Delete old version if necessary
						if (mod.RemoveOld || !string.IsNullOrEmpty(mod.RemovePattern))
						{
							progress?.ReportProgress($"{modTask}\n" +
													 $"Deleting old versions...");

							var deletePattern = mod.RemovePattern ?? $"{modId}-.*";
							var toDelete = currentModFiles.Where(file => Regex.IsMatch(file, deletePattern)).ToList();

							foreach (var file in toDelete)
								File.Delete(file);
						}

						// Install new version if necessary
						if (!string.IsNullOrEmpty(mod.FileUri))
						{
							var fileName = Path.GetFileName(mod.FileUri) ?? $"{modId}.jar";
							var filePath = Path.Combine(minecraftDir, "mods", fileName);

							downloadTask = $"{modTask}\n" +
										   $"Downloading mod archive: {fileName}";

							await downloader.DownloadFileAsync(mod.FileUri, filePath, token);
						}
					}
				}

				if (version.ConfigReplacements != null)
				{
					var configsTask = $"{versionTask}\nApplying config replacements...";

					// Apply config replacements
					progress?.ReportProgress(-1, configsTask);
					var configDir = Path.Combine(minecraftDir, "config");
					var currentConfig = 0;
					foreach (var (configPath, replacements) in version.ConfigReplacements)
					{
						var configFilePath = Path.Combine(configDir, configPath);

						if (File.Exists(configFilePath))
						{
							progress?.ReportProgress(( currentConfig++ / (double) version.ConfigReplacements.Count ),
													 $"{configsTask}\n" +
													 $"Config file {currentConfig} of {version.ConfigReplacements.Count}: " +
													 $"{configPath}");

							var configFileContents = await File.ReadAllTextAsync(configFilePath, Encoding.UTF8, token).ConfigureAwait(false);

							foreach (var replacement in replacements)
								configFileContents = Regex.Replace(configFileContents, replacement.Replace, replacement.With);

							await File.WriteAllTextAsync(configFilePath, configFileContents, Encoding.UTF8, token).ConfigureAwait(false);
						}
					}
				}

				if (version.ExtraFiles != null)
				{
					var extraFilesTask = $"{versionTask}\nDownloading extra files...";

					// Download extra files
					progress?.ReportProgress(-1, extraFilesTask);
					var currentFile = 0;
					foreach (var (filename, url) in version.ExtraFiles)
					{
						var filePath = Path.Combine(minecraftDir, filename);

						downloadTask = $"{extraFilesTask}\n" +
									   $"Downloading extra file {currentFile++} of {version.ExtraFiles.Count}: {filename}";

						try
						{
							if (File.Exists(filePath))
								File.Delete(filePath);

							await downloader.DownloadFileAsync(url, filePath, token);
						}
						catch
						{
							throw new Exception($"Could not download extra file \"{filename}\" from {url}");
						}
					}
				}
			}

			#region VR Stuff

			if (pack.SupportsVR)
			{
				// Handle VR-specific pack settings
				string vrTask = $"Setting up Vivecraft for {( vrEnabled ? "VR" : "non-VR" )} play...";

				progress?.ReportProgress(-1, vrTask);

				var optifineDownloadRegex = new Regex(@"href=(['""])(downloadx\?f=[^'""]+)\1", RegexOptions.Compiled | RegexOptions.IgnoreCase);

				// Set up temporary directory
				var installDirectory = Path.Combine(Path.GetTempPath(), "MinecraftInstaller");

				if (!Directory.Exists(installDirectory))
					Directory.CreateDirectory(installDirectory);

				if (!Directory.Exists(librariesDir))
					Directory.CreateDirectory(librariesDir);

				if (!Directory.Exists(patchesDir))
					Directory.CreateDirectory(patchesDir);

				// Download Vivecraft installer
				var vivecraftVersion = vrEnabled ? Constants.Vivecraft.VivecraftVersionVr : Constants.Vivecraft.VivecraftVersionNonVr;
				var vivecraftRevision = vrEnabled ? Constants.Vivecraft.VivecraftRevisionVr : Constants.Vivecraft.VivecraftRevisionNonVr;
				var vivecraftInstallerFilename = Path.Combine(installDirectory, "vivecraft_installer.exe");

				downloadTask = $"{vrTask}\n" +
							   $"Downloading Vivecraft installer...";

				var vivecraftInstallerUri = Constants.Vivecraft.GetVivecraftInstallerUri(vrEnabled);

				await downloader.DownloadFileAsync(vivecraftInstallerUri, vivecraftInstallerFilename, token);

				progress?.ReportProgress(-1, $"{vrTask}\nExtracting Vivecraft installer...");

				await using (var fs = File.Open(vivecraftInstallerFilename, FileMode.Open, FileAccess.Read))
				{
					using var zip = new ZipFile(fs);
					var versionJarEntry = zip.GetEntry("version.jar");

					if (versionJarEntry == null || !versionJarEntry.IsFile)
						throw new Exception("Could not find Vivecraft jar file in the installer");

					var minecriftFilename = $"minecrift-{vivecraftVersion}-{vivecraftRevision}.jar";

					await using var entryStream = zip.GetInputStream(versionJarEntry);
					await using var destStream = File.Open(Path.Combine(librariesDir, minecriftFilename), FileMode.Create, FileAccess.Write);

					await entryStream.CopyToAsync(destStream, token);
					await destStream.FlushAsync(token);
				}

				// Download Optifine
				downloadTask = $"{vrTask}\n" +
							   $"Fetching Optifine download information...";

				var optifineDownloadPage = await http.GetStringAsync(Constants.Vivecraft.OptifineMirrorUri, token);

				if (string.IsNullOrEmpty(optifineDownloadPage))
					throw new Exception("Could not download Optifine from the mirror");

				var mirrorPageMatch = optifineDownloadRegex.Match(optifineDownloadPage);

				if (!mirrorPageMatch.Success)
					throw new Exception("Unexpected response from Optifine mirror");

				var optifineDownloadUrl = Constants.Vivecraft.OptifineBaseUri + mirrorPageMatch.Groups[2].Value;
				var optifineLibraryPath = Path.Combine(librariesDir, Constants.Vivecraft.OptifineLibraryFilename);

				downloadTask = $"{vrTask}\n" +
							   $"Downloading Optifine archive...";

				await downloader.DownloadFileAsync(optifineDownloadUrl, optifineLibraryPath, token);

				// Write patch file
				progress?.ReportProgress(-1, "Writing Vivecraft patch file...");

				var patchFileJson = JsonSerializer.Serialize(
					vrEnabled ? PrismPatchDefinitions.VivecraftPatchVr : PrismPatchDefinitions.VivecraftPatchNonVr,
					new JsonSerializerOptions(JsonSerializerOptions) {
						DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
					}
				);

				await File.WriteAllTextAsync(Path.Combine(patchesDir, "vivecraft.json"), patchFileJson, token).ConfigureAwait(false);

				// Clean up temporary installation files
				progress?.ReportProgress(-1, "Cleaning up temporary files...");

				try
				{
					Directory.Delete(installDirectory);
				}
				catch
				{
					/* Ignored */
				}
			}

			#endregion

			// Finally write the current version to the instance version file
			// We do this last so that if a version upgrade fails, the user
			// can resume at the last fully completed version
			await using var packMetaFileStream = File.Open(metadataFile, FileMode.Create, FileAccess.Write, FileShare.None);

			await JsonSerializer.SerializeAsync(packMetaFileStream, instanceMetadata, JsonSerializerOptions, token);
		}
		finally
		{
			try
			{
				Directory.Delete(tempDir, true);
			}
			catch
			{
				// Ignored
			}
		}
	}

	public async Task NewInstanceAsync(PrismConfig prismConfig, string instName, bool vrEnabled, CancellationToken token, ProgressReporter progress = null, int? maxRamMb = null)
	{
		var newInstDir = Path.Combine(prismConfig.InstancesFolder, instName);
		var newInstCfg = Path.Combine(newInstDir, "instance.cfg");

		if (Directory.Exists(newInstDir))
			throw new InvalidOperationException("An instance with the specified name already exists");

		progress?.ReportProgress("Creating new instance directory...");

		Directory.CreateDirectory(newInstDir);

		progress?.ReportProgress("Loading pack metadata from server...");

		var pack = await LoadConfigAsync(token);
		var instance = new PrismInstance {
			Name = instName,
			Icon = IconName,
			InstanceType = pack.InstanceType,
			OverrideJavaArgs = true,
			JvmArgs = Constants.Vivecraft.VivecraftJvmArgs,
		};

		token.ThrowIfCancellationRequested();
		progress?.ReportProgress("Creating new instance config file...");

		await using (var fs = File.Open(newInstCfg, FileMode.Create, FileAccess.Write, FileShare.None))
		{
			await using var sw = new StreamWriter(fs);

			await ConfigReader.WriteConfig(instance, sw);
		}

		await SetupPackAsync(prismConfig, pack, newInstDir, true, vrEnabled, token, progress, maxRamMb);
	}

	public async Task ConvertInstance(PrismConfig prismConfig, string instPath, bool forceRebuild, bool vrEnabled, CancellationToken token, ProgressReporter progress = null, int? maxRamMb = null)
	{
		if (!Directory.Exists(instPath))
			throw new InvalidOperationException("The specified instance folder was not found");

		if (Directory.Exists(Path.Combine(instPath, "minecraft")) || !Directory.Exists(Path.Combine(instPath, ".minecraft")))
			throw new InvalidOperationException("The existing instance is using the old MultiMC pack format. " +
												"This installer cannot upgrade an instance to the new Prism Launcher pack format.\n\n" +
												"Please use the \"New Instance\" option to create a new Prism Launcher instance.");

		var instCfg = Path.Combine(instPath, "instance.cfg");
		var pack = await LoadConfigAsync(token);
		PrismInstance instance;

		await using (var fs = File.Open(instCfg, FileMode.Open, FileAccess.Read, FileShare.Read))
		{
			using var sr = new StreamReader(fs);

			instance = await ConfigReader.ReadConfig<PrismInstance>(sr);
		}

		token.ThrowIfCancellationRequested();

		if (instance.InstanceType != pack.InstanceType)
		{
			throw new InvalidOperationException("The existing instance is set up for a different version of Minecraft " +
												"than the target instance. The upgrader cannot convert an existing instance " +
												"to a different Minecraft version.\n\n" +
												"Please create a new instance, or manually upgrade the existing instance to " +
												$"Minecraft {pack.IntendedMinecraftVersion}");
		}

		// Apply Vivecraft JVM arguments
		instance.OverrideJavaArgs = true;
		instance.JvmArgs = Constants.Vivecraft.VivecraftJvmArgs;
		instance.Icon = IconName;

		await using (var fs = File.Open(instCfg, FileMode.Open, FileAccess.Write, FileShare.Read))
		{
			await using var sr = new StreamWriter(fs);

			await ConfigReader.WriteConfig(instance, sr);
		}

		await SetupPackAsync(prismConfig, pack, instPath, forceRebuild, vrEnabled, token, progress, maxRamMb);
	}
}