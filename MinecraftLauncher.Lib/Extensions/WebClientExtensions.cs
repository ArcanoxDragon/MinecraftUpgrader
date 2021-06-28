using System.Net;
using System.Threading.Tasks;

namespace MinecraftLauncher.Extensions
{
	public static class WebClientExtensions
	{
		public static async Task DownloadFileWrappedAsync(this WebClient webClient, string address, string fileName)
		{
			try
			{
				await webClient.DownloadFileTaskAsync(address, fileName);
			}
			catch (WebException ex)
			{
				// Add the URI into the exception message
				throw new WebException($"Unable to download file from {address}: {ex.Message}", ex, ex.Status, ex.Response);
			}
		}
	}
}