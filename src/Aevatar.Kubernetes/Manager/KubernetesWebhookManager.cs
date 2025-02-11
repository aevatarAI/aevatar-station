using Aevatar.Kubernetes.Adapter;
using Aevatar.Kubernetes.ResourceDefinition;
using Aevatar.Options;
using Aevatar.WebHook.Deploy;
using k8s.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Kubernetes.Manager;

public class KubernetesWebhookManager: IWebhookDeployManager, ISingletonDependency
{
    // private readonly k8s.Kubernetes _k8sClient;
    private readonly KubernetesOptions _kubernetesOptions;
    private readonly ILogger<KubernetesWebhookManager> _logger;
    private readonly IKubernetesClientAdapter _kubernetesClientAdapter;
    private readonly DaippDeployOptions _daippDeployOptions;
    public KubernetesWebhookManager(ILogger<KubernetesWebhookManager> logger,
        IKubernetesClientAdapter kubernetesClientAdapter,
        IOptionsSnapshot<KubernetesOptions> kubernetesOptions,
        IOptionsSnapshot<DaippDeployOptions> daippDeployOptions)
    {
        _logger = logger;
        _kubernetesClientAdapter = kubernetesClientAdapter;
        _kubernetesOptions = kubernetesOptions.Value;
        _daippDeployOptions = daippDeployOptions.Value;
    }

    public async Task<string> CreateNewWebHookAsync(string appId, string version, string imageName)
    {
        return await CreatePodAsync(appId, version, imageName,
            GetWebhookConfigContent(appId, version, KubernetesConstants.WebhookSettingTemplateFilePath),
            KubernetesConstants.WebhookCommand);
    }

    public async Task DestroyWebHookAsync(string appId, string version)
    {
        await DestroyPodsAsync(appId, version);
    }
    
