namespace Aevatar.Options;

public class KubernetesOptions
{
    public string KubeConfigPath { get; set; } = "KubeConfig/config.txt";
    public string AppNameSpace { get; set; }
    public int AppPodReplicas { get; set; } = 1;
    public string HostName { get; set; }
    public string AppPodRequestCpuCore { get; set; } = "1";
    public string AppPodRequestMemory { get; set; } = "2Gi";
}