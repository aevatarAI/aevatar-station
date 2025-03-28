using System.Text.Json.Serialization;
using k8s.Models;

namespace Aevatar.Kubernetes.ResourceDefinition;

public class ServiceMonitor
{
    [JsonPropertyName("apiVersion")]
    public string ApiVersion { get; set; } = "monitoring.coreos.com/v1";
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "ServiceMonitor";
    [JsonPropertyName("metadata")]
    public V1ObjectMeta Metadata { get; set; }
    [JsonPropertyName("spec")]
    public ServiceMonitorSpec Spec { get; set; }
}

public class ServiceMonitorSpec
{
    [JsonPropertyName("endpoints")]
    public List<Endpoint> Endpoints { get; set; }
    [JsonPropertyName("namespaceSelector")]
    public NamespaceSelector NamespaceSelector { get; set; }
    [JsonPropertyName("jobLabel")]
    public string JobLabel { get; set; }
    [JsonPropertyName("selector")]
    public Selector Selector { get; set; }
}

public class Endpoint
{
    [JsonPropertyName("port")]
    public string Port { get; set; }
    [JsonPropertyName("interval")]
    public string Interval { get; set; }
    [JsonPropertyName("path")]
    public string Path { get; set; }
    [JsonPropertyName("relabelings")]
    public List<Relabeling> Relabelings { get; set; }
}

public class NamespaceSelector
{
    [JsonPropertyName("matchNames")]
    public List<string> MatchNames { get; set; }
}

public class Selector
{
    [JsonPropertyName("matchLabels")]
    public Dictionary<string, string> MatchLabels { get; set; }
}

public class Relabeling
{
    [JsonPropertyName("action")]
    public string Action { get; set; }
    [JsonPropertyName("replacement")]
    public string Replacement { get; set; }
    [JsonPropertyName("sourceLabels")]
    public List<string> SourceLabels { get; set; }
    [JsonPropertyName("targetLabel")]
    public string TargetLabel { get; set; }
}