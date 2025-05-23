using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Aevetar.Developer.Logger.Entities;

public class AppLogInfo
{
    [JsonPropertyName("@m")] public string Message { get; set; }
    [JsonPropertyName("@i")] public string LogId { get; set; }
    [JsonPropertyName("@t")] public DateTime Time { get; set; }

    [JsonPropertyName("@l")] public string Level { get; set; }
    [JsonPropertyName("@x")] public string Exception { get; set; }

    public string SourceContext { get; set; }

    public string HostId { get; set; }

    public string Version { get; set; }

    public string Application { get; set; }

    public string Environment { get; set; }
}