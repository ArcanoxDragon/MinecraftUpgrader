using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Humanizer;
using ICSharpCode.SharpZipLib.Zip;
using MinecraftUpgrader.Async;
using MinecraftUpgrader.Config;
using MinecraftUpgrader.Prism;
using MinecraftUpgrader.Zip;

namespace MinecraftUpgrader.Utility;

public static class PrismInstaller
{
	public static async Task<string> InstallPrism(ProgressReporter progress, CancellationToken cancellationToken = default)
	{
		var tempDir = Path.GetTempPath();
		var tempFile = Path.Combine(tempDir, "prismlauncher.zip");

		using (var http = new HttpClient())
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
									   DirectoryName = "PrismLauncher",
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