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
using Aevatar.Enum;
using Aevatar.Application.Grains.Agents.Configuration;
using Aevatar.Common;
using Aevatar.Domain.Shared.Configuration;

namespace Aevatar.Kubernetes.Manager;

public class KubernetesHostManager : IHostDeployManager,IHostCopyManager,ISingletonDependency
{
    // private readonly k8s.Kubernetes _k8sClient;
    private readonly KubernetesOptions _kubernetesOptions;
    private readonly HostDeployOptions _hostDeployOptions;
    private readonly ILogger<KubernetesHostManager> _logger;
    private readonly IKubernetesClientAdapter _kubernetesClientAdapter;
    private readonly IGrainFactory _grainFactory;

    public KubernetesHostManager(ILogger<KubernetesHostManager> logger,
        IKubernetesClientAdapter kubernetesClientAdapter,
        IOptionsSnapshot<KubernetesOptions> kubernetesOptions,
        IOptionsSnapshot<HostDeployOptions> hostDeployOptions,
        IGrainFactory grainFactory)
    {
        _logger = logger;
        _kubernetesOptions = kubernetesOptions.Value;
        _hostDeployOptions = hostDeployOptions.Value;
        _kubernetesClientAdapter = kubernetesClientAdapter;
        _grainFactory = grainFactory;
        
    }

    #region Create

    public async Task CreateApplicationAsync(string appId, string version, string corsUrls, Guid tenantId)
    {
        await CreateHostSiloAsync(appId, version, _hostDeployOptions.HostSiloImageName, tenantId);
        await CreateHttpClientAsync(appId, version, _hostDeployOptions.HostClientImageName,
            GetHostClientConfigContent(appId, version, KubernetesConstants.HostClientSettingTemplateFilePath, corsUrls),
            KubernetesConstants.HostClientCommand, _kubernetesOptions.DeveloperHostName,false);
    }

    public async Task<string> CreateNewWebHookAsync(string appId, string version, string imageName)
    {
        return await CreateHttpClientAsync(appId, version, imageName,
            GetWebhookConfigContent(appId, version, KubernetesConstants.WebhookSettingTemplateFilePath),
            KubernetesConstants.WebhookCommand, _kubernetesOptions.WebhookHostName,true);
    }

    private async Task CreateHostSiloAsync(string appId, string version, string imageName, Guid tenantId)
    {
        var appSettingsContent = GetHostSiloConfigContent(appId, version,
            KubernetesConstants.HostSiloSettingTemplateFilePath, tenantId);
        var configFiles = new Dictionary<string, string>
        {
            { KubernetesConstants.AppSettingFileName, appSettingsContent },
            {
                KubernetesConstants.AppSettingSharedFileName,
                GetHostSiloConfigContent(appId, version, KubernetesConstants.AppSettingSharedFileName, tenantId)
            },
            {
                KubernetesConstants.AppSettingHttpApiHostSharedFileName,
                GetHostSiloConfigContent(appId, version, KubernetesConstants.AppSettingHttpApiHostSharedFileName,
                    tenantId)
            },
            {
                KubernetesConstants.AppSettingSiloSharedFileName,
                GetHostSiloConfigContent(appId, version, KubernetesConstants.AppSettingSiloSharedFileName, tenantId)
            }
        };
        // Add business configuration file if available
        await AddBusinessConfigToConfigFilesAsync(appId, configFiles, HostTypeEnum.Silo);
        
        var hostName = GetHostName(appId, KubernetesConstants.HostSilo);
        await EnsureConfigMapAsync(hostName, version, ConfigMapHelper.GetAppSettingConfigMapName, configFiles,
            ConfigMapHelper.CreateAppSettingConfigMapDefinition);

        var fileBeatConfigContent = new Dictionary<string, string>
        {
            {
                KubernetesConstants.FileBeatConfigFileName,
                GetHostSiloConfigContent(appId, version, KubernetesConstants.HostFileBeatConfigTemplateFilePath,
                    tenantId)
            }
        };
        await EnsureConfigMapAsync(hostName, version, ConfigMapHelper.GetAppFileBeatConfigMapName,
            fileBeatConfigContent, ConfigMapHelper.CreateFileBeatConfigMapDefinition);

        // Ensure Deployment is created
        var deploymentName = DeploymentHelper.GetAppDeploymentName(hostName, version);
        var deploymentLabelName = DeploymentHelper.GetAppDeploymentLabelName(hostName, version);
        var containerName = ContainerHelper.GetAppContainerName(hostName, version);
        await EnsureDeploymentAsync(hostName, version, imageName, deploymentName, deploymentLabelName, containerName,
            KubernetesConstants.HostSiloCommand, _kubernetesOptions.AppPodReplicas,
            KubernetesConstants.SiloContainerTargetPort, KubernetesConstants.QueryPodMaxSurge,
            KubernetesConstants.QueryPodMaxUnavailable, GetHealthPath(), true);

        // Ensure Service is created
        await EnsureServiceAsync(hostName, version, DeploymentHelper.GetAppDeploymentLabelName(hostName, version),
            KubernetesConstants.SiloContainerTargetPort);
    }

