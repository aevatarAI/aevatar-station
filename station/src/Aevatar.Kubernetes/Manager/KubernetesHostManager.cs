using System.Text.RegularExpressions;
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

public class KubernetesHostManager : IHostDeployManager, ISingletonDependency
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
            KubernetesConstants.WebhookCommand, _kubernetesOptions.WebhookHostName);
    }

    public async Task DestroyWebHookAsync(string appId, string version)
    {
        await DestroyPodsAsync(appId, version);
    }

    private async Task<string> CreatePodAsync(string appId, string version, string imageName, string config,
        List<string> Command, string hostName)
    {
        // Ensure ConfigMaps (AppSettings and SideCar Configs) are created
        var configFiles = new Dictionary<string, string>
        {
            { KubernetesConstants.AppSettingFileName, config },
            {
                KubernetesConstants.AppSettingSharedFileName,
                GetHostClientConfigContent(appId, version, KubernetesConstants.AppSettingSharedFileName, null)
            },
            {
                KubernetesConstants.AppSettingHttpApiHostSharedFileName,
                GetHostClientConfigContent(appId, version, KubernetesConstants.AppSettingHttpApiHostSharedFileName,
                    null)
            },
            {
                KubernetesConstants.AppSettingSiloSharedFileName,
                GetHostClientConfigContent(appId, version, KubernetesConstants.AppSettingSiloSharedFileName, null)
            }
        };
        await EnsureConfigMapAsync(
            appId,
            version,
            ConfigMapHelper.GetAppSettingConfigMapName,
            configFiles,
            ConfigMapHelper.CreateAppSettingConfigMapDefinition);
        _logger.LogInformation(
            $"[KubernetesAppManager] ConfigMap injected for appId={appId}, version={version}, keys=[{string.Join(",", configFiles.Keys)}]");

        await EnsureConfigMapAsync(
            appId,
            version,
            ConfigMapHelper.GetAppFileBeatConfigMapName,
            new Dictionary<string, string>
            {
                {
                    KubernetesConstants.FileBeatConfigFileName,
                    GetWebhookConfigContent(appId, version, KubernetesConstants.WebhookFileBeatConfigTemplateFilePath)
                }
            },
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
        await EnsureIngressAsync(appId, version, hostName, rulePath, serviceName,
            KubernetesConstants.WebhookContainerTargetPort);

        return hostName.TrimEnd('/') + rulePath;
    }

    private async Task EnsureConfigMapAsync(
        string appId,
        string version,
        Func<string, string, string> getConfigMapNameFunc,
        Dictionary<string, string> configContent,
        Func<string, Dictionary<string, string>, V1ConfigMap> createConfigMapDefinitionFunc)
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
            await _kubernetesClientAdapter.ReplaceNamespacedConfigMapAsync(configMap, configMapName,
                KubernetesConstants.AppNameSpace);
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
        var rawContent = File.ReadAllText(templateFilePath);
        var unescapedContent = Regex.Unescape(rawContent);
        return unescapedContent
            .Replace(KubernetesConstants.PlaceHolderAppId, appId.ToLower())
            .Replace(KubernetesConstants.PlaceHolderVersion, version.ToLower())
            .Replace(KubernetesConstants.PlaceHolderNameSpace, KubernetesConstants.AppNameSpace.ToLower());
    }

    private static string GetHostSiloConfigContent(string appId, string version, string templateFilePath)
    {
        var configContent = File.ReadAllText(templateFilePath);
        var unescapedContent = Regex.Unescape(configContent);
        return unescapedContent.Replace(KubernetesConstants.HostPlaceHolderAppId, appId.ToLower())
            .Replace(KubernetesConstants.HostPlaceHolderVersion, version.ToLower())
            .Replace(KubernetesConstants.HostPlaceHolderNameSpace, KubernetesConstants.AppNameSpace.ToLower());
    }

    private static string GetHostClientConfigContent(string appId, string version, string templateFilePath,
        [CanBeNull] string corsUrls)
    {
        var configContent = File.ReadAllText(templateFilePath);
        var unescapedContent = Regex.Unescape(configContent);
        unescapedContent = unescapedContent.Replace(KubernetesConstants.HostPlaceHolderAppId, appId.ToLower())
            .Replace(KubernetesConstants.HostPlaceHolderVersion, version.ToLower())
            .Replace(KubernetesConstants.HostPlaceHolderNameSpace, KubernetesConstants.AppNameSpace.ToLower());
        if (corsUrls != null)
        {
            unescapedContent = unescapedContent.Replace(KubernetesConstants.HostClientCors, corsUrls);
        }

        return unescapedContent;
    }

    private async Task EnsureDeploymentAsync(
        string appId, string version, string imageName, string deploymentName,
        string deploymentLabelName, string containerName, List<string> command, int replicas,
        int containerPort, string maxSurge, string maxUnavailable, string healthPath, bool isSilo = false)
    {
        var configMapName = ConfigMapHelper.GetAppSettingConfigMapName(appId, version);
        var sideCarConfigName = ConfigMapHelper.GetAppFileBeatConfigMapName(appId, version);

        var deployment = DeploymentHelper.CreateAppDeploymentWithFileBeatSideCarDefinition(
            appId, version, imageName, deploymentName, deploymentLabelName, command, replicas, containerName,
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
            await _kubernetesClientAdapter.ReplaceNamespacedDeploymentAsync(deployment, deploymentName,
                KubernetesConstants.AppNameSpace);
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
        await CreateHostSiloAsync(GetHostName(appId, KubernetesConstants.HostSilo), version,
            _HostDeployOptions.HostSiloImageName,
            GetHostSiloConfigContent(appId, version, KubernetesConstants.HostSiloSettingTemplateFilePath));
        // await EnsurePhaAsync(appId, version);
        await CreatePodAsync(GetHostName(appId, KubernetesConstants.HostClient), version,
            _HostDeployOptions.HostClientImageName,
            GetHostClientConfigContent(appId, version, KubernetesConstants.HostClientSettingTemplateFilePath, corsUrls),
            KubernetesConstants.HostClientCommand, _kubernetesOptions.DeveloperHostName);
        return "";
    }

    private string GetHostName(string appId, string appType)
    {
        return $"{appId}-{appType}";
    }

    private async Task CreateHostSiloAsync(string appId, string version, string imageName, string hostSiloConfigContent)
    {
        var appSettingsContent = hostSiloConfigContent;
        var configFiles = new Dictionary<string, string>
        {
            { KubernetesConstants.AppSettingFileName, appSettingsContent },
            {
                KubernetesConstants.AppSettingSharedFileName,
                GetHostSiloConfigContent(appId, version, KubernetesConstants.AppSettingSharedFileName)
            },
            {
                KubernetesConstants.AppSettingHttpApiHostSharedFileName,
                GetHostSiloConfigContent(appId, version, KubernetesConstants.AppSettingHttpApiHostSharedFileName)
            },
            {
                KubernetesConstants.AppSettingSiloSharedFileName,
                GetHostSiloConfigContent(appId, version, KubernetesConstants.AppSettingSiloSharedFileName)
            }
        };
        await EnsureConfigMapAsync(
            appId,
            version,
            ConfigMapHelper.GetAppSettingConfigMapName,
            configFiles,
            ConfigMapHelper.CreateAppSettingConfigMapDefinition);

        await EnsureConfigMapAsync(
            appId,
            version,
            ConfigMapHelper.GetAppFileBeatConfigMapName,
            new Dictionary<string, string>
            {
                {
                    KubernetesConstants.FileBeatConfigFileName,
                    GetHostSiloConfigContent(appId, version, KubernetesConstants.HostFileBeatConfigTemplateFilePath)
                }
            },
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
            KubernetesConstants.SiloContainerTargetPort,
            KubernetesConstants.QueryPodMaxSurge,
            KubernetesConstants.QueryPodMaxUnavailable,
            "", true);

        // Ensure Service is created
        string serviceName = ServiceHelper.GetAppServiceName(appId, version);
        await EnsureServiceAsync(
            appId, version, serviceName,
            DeploymentHelper.GetAppDeploymentLabelName(appId, version),
            KubernetesConstants.SiloContainerTargetPort);
    }

    private async Task EnsurePhaAsync(string appId, string version)
    {
        var hpa = await _kubernetesClientAdapter.ReadNamespacedHorizontalPodAutoscalerAsync(appId, version);
        if (hpa == null)
        {
            await _kubernetesClientAdapter.CreateNamespacedHorizontalPodAutoscalerAsync(
                HPAHelper.CreateHPA(appId, version), KubernetesConstants.AppNameSpace);
        }
    }

    public async Task DestroyHostAsync(string appId, string version)
    {
        await DestroyHostSiloAsync(GetHostName(appId, KubernetesConstants.HostSilo), version);
        await DestroyPodsAsync(GetHostName(appId, KubernetesConstants.HostClient), version);
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
        var deployment =
            await _kubernetesClientAdapter.ReadNamespacedDeploymentAsync(deploymentName,
                KubernetesConstants.AppNameSpace);

        // Add or update the 'restartedAt' annotation to trigger the restart
        var annotations = deployment.Spec.Template.Metadata.Annotations ?? new Dictionary<string, string>();
        annotations["kubectl.kubernetes.io/restartedAt"] = DateTime.UtcNow.ToString("s");
        deployment.Spec.Template.Metadata.Annotations = annotations;

        // Update the Deployment to apply the changes
        await _kubernetesClientAdapter.ReplaceNamespacedDeploymentAsync(deployment, deploymentName,
            KubernetesConstants.AppNameSpace);
        _logger.LogInformation(
            $"[KubernetesAppManager] Deployment {deploymentName} restarted at {annotations["kubectl.kubernetes.io/restartedAt"]}");
    }

    public async Task CopyHostAsync(string sourceClientId, string newClientId, string version, string corsUrls)
    {
        _logger.LogInformation($"[KubernetesHostManager] Starting copy operation from {sourceClientId} to {newClientId}");

        // Validate source exists
        await ValidateSourceHostExistsAsync(sourceClientId, version);
        
        // Validate target doesn't exist
        await ValidateTargetHostNotExistsAsync(newClientId, version);

        try
        {
            // Copy Silo resources
            await CopyHostSiloResourcesAsync(sourceClientId, newClientId, version);
            
            // Copy Client resources
            await CopyHostClientResourcesAsync(sourceClientId, newClientId, version, corsUrls);

            _logger.LogInformation($"[KubernetesHostManager] Successfully copied host from {sourceClientId} to {newClientId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[KubernetesHostManager] Failed to copy host from {sourceClientId} to {newClientId}");
            
            // Attempt cleanup of partially created resources
            await CleanupPartialCopyAsync(newClientId, version);
            throw;
        }
    }

    private async Task ValidateSourceHostExistsAsync(string sourceClientId, string version)
    {
        var sourceSiloName = GetHostName(sourceClientId, KubernetesConstants.HostSilo);
        var sourceClientName = GetHostName(sourceClientId, KubernetesConstants.HostClient);
        
        var siloDeploymentExists = await DeploymentExistsAsync(DeploymentHelper.GetAppDeploymentName(sourceSiloName, version));
        var clientDeploymentExists = await DeploymentExistsAsync(DeploymentHelper.GetAppDeploymentName(sourceClientName, version));

        if (!siloDeploymentExists || !clientDeploymentExists)
        {
            throw new InvalidOperationException($"Source host {sourceClientId} does not exist or is incomplete");
        }
    }

    private async Task ValidateTargetHostNotExistsAsync(string newClientId, string version)
    {
        var targetSiloName = GetHostName(newClientId, KubernetesConstants.HostSilo);
        var targetClientName = GetHostName(newClientId, KubernetesConstants.HostClient);
        
        var siloDeploymentExists = await DeploymentExistsAsync(DeploymentHelper.GetAppDeploymentName(targetSiloName, version));
        var clientDeploymentExists = await DeploymentExistsAsync(DeploymentHelper.GetAppDeploymentName(targetClientName, version));

        if (siloDeploymentExists || clientDeploymentExists)
        {
            throw new InvalidOperationException($"Target host {newClientId} already exists");
        }
    }

    private async Task CopyHostSiloResourcesAsync(string sourceClientId, string newClientId, string version)
    {
        var sourceAppId = GetHostName(sourceClientId, KubernetesConstants.HostSilo);
        var targetAppId = GetHostName(newClientId, KubernetesConstants.HostSilo);

        // Copy ConfigMaps
        await CopyConfigMapAsync(sourceAppId, targetAppId, version, ConfigMapHelper.GetAppSettingConfigMapName);
        await CopyConfigMapAsync(sourceAppId, targetAppId, version, ConfigMapHelper.GetAppFileBeatConfigMapName);

        // Copy Deployment
        await CopyDeploymentAsync(sourceAppId, targetAppId, version);

        // Copy Service
        await CopyServiceAsync(sourceAppId, targetAppId, version);
    }

    private async Task CopyHostClientResourcesAsync(string sourceClientId, string newClientId, string version, string corsUrls)
    {
        var sourceAppId = GetHostName(sourceClientId, KubernetesConstants.HostClient);
        var targetAppId = GetHostName(newClientId, KubernetesConstants.HostClient);

        // Copy ConfigMaps with original configuration
        await CopyConfigMapAsync(sourceAppId, targetAppId, version, ConfigMapHelper.GetAppSettingConfigMapName);
        await CopyConfigMapAsync(sourceAppId, targetAppId, version, ConfigMapHelper.GetAppFileBeatConfigMapName);

        // Copy Deployment
        await CopyDeploymentAsync(sourceAppId, targetAppId, version);

        // Copy Service
        await CopyServiceAsync(sourceAppId, targetAppId, version);

        // Copy Ingress
        await CopyIngressAsync(sourceAppId, targetAppId, version);
    }

    private async Task CopyConfigMapAsync(string sourceAppId, string targetAppId, string version, Func<string, string, string> getConfigMapNameFunc)
    {
        var sourceConfigMapName = getConfigMapNameFunc(sourceAppId, version);
        var targetConfigMapName = getConfigMapNameFunc(targetAppId, version);

        var sourceConfigMap = await _kubernetesClientAdapter.ReadNamespacedConfigMapAsync(sourceConfigMapName, KubernetesConstants.AppNameSpace);
        
        var targetConfigMap = new V1ConfigMap
        {
            Metadata = new V1ObjectMeta
            {
                Name = targetConfigMapName,
                NamespaceProperty = KubernetesConstants.AppNameSpace,
                Labels = sourceConfigMap.Metadata.Labels
            },
            Data = ReplaceClientIdInConfigData(sourceConfigMap.Data, sourceAppId, targetAppId)
        };

        await _kubernetesClientAdapter.CreateConfigMapAsync(targetConfigMap, KubernetesConstants.AppNameSpace);
        _logger.LogInformation($"[KubernetesHostManager] ConfigMap {targetConfigMapName} copied successfully");
    }


    private Dictionary<string, string> ReplaceClientIdInConfigData(IDictionary<string, string> sourceData, string sourceAppId, string targetAppId)
    {
        if (sourceData == null) return null;

        var targetData = new Dictionary<string, string>();
        foreach (var kvp in sourceData)
        {
            var content = kvp.Value;
            if (!string.IsNullOrEmpty(content))
            {
                // Extract and replace the base clientId (remove -silo or -client suffix)
                var sourceClientId = sourceAppId.Replace($"-{KubernetesConstants.HostSilo}", "").Replace($"-{KubernetesConstants.HostClient}", "");
                var targetClientId = targetAppId.Replace($"-{KubernetesConstants.HostSilo}", "").Replace($"-{KubernetesConstants.HostClient}", "");
                content = content.Replace(sourceClientId, targetClientId);
            }
            targetData[kvp.Key] = content;
        }
        return targetData;
    }

    private async Task CopyDeploymentAsync(string sourceAppId, string targetAppId, string version)
    {
        var sourceDeploymentName = DeploymentHelper.GetAppDeploymentName(sourceAppId, version);
        var targetDeploymentName = DeploymentHelper.GetAppDeploymentName(targetAppId, version);

        var sourceDeployment = await _kubernetesClientAdapter.ReadNamespacedDeploymentAsync(sourceDeploymentName, KubernetesConstants.AppNameSpace);
        
        var targetDeployment = new V1Deployment
        {
            Metadata = new V1ObjectMeta
            {
                Name = targetDeploymentName,
                NamespaceProperty = KubernetesConstants.AppNameSpace,
                Labels = ReplaceClientIdInLabels(sourceDeployment.Metadata.Labels, sourceAppId, targetAppId)
            },
            Spec = new V1DeploymentSpec
            {
                Replicas = sourceDeployment.Spec.Replicas,
                Selector = new V1LabelSelector
                {
                    MatchLabels = ReplaceClientIdInLabels(sourceDeployment.Spec.Selector.MatchLabels, sourceAppId, targetAppId)
                },
                Template = new V1PodTemplateSpec
                {
                    Metadata = new V1ObjectMeta
                    {
                        Labels = ReplaceClientIdInLabels(sourceDeployment.Spec.Template.Metadata.Labels, sourceAppId, targetAppId)
                    },
                    Spec = CopyPodSpecWithUpdatedConfigMaps(sourceDeployment.Spec.Template.Spec, sourceAppId, targetAppId, version)
                },
                Strategy = sourceDeployment.Spec.Strategy
            }
        };

        await _kubernetesClientAdapter.CreateDeploymentAsync(targetDeployment, KubernetesConstants.AppNameSpace);
        _logger.LogInformation($"[KubernetesHostManager] Deployment {targetDeploymentName} copied successfully");
    }

    private V1PodSpec CopyPodSpecWithUpdatedConfigMaps(V1PodSpec sourcePodSpec, string sourceAppId, string targetAppId, string version)
    {
        var targetPodSpec = new V1PodSpec
        {
            Containers = sourcePodSpec.Containers.Select(container => new V1Container
            {
                Name = container.Name.Replace(sourceAppId, targetAppId),
                Image = container.Image,
                Ports = container.Ports,
                Env = ReplaceClientIdInEnvVars(container.Env, sourceAppId, targetAppId),
                Resources = container.Resources,
                VolumeMounts = container.VolumeMounts?.Select(vm => new V1VolumeMount
                {
                    Name = vm.Name.Replace(sourceAppId, targetAppId),
                    MountPath = vm.MountPath,
                    ReadOnlyProperty = vm.ReadOnlyProperty,
                    SubPath = vm.SubPath
                }).ToList(),
                LivenessProbe = container.LivenessProbe,
                ReadinessProbe = container.ReadinessProbe,
                Command = container.Command,
                Args = container.Args
            }).ToList(),
            Volumes = sourcePodSpec.Volumes?.Select(volume => new V1Volume
            {
                Name = volume.Name.Replace(sourceAppId, targetAppId),
                ConfigMap = volume.ConfigMap != null ? new V1ConfigMapVolumeSource
                {
                    Name = volume.ConfigMap.Name.Replace(sourceAppId, targetAppId),
                    Items = volume.ConfigMap.Items
                } : null,
                EmptyDir = volume.EmptyDir
            }).ToList(),
            RestartPolicy = sourcePodSpec.RestartPolicy,
            NodeSelector = sourcePodSpec.NodeSelector,
            Affinity = sourcePodSpec.Affinity,
            Tolerations = sourcePodSpec.Tolerations
        };

        return targetPodSpec;
    }

    private Dictionary<string, string> ReplaceClientIdInLabels(IDictionary<string, string> sourceLabels, string sourceAppId, string targetAppId)
    {
        if (sourceLabels == null) return null;

        var targetLabels = new Dictionary<string, string>();
        foreach (var kvp in sourceLabels)
        {
            var value = kvp.Value;
            if (!string.IsNullOrEmpty(value))
            {
                value = value.Replace(sourceAppId, targetAppId);
            }
            targetLabels[kvp.Key] = value;
        }
        return targetLabels;
    }

    private IList<V1EnvVar> ReplaceClientIdInEnvVars(IList<V1EnvVar> sourceEnvVars, string sourceAppId, string targetAppId)
    {
        if (sourceEnvVars == null) return null;

        var targetEnvVars = new List<V1EnvVar>();
        foreach (var envVar in sourceEnvVars)
        {
            var newEnvVar = new V1EnvVar
            {
                Name = envVar.Name,
                Value = envVar.Value,
                ValueFrom = envVar.ValueFrom
            };

            // Replace clientId in environment variable values
            if (!string.IsNullOrEmpty(newEnvVar.Value))
            {
                // Extract and replace the base clientId (remove -silo or -client suffix)
                var sourceClientId = sourceAppId.Replace($"-{KubernetesConstants.HostSilo}", "").Replace($"-{KubernetesConstants.HostClient}", "");
                var targetClientId = targetAppId.Replace($"-{KubernetesConstants.HostSilo}", "").Replace($"-{KubernetesConstants.HostClient}", "");
                newEnvVar.Value = newEnvVar.Value.Replace(sourceClientId, targetClientId);
            }

            targetEnvVars.Add(newEnvVar);
        }
        return targetEnvVars;
    }

    private async Task CopyServiceAsync(string sourceAppId, string targetAppId, string version)
    {
        var sourceServiceName = ServiceHelper.GetAppServiceName(sourceAppId, version);
        var targetServiceName = ServiceHelper.GetAppServiceName(targetAppId, version);

        var services = await _kubernetesClientAdapter.ListServiceAsync(KubernetesConstants.AppNameSpace);
        var sourceService = services.Items.FirstOrDefault(s => s.Metadata.Name == sourceServiceName);
        
        if (sourceService == null)
        {
            _logger.LogWarning($"[KubernetesHostManager] Source service {sourceServiceName} not found, skipping service copy");
            return;
        }

        var targetService = new V1Service
        {
            Metadata = new V1ObjectMeta
            {
                Name = targetServiceName,
                NamespaceProperty = KubernetesConstants.AppNameSpace,
                Labels = ReplaceClientIdInLabels(sourceService.Metadata.Labels, sourceAppId, targetAppId)
            },
            Spec = new V1ServiceSpec
            {
                Selector = ReplaceClientIdInLabels(sourceService.Spec.Selector, sourceAppId, targetAppId),
                Ports = sourceService.Spec.Ports,
                Type = sourceService.Spec.Type
            }
        };

        await _kubernetesClientAdapter.CreateServiceAsync(targetService, KubernetesConstants.AppNameSpace);
        _logger.LogInformation($"[KubernetesHostManager] Service {targetServiceName} copied successfully");
    }

    private async Task CopyIngressAsync(string sourceAppId, string targetAppId, string version)
    {
        var sourceIngressName = IngressHelper.GetAppIngressName(sourceAppId, version);
        var targetIngressName = IngressHelper.GetAppIngressName(targetAppId, version);

        var ingresses = await _kubernetesClientAdapter.ListIngressAsync(KubernetesConstants.AppNameSpace);
        var sourceIngress = ingresses.Items.FirstOrDefault(i => i.Metadata.Name == sourceIngressName);
        
        if (sourceIngress == null)
        {
            _logger.LogWarning($"[KubernetesHostManager] Source ingress {sourceIngressName} not found, skipping ingress copy");
            return;
        }

        var targetIngress = new V1Ingress
        {
            Metadata = new V1ObjectMeta
            {
                Name = targetIngressName,
                NamespaceProperty = KubernetesConstants.AppNameSpace,
                Labels = ReplaceClientIdInLabels(sourceIngress.Metadata.Labels, sourceAppId, targetAppId),
                Annotations = sourceIngress.Metadata.Annotations
            },
            Spec = new V1IngressSpec
            {
                IngressClassName = sourceIngress.Spec.IngressClassName,
                Rules = sourceIngress.Spec.Rules?.Select(rule => new V1IngressRule
                {
                    Host = rule.Host,
                    Http = new V1HTTPIngressRuleValue
                    {
                        Paths = rule.Http.Paths?.Select(path => new V1HTTPIngressPath
                        {
                            Path = path.Path.Replace(sourceAppId.Replace($"-{KubernetesConstants.HostClient}", ""), targetAppId.Replace($"-{KubernetesConstants.HostClient}", "")),
                            PathType = path.PathType,
                            Backend = new V1IngressBackend
                            {
                                Service = new V1IngressServiceBackend
                                {
                                    Name = path.Backend.Service.Name.Replace(sourceAppId, targetAppId),
                                    Port = path.Backend.Service.Port
                                }
                            }
                        }).ToList()
                    }
                }).ToList()
            }
        };

        await _kubernetesClientAdapter.CreateIngressAsync(targetIngress, KubernetesConstants.AppNameSpace);
        _logger.LogInformation($"[KubernetesHostManager] Ingress {targetIngressName} copied successfully");
    }

    private async Task CleanupPartialCopyAsync(string newClientId, string version)
    {
        try
        {
            _logger.LogInformation($"[KubernetesHostManager] Cleaning up partial copy for {newClientId}");
            
            // Attempt to destroy what was created
            var targetSiloName = GetHostName(newClientId, KubernetesConstants.HostSilo);
            var targetClientName = GetHostName(newClientId, KubernetesConstants.HostClient);
            
            // Try to clean up silo resources
            try { await DestroyHostSiloAsync(targetSiloName, version); } catch { }
            
            // Try to clean up client resources  
            try { await DestroyPodsAsync(targetClientName, version); } catch { }
            
            _logger.LogInformation($"[KubernetesHostManager] Cleanup completed for {newClientId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[KubernetesHostManager] Failed to cleanup partial copy for {newClientId}");
        }
    }
}