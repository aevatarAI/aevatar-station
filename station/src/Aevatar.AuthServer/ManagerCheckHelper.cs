using Newtonsoft.Json;
using Aevatar.Dtos;
using Serilog;

namespace Aevatar;

public class ManagerCheckHelper
{
    private static readonly HttpClient httpClient = new HttpClient();

    public static async Task<bool?> CheckManagerFromCache(string checkUrl, string manager, string caHash)
    {
        try
        {
            var url = $"{checkUrl}?manager={manager}";
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return !content.IsNullOrEmpty() && caHash.Equals(JsonConvert.DeserializeObject<ManagerCacheDto>(content)?.CaHash);
        }
        catch (Exception e)
        {
            Log.ForContext<ManagerCheckHelper>().Error($"CheckManagerFromCache fail ${e.Message}", e);
            return false;
        }
    }
}