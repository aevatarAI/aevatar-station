using System.Text.Json.Serialization;

namespace Aevatar.Developer.Logger.Entities;

public class HostLogIndex
{
    [JsonPropertyName("@timestamp")] public DateTime Timestamp { get; set; }
    
    [JsonPropertyName("app_log.@t")] public DateTime AppLogTime { get; set; }
    
    [JsonPropertyName("app_log.@m")] public string? AppLogMessage { get; set; }
    
    [JsonPropertyName("app_log.@i")] public string? AppLogId { get; set; }
    
    [JsonPropertyName("app_log.@l")] public string? AppLogLevel { get; set; }
    
    [JsonPropertyName("app_log.@tr")] public string? AppLogTraceId { get; set; }
    
    [JsonPropertyName("app_log.@sp")] public string? AppLogSpanId { get; set; }
    
    [JsonPropertyName("app_log.LogCategory")] public string? LogCategory { get; set; }
    
    [JsonPropertyName("app_log.WorkflowId")] public string? WorkflowId { get; set; }
    
    [JsonPropertyName("app_log.GrainId")] public string? GrainId { get; set; }
    
    [JsonPropertyName("app_log.SourceContext")] public string? SourceContext { get; set; }
    
    [JsonPropertyName("app_log.Application")] public string? Application { get; set; }
    
}