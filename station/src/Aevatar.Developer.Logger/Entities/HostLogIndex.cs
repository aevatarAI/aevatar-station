using System.Text.Json.Serialization;

namespace Aevatar.Developer.Logger.Entities;

public class HostLogIndex
{
    [JsonPropertyName("@timestamp")] public DateTime Timestamp { get; set; }

    [JsonPropertyName("app_log")] public AppLogInfo? AppLog { get; set; }
}