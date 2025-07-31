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
    
    // FileName
    public const string AppSettingFileName = "appsettings.json";
    public const string AppSettingSharedFileName = "appsettings.Shared.json";
    public const string AppSettingHttpApiHostSharedFileName = "appsettings.HttpApi.Host.Shared.json";
    public const string AppSettingSiloSharedFileName = "appsettings.Silo.Shared.json";
    
    // FileMountPath
    public const string AppSettingFileMountPath = "/app/appsettings.json";
    public const string AppLogFileMountPath = "/app/Logs";
    public const string AppSettingSharedFileMountPath = "/app/appsettings.Shared.json";
    public const string AppSettingHttpApiHostSharedFileMountPath = "/app/appsettings.HttpApi.Host.Shared.json";
    public const string AppSettingSiloSharedFileMountPath = "/app/appsettings.Silo.Shared.json";
    public const string AppSettingBusinessFileMountPath = "/app/appsettings.business.json";

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
    public const int SiloContainerTargetPort = 8080;
    public const int SiloHealthCheckPort = 8081;
    public const string QueryPodMaxSurge = "50%";
    public const string QueryPodMaxUnavailable = "0";

    //Host manager
    public const string HostSiloSettingTemplateFilePath = "HostConfigTemplate/silo-appsettings-template.json";
    public const string HostClientSettingTemplateFilePath = "HostConfigTemplate/client-appsettings-template.json";
    public const string HostFileBeatConfigTemplateFilePath = "HostConfigTemplate/filebeat-template.yml";
    public const string HostPlaceHolderAppId = "[HostId]";
    public const string HostPlaceHolderVersion = "[Version]";
    public const string HostPlaceHolderNameSpace = "[NameSpace]";
    public const string HostPlaceHolderTenantId = "[TenantId]";
    public const string HostPlaceHolderOrleans = "[Orleans]";
    public const string HostSilo = "silo";
    public const string HostClient = "client";
    public const string HostClientCors = "[Cors]";

    public static readonly List<string> HostSiloCommand = new() { "dotnet", "Aevatar.Silo.dll" };
    public static readonly List<string> HostClientCommand = new() { "dotnet", "Aevatar.Developer.Host.dll" };

    public const string HostQueryPodMaxSurge = "50%";
    public const string HostQueryPodMaxUnavailable = "0";

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