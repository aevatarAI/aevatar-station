using System.Text.Json.Serialization;
using Aevetar.Developer.Logger.Entities;

namespace Aevatar.Developer.Logger.Entities;

public class HostLogIndex
{
    [JsonPropertyName("@timestamp")] public DateTime Timestamp { get; set; }

    public AppLogInfo App_log { get; set; }
}