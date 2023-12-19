using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Humanizer;
using ICSharpCode.SharpZipLib.Zip;
using MinecraftUpgrader.Async;
using MinecraftUpgrader.Config;
using MinecraftUpgrader.MultiMC;
using MinecraftUpgrader.Zip;

namespace MinecraftUpgrader.Utility
{
	public static class MultiMcInstaller
	{
		public static async Task<string> InstallMultiMC(ProgressReporter progress, CancellationToken cancellationToken = default)
		{
			var tempDir = Path.GetTempPath();
			var tempFile = Path.Combine(tempDir, "multimc.zip");

			using (var http = new HttpClient())
			{
				var downloader = new FileDownloader(http);

				downloader.ProgressChanged += (_, args) => {
					var dlSize = args.BytesReceived.Bytes();
					var totalSize = args.TotalBytesToReceive.Bytes();

					progress?.ReportProgress(args.BytesReceived / (double) args.TotalBytesToReceive,
											 $"Downloading MultiMC... ({dlSize.ToString("0.##")} / {totalSize.ToString("0.##")})");
				};

				await downloader.DownloadFileAsync(Constants.Paths.MultiMcDownload, tempFile, cancellationToken);
			}

			var mmcInstallPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MultiMC");

			if (!Directory.Exists(mmcInstallPath))
				Directory.CreateDirectory(mmcInstallPath);

			await using (var fs = File.Open(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				using var zip = new ZipFile(fs);

				progress?.ReportProgress(-1, "Extracting MultiMC...");

				await zip.ExtractAsync(mmcInstallPath,
									   new ZipExtractOptions {
										   DirectoryName = "MultiMC",
										   RecreateFolderStructure = false,
										   OverwriteExisting = true,
										   CancellationToken = cancellationToken,
										   ProgressReporter = progress,
									   });
			}

			File.Delete(tempFile);

			progress?.ReportProgress(-1, "Setting up MultiMC...");

			var mmcConfig = await MmcConfigReader.CreateConfig(mmcInstallPath);
			var javaPath = AppConfig.Get().JavaPath;
			var javaVersion = AppConfig.Get().JavaVersion;

			if (javaPath != null && javaVersion != null)
			{
				mmcConfig.JavaPath = javaPath.Replace("java.exe", "javaw.exe");
				mmcConfig.JavaVersion = javaVersion;

				await MmcConfigReader.UpdateConfig(mmcInstallPath, mmcConfig);
			}

			AppConfig.Update(cfg => cfg.LastMmcPath = mmcInstallPath);

			return mmcInstallPath;
		}
	}
}