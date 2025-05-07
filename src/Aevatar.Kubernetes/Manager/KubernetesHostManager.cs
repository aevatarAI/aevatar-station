using Aevatar.Kubernetes.Adapter;
using Aevatar.Kubernetes.ResourceDefinition;
using Aevatar.Options;
using Aevatar.WebHook.Deploy;
using JetBrains.Annotations;
using k8s.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Kubernetes.Manager;

public class KubernetesHostManager: IHostDeployManager, ISingletonDependency
{
    // private readonly k8s.Kubernetes _k8sClient;
    private readonly KubernetesOptions _kubernetesOptions;
    private readonly ILogger<KubernetesHostManager> _logger;
    private readonly IKubernetesClientAdapter _kubernetesClientAdapter;
    private readonly HostDeployOptions _HostDeployOptions;
    public KubernetesHostManager(ILogger<KubernetesHostManager> logger,
        IKubernetesClientAdapter kubernetesClientAdapter,
        IOptionsSnapshot<KubernetesOptions> kubernetesOptions,
        IOptionsSnapshot<HostDeployOptions> HostDeployOptions)
    {
        _logger = logger;
        _kubernetesClientAdapter = kubernetesClientAdapter;
        _kubernetesOptions = kubernetesOptions.Value;
        _HostDeployOptions = HostDeployOptions.Value;
    }

    public async Task<string> CreateNewWebHookAsync(string appId, string version, string imageName)
    {
        return await CreatePodAsync(appId, version, imageName,
            GetWebhookConfigContent(appId, version, KubernetesConstants.WebhookSettingTemplateFilePath),
            KubernetesConstants.WebhookCommand,_kubernetesOptions.WebhookHostName);
    }

    public async Task DestroyWebHookAsync(string appId, string version)
    {
        await DestroyPodsAsync(appId, version);
    }
    
    private async Task<string> CreatePodAsync(string appId, string version, string imageName, string config,
        List<string> Command, string hostName)
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
            _kubernetesOptions.AppPodReplicas, 
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
    var configMap = createConfigMapDefinitionFunc(configMapName, configContent);
    if (!configMaps.Items.Any(configMap => configMap.Metadata.Name == configMapName))
    {
        await _kubernetesClientAdapter.CreateConfigMapAsync(configMap, KubernetesConstants.AppNameSpace);
        _logger.LogInformation("[KubernetesAppManager] ConfigMap {configMapName} created", configMapName);
    }
    else
    {
        await _kubernetesClientAdapter.ReplaceNamespacedConfigMapAsync(configMap, configMapName,KubernetesConstants.AppNameSpace);
    }
}
  
