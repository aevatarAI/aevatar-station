namespace Aevatar.Kubernetes.Abstractions;

public class KubernetesLogResult
{
    public string[] Lines { get; set; } = Array.Empty<string>();
    public bool HasMore { get; set; }
    public string Error { get; set; } = string.Empty;
}