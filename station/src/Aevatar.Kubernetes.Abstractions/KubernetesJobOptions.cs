namespace Aevatar.Kubernetes.Abstractions;

public class KubernetesJobOptions
{
    public string JobName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string[] Command { get; set; } = Array.Empty<string>();
    public string[] Args { get; set; } = Array.Empty<string>();
    public string CpuLimit { get; set; } = string.Empty;
    public string MemoryLimit { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; }
}