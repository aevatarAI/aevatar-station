namespace Aevatar.Kubernetes.Options;

public class KubernetesOptions
{
    public string ConfigPath { get; set; } = string.Empty;
    
    public string Namespace { get; set; } = "default";
    
    public string ClusterName { get; set; } = string.Empty;
    
    public string ServiceAccountName { get; set; } = string.Empty;
    
    public bool EnableMonitoring { get; set; } = true;
    
    // Additional properties from the actual code
    public string KubeConfigPath { get; set; } = string.Empty;
    
    public string DeveloperHostName { get; set; } = string.Empty;
    
    public string WebhookHostName { get; set; } = string.Empty;
    
    public int AppPodReplicas { get; set; } = 1;
    
    public string RequestCpuCore { get; set; } = "100m";
    
    public string RequestMemory { get; set; } = "128Mi";
}

public class HostDeployOptions
{
    public string ImageName { get; set; } = string.Empty;
    
    public string ImageTag { get; set; } = "latest";
    
    public int Replicas { get; set; } = 1;
    
    public Dictionary<string, string> Labels { get; set; } = new();
    
    public Dictionary<string, string> Annotations { get; set; } = new();
    
    // Additional properties from the actual code
    public string HostSiloImageName { get; set; } = string.Empty;
    
    public string HostClientImageName { get; set; } = string.Empty;
} 