    private async Task<string> CreatePodAsync(string appId, string version, string imageName,string config,List<string> Command )
    {
        // Ensure ConfigMaps (AppSettings and SideCar Configs) are created
        await EnsureConfigMapAsync(
            appId, 
            version, 
            ConfigMapHelper.GetAppSettingConfigMapName, 
            config, 
            ConfigMapHelper.CreateAppSettingConfigMapDefinition);

        await EnsureConfigMapAsync(
            appId, 
            version, 
            ConfigMapHelper.GetAppFileBeatConfigMapName, 
            GetWebhookConfigContent(appId, version, KubernetesConstants.WebhookFileBeatConfigTemplateFilePath), 
            ConfigMapHelper.CreateFileBeatConfigMapDefinition);

        // Ensure Deployment is created
        string deploymentName = DeploymentHelper.GetAppDeploymentName(appId, version);
        string deploymentLabelName = DeploymentHelper.GetAppDeploymentLabelName(appId, version);
        string containerName = ContainerHelper.GetAppContainerName(appId, version);
        await EnsureDeploymentAsync(
            appId, version, imageName, 
            deploymentName, deploymentLabelName, containerName, 
            Command,
            1, 
            KubernetesConstants.WebhookContainerTargetPort, 
            KubernetesConstants.QueryPodMaxSurge, 
            KubernetesConstants.QueryPodMaxUnavailable, 
            GetHealthPath());

        // Ensure Service is created
        string serviceName = ServiceHelper.GetAppServiceName(appId, version);
        await EnsureServiceAsync(
            appId, version, serviceName,
            DeploymentHelper.GetAppDeploymentLabelName(appId, version),
            KubernetesConstants.WebhookContainerTargetPort);

        // Ensure Ingress is created
        string rulePath = $"/{appId}".ToLower(); 
        var hostName = _kubernetesOptions.HostName;
        await EnsureIngressAsync(appId, version,hostName, rulePath, serviceName, KubernetesConstants.WebhookContainerTargetPort);

        return hostName.TrimEnd('/') + rulePath;
    }
  private async Task EnsureConfigMapAsync(
    string appId, 
    string version, 
    Func<string, string, string> getConfigMapNameFunc, 
    string configContent, 
    Func<string, string, V1ConfigMap> createConfigMapDefinitionFunc)
{
    string configMapName = getConfigMapNameFunc(appId, version);
    var configMaps = await _kubernetesClientAdapter.ListConfigMapAsync(KubernetesConstants.AppNameSpace);
    if (!configMaps.Items.Any(configMap => configMap.Metadata.Name == configMapName))
    {
        var configMap = createConfigMapDefinitionFunc(configMapName, configContent);
        await _kubernetesClientAdapter.CreateConfigMapAsync(configMap, KubernetesConstants.AppNameSpace);
        _logger.LogInformation("[KubernetesAppManager] ConfigMap {configMapName} created", configMapName);
    }
}

private static string GetWebhookConfigContent(string appId, string version, string templateFilePath)
{
    string configContent = File.ReadAllText(templateFilePath)
        .Replace(KubernetesConstants.PlaceHolderAppId, appId.ToLower())
        .Replace(KubernetesConstants.PlaceHolderVersion, version.ToLower())
        .Replace(KubernetesConstants.PlaceHolderNameSpace, KubernetesConstants.AppNameSpace.ToLower());
    return configContent;
}

private static string GetAippSiloConfigContent(string appId, string version, string templateFilePath)
{
    string configContent = File.ReadAllText(templateFilePath)
        .Replace(KubernetesConstants.AippPlaceHolderAppId, appId.ToLower())
        .Replace(KubernetesConstants.AippPlaceHolderVersion, version.ToLower())
        .Replace(KubernetesConstants.AippPlaceHolderNameSpace, KubernetesConstants.AppNameSpace.ToLower());
    return configContent;
}

private async Task EnsureDeploymentAsync(
    string appId, string version, string imageName, string deploymentName, 
    string deploymentLabelName, string containerName,List<string> command,int replicas, 
    int containerPort, string maxSurge, string maxUnavailable, string healthPath,bool isSilo = false)
{
    var deployments = await _kubernetesClientAdapter.ListDeploymentAsync(KubernetesConstants.AppNameSpace);
    if (!deployments.Items.Any(item => item.Metadata.Name == deploymentName))
    {
        var configMapName = ConfigMapHelper.GetAppSettingConfigMapName(appId, version);
        var sideCarConfigName = ConfigMapHelper.GetAppFileBeatConfigMapName(appId, version);

        var deployment = DeploymentHelper.CreateAppDeploymentWithFileBeatSideCarDefinition(
            appId, version, imageName, deploymentName, deploymentLabelName, command,replicas, containerName,
            containerPort, configMapName, sideCarConfigName, 
            _kubernetesOptions.RequestCpuCore, _kubernetesOptions.RequestMemory, 
            maxSurge, maxUnavailable, isSilo, healthPath);

        await _kubernetesClientAdapter.CreateDeploymentAsync(deployment, KubernetesConstants.AppNameSpace);
        _logger.LogInformation("[KubernetesAppManager] Deployment {deploymentName} created", deploymentName);
    }
}

private async Task EnsureServiceAsync(
    string appId, string version, string serviceName, 
    string deploymentLabelName, int targetPort)
{
    var services = await _kubernetesClientAdapter.ListServiceAsync(KubernetesConstants.AppNameSpace);
    if (!services.Items.Any(item => item.Metadata.Name == serviceName))
    {
        string serviceLabelName = ServiceHelper.GetAppServiceLabelName(appId, version);
        string servicePortName = ServiceHelper.GetAppServicePortName(version);

        var service = ServiceHelper.CreateAppClusterIPServiceDefinition(
            appId, serviceName, serviceLabelName, deploymentLabelName, 
            servicePortName, targetPort, targetPort);

        await _kubernetesClientAdapter.CreateServiceAsync(service, KubernetesConstants.AppNameSpace);
        _logger.LogInformation("[KubernetesAppManager] Service {serviceName} created", serviceName);
    }
}

private async Task EnsureIngressAsync(
    string appId, string version,
    string hostName, string rulePath, string serviceName, int targetPort)
{
    var ingressName = IngressHelper.GetAppIngressName(appId, version);
    var ingresses = await _kubernetesClientAdapter.ListIngressAsync(KubernetesConstants.AppNameSpace);
    if (!ingresses.Items.Any(item => item.Metadata.Name == ingressName))
    {
        var ingress = IngressHelper.CreateAppIngressDefinition(
            ingressName, hostName, rulePath, serviceName, targetPort);

        await _kubernetesClientAdapter.CreateIngressAsync(ingress, KubernetesConstants.AppNameSpace);
        _logger.LogInformation("[KubernetesAppManager] Ingress {ingressName} created", ingressName);
    }
}
    private string GetHealthPath()
    {
        return "/health";
    }