    private async Task<string> CreateHttpClientAsync(string appId, string version, string imageName, string config,
        List<string> Command, string hostName,bool isWebhook)
    {
        // Ensure ConfigMaps (AppSettings and SideCar Configs) are created
        var appHostName = isWebhook ? appId : GetHostName(appId, KubernetesConstants.HostClient);
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

        // Add business configuration file if available
        var hostType = isWebhook ? HostTypeEnum.WebHook : HostTypeEnum.Client;
        await AddBusinessConfigToConfigFilesAsync(appId, configFiles, hostType);
        await EnsureConfigMapAsync(appHostName, version, ConfigMapHelper.GetAppSettingConfigMapName, configFiles,
            ConfigMapHelper.CreateAppSettingConfigMapDefinition);
        _logger.LogInformation(
            $"[KubernetesAppManager] ConfigMap injected for appId={appId}, version={version}, keys=[{string.Join(",", configFiles.Keys)}]");

        var fileBeatConfigContent = new Dictionary<string, string>
        {
            {
                KubernetesConstants.FileBeatConfigFileName,
                GetWebhookConfigContent(appId, version, KubernetesConstants.WebhookFileBeatConfigTemplateFilePath)
            }
        };
        await EnsureConfigMapAsync(appHostName, version, ConfigMapHelper.GetAppFileBeatConfigMapName, fileBeatConfigContent,
            ConfigMapHelper.CreateFileBeatConfigMapDefinition);

        // Ensure Deployment is created
        var deploymentName = DeploymentHelper.GetAppDeploymentName(appHostName, version);
        var deploymentLabelName = DeploymentHelper.GetAppDeploymentLabelName(appHostName, version);
        var containerName = ContainerHelper.GetAppContainerName(appHostName, version);
        await EnsureDeploymentAsync(
            appHostName, version, imageName,
            deploymentName, deploymentLabelName, containerName,
            Command,
            _kubernetesOptions.AppPodReplicas,
            KubernetesConstants.WebhookContainerTargetPort,
            KubernetesConstants.QueryPodMaxSurge,
            KubernetesConstants.QueryPodMaxUnavailable,
            GetHealthPath());

        // Ensure Service is created
        var serviceLabel = DeploymentHelper.GetAppDeploymentLabelName(appHostName, version);
        await EnsureServiceAsync(appHostName, version, serviceLabel, KubernetesConstants.WebhookContainerTargetPort);

        // Ensure Ingress is created
        var rulePath = $"/{appHostName}".ToLower();
        await EnsureIngressAsync(appHostName, version, hostName, rulePath,
            KubernetesConstants.WebhookContainerTargetPort);

        return hostName.TrimEnd('/') + rulePath;
    }

    #endregion

    #region Update

    private async Task UpdateSiloConfigMapAsync(string appId, string version, Guid tenantId)
    {
        _logger.LogInformation(
            $"[KubernetesHostManager] Starting to update Host Silo ConfigMap for appResourceId: {appId}, version: {version}, tenantId: {tenantId}");

        var hostSiloId = GetHostName(appId, KubernetesConstants.HostSilo);
        var hostSiloConfigContent =
            GetHostSiloConfigContent(appId, version, KubernetesConstants.HostSiloSettingTemplateFilePath, tenantId);
        var configFiles = new Dictionary<string, string>
        {
            { KubernetesConstants.AppSettingFileName, hostSiloConfigContent },
            {
                KubernetesConstants.AppSettingSharedFileName,
                GetHostSiloConfigContent(appId, version, KubernetesConstants.AppSettingSharedFileName, tenantId)
            },
            {
                KubernetesConstants.AppSettingHttpApiHostSharedFileName,
                GetHostSiloConfigContent(appId, version, KubernetesConstants.AppSettingHttpApiHostSharedFileName,
                    tenantId)
            },
            {
                KubernetesConstants.AppSettingSiloSharedFileName,
                GetHostSiloConfigContent(appId, version, KubernetesConstants.AppSettingSiloSharedFileName, tenantId)
            }
        };
        // Add business configuration file if available
        await AddBusinessConfigToConfigFilesAsync(appId, configFiles, HostTypeEnum.Silo);
        await EnsureConfigMapAsync(hostSiloId, version, ConfigMapHelper.GetAppSettingConfigMapName,
            configFiles, ConfigMapHelper.CreateAppSettingConfigMapDefinition);

        _logger.LogInformation(
            $"[KubernetesHostManager] Successfully updated Host Silo ConfigMap for appResourceId: {appId}, version: {version}, tenantId: {tenantId}");
    }

