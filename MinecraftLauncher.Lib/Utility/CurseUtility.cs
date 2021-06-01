using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MinecraftLauncher.Modpack;
using Newtonsoft.Json;

namespace MinecraftLauncher.Utility
{
	public static class CurseUtility
	{
		private const string MetabaseUrl = "https://cursemeta.dries007.net";

		public static async Task<CurseFileInfo> GetFileInfoAsync(int projectId, int fileId, CancellationToken cancellationToken = default)
		{
			using var http = new HttpClient();
			var requestUrl = $"{MetabaseUrl}/{projectId}/{fileId}.json";
			var response = await http.GetAsync(requestUrl, cancellationToken);

			response.EnsureSuccessStatusCode();

			var responseJson = await response.Content.ReadAsStringAsync();

			return JsonConvert.DeserializeObject<CurseFileInfo>(responseJson);
		}
	}
}