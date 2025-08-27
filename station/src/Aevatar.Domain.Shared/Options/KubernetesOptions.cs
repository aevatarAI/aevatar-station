namespace Aevatar.Options;

public class KubernetesOptions
{
    public string KubeConfigPath { get; set; } = "KubeConfig/config.txt";
    public string AppNameSpace { get; set; }
    public int AppPodReplicas { get; set; } = 1;
    public string WebhookHostName { get; set; }
    public string DeveloperHostName { get; set; }
    public string RequestCpuCore { get; set; } = "1";
    public string RequestMemory { get; set; } = "2Gi";
}