    private async Task UpdateHttpClientConfigMapAsync(string appId, string version, string corsUrls, Guid projectId)
    {
        _logger.LogInformation(
            $"[KubernetesHostManager] Starting to update Host Client ConfigMap for appResourceId: {appId}, version: {version}, corsUrls: {corsUrls}, tenantId: {projectId}");

        var hostClientId = GetHostName(appId, KubernetesConstants.HostClient);
        var config = GetHostClientConfigContent(appId, version, KubernetesConstants.HostClientSettingTemplateFilePath,
            corsUrls);
        var hostClientConfigContent = new Dictionary<string, string>
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
        // Add business configuration file if available
        await AddBusinessConfigToConfigFilesAsync(appId, hostClientConfigContent, HostTypeEnum.Silo);
        
        await EnsureConfigMapAsync(hostClientId, version, ConfigMapHelper.GetAppSettingConfigMapName,
            hostClientConfigContent, ConfigMapHelper.CreateAppSettingConfigMapDefinition);

        _logger.LogInformation(
            $"[KubernetesHostManager] Successfully updated Host Client ConfigMap for appResourceId: {appId}, version: {version}, corsUrls: {corsUrls}, tenantId: {projectId}");
    }

    #endregion

    #region Restart

    public async Task UpgradeApplicationAsync(string appId, string version, string corsUrls, Guid tenantId)
    {
        _logger.LogInformation(
            $"Updating service configuration for: {appId} with CORS URLs: {corsUrls}, project: {tenantId}");

        try
        {
            await UpdateSiloConfigMapAsync(appId, version, tenantId);
            await UpdateHttpClientConfigMapAsync(appId, version, corsUrls, tenantId);

            _logger.LogInformation($"Service configuration updated successfully: {appId}");

            var siloAppId = GetHostName(appId, KubernetesConstants.HostSilo);
            var siloDeploymentName = DeploymentHelper.GetAppDeploymentName(siloAppId, version);
            await RestartDeploymentAsync(siloDeploymentName);

            var clientAppId = GetHostName(appId, KubernetesConstants.HostClient);
            var clientDeploymentName = DeploymentHelper.GetAppDeploymentName(clientAppId, version);
            await RestartDeploymentAsync(clientDeploymentName);

            _logger.LogInformation(
                $"Restarting Host services with project context: Silo={siloDeploymentName}, Client={clientDeploymentName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to update service configuration: {appId}");
            throw;
        }
    }

    public async Task RestartHostAsync(string appId, string version)
    {
        var deploymentName = DeploymentHelper.GetAppDeploymentName(appId, version);
        await RestartDeploymentAsync(deploymentName);
    }

    public async Task RestartWebHookAsync(string appId, string version)
    {
        var deploymentName = DeploymentHelper.GetAppDeploymentName(appId, version);
        await RestartDeploymentAsync(deploymentName);
    }

    #endregion

    #region Destroy

    public async Task DestroyApplicationAsync(string appId, string version)
    {
        await DestroyHostSiloAsync(GetHostName(appId, KubernetesConstants.HostSilo), version);
        await DestroyHttpClientAsync(GetHostName(appId, KubernetesConstants.HostClient), version);
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

        // Delete Service
        var serviceName = ServiceHelper.GetAppServiceName(appId, version);
        await EnsureServiceDeletedAsync(serviceName);
        
        _logger.LogInformation(
            $"[KubernetesHostManager] Successfully destroyed Host Silo resources for hostName: {appId}, version: {version}");
    }

    private async Task DestroyHttpClientAsync(string appId, string version)
    {
        _logger.LogInformation(
            $"[KubernetesHostManager] Starting to destroy Host Client resources for hostName: {appId}, version: {version}");

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

        _logger.LogInformation(
            $"[KubernetesHostManager] Successfully destroyed Host Client resources for hostName: {appId}, version: {version}");
    }

    public async Task DestroyWebHookAsync(string appId, string version)
    {
        await DestroyHttpClientAsync(appId, version);
    }

    #endregion

    #region Get config content

    private static string GetWebhookConfigContent(string appId, string version, string templateFilePath)
    {
        var rawContent = File.ReadAllText(templateFilePath);
        var unescapedContent = Regex.Unescape(rawContent);
        return unescapedContent
            .Replace(KubernetesConstants.PlaceHolderAppId, appId.ToLower())
            .Replace(KubernetesConstants.PlaceHolderVersion, version.ToLower())
            .Replace(KubernetesConstants.PlaceHolderNameSpace, KubernetesConstants.AppNameSpace.ToLower());
    }