    private async Task DestroyPodsAsync(string appId, string version)
    {
        // Delete Deployment
        var deploymentName = DeploymentHelper.GetAppDeploymentName(appId, version);
        await EnsureDeploymentDeletedAsync(deploymentName);

        // Delete AppSetting ConfigMap
        var appSettingConfigMapName = ConfigMapHelper.GetAppSettingConfigMapName(appId, version);
        await EnsureConfigMapDeletedAsync(appSettingConfigMapName);

        // Delete SideCar ConfigMap
        var sideCarConfigMapName = ConfigMapHelper.GetAppFileBeatConfigMapName(appId, version);
        await EnsureConfigMapDeletedAsync(sideCarConfigMapName);

        // Delete Service
        var serviceName = ServiceHelper.GetAppServiceName(appId, version);
        await EnsureServiceDeletedAsync(serviceName);

        // Delete Ingress
        var ingressName = IngressHelper.GetAppIngressName(appId, version);
        await EnsureIngressDeletedAsync(ingressName);
    }

    private async Task EnsureIngressDeletedAsync(string ingressName)
    {
        var ingresses = await _kubernetesClientAdapter.ListIngressAsync(KubernetesConstants.AppNameSpace);
        var ingressExists = ingresses.Items.Any(item => item.Metadata.Name == ingressName);
        if (ingressExists)
        {
            await _kubernetesClientAdapter.DeleteIngressAsync(ingressName, KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]Ingress {ingressName} deleted.", ingressName);
        }
    }

    private async Task EnsureServiceDeletedAsync(string serviceName)
    {
        var services = await _kubernetesClientAdapter.ListServiceAsync(KubernetesConstants.AppNameSpace);
        var serviceExists = services.Items.Any(item => item.Metadata.Name == serviceName);
        if (serviceExists)
        {
            await _kubernetesClientAdapter.DeleteServiceAsync(serviceName, KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]Service {serviceName} deleted.", serviceName);
        }
    }

    private async Task EnsureDeploymentDeletedAsync(string resourceName)
    {
        var deployments = await _kubernetesClientAdapter.ListDeploymentAsync(KubernetesConstants.AppNameSpace);
        var deploymentExists = deployments.Items.Any(d => d.Metadata.Name == resourceName);

        if (deploymentExists)
        {
            await _kubernetesClientAdapter.DeleteDeploymentAsync(resourceName, KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager] Deployment {resourceName} deleted.", resourceName);
        }
        else
        {
            _logger.LogWarning("[KubernetesAppManager] Deployment {resourceName} does not exist.", resourceName);
        }
    }
    
    private async Task EnsureConfigMapDeletedAsync(string resourceName)
    {
        var configMaps = await _kubernetesClientAdapter.ListConfigMapAsync(KubernetesConstants.AppNameSpace);
        var configMapExists = configMaps.Items.Any(cm => cm.Metadata.Name == resourceName);

        if (configMapExists)
        {
            await _kubernetesClientAdapter.DeleteConfigMapAsync(resourceName, KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager] ConfigMap {resourceName} deleted.", resourceName);
        }
        else
        {
            _logger.LogWarning("[KubernetesAppManager] ConfigMap {resourceName} does not exist.", resourceName);
        }
    }
    public async Task RestartWebHookAsync(string appId, string version)
    {
        var deploymentName = DeploymentHelper.GetAppDeploymentName(appId, version);
        await RestartDeploymentAsync(deploymentName);
    }

    public async Task<string> CreateNewDaippAsync(string appId, string version)
    {
        await CreateDaippSiloAsync(appId+"-silo", version, _daippDeployOptions.DaippSiloImageName);

        // await EnsurePhaAsync(appId, version);
       await CreatePodAsync(appId+"-client", version, _daippDeployOptions.DaippClientImageName,
           GetAippSiloConfigContent(appId, version, KubernetesConstants.AippClientSettingTemplateFilePath),
           KubernetesConstants.AippClientCommand);
        return "";
    }

