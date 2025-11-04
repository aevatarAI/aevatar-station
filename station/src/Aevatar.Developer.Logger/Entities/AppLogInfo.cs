using System.Text.Json.Serialization;

namespace Aevatar.Developer.Logger.Entities;

public class AppLogInfo
{
    [JsonPropertyName("@m")] public string? Message { get; set; }
    [JsonPropertyName("@i")] public string? LogId { get; set; }
    [JsonPropertyName("@t")] public DateTime Time { get; set; }
    [JsonPropertyName("@l")] public string? Level { get; set; }
    [JsonPropertyName("@x")] public string? Exception { get; set; }
    [JsonPropertyName("@tr")] public string? TraceId { get; set; }
    [JsonPropertyName("@sp")] public string? SpanId { get; set; }
    
    [JsonPropertyName("LogCategory")] public string? LogCategory { get; set; }
    [JsonPropertyName("WorkflowId")] public string? WorkflowId { get; set; }
    [JsonPropertyName("RoundId")] public long? RoundId { get; set; }
    [JsonPropertyName("GrainId")] public string? GrainId { get; set; }
    
    [JsonPropertyName("SourceContext")] public string? SourceContext { get; set; }
    [JsonPropertyName("Application")] public string? Application { get; set; }
    [JsonPropertyName("Environment")] public string? Environment { get; set; }
    [JsonPropertyName("MethodName")] public string? MethodName { get; set; }
}