    private static string GetHostSiloConfigContent(string appId, string version, string templatePath, Guid projectId)
    {
        var configContent = File.ReadAllText(templatePath);
        var unescapedContent = Regex.Unescape(configContent);
        return unescapedContent.Replace(KubernetesConstants.HostPlaceHolderAppId, appId.ToLower())
            .Replace(KubernetesConstants.HostPlaceHolderVersion, version.ToLower())
            .Replace(KubernetesConstants.HostPlaceHolderNameSpace, KubernetesConstants.AppNameSpace.ToLower())
            .Replace(KubernetesConstants.HostPlaceHolderTenantId, projectId.ToString());
    }

    private static string GetHostClientConfigContent(string appId, string version, string templateFilePath,
        [CanBeNull] string corsUrls, Guid? tenantId = null)
    {
        var configContent = File.ReadAllText(templateFilePath);
        var unescapedContent = Regex.Unescape(configContent);
        unescapedContent = unescapedContent.Replace(KubernetesConstants.HostPlaceHolderAppId, appId.ToLower())
            .Replace(KubernetesConstants.HostPlaceHolderVersion, version.ToLower())
            .Replace(KubernetesConstants.HostPlaceHolderNameSpace, KubernetesConstants.AppNameSpace.ToLower())
            .Replace(KubernetesConstants.HostPlaceHolderTenantId, tenantId.ToString());
        if (corsUrls != null)
        {
            unescapedContent = unescapedContent.Replace(KubernetesConstants.HostClientCors, corsUrls);
        }

        return unescapedContent;
    }

    #endregion

    #region Common

    public async Task UpdateDeploymentImageAsync(string appId, string version, string newImage)
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
                _logger.LogInformation($"Updated deployment {deploymentName} to use image {newImage} with latest business configuration");
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


    private string GetHealthPath() => "/health";

    private string GetHostName(string appId, string appType) => $"{appId}-{appType}";

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
    #endregion

   

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


    private async Task EnsureDeploymentAsync(string applicationName, string version, string imageName,
        string deploymentName, string deploymentLabelName, string containerName, List<string> command, int replicas,
        int containerPort, string maxSurge, string maxUnavailable, string healthPath, bool isSilo = false)
    {
        var configMapName = ConfigMapHelper.GetAppSettingConfigMapName(applicationName, version);
        var sideCarConfigName = ConfigMapHelper.GetAppFileBeatConfigMapName(applicationName, version);
        var deployment = DeploymentHelper.CreateAppDeploymentWithFileBeatSideCarDefinition(
            applicationName, version, imageName, deploymentName, deploymentLabelName, command, replicas, containerName,
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
            _logger.LogInformation("[KubernetesAppManager] Update Deployment {deploymentName} updated", deploymentName);
        }
    }

    private async Task EnsureServiceAsync(string hostName, string version, string deploymentLabelName, int targetPort)
    {
        var serviceName = ServiceHelper.GetAppServiceName(hostName, version);
        var services = await _kubernetesClientAdapter.ListServiceAsync(KubernetesConstants.AppNameSpace);
        if (!services.Items.Any(item => item.Metadata.Name == serviceName))
        {
            var serviceLabelName = ServiceHelper.GetAppServiceLabelName(hostName, version);
            var servicePortName = ServiceHelper.GetAppServicePortName(version);

            var service = ServiceHelper.CreateAppClusterIPServiceDefinition(hostName, serviceName, serviceLabelName,
                deploymentLabelName, servicePortName, targetPort, targetPort);

            await _kubernetesClientAdapter.CreateServiceAsync(service, KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager] Service {serviceName} created", serviceName);
        }
    }

