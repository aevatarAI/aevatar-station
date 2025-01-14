namespace Aevatar.Kubernetes.ResourceDefinition;

public class ContainerHelper
{
    public static string GetAppContainerName(string appId, string version,string clientType, string chainId)
    {
        appId = appId.Replace("_", "-");
        var name = $"container-{version}-{clientType}";
        if (!string.IsNullOrWhiteSpace(chainId))
        {
            name += $"-{chainId}";
        }

        return name.ToLower();
    }
}