public async Task UpdateDockerImageAsync(string appId, string version, string newImage)
    {
        var deployments = await _kubernetesClientAdapter.ListDeploymentAsync(KubernetesConstants.AppNameSpace);
        var deploymentName =
            DeploymentHelper.GetAppDeploymentName(appId, version);
        var deploymentExists = deployments.Items.Any(item => item.Metadata.Name == deploymentName);
        if (deploymentExists)
        {
            var deployment =
                await _kubernetesClientAdapter.ReadNamespacedDeploymentAsync(deploymentName,
                    KubernetesConstants.AppNameSpace);
            // Add or update annotations to trigger rolling updates
            var annotations = deployment.Spec.Template.Metadata.Annotations ?? new Dictionary<string, string>();
            annotations["kubectl.kubernetes.io/restartedAt"] = DateTime.UtcNow.ToString("s");
            deployment.Spec.Template.Metadata.Annotations = annotations;
            //Update container image 
            var containers = deployment.Spec.Template.Spec.Containers;
            var containerName =
                ContainerHelper.GetAppContainerName(appId, version);

            var container = containers.FirstOrDefault(c => c.Name == containerName);
            if (container != null)
            {
                container.Image = newImage;
                await _kubernetesClientAdapter.ReplaceNamespacedDeploymentAsync(deployment, deploymentName,
                    KubernetesConstants.AppNameSpace);
                _logger.LogInformation($"Updated deployment {deploymentName} to use image {newImage}");
            }
            else
            {
                _logger.LogError($"Container {containerName} not found in deployment {deploymentName}");
            }
        }
        else
        {
            _logger.LogError($"Deployment {deploymentName} does not exist!");
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

private static string GetHostSiloConfigContent(string appId, string version, string templateFilePath)
{
    string configContent = File.ReadAllText(templateFilePath)
        .Replace(KubernetesConstants.HostPlaceHolderAppId, appId.ToLower())
        .Replace(KubernetesConstants.HostPlaceHolderVersion, version.ToLower())
        .Replace(KubernetesConstants.HostPlaceHolderNameSpace, KubernetesConstants.AppNameSpace.ToLower());
    return configContent;
}

private static string GetHostClientConfigContent(string appId, string version, string templateFilePath,[CanBeNull] string corsUrls)
{
    string configContent = File.ReadAllText(templateFilePath)
        .Replace(KubernetesConstants.HostPlaceHolderAppId, appId.ToLower())
        .Replace(KubernetesConstants.HostPlaceHolderVersion, version.ToLower())
        .Replace(KubernetesConstants.HostPlaceHolderNameSpace, KubernetesConstants.AppNameSpace.ToLower());
    if (corsUrls != null)
    {
        configContent = configContent.Replace(KubernetesConstants.HostClientCors, corsUrls);
    }
    return configContent;
}

private async Task EnsureDeploymentAsync(
    string appId, string version, string imageName, string deploymentName, 
    string deploymentLabelName, string containerName,List<string> command,int replicas, 
    int containerPort, string maxSurge, string maxUnavailable, string healthPath,bool isSilo = false)
{
    
    var configMapName = ConfigMapHelper.GetAppSettingConfigMapName(appId, version);
    var sideCarConfigName = ConfigMapHelper.GetAppFileBeatConfigMapName(appId, version);

    var deployment = DeploymentHelper.CreateAppDeploymentWithFileBeatSideCarDefinition(
        appId, version, imageName, deploymentName, deploymentLabelName, command,replicas, containerName,
        containerPort, configMapName, sideCarConfigName, 
        _kubernetesOptions.RequestCpuCore, _kubernetesOptions.RequestMemory, 
        maxSurge, maxUnavailable, isSilo, healthPath);
    var deployments = await _kubernetesClientAdapter.ListDeploymentAsync(KubernetesConstants.AppNameSpace);
    
    if (!deployments.Items.Any(item => item.Metadata.Name == deploymentName))
    {
        await _kubernetesClientAdapter.CreateDeploymentAsync(deployment, KubernetesConstants.AppNameSpace);
        _logger.LogInformation("[KubernetesAppManager] Deployment {deploymentName} created", deploymentName);
    }
    else
    {       
        await _kubernetesClientAdapter.ReplaceNamespacedDeploymentAsync(deployment, deploymentName,KubernetesConstants.AppNameSpace);
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
        return "";
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

    public async Task<string> CreateHostAsync(string appId, string version, string corsUrls)
    {
        await CreateHostSiloAsync(GetHostName(appId,KubernetesConstants.HostSilo) , version, _HostDeployOptions.HostSiloImageName,
            GetHostSiloConfigContent(appId,version,KubernetesConstants.HostSiloSettingTemplateFilePath));

        // await EnsurePhaAsync(appId, version);
       await CreatePodAsync(GetHostName(appId,KubernetesConstants.HostClient), version, _HostDeployOptions.HostClientImageName,
           GetHostClientConfigContent(appId, version, KubernetesConstants.HostClientSettingTemplateFilePath,corsUrls),
           KubernetesConstants.HostClientCommand,_kubernetesOptions.DeveloperHostName);
        return "";
    }

    private string GetHostName(string appId,string appType)
    {
        return $"{appId}-{appType}";
    }

    private async Task CreateHostSiloAsync(string appId, string version, string imageName, string hostSiloConfigContent)
    {
        await EnsureConfigMapAsync(
            appId, 
            version, 
            ConfigMapHelper.GetAppSettingConfigMapName,
            hostSiloConfigContent,
            ConfigMapHelper.CreateAppSettingConfigMapDefinition);

        await EnsureConfigMapAsync(
            appId, 
            version, 
            ConfigMapHelper.GetAppFileBeatConfigMapName, 
            GetHostSiloConfigContent(appId,version,KubernetesConstants.HostFileBeatConfigTemplateFilePath),
            ConfigMapHelper.CreateFileBeatConfigMapDefinition);

        // Ensure Deployment is created
        string deploymentName = DeploymentHelper.GetAppDeploymentName(appId, version);
        string deploymentLabelName = DeploymentHelper.GetAppDeploymentLabelName(appId, version);
        string containerName = ContainerHelper.GetAppContainerName(appId, version);
        await EnsureDeploymentAsync(
            appId, version, imageName, 
            deploymentName, deploymentLabelName, containerName, 
            KubernetesConstants.HostSiloCommand,
            _kubernetesOptions.AppPodReplicas, 
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

    public async Task DestroyHostAsync(string appId, string version)
    {
        await DestroyHostSiloAsync(GetHostName(appId,KubernetesConstants.HostSilo), version);
        await DestroyPodsAsync(GetHostName(appId,KubernetesConstants.HostClient), version);
    }

    private async Task DestroyHostSiloAsync(string appId, string version)
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

    public async Task RestartHostAsync(string appId, string version)
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