using Microsoft.Extensions.Configuration;

namespace Aevatar.Kubernetes;

public class KubernetesConstants
{
     public const string CoreApiVersion = "v1";
     public const string NginxIngressClassName = "nginx";
     //resource definition
     public const string NodeAffinityValue = "Aevatar-app";
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
     public const string FileBeatContainerName = "filebeat-sidecar";
     
     //manager
     public const string AppClientTypeQuery = "Query";
     public const string AppSettingTemplateFilePath = "WebhookConfigTemplate/appsettings-template.json";
     public const string AppFileBeatConfigTemplateFilePath = "WebhookConfigTemplate/filebeat-template.yml";
     public const string PlaceHolderAppId = "[WebhookId]";
     public const string PlaceHolderVersion = "[Version]";
     public const string PlaceHolderNameSpace = "[NameSpace]";
     public const int AppContainerTargetPort = 8308;
     public const string FullPodMaxSurge = "0";
     public const string FullPodMaxUnavailable = "1";
     public const string QueryPodMaxSurge = "50%";
     public const string QueryPodMaxUnavailable = "0";
     
     //Prometheus
     public const string MonitorLabelKey = "monitor";
     public const string MonitorGroup = "monitoring.coreos.com";
     public const string MonitorPlural = "servicemonitors";
     public const string MetricsPath = "/metrics";
     
     public static void Initialize(IConfiguration configuration)
     {
          AppNameSpace = configuration["Kubernetes:AppNameSpace"] ?? "Aevatar-app";
     }
}