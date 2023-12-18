using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CmlLib.Core;
using CmlLib.Core.Downloader;
using CmlLib.Core.Version;
using CmlLib.Core.VersionLoader;
using ICSharpCode.SharpZipLib.Zip;
using MinecraftLauncher.Async;
using MinecraftLauncher.Extensions;
using MinecraftLauncher.Utility;
using Newtonsoft.Json.Linq;
using Semver;

namespace MinecraftLauncher.Modpack
{
	public sealed class VivecraftInstallResult
	{
		public string VersionNameVr    { get; set; }
		public string VersionNameNonVr { get; set; }
	}

	public class VivecraftInstaller
	{
		private static readonly Regex OptifineDownloadRegex = new(@"href=(['""])(downloadx\?f=[^'""]+)\1", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		public VivecraftInstaller(string profilePath)
		{
			ProfilePath = profilePath;
		}

		public string ProfilePath { get; }

		public async Task<VivecraftInstallResult> InstallVivecraftAsync(PackMetadata pack, string baseForgeVersionName, ProgressReporter progress = default, CancellationToken cancellationToken = default)
		{
			var librariesDir = Path.Combine(ProfilePath, "libraries");

			using var web = new WebClient();

			var downloadTask = "";

			web.DownloadProgressChanged += (_, args) => {
				// ReSharper disable once AccessToModifiedClosure
				progress.ReportDownloadProgress(downloadTask, args);
			};

			// Handle VR-specific pack settings
			const string vrTask = "Setting up Vivecraft...";

			progress?.ReportProgress(-1, vrTask);

			// Set up temporary directory
			var installDirectory = Path.Combine(Path.GetTempPath(), "MinecraftLauncher");

			if (!Directory.Exists(installDirectory))
				Directory.CreateDirectory(installDirectory);
			if (!Directory.Exists(librariesDir))
				Directory.CreateDirectory(librariesDir);

			cancellationToken.ThrowIfCancellationRequested();

			// Download Optifine
			downloadTask = $"{vrTask}\nFetching Optifine download information...";

			var optifineDownloadPage = await web.DownloadStringTaskAsync(pack.OptifineUri);

			if (string.IsNullOrEmpty(optifineDownloadPage))
				throw new Exception("Could not download Optifine from the mirror");

			var mirrorPageMatch = OptifineDownloadRegex.Match(optifineDownloadPage);

			if (!mirrorPageMatch.Success)
				throw new Exception("Unexpected response from Optifine mirror");

			var optifineDownloadUrl = Constants.Optifine.BaseUri + mirrorPageMatch.Groups[2].Value;

			downloadTask = $"{vrTask}\nDownloading Optifine archive...";

			var optifinePath = Path.Combine(librariesDir, "optifine", "OptiFine", pack.OptifineVersion);
			var optifineFilename = Path.Combine(optifinePath, $"OptiFine-{pack.OptifineVersion}.jar");

			Directory.CreateDirectory(optifinePath);

			await web.DownloadFileWrappedAsync(optifineDownloadUrl, optifineFilename);
			cancellationToken.ThrowIfCancellationRequested();

			async Task<string> SetupVersionAsync(bool vrEnabled, string versionSuffix)
			{
				var minecriftVersion = vrEnabled ? pack.MinecriftVersionVr : pack.MinecriftVersionNonVr;
				var vivecraftVersionFilename = Path.Combine(installDirectory, "version.json");

				// Download Vivecraft installer
				var vivecraftInstallerFilename = Path.Combine(installDirectory, "vivecraft_installer.exe");
				var vivecraftInstallerUri = vrEnabled ? pack.VivecraftUriVr : pack.VivecraftUriNonVr;

				downloadTask = $"{vrTask}\nDownloading Vivecraft installer...";

				// ReSharper disable once AccessToDisposedClosure
				await web.DownloadFileWrappedAsync(vivecraftInstallerUri, vivecraftInstallerFilename);

				cancellationToken.ThrowIfCancellationRequested();
				progress?.ReportProgress(-1, $"{vrTask}\nExtracting Vivecraft installer...");

				using (var fs = File.Open(vivecraftInstallerFilename, FileMode.Open, FileAccess.Read))
				{
					using var zip = new ZipFile(fs);
					var versionJarEntry = zip.GetEntry("version.jar");
					var versionJsonEntry = zip.GetEntry("version-forge.json");

					if (versionJarEntry is not { IsFile: true })
						throw new Exception("Could not find Vivecraft jar file in the Vivecraft installer");
					if (versionJsonEntry is not { IsFile: true })
						throw new Exception("Could not find Version JSON file in the Vivecraft installer");

					var minecriftPath = Path.Combine(librariesDir, "com", "mtbs3d", "minecrift", minecriftVersion);
					var minecriftFilename = Path.Combine(minecriftPath, $"minecrift-{minecriftVersion}.jar");

					Directory.CreateDirectory(minecriftPath);

					// Extract Vivecraft (Minecrift) JAR file
					using (var jarEntryStream = zip.GetInputStream(versionJarEntry))
					{
						using var jarDestStream = File.Open(minecriftFilename, FileMode.Create, FileAccess.Write);

						await jarEntryStream.CopyToAsync(jarDestStream);
						await jarDestStream.FlushAsync(cancellationToken);
					}

					// Extract version JSON file
					using (var jsonEntryStream = zip.GetInputStream(versionJsonEntry))
					{
						using var jsonDestStream = File.Open(vivecraftVersionFilename, FileMode.Create, FileAccess.Write);

						await jsonEntryStream.CopyToAsync(jsonDestStream);
						await jsonEntryStream.FlushAsync(cancellationToken);
					}
				}

				cancellationToken.ThrowIfCancellationRequested();

				// Copy base version to new version
				var baseVersionFolder = Path.GetFullPath(Path.Combine(ProfilePath, "versions", baseForgeVersionName));
				var newVersionName = $"{baseForgeVersionName}-{versionSuffix}";
				var newVersionFolder = Path.GetFullPath(Path.Combine(ProfilePath, "versions", newVersionName));

				foreach (var file in Directory.EnumerateFiles(baseVersionFolder, "*", SearchOption.AllDirectories))
				{
					var fullFilePath = Path.GetFullPath(file);
					var newFilePath = fullFilePath.Replace(baseVersionFolder, newVersionFolder);
					var newDirectory = Path.GetDirectoryName(newFilePath);

					Directory.CreateDirectory(newDirectory);
					File.Copy(fullFilePath, newFilePath, true);
				}

				// Rename version file
				var oldVersionFileName = Path.Combine(newVersionFolder, $"{baseForgeVersionName}.json");
				var newVersionFileName = Path.Combine(newVersionFolder, $"{newVersionName}.json");

				if (File.Exists(newVersionFileName))
					File.Delete(newVersionFileName);

				File.Move(oldVersionFileName, newVersionFileName);

				// Load the Vivecraft version file to reference it
				var vivecraftVersionJson = File.ReadAllText(vivecraftVersionFilename);
				var vivecraftVersionObj = JObject.Parse(vivecraftVersionJson);

				// Load the new version file to change it
				var newVersionJson = File.ReadAllText(newVersionFileName);
				var newVersionObj = JObject.Parse(newVersionJson);

				// Merge the Vivecraft libraries into the new version
				MergeLibraries(vivecraftVersionObj, newVersionObj);

				// Write new version file
				newVersionObj["id"] = newVersionName;
				newVersionJson      = newVersionObj.ToString();

				File.WriteAllText(newVersionFileName, newVersionJson);

				// Download the required libraries
				var mcPath = new MinecraftPath(ProfilePath);
				var mcVersionLoader = new MojangVersionLoader();
				var mcVersions = await mcVersionLoader.GetVersionMetadatasAsync();
				var mcVersion = await mcVersions.GetVersionAsync(newVersionName);
				var launcher = new CMLauncher(mcPath);

				launcher.FileChanged += args => progress?.ReportProgress(args.ProgressedFileCount / (double) args.TotalFileCount,
																		  $"Downloading Minecraft assets...\nDownloading file {args.FileName} (file {args.ProgressedFileCount} / {args.TotalFileCount})");

				await launcher.CheckAndDownloadAsync(mcVersion);

				// Return the version name
				return newVersionName;
			}

			cancellationToken.ThrowIfCancellationRequested();

			var result = new VivecraftInstallResult {
				VersionNameNonVr = await SetupVersionAsync(false, "nonvr"),
				VersionNameVr    = await SetupVersionAsync(true, "vr"),
			};

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

			return result;
		}

		private void MergeLibraries(JObject fromVersion, JObject toVersion)
		{
			var leftLibraryObjs = (JArray) fromVersion["libraries"];
			var rightLibraryObjs = (JArray) toVersion["libraries"];
			var leftLibraries = leftLibraryObjs.Select(ParseLibrary).ToList();
			var rightLibraries = rightLibraryObjs.Select(ParseLibrary).ToList();

			foreach (var leftLibrary in leftLibraries.Where(l => l.Valid))
			{
				var rightLibrary = rightLibraries.SingleOrDefault(l => l.Name == leftLibrary.Name);

				if (rightLibrary is null)
				{
					// Add the library to the right immediately

					rightLibraryObjs.Add(leftLibrary.From);
				}
				else
				{
					if (leftLibrary.Version == null || rightLibrary.Version == null)
					{
						// Couldn't parse semantic version on one side, but the library is present on both sides,
						// so we just assume we don't have to do anything
						continue;
					}

					if (leftLibrary.Version > rightLibrary.Version)
					{
						// Remove the existing library on the right and then add the newer one from the left
						rightLibraryObjs.Remove(rightLibrary.From);
						rightLibraryObjs.Add(leftLibrary.From);
					}
				}
			}

			toVersion["libraries"] = rightLibraryObjs;
		}

		private ParsedLibrary ParseLibrary(JToken libraryObj)
		{
			var name = libraryObj.Value<string>("name");

			if (name is null)
				return null;

			var library = new ParsedLibrary {
				Key  = name,
				From = libraryObj,
			};

			var nameSplit = name.Split(':');

			if (nameSplit.Length != 3)
				return library;

			var libraryName = string.Join(":", nameSplit[0], nameSplit[1]);
			var versionStr = nameSplit[2];

			library.Name          = libraryName;
			library.VersionString = versionStr;
			library.Valid         = true;

			if (SemVersion.TryParse(versionStr, out var version))
			{
				library.Version = version;
			}

			return library;
		}

		private sealed class ParsedLibrary
		{
			public string     Key           { get; set; }
			public string     Name          { get; set; }
			public string     VersionString { get; set; }
			public SemVersion Version       { get; set; }
			public bool       Valid         { get; set; }
			public JToken     From          { get; set; }

			public override string ToString()
				=> $"ParsedLibrary{{ Name={Name}, VersionString={VersionString}, Version={Version} }}";
		}
	}
}