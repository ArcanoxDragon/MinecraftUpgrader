﻿#pragma warning disable 162
// ReSharper disable HeuristicUnreachableCode

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using Humanizer;
using Microsoft.Extensions.Options;
using MinecraftLauncher.Extensions;
using MinecraftLauncher.Options;
using MinecraftLauncher.Utility;

namespace MinecraftLauncher
{
	public partial class UpdateForm : Form
	{
		public enum Result
		{
			NeedsRestart,
			NoUpdateNeeded,
			Failed,
		}

		private readonly string remoteFileUrl;
		private readonly string remoteMd5Url;

		public UpdateForm(IOptions<PackBuilderOptions> options)
		{
			this.remoteFileUrl = $"{options.Value.ModPackUrl}/MinecraftLauncher.exe";
			this.remoteMd5Url  = $"{options.Value.ModPackUrl}/installercheck.php";

			this.InitializeComponent();
		}

		public async Task<Result> CheckForUpdatesAsync()
		{
			try
			{
				this.Show();

#if DEBUG
				const bool SkipUpdate = true;
#else
				const bool SkipUpdate = false;
#endif

				var curProcess = Process.GetCurrentProcess();
				var processFilePath = curProcess.MainModule?.FileName;
				var processFileOldPath = $"{processFilePath}.old";

				if (string.IsNullOrEmpty(processFilePath))
				{
					MessageBox.Show(this,
									"Fatal error...could not locate the running program on the file system.",
									"Update Failed",
									MessageBoxButtons.OK,
									MessageBoxIcon.Error);

					return Result.Failed;
				}

				var otherProcesses = Process.GetProcessesByName(curProcess.ProcessName)
											.Where(p => p.Id != curProcess.Id);

				foreach (var process in otherProcesses)
				{
					process.WaitForExit(3000);
					process.Kill();
				}

				if (File.Exists(processFileOldPath))
					File.Delete(processFileOldPath);

				using var web = new WebClient();

				var localHash = CryptoUtility.CalculateFileMd5(processFilePath);
				var remoteHash = await web.DownloadStringTaskAsync(this.remoteMd5Url);

				// ReSharper disable once RedundantLogicalConditionalExpressionOperand
				// ReSharper disable once ConditionIsAlwaysTrueOrFalse
				if (SkipUpdate || localHash == remoteHash)
					return Result.NoUpdateNeeded;

				this.lbStatus.Text    = "Updating program...";
				this.pbDownload.Style = ProgressBarStyle.Continuous;

				web.DownloadProgressChanged += (o, e) => this.Invoke(new Action(() => {
					var dlSize = e.BytesReceived.Bytes();
					var totalSize = e.TotalBytesToReceive.Bytes();

					this.lbProgress.Text  = $"Downloading updates... {dlSize.ToString("0.##")} / {totalSize.ToString("0.##")} ({e.ProgressPercentage}%)";
					this.pbDownload.Value = e.ProgressPercentage;
				}));

				var updateFilePath = Path.Combine(Path.GetTempPath(), "MinecraftLauncher.exe");

				if (File.Exists(updateFilePath))
					File.Delete(updateFilePath);

				await web.DownloadFileWrappedAsync(this.remoteFileUrl, updateFilePath);

				this.lbStatus.Text    = "Installing updates...";
				this.pbDownload.Style = ProgressBarStyle.Marquee;

				File.Move(processFilePath, processFileOldPath);
				File.Move(updateFilePath, processFilePath);

				var newLocalHash = CryptoUtility.CalculateFileMd5(processFilePath);

				if (newLocalHash != remoteHash)
				{
					MessageBox.Show(this,
									"Checksum mismatch! The update file is invalid. Update failed.",
									"Update Failed",
									MessageBoxButtons.OK,
									MessageBoxIcon.Error);

					File.Delete(processFilePath);
					File.Move(processFileOldPath, processFilePath);

					return Result.Failed;
				}

				MessageBox.Show(this,
								"Update complete! Click OK to restart the program and apply the update.",
								"Update Complete",
								MessageBoxButtons.OK,
								MessageBoxIcon.Information);

				Process.Start(new ProcessStartInfo {
					FileName        = processFilePath,
					UseShellExecute = true,
				});

				return Result.NeedsRestart;
			}
			catch (Exception ex)
			{
				Console.WriteLine("Exception in updater:");
				Console.WriteLine(ex);
				throw;
			}
			finally
			{
				this.Close();
			}
		}
	}
}