    private async Task CreateDaippSiloAsync(string appId, string version, string imageName)
    {
        await EnsureConfigMapAsync(
            appId, 
            version, 
            ConfigMapHelper.GetAppSettingConfigMapName,
            GetAippSiloConfigContent(appId,version,KubernetesConstants.AippSiloSettingTemplateFilePath),
            ConfigMapHelper.CreateAppSettingConfigMapDefinition);

        await EnsureConfigMapAsync(
            appId, 
            version, 
            ConfigMapHelper.GetAppFileBeatConfigMapName, 
            GetAippSiloConfigContent(appId,version,KubernetesConstants.AippFileBeatConfigTemplateFilePath),
            ConfigMapHelper.CreateFileBeatConfigMapDefinition);

        // Ensure Deployment is created
        string deploymentName = DeploymentHelper.GetAppDeploymentName(appId, version);
        string deploymentLabelName = DeploymentHelper.GetAppDeploymentLabelName(appId, version);
        string containerName = ContainerHelper.GetAppContainerName(appId, version);
        await EnsureDeploymentAsync(
            appId, version, imageName, 
            deploymentName, deploymentLabelName, containerName, 
            KubernetesConstants.AippSiloCommand,
            1, 
            KubernetesConstants.WebhookContainerTargetPort, 
            KubernetesConstants.QueryPodMaxSurge, 
            KubernetesConstants.QueryPodMaxUnavailable, 
            "",true);
    }

    private async Task EnsurePhaAsync(string appId, string version)
    {
        var hpa = await _kubernetesClientAdapter.ReadNamespacedHorizontalPodAutoscalerAsync(appId, version);
        if (hpa == null)
        {
            await _kubernetesClientAdapter.CreateNamespacedHorizontalPodAutoscalerAsync(HPAHelper.CreateHPA(appId, version), KubernetesConstants.AppNameSpace);
        }
    }

    public async Task DestroyDaippAsync(string appId, string version)
    {
        await DestroyDaippSiloAsync(appId + "-silo", version);
        await DestroyPodsAsync(appId + "-client", version);
    }

    private async Task DestroyDaippSiloAsync(string appId, string version)
    {
        // Delete Deployment
        var deploymentName = DeploymentHelper.GetAppDeploymentName(appId, version);
        await EnsureDeploymentDeletedAsync(deploymentName);

        // Delete AppSetting ConfigMap
        var appSettingConfigMapName = ConfigMapHelper.GetAppSettingConfigMapName(appId, version);
        await EnsureConfigMapDeletedAsync(appSettingConfigMapName);

        // Delete SideCar ConfigMap
        var sideCarConfigMapName = ConfigMapHelper.GetAppFileBeatConfigMapName(appId, version);
        await EnsureConfigMapDeletedAsync(sideCarConfigMapName);
    }

    public async Task RestartDaippAsync(string appId, string version)
    {
        var deploymentName = DeploymentHelper.GetAppDeploymentName(appId, version);
        await RestartDeploymentAsync(deploymentName);
    }

    private async Task RestartDeploymentAsync(string deploymentName)
    {
        // Check if the deployment exists in the namespace
        var deploymentExists = await DeploymentExistsAsync(deploymentName);
        if (!deploymentExists)
        {
            _logger.LogError($"Deployment {deploymentName} does not exist!");
            return;
        }

        // Trigger a rolling restart by updating the 'restartedAt' annotation
        await ApplyRollingRestartAsync(deploymentName);
    }

    private async Task<bool> DeploymentExistsAsync(string deploymentName)
    {
        var deployments = await _kubernetesClientAdapter.ListDeploymentAsync(KubernetesConstants.AppNameSpace);
        return deployments.Items.Any(item => item.Metadata.Name == deploymentName);
    }

    private async Task ApplyRollingRestartAsync(string deploymentName)
    {
        // Read the existing deployment
        var deployment = await _kubernetesClientAdapter.ReadNamespacedDeploymentAsync(deploymentName, KubernetesConstants.AppNameSpace);

        // Add or update the 'restartedAt' annotation to trigger the restart
        var annotations = deployment.Spec.Template.Metadata.Annotations ?? new Dictionary<string, string>();
        annotations["kubectl.kubernetes.io/restartedAt"] = DateTime.UtcNow.ToString("s");
        deployment.Spec.Template.Metadata.Annotations = annotations;

        // Update the Deployment to apply the changes
        await _kubernetesClientAdapter.ReplaceNamespacedDeploymentAsync(deployment, deploymentName, KubernetesConstants.AppNameSpace);
        _logger.LogInformation($"[KubernetesAppManager] Deployment {deploymentName} restarted at {annotations["kubectl.kubernetes.io/restartedAt"]}");
    }
   
}