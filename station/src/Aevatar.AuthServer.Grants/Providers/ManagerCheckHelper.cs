using Newtonsoft.Json;

namespace Aevatar.AuthServer.Grants.Providers;

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
            return false;
        }
    }
}