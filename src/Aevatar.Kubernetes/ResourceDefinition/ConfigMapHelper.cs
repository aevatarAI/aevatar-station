using k8s.Models;

namespace Aevatar.Kubernetes.ResourceDefinition;

public class ConfigMapHelper
{
    public static string GetAppSettingConfigMapName(string appId, string version)
    {
        appId = appId.Replace("_", "-");
        var name = $"appsettings-config-{appId}-{version}";
        return name.ToLower();
    }

    public static string GetAppFileBeatConfigMapName(string appId, string version)
    {
        appId = appId.Replace("_", "-");
        var name =  $"filebeat-config-{appId}-{version}";
        return name.ToLower();
    }

    /// <summary>
    /// Create appsettings.json configmap resource definition
    /// </summary>
    /// <param name="configMapName">appsettings-config</param>
    /// <param name="configFiles">Dictionary of config files</param>
    /// <returns></returns>
    public static V1ConfigMap CreateAppSettingConfigMapDefinition(string configMapName, Dictionary<string, string> configFiles)
    {
        var configMap = new V1ConfigMap
        {
            Metadata = new V1ObjectMeta { Name = configMapName, NamespaceProperty = KubernetesConstants.AppNameSpace },
            Data = configFiles
        };
        return configMap;
    }
    
    /// <summary>
    /// Create filebeat.yml configmap resource definition
    /// </summary>
    /// <param name="configMapName"></param>
    /// <param name="configFiles"></param>
    /// <returns></returns>
    public static V1ConfigMap CreateFileBeatConfigMapDefinition(string configMapName, Dictionary<string, string> configFiles)
    {
        var configMap = new V1ConfigMap
        {
            Metadata = new V1ObjectMeta { Name = configMapName, NamespaceProperty = KubernetesConstants.AppNameSpace },
            Data = configFiles
        };
        return configMap;
    }
}