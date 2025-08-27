using Newtonsoft.Json;

namespace Aevatar.SignalR.Extensions;

public static class ObjectArrayExtensions
{
    public static string[] ToStrings(this object[] objects)
    {
        return objects.Select(JsonConvert.SerializeObject).ToArray();
    }
}