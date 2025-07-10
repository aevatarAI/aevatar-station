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
    private readonly HostDeployOptions _hostDeployOptions;
    private readonly ILogger<KubernetesHostManager> _logger;
    private readonly IKubernetesClientAdapter _kubernetesClientAdapter;

    public KubernetesHostManager(ILogger<KubernetesHostManager> logger,
        IKubernetesClientAdapter kubernetesClientAdapter, IOptionsSnapshot<KubernetesOptions> kubernetesOptions,
        IOptionsSnapshot<HostDeployOptions> hostDeployOptions)
    {
        _logger = logger;
        _kubernetesOptions = kubernetesOptions.Value;
        _hostDeployOptions = hostDeployOptions.Value;
        _kubernetesClientAdapter = kubernetesClientAdapter;
    }

    #region Create

    public async Task CreateApplicationAsync(string appId, string version, string corsUrls, Guid tenantId)
    {
        await CreateHostSiloAsync(appId, version, _hostDeployOptions.HostSiloImageName, tenantId);
        await CreateHttpClientAsync(appId, version, _hostDeployOptions.HostClientImageName,
            GetHostClientConfigContent(appId, version, KubernetesConstants.HostClientSettingTemplateFilePath, corsUrls),
            KubernetesConstants.HostClientCommand, _kubernetesOptions.DeveloperHostName);
    }

    public async Task<string> CreateNewWebHookAsync(string appId, string version, string imageName)
    {
        return await CreateHttpClientAsync(appId, version, imageName,
            GetWebhookConfigContent(appId, version, KubernetesConstants.WebhookSettingTemplateFilePath),
            KubernetesConstants.WebhookCommand, _kubernetesOptions.WebhookHostName);
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
            KubernetesConstants.QueryPodMaxUnavailable, "", true);

        // Ensure Service is created
        await EnsureServiceAsync(hostName, version, DeploymentHelper.GetAppDeploymentLabelName(appId, version),
            KubernetesConstants.SiloContainerTargetPort);
    }

    private async Task<string> CreateHttpClientAsync(string appId, string version, string imageName, string config,
        List<string> Command, string hostName)
    {
        // Ensure ConfigMaps (AppSettings and SideCar Configs) are created
        var appHostName = GetHostName(appId, KubernetesConstants.HostClient);
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

        // Delete Ingress
        var ingressName = IngressHelper.GetAppIngressName(appId, version);
        await EnsureIngressDeletedAsync(ingressName);

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


    private string GetHealthPath() => "";

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

    #endregion

    #region Ensure

    private async Task EnsureConfigMapAsync(string appId, string version,
        Func<string, string, string> getConfigMapNameFunc, Dictionary<string, string> configContent,
        Func<string, Dictionary<string, string>, V1ConfigMap> createConfigMapDefinitionFunc)
    {
        var configMapName = getConfigMapNameFunc(appId, version);
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

    #endregion
}