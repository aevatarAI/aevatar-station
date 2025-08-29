using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Aevatar.GAgents.Telegram.Helper;

public static class HttpResponseExtension
{
    public static async Task<T> DeserializeSnakeCaseAsync<T>(this HttpContent content)
    {
        var respData = await content.ReadAsStringAsync();
        var serializeSetting = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            }
        };

        return JsonConvert.DeserializeObject<T>(respData, serializeSetting);
    }
}