    private async Task EnsureIngressAsync(string appHostName, string version, string hostName, string rulePath,
        int targetPort)
    {
        var serviceName = ServiceHelper.GetAppServiceName(appHostName, version);
        var ingressName = IngressHelper.GetAppIngressName(appHostName, version);
        var ingresses = await _kubernetesClientAdapter.ListIngressAsync(KubernetesConstants.AppNameSpace);
        if (!ingresses.Items.Any(item => item.Metadata.Name == ingressName))
        {
            var ingress = IngressHelper.CreateAppIngressDefinition(ingressName, hostName, rulePath, serviceName,
                targetPort);

            await _kubernetesClientAdapter.CreateIngressAsync(ingress, KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager] Ingress {ingressName} created", ingressName);
        }
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
    private async Task EnsurePhaAsync(string appId, string version)
    {
        var hpa = await _kubernetesClientAdapter.ReadNamespacedHorizontalPodAutoscalerAsync(appId, version);
        if (hpa == null)
        {
            await _kubernetesClientAdapter.CreateNamespacedHorizontalPodAutoscalerAsync(
                HPAHelper.CreateHPA(appId, version), KubernetesConstants.AppNameSpace);
        }
    }

    private async Task RestartAllRelatedDeploymentsAsync(string appId)
    {
        // Use label selector to find all deployments with the same appId
        var labelSelector = $"{KubernetesConstants.AppIdLabelKey}={appId}";
        var deployments = await _kubernetesClientAdapter.ListDeploymentAsync(KubernetesConstants.AppNameSpace, labelSelector);
        
        if (!deployments.Items.Any())
        {
            _logger.LogWarning($"No deployments found for appId: {appId}");
            return;
        }

        _logger.LogInformation($"Found {deployments.Items.Count} deployments for appId: {appId}");
        
        // Restart all related deployments
        foreach (var deployment in deployments.Items)
        {
            var deploymentName = deployment.Metadata.Name;
            _logger.LogInformation($"Restarting deployment: {deploymentName}");
            await ApplyRollingRestartAsync(deploymentName);
        }
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

    public async Task CopyHostAsync(string sourceClientId, string newClientId, string version)
    {
        _logger.LogInformation($"[KubernetesHostManager] Starting copy operation from {sourceClientId} to {newClientId}");

        // Validate source exists and target doesn't exist concurrently
        await Task.WhenAll(
            ValidateSourceHostExistsAsync(sourceClientId, version),
            ValidateTargetHostNotExistsAsync(newClientId, version)
        );

        try
        {
            // Copy Silo and Client resources concurrently
            await Task.WhenAll(
                CopyHostSiloResourcesAsync(sourceClientId, newClientId, version),
                CopyHostClientResourcesAsync(sourceClientId, newClientId, version)
            );

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
        
        // Check both deployments concurrently
        var deploymentTasks = await Task.WhenAll(
            DeploymentExistsAsync(DeploymentHelper.GetAppDeploymentName(sourceSiloName, version)),
            DeploymentExistsAsync(DeploymentHelper.GetAppDeploymentName(sourceClientName, version))
        );
        
        var siloDeploymentExists = deploymentTasks[0];
        var clientDeploymentExists = deploymentTasks[1];

        if (!siloDeploymentExists || !clientDeploymentExists)
        {
            throw new InvalidOperationException($"Source host {sourceClientId} does not exist or is incomplete");
        }
    }

    private async Task ValidateTargetHostNotExistsAsync(string newClientId, string version)
    {
        var targetSiloName = GetHostName(newClientId, KubernetesConstants.HostSilo);
        var targetClientName = GetHostName(newClientId, KubernetesConstants.HostClient);
        
        // Check both deployments concurrently
        var deploymentTasks = await Task.WhenAll(
            DeploymentExistsAsync(DeploymentHelper.GetAppDeploymentName(targetSiloName, version)),
            DeploymentExistsAsync(DeploymentHelper.GetAppDeploymentName(targetClientName, version))
        );
        
        var siloDeploymentExists = deploymentTasks[0];
        var clientDeploymentExists = deploymentTasks[1];

        if (siloDeploymentExists || clientDeploymentExists)
        {
            throw new InvalidOperationException($"Target host {newClientId} already exists");
        }
    }

    private async Task CopyHostSiloResourcesAsync(string sourceClientId, string newClientId, string version)
    {
        var sourceAppId = GetHostName(sourceClientId, KubernetesConstants.HostSilo);
        var targetAppId = GetHostName(newClientId, KubernetesConstants.HostSilo);

        // Copy ConfigMaps concurrently
        await Task.WhenAll(
            CopyConfigMapAsync(sourceAppId, targetAppId, version, ConfigMapHelper.GetAppSettingConfigMapName),
            CopyConfigMapAsync(sourceAppId, targetAppId, version, ConfigMapHelper.GetAppFileBeatConfigMapName)
        );

        // Copy Deployment and Service concurrently (ConfigMaps must be ready first)
        await Task.WhenAll(
            CopyDeploymentAsync(sourceAppId, targetAppId, version),
            CopyServiceAsync(sourceAppId, targetAppId, version)
        );
    }

    private async Task CopyHostClientResourcesAsync(string sourceClientId, string newClientId, string version)
    {
        var sourceAppId = GetHostName(sourceClientId, KubernetesConstants.HostClient);
        var targetAppId = GetHostName(newClientId, KubernetesConstants.HostClient);

        // Copy ConfigMaps concurrently
        await Task.WhenAll(
            CopyConfigMapAsync(sourceAppId, targetAppId, version, ConfigMapHelper.GetAppSettingConfigMapName),
            CopyConfigMapAsync(sourceAppId, targetAppId, version, ConfigMapHelper.GetAppFileBeatConfigMapName)
        );

        // Copy Deployment, Service and Ingress concurrently (ConfigMaps must be ready first)
        await Task.WhenAll(
            CopyDeploymentAsync(sourceAppId, targetAppId, version),
            CopyServiceAsync(sourceAppId, targetAppId, version),
            CopyIngressAsync(sourceAppId, targetAppId, version)
        );
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

        await CopyDeploymentCoreAsync(
            sourceDeploymentName,
            targetDeploymentName,
            $"from {sourceAppId} to {targetAppId} (version {version})",
            sourceDeployment => new DeploymentCopyContext
            {
                MetadataLabels = ReplaceClientIdInLabels(sourceDeployment.Metadata.Labels, sourceAppId, targetAppId),
                SelectorLabels = ReplaceClientIdInLabels(sourceDeployment.Spec.Selector.MatchLabels, sourceAppId, targetAppId),
                TemplateLabels = ReplaceClientIdInLabels(sourceDeployment.Spec.Template.Metadata.Labels, sourceAppId, targetAppId),
                PodSpec = CopyPodSpecWithUpdatedConfigMaps(sourceDeployment.Spec.Template.Spec, sourceAppId, targetAppId, version)
            });
    }

    /// <summary>
    /// Core deployment copying logic - handles common structure and error handling
    /// Uses strategy pattern for different label and PodSpec update approaches
    /// </summary>
    private async Task CopyDeploymentCoreAsync(
        string sourceDeploymentName,
        string targetDeploymentName,
        string operationDescription,
        Func<V1Deployment, DeploymentCopyContext> contextFactory)
    {
        _logger.LogInformation($"[KubernetesHostManager] Starting deployment copy {operationDescription}");

        try
        {
            // Fetch source deployment with null check
            var sourceDeployment = await _kubernetesClientAdapter.ReadNamespacedDeploymentAsync(
                sourceDeploymentName, KubernetesConstants.AppNameSpace);
            if (sourceDeployment == null)
            {
                throw new InvalidOperationException($"Source deployment {sourceDeploymentName} not found in namespace {KubernetesConstants.AppNameSpace}");
            }

            // Generate copy context using the provided strategy
            var context = contextFactory(sourceDeployment);

            // Create target deployment with common structure
            var targetDeployment = new V1Deployment
            {
                Metadata = new V1ObjectMeta
                {
                    Name = targetDeploymentName,
                    NamespaceProperty = KubernetesConstants.AppNameSpace,
                    Labels = context.MetadataLabels
                },
                Spec = new V1DeploymentSpec
                {
                    Replicas = sourceDeployment.Spec.Replicas,
                    Selector = new V1LabelSelector
                    {
                        MatchLabels = context.SelectorLabels
                    },
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta
                        {
                            Labels = context.TemplateLabels
                        },
                        Spec = context.PodSpec
                    },
                    Strategy = sourceDeployment.Spec.Strategy
                }
            };

            await _kubernetesClientAdapter.CreateDeploymentAsync(targetDeployment, KubernetesConstants.AppNameSpace);
            _logger.LogInformation($"[KubernetesHostManager] Deployment {targetDeploymentName} copied successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[KubernetesHostManager] Failed to copy deployment {operationDescription}");
            throw;
        }
    }

    /// <summary>
    /// Context for deployment copying operations
    /// Encapsulates the different strategies for label and PodSpec updates
    /// </summary>
    private class DeploymentCopyContext
    {
        public Dictionary<string, string> MetadataLabels { get; init; } = new();
        public Dictionary<string, string> SelectorLabels { get; init; } = new();
        public Dictionary<string, string> TemplateLabels { get; init; } = new();
        public V1PodSpec PodSpec { get; init; } = new();
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

    public async Task CopyDeploymentWithPatternAsync(string clientId, string sourceVersion, string targetVersion, 
        string siloNamePattern)
    {
        var hostName = GetHostName(clientId, KubernetesConstants.HostSilo);
        var sourceDeploymentName = DeploymentHelper.GetAppDeploymentName(hostName, sourceVersion);
        var targetDeploymentName = DeploymentHelper.GetAppDeploymentName(hostName, targetVersion);
        
        var sourceContainerName = sourceDeploymentName.Replace("deployment-", "container-");
        var targetContainerName = targetDeploymentName.Replace("deployment-", "container-");

        await CopyDeploymentCoreAsync(
            sourceDeploymentName,
            targetDeploymentName,
            $"client {clientId} from version {sourceVersion} to {targetVersion} with silo pattern {siloNamePattern}",
            sourceDeployment => new DeploymentCopyContext
            {
                MetadataLabels = ReplaceDeploymentNameInLabels(sourceDeployment.Metadata.Labels, sourceDeploymentName, targetDeploymentName),
                SelectorLabels = UpdateSelectorLabels(sourceDeployment.Spec.Selector.MatchLabels, targetDeploymentName),
                TemplateLabels = UpdateTemplateLabels(sourceDeployment.Spec.Template.Metadata.Labels, targetDeploymentName),
                PodSpec = UpdatePodSpecWithNewPattern(sourceDeployment.Spec.Template.Spec, sourceContainerName, targetContainerName, siloNamePattern)
            });
    }

    private Dictionary<string, string> ReplaceDeploymentNameInLabels(IDictionary<string, string> sourceLabels, string sourceDeployment, string targetDeployment)
    {
        if (sourceLabels == null) return new Dictionary<string, string>();
        
        var targetLabels = new Dictionary<string, string>();
        foreach (var label in sourceLabels)
        {
            var newValue = label.Value?.Replace(sourceDeployment, targetDeployment) ?? label.Value;
            targetLabels[label.Key] = newValue;
        }
        return targetLabels;
    }

    private Dictionary<string, string> UpdateSelectorLabels(IDictionary<string, string> sourceSelector, string targetDeploymentName)
    {
        if (sourceSelector == null) return new Dictionary<string, string>();
        
        var targetSelector = new Dictionary<string, string>();
        foreach (var selector in sourceSelector)
        {
            if (selector.Key == "app")
            {
                targetSelector[selector.Key] = targetDeploymentName;
            }
            else
            {
                targetSelector[selector.Key] = selector.Value;
            }
        }
        return targetSelector;
    }

    private Dictionary<string, string> UpdateTemplateLabels(IDictionary<string, string> sourceLabels, string targetDeploymentName)
    {
        if (sourceLabels == null) return new Dictionary<string, string>();
        
        var targetLabels = new Dictionary<string, string>();
        foreach (var label in sourceLabels)
        {
            if (label.Key == "app")
            {
                targetLabels[label.Key] = targetDeploymentName;
            }
            else
            {
                targetLabels[label.Key] = label.Value;
            }
        }
        return targetLabels;
    }

    private V1PodSpec UpdatePodSpecWithNewPattern(V1PodSpec sourcePodSpec, string sourceContainerName, string targetContainerName, string siloNamePattern)
    {
        // Follow the same pattern as CopyPodSpecWithUpdatedConfigMaps but with pattern updates
        var targetPodSpec = new V1PodSpec
        {
            Containers = sourcePodSpec.Containers.Select(container => new V1Container
            {
                Name = container.Name == sourceContainerName ? targetContainerName : container.Name,
                Image = container.Image,
                Command = container.Command, // Critical: Copy the startup command
                Args = container.Args, // Critical: Copy the startup arguments
                Ports = container.Ports,
                Env = UpdateEnvironmentVariables(container.Env, siloNamePattern), // Only difference: update SILO_NAME_PATTERN
                Resources = container.Resources,
                VolumeMounts = container.VolumeMounts, // Keep original volume mounts
                LivenessProbe = container.LivenessProbe,
                ReadinessProbe = container.ReadinessProbe,
                SecurityContext = container.SecurityContext
            }).ToList(),
            Volumes = sourcePodSpec.Volumes, // Keep original volumes/ConfigMaps - they can be shared
            RestartPolicy = sourcePodSpec.RestartPolicy,
            NodeSelector = sourcePodSpec.NodeSelector,
            Affinity = sourcePodSpec.Affinity,
            Tolerations = sourcePodSpec.Tolerations
        };

        return targetPodSpec;
    }

    private IList<V1EnvVar> UpdateEnvironmentVariables(IList<V1EnvVar> sourceEnvVars, string siloNamePattern)
    {
        if (sourceEnvVars == null) return new List<V1EnvVar>();

        var targetEnvVars = new List<V1EnvVar>();
        var siloPatternUpdated = false;

        foreach (var envVar in sourceEnvVars)
        {
            if (envVar.Name == "SILO_NAME_PATTERN")
            {
                targetEnvVars.Add(new V1EnvVar(envVar.Name, siloNamePattern));
                siloPatternUpdated = true;
            }
            else
            {
                targetEnvVars.Add(new V1EnvVar(envVar.Name, envVar.Value)
                {
                    ValueFrom = envVar.ValueFrom
                });
            }
        }

        // Add SILO_NAME_PATTERN if it wasn't in the original environment variables
        if (!siloPatternUpdated && !string.IsNullOrEmpty(siloNamePattern))
        {
            targetEnvVars.Add(new V1EnvVar("SILO_NAME_PATTERN", siloNamePattern));
        }

        return targetEnvVars;
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
            try { await DestroyHttpClientAsync(targetClientName, version); } catch { }
            
            _logger.LogInformation($"[KubernetesHostManager] Cleanup completed for {newClientId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[KubernetesHostManager] Failed to cleanup partial copy for {newClientId}");
        }
    }

    /// <summary>
    /// Adds business configuration as a separate file to the ConfigMap
    /// </summary>
    private async Task AddBusinessConfigToConfigFilesAsync(string hostId, Dictionary<string, string> configFiles, HostTypeEnum hostType)
    {
        try
        {
            // Get business configuration from HostConfigurationGAgent
            var grainKey = $"{hostId}:{hostType}";
            var configAgent = _grainFactory.GetGrain<IHostConfigurationGAgent>(GuidUtil.StringToGuid(grainKey));
            var businessConfigResult = await configAgent.GetBusinessConfigurationJsonAsync();

            if (string.IsNullOrWhiteSpace(businessConfigResult.ConfigurationJson) || businessConfigResult.ConfigurationJson == "{}")
            {
                _logger.LogDebug("No business configuration found for {HostId}:{HostType}", hostId, hostType);
                // Add empty business config file
                configFiles[SecureConfigurationExtensions.DefaultBusinessConfigPath] = "{}";
                return;
            }

            // Add business configuration as a separate file in ConfigMap
            configFiles[SecureConfigurationExtensions.DefaultBusinessConfigPath] = businessConfigResult.ConfigurationJson;
            
            _logger.LogInformation("Business configuration added to ConfigMap for {HostId}:{HostType}", hostId, hostType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load business configuration for {HostId}:{HostType}, adding empty config", hostId, hostType);
            // Add empty business config file as fallback
            configFiles[SecureConfigurationExtensions.DefaultBusinessConfigPath] = "{}";
        }
    }

    /// <summary>
    /// Updates existing K8s ConfigMaps with the latest business configuration for specific host type
    /// </summary>
    public async Task UpdateBusinessConfigurationAsync(string hostId, string version, HostTypeEnum hostType)
    {
        _logger.LogInformation("Updating business configuration for hostId: {HostId}, version: {Version}, hostType: {HostType}", 
            hostId, version, hostType);

        string appId;
        switch (hostType)
        {
            case HostTypeEnum.WebHook:
                appId = hostId; // WebHook uses hostId directly
                break;
            case HostTypeEnum.Silo:
                appId = GetHostName(hostId, KubernetesConstants.HostSilo);
                break;
            case HostTypeEnum.Client:
                appId = GetHostName(hostId, KubernetesConstants.HostClient);
                break;
            default:
                throw new ArgumentException($"Unknown host type: {hostType}");
        }

        await UpdateConfigMapWithBusinessConfigAsync(appId, version, hostType);
        await RestartAllRelatedDeploymentsAsync(appId);
        _logger.LogInformation("Business configuration update completed for hostId: {HostId}, hostType: {HostType}", 
            hostId, hostType);
    }

    /// <summary>
    /// Updates a specific ConfigMap with business configuration for a given host type
    /// </summary>
    private async Task UpdateConfigMapWithBusinessConfigAsync(string appId, string version, HostTypeEnum hostType)
    {
        try
        {
            var configMapName = ConfigMapHelper.GetAppSettingConfigMapName(appId, version);
            
            // Check if ConfigMap exists
            var configMaps = await _kubernetesClientAdapter.ListConfigMapAsync(KubernetesConstants.AppNameSpace);
            var existingConfigMap = configMaps.Items.FirstOrDefault(cm => cm.Metadata.Name == configMapName);
            
            if (existingConfigMap == null)
            {
                _logger.LogDebug("ConfigMap {ConfigMapName} not found, skipping business config update", configMapName);
                return;
            }

            // Get current ConfigMap data
            var updatedConfigData = new Dictionary<string, string>(existingConfigMap.Data ?? new Dictionary<string, string>());

            // Extract hostId from appId for grain key (remove suffixes like -silo, -client)
            var hostId = appId.Replace($"-{KubernetesConstants.HostSilo}", "").Replace($"-{KubernetesConstants.HostClient}", "");

            // Add/update business configuration
            await AddBusinessConfigToConfigFilesAsync(hostId, updatedConfigData, hostType);

            // Create updated ConfigMap
            var updatedConfigMap = ConfigMapHelper.CreateAppSettingConfigMapDefinition(configMapName, updatedConfigData);

            // Update the ConfigMap
            await _kubernetesClientAdapter.ReplaceNamespacedConfigMapAsync(updatedConfigMap, configMapName, KubernetesConstants.AppNameSpace);
            
            _logger.LogInformation("ConfigMap {ConfigMapName} updated with latest business configuration for {HostType}", configMapName, hostType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update ConfigMap for appId: {AppId}, hostType: {HostType}", appId, hostType);
        }
    }

}