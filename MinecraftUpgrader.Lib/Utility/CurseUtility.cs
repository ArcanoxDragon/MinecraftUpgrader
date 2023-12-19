using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MinecraftUpgrader.Modpack;
using Newtonsoft.Json;

namespace MinecraftUpgrader.Utility
{
	public static class CurseUtility
	{
		private const string MetabaseUrl = "https://cursemeta.dries007.net";

		public static async Task<CurseFileInfo> GetFileInfoAsync(int projectId, int fileId, CancellationToken cancellationToken = default)
		{
			using var http = new HttpClient();
			var requestUrl = $"{MetabaseUrl}/{projectId}/{fileId}.json";
			var responseJson = await http.GetStringAsync(requestUrl, cancellationToken);

			return JsonConvert.DeserializeObject<CurseFileInfo>(responseJson);
		}
	}
}