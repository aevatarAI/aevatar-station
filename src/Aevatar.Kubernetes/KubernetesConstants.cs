using Microsoft.Extensions.Configuration;

namespace Aevatar.Kubernetes;

public class KubernetesConstants
{
     public const string CoreApiVersion = "v1";
     public const string NginxIngressClassName = "nginx";
     //resource definition
     public const string NodeAffinityValue = "ai";
     public static string AppNameSpace { get; private set; }
     public const string AppLabelKey = "app";
     public const string AppIdLabelKey = "app-id";
     public const string AppVersionLabelKey = "app-version";
     public const string AppPodTypeLabelKey = "app-pod-type";
     public const string AppPodChainIdLabelKey = "app-pod-chainid";
     public const string AppSettingFileName = "appsettings.json";
     public const string AppSettingFileMountPath = "/app/appsettings.json";
     public const string AppLogFileMountPath = "/app/Logs";
     
     //FileBeat
     public const string FileBeatImage = "docker.elastic.co/beats/filebeat:7.16.2";
     public const string FileBeatConfigMountPath = "/etc/filebeat/filebeat.yml";
     public const string FileBeatConfigFileName = "filebeat.yml";
     public const string FileBeatLogILMPolicyName = "filebeat-log-policy";
     public const string FileBeatContainerName = "filebeat-sidecar";
     
     //webhook manager
     public const string WebhookSettingTemplateFilePath = "WebhookConfigTemplate/appsettings-template.json";
     public const string WebhookFileBeatConfigTemplateFilePath = "WebhookConfigTemplate/filebeat-template.yml";
     public const string PlaceHolderAppId = "[WebhookId]";
     public const string PlaceHolderVersion = "[Version]";
     public const string PlaceHolderNameSpace = "[NameSpace]";
     public static readonly List<string> WebhookCommand = new() { "dotnet", "Aevatar.WebHook.Host.dll" };

     public const int WebhookContainerTargetPort = 8308;
     public const string QueryPodMaxSurge = "50%";
     public const string QueryPodMaxUnavailable = "0";
     
     //Aipp manager
     public const string AippSettingTemplateFilePath = "AippConfigTemplate/appsettings-template.json";
     public const string AippFileBeatConfigTemplateFilePath = "AippConfigTemplate/filebeat-template.yml";
     public const string AippPlaceHolderAppId = "[AippId]";
     public const string AippPlaceHolderVersion = "[Version]";
     public const string AippPlaceHolderNameSpace = "[NameSpace]";
     public static readonly List<string> AippCommand = new() { "dotnet", "Aevatar.Daipp.Silo.dll" };
     public const  int   AippContainerContainerPort = 10001;
     public const string AippQueryPodMaxSurge = "50%";
     public const string AippQueryPodMaxUnavailable = "0";
     //Prometheus
     public const string MonitorLabelKey = "monitor";
     public const string MonitorGroup = "monitoring.coreos.com";
     public const string MonitorPlural = "servicemonitors";
     public const string MetricsPath = "/metrics";
     
     public static void Initialize(IConfiguration configuration)
     {
          AppNameSpace = configuration["Kubernetes:AppNameSpace"] ?? "Aevatar-webhook";
     }
}