using System.Net;
using Humanizer;
using MinecraftLauncher.Async;

namespace MinecraftLauncher.Extensions
{
	public static class ProgressReporterExtensions
	{
		public static void ReportDownloadProgress(this ProgressReporter progress, string taskName, DownloadProgressChangedEventArgs args)
		{
			if (progress is null) return;

			var downloaded = args.BytesReceived.Bytes();
			var total = args.TotalBytesToReceive.Bytes();

			if (args.TotalBytesToReceive > 0)
			{
				// TODO: When Humanizer update is released, replace ToString calls below with interpolation shorthand
				progress?.ReportProgress(args.BytesReceived / (double) args.TotalBytesToReceive,
										 $"{taskName} ({downloaded.ToString("0.##")} / {total.ToString("0.##")})");
			}
			else
			{
				// TODO: When Humanizer update is released, replace ToString calls below with interpolation shorthand
				progress?.ReportProgress(-1, $"{taskName} ({downloaded.ToString("0.##")})");
			}
		}
	}
}