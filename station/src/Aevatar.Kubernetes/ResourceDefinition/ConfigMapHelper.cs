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
    /// <param name="configFileName">appsettings.json</param>
    /// <param name="nameSpace">nameSpace</param>
    /// <param name="appSettingsContent">File.ReadAllText(appSettingsPath)</param>
    /// <returns></returns>
    public static V1ConfigMap CreateAppSettingConfigMapDefinition(string configMapName,
        string appSettingsContent)
    {
        // Create a ConfigMap
        var configMap = new V1ConfigMap
        {
            Metadata = new V1ObjectMeta { Name = configMapName, NamespaceProperty = KubernetesConstants.AppNameSpace },
            Data = new Dictionary<string, string>
            {
                { KubernetesConstants.AppSettingFileName, appSettingsContent }
            }
        };

        return configMap;
    }
    
    /// <summary>
    /// Create filebeat.yml configmap resource definition
    /// </summary>
    /// <param name="configMapName"></param>
    /// <param name="configFileContent"></param>
    /// <returns></returns>
    public static V1ConfigMap CreateFileBeatConfigMapDefinition(string configMapName,
        string configFileContent)
    {
        // Create a ConfigMap
        var configMap = new V1ConfigMap
        {
            Metadata = new V1ObjectMeta { Name = configMapName, NamespaceProperty = KubernetesConstants.AppNameSpace },
            Data = new Dictionary<string, string>
            {
                { KubernetesConstants.FileBeatConfigFileName, configFileContent }
            }
        };

        return configMap;
    }
}