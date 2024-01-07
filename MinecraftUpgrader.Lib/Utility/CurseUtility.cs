using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MinecraftUpgrader.Modpack;

namespace MinecraftUpgrader.Utility;

public static class CurseUtility
{
	private const string MetabaseUrl = "https://cursemeta.dries007.net";

	public static async Task<CurseFileInfo> GetFileInfoAsync(int projectId, int fileId, CancellationToken cancellationToken = default)
	{
		using var http = WebUtility.CreateHttpClient();
		var requestUrl = $"{MetabaseUrl}/{projectId}/{fileId}.json";
		var responseJson = await http.GetStringAsync(requestUrl, cancellationToken);

		return JsonSerializer.Deserialize<CurseFileInfo>(responseJson);
	}
}