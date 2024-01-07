using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Humanizer;
using ICSharpCode.SharpZipLib.Zip;
using MinecraftUpgrader.Async;
using MinecraftUpgrader.Config;
using MinecraftUpgrader.Prism;
using MinecraftUpgrader.Zip;

namespace MinecraftUpgrader.Utility;

public static class PrismUtility
{
	public static bool TryAutoLocatePrism(out string programPath, out string configFolder)
	{
		// Try and locate Prism in known locations

		programPath = null;
		configFolder = null;

		var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		var prismProgramFolder = Path.Combine(localAppData, "Programs", "PrismLauncher");
		var prismProgramPath = Path.Combine(prismProgramFolder, "prismlauncher.exe");

		if (!File.Exists(prismProgramPath))
			return false;

		// Found non-portable version. See if we can find the config file too.
		var prismConfigFolder = Path.Combine(appData, "PrismLauncher");
		var prismConfigFile = Path.Combine(prismConfigFolder, "prismlauncher.cfg");

		if (!File.Exists(prismConfigFile))
			return false;

		programPath = prismProgramPath;
		configFolder = prismConfigFolder;
		return true;
	}

	public static async Task<string> InstallPrism(ProgressReporter progress, CancellationToken cancellationToken = default)
	{
		var tempDir = Path.GetTempPath();
		var tempFile = Path.Combine(tempDir, "prismlauncher.zip");

		using (var http = WebUtility.CreateHttpClient())
		{
			var downloader = new FileDownloader(http);

			downloader.ProgressChanged += (_, args) => {
				var dlSize = args.BytesReceived.Bytes();
				var totalSize = args.TotalBytesToReceive.Bytes();

				progress?.ReportProgress(args.BytesReceived / (double) args.TotalBytesToReceive,
										 $"Downloading Prism Launcher... ({dlSize.ToString("0.##")} / {totalSize.ToString("0.##")})");
			};

			await downloader.DownloadFileAsync(Constants.Paths.PrismDownload, tempFile, cancellationToken);
		}

		var installPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PrismLauncher");

		if (!Directory.Exists(installPath))
			Directory.CreateDirectory(installPath);

		await using (var fs = File.Open(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read))
		{
			using var zip = new ZipFile(fs);

			progress?.ReportProgress(-1, "Extracting Prism Launcher...");

			await zip.ExtractAsync(installPath,
								   new ZipExtractOptions {
									   RecreateFolderStructure = false,
									   OverwriteExisting = true,
									   CancellationToken = cancellationToken,
									   ProgressReporter = progress,
								   });
		}

		File.Delete(tempFile);

		progress?.ReportProgress(-1, "Setting up Prism Launcher...");

		var prismConfig = await PrismConfigReader.CreateConfig(installPath);
		var javaPath = AppConfig.Get().JavaPath;
		var javaVersion = AppConfig.Get().JavaVersion;

		if (javaPath != null && javaVersion != null)
		{
			prismConfig.JavaPath = javaPath.Replace("java.exe", "javaw.exe");
			prismConfig.JavaVersion = javaVersion;

			await PrismConfigReader.UpdateConfig(installPath, prismConfig);
		}

		AppConfig.Update(cfg => cfg.LastPrismPath = installPath);

		return installPath;
	}
}