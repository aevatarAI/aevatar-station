namespace Aevatar.Kubernetes.ResourceDefinition;

public class ContainerHelper
{
    public static string GetAppContainerName(string appId, string version)
    {
        appId = appId.Replace("_", "-");
        var name = $"container-{appId}-{version}";
        return name.ToLower();
    }
}