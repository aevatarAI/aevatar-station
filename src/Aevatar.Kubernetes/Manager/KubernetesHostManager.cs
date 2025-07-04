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

    #region Logging Helpers

    private void LogResourceOperation(string operation, string resourceType, string resourceName,
        string? additionalInfo = null, string? appId = null, string? version = null)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = operation,
            ["ResourceType"] = resourceType,
            ["ResourceName"] = resourceName,
            ["AppId"] = appId ?? "unknown",
            ["Version"] = version ?? "unknown"
        });

        var message = additionalInfo != null
            ? $"[KubernetesHostManager] {operation} {resourceType} {resourceName} - {additionalInfo}"
            : $"[KubernetesHostManager] {operation} {resourceType} {resourceName}";

        _logger.LogInformation(message);
    }

    private void LogResourceOperationError(string operation, string resourceType, string resourceName,
        Exception? exception = null, string? errorMessage = null, string? appId = null, string? version = null)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = operation,
            ["ResourceType"] = resourceType,
            ["ResourceName"] = resourceName,
            ["AppId"] = appId ?? "unknown",
            ["Version"] = version ?? "unknown"
        });

        var message = errorMessage != null
            ? $"[KubernetesHostManager] {operation} {resourceType} {resourceName} failed - {errorMessage}"
            : $"[KubernetesHostManager] {operation} {resourceType} {resourceName} failed";

        if (exception != null)
        {
            _logger.LogError(exception, message);
        }
        else
        {
            _logger.LogError(message);
        }
    }

    private void LogResourceOperationWarning(string operation, string resourceType, string resourceName,
        string warningMessage, string? appId = null, string? version = null)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = operation,
            ["ResourceType"] = resourceType,
            ["ResourceName"] = resourceName,
            ["AppId"] = appId ?? "unknown",
            ["Version"] = version ?? "unknown"
        });

        var message = $"[KubernetesHostManager] {operation} {resourceType} {resourceName} - {warningMessage}";
        _logger.LogWarning(message);
    }

    #endregion

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
        await EnsureConfigMapAsync(
            // appId,
            appHostName,
            version,
            ConfigMapHelper.GetAppSettingConfigMapName,
            configFiles,
            ConfigMapHelper.CreateAppSettingConfigMapDefinition);
        LogResourceOperation("ConfigMap", "Injection", $"appResourceId={appId}",
            $"keys=[{string.Join(",", configFiles.Keys)}]", appId, version);

        await EnsureConfigMapAsync(
            // appId,
            appHostName,
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
        var deploymentName = DeploymentHelper.GetAppDeploymentName(appHostName, version);
        var deploymentLabelName = DeploymentHelper.GetAppDeploymentLabelName(appHostName, version);
        var containerName = ContainerHelper.GetAppContainerName(appHostName, version);
        await EnsureDeploymentAsync(appHostName, version, imageName, deploymentName, deploymentLabelName,
            containerName, Command,
            _kubernetesOptions.AppPodReplicas,
            KubernetesConstants.WebhookContainerTargetPort,
            KubernetesConstants.QueryPodMaxSurge,
            KubernetesConstants.QueryPodMaxUnavailable,
            GetHealthPath());

        // Ensure Service is created
        var serviceName = ServiceHelper.GetAppServiceName(appHostName, version);
        await EnsureServiceAsync(
            appHostName, version, serviceName,
            DeploymentHelper.GetAppDeploymentLabelName(appId, version),
            KubernetesConstants.WebhookContainerTargetPort);

        // Ensure Ingress is created
        var rulePath = $"/{appId}".ToLower();
        await EnsureIngressAsync(appHostName, version, hostName, rulePath, serviceName,
            KubernetesConstants.WebhookContainerTargetPort);

        return hostName.TrimEnd('/') + rulePath;
    }

    private async Task EnsureConfigMapAsync(
        string appResourceId,
        string version,
        Func<string, string, string> getConfigMapNameFunc,
        Dictionary<string, string> configContent,
        Func<string, Dictionary<string, string>, V1ConfigMap> createConfigMapDefinitionFunc)
    {
        // Ensure namespace exists before creating ConfigMap

        var configMapName = getConfigMapNameFunc(appResourceId, version);
        var configMaps = await _kubernetesClientAdapter.ListConfigMapAsync(KubernetesConstants.AppNameSpace);
        var configMap = createConfigMapDefinitionFunc(configMapName, configContent);
        if (!configMaps.Items.Any(configMap => configMap.Metadata.Name == configMapName))
        {
            await _kubernetesClientAdapter.CreateConfigMapAsync(configMap, KubernetesConstants.AppNameSpace);
            LogResourceOperation("Create", "ConfigMap", configMapName, "created", appResourceId, version);
        }
        else
        {
            await _kubernetesClientAdapter.ReplaceNamespacedConfigMapAsync(configMap, configMapName,
                KubernetesConstants.AppNameSpace);
            LogResourceOperation("Update", "ConfigMap", configMapName, "updated", appResourceId, version);
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
                LogResourceOperation("Update", "Deployment", deploymentName, $"image updated to {newImage}", appId,
                    version);
            }
            else
            {
                LogResourceOperationError("Update", "Container", containerName, null,
                    $"Container not found in deployment {deploymentName}", appId, version);
            }
        }
        else
        {
            LogResourceOperationError("Update", "Deployment", deploymentName, null,
                "Deployment does not exist", appId, version);
        }
    }

    private static string ProcessConfigTemplate(string templateFilePath, Dictionary<string, string> replacements)
    {
        var rawContent = File.ReadAllText(templateFilePath);
        var unescapedContent = Regex.Unescape(rawContent);

        return replacements.Aggregate(unescapedContent,
            (current, replacement) => current.Replace(replacement.Key, replacement.Value));
    }

    private static string GetWebhookConfigContent(string appId, string version, string templateFilePath)
    {
        var replacements = new Dictionary<string, string>
        {
            { KubernetesConstants.PlaceHolderAppId, appId.ToLower() },
            { KubernetesConstants.PlaceHolderVersion, version.ToLower() },
            { KubernetesConstants.PlaceHolderNameSpace, KubernetesConstants.AppNameSpace.ToLower() }
        };

        return ProcessConfigTemplate(templateFilePath, replacements);
    }

    private static string GetHostSiloConfigContent(string appId, string version, string templateFilePath,
        Guid projectId)
    {
        var replacements = new Dictionary<string, string>
        {
            { KubernetesConstants.HostPlaceHolderAppId, appId.ToLower() },
            { KubernetesConstants.HostPlaceHolderVersion, version.ToLower() },
            { KubernetesConstants.HostPlaceHolderNameSpace, KubernetesConstants.AppNameSpace.ToLower() },
            { KubernetesConstants.HostPlaceHolderProjectId, projectId.ToString() }
        };

        return ProcessConfigTemplate(templateFilePath, replacements);
    }

    private static string GetHostClientConfigContent(string appId, string version, string templateFilePath,
        [CanBeNull] string corsUrls, Guid? projectId = null)
    {
        var replacements = new Dictionary<string, string>
        {
            { KubernetesConstants.HostPlaceHolderAppId, appId.ToLower() },
            { KubernetesConstants.HostPlaceHolderVersion, version.ToLower() },
            { KubernetesConstants.HostPlaceHolderNameSpace, KubernetesConstants.AppNameSpace.ToLower() }
        };

        if (projectId.HasValue)
        {
            replacements[KubernetesConstants.HostPlaceHolderProjectId] = projectId.Value.ToString();
        }

        if (corsUrls != null)
        {
            replacements[KubernetesConstants.HostClientCors] = corsUrls;
        }

        return ProcessConfigTemplate(templateFilePath, replacements);
    }

    private async Task EnsureDeploymentAsync(
        string appDeploymentId, string version, string imageName, string deploymentName,
        string deploymentLabelName, string containerName, List<string> command, int replicas,
        int containerPort, string maxSurge, string maxUnavailable, string healthPath, bool isSilo = false)
    {
        var configMapName = ConfigMapHelper.GetAppSettingConfigMapName(appDeploymentId, version);
        var sideCarConfigName = ConfigMapHelper.GetAppFileBeatConfigMapName(appDeploymentId, version);

        var deployment = DeploymentHelper.CreateAppDeploymentWithFileBeatSideCarDefinition(
            appDeploymentId, version, imageName, deploymentName, deploymentLabelName, command, replicas, containerName,
            containerPort, configMapName, sideCarConfigName,
            _kubernetesOptions.RequestCpuCore, _kubernetesOptions.RequestMemory,
            maxSurge, maxUnavailable, isSilo, healthPath);
        var deployments = await _kubernetesClientAdapter.ListDeploymentAsync(KubernetesConstants.AppNameSpace);

        if (!deployments.Items.Any(item => item.Metadata.Name == deploymentName))
        {
            await _kubernetesClientAdapter.CreateDeploymentAsync(deployment, KubernetesConstants.AppNameSpace);
            LogResourceOperation("Create", "Deployment", deploymentName, "created", appDeploymentId, version);
        }
        else
        {
            await _kubernetesClientAdapter.ReplaceNamespacedDeploymentAsync(deployment, deploymentName,
                KubernetesConstants.AppNameSpace);
            LogResourceOperation("Update", "Deployment", deploymentName, "updated", appDeploymentId, version);
        }
    }

    private async Task EnsureServiceAsync(
        string appServiceId, string version, string serviceName,
        string deploymentLabelName, int targetPort)
    {
        var services = await _kubernetesClientAdapter.ListServiceAsync(KubernetesConstants.AppNameSpace);
        if (!services.Items.Any(item => item.Metadata.Name == serviceName))
        {
            var serviceLabelName = ServiceHelper.GetAppServiceLabelName(appServiceId, version);
            var servicePortName = ServiceHelper.GetAppServicePortName(version);

            var service = ServiceHelper.CreateAppClusterIPServiceDefinition(
                appServiceId, serviceName, serviceLabelName, deploymentLabelName,
                servicePortName, targetPort, targetPort);

            await _kubernetesClientAdapter.CreateServiceAsync(service, KubernetesConstants.AppNameSpace);
            LogResourceOperation("Create", "Service", serviceName, "created", appServiceId, version);
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
            LogResourceOperation("Create", "Ingress", ingressName, "created", appId, version);
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
            LogResourceOperation("Delete", "Ingress", ingressName, "deleted");
        }
    }

    private async Task EnsureServiceDeletedAsync(string serviceName)
    {
        var services = await _kubernetesClientAdapter.ListServiceAsync(KubernetesConstants.AppNameSpace);
        var serviceExists = services.Items.Any(item => item.Metadata.Name == serviceName);
        if (serviceExists)
        {
            await _kubernetesClientAdapter.DeleteServiceAsync(serviceName, KubernetesConstants.AppNameSpace);
            LogResourceOperation("Delete", "Service", serviceName, "deleted");
        }
    }

    private async Task EnsureDeploymentDeletedAsync(string resourceName)
    {
        var deployments = await _kubernetesClientAdapter.ListDeploymentAsync(KubernetesConstants.AppNameSpace);
        var deploymentExists = deployments.Items.Any(d => d.Metadata.Name == resourceName);

        if (deploymentExists)
        {
            await _kubernetesClientAdapter.DeleteDeploymentAsync(resourceName, KubernetesConstants.AppNameSpace);
            LogResourceOperation("Delete", "Deployment", resourceName, "deleted");
        }
        else
        {
            LogResourceOperationWarning("Delete", "Deployment", resourceName, "does not exist");
        }
    }

    private async Task EnsureConfigMapDeletedAsync(string resourceName)
    {
        var configMaps = await _kubernetesClientAdapter.ListConfigMapAsync(KubernetesConstants.AppNameSpace);
        var configMapExists = configMaps.Items.Any(cm => cm.Metadata.Name == resourceName);

        if (configMapExists)
        {
            await _kubernetesClientAdapter.DeleteConfigMapAsync(resourceName, KubernetesConstants.AppNameSpace);
            LogResourceOperation("Delete", "ConfigMap", resourceName, "deleted");
        }
        else
        {
            LogResourceOperationWarning("Delete", "ConfigMap", resourceName, "does not exist");
        }
    }

    public async Task RestartWebHookAsync(string appId, string version)
    {
        var deploymentName = DeploymentHelper.GetAppDeploymentName(appId, version);
        await RestartDeploymentAsync(deploymentName);
    }


    public async Task<string> CreateHostAsync(string appId, string version, string corsUrls, Guid projectId)
    {
        _logger.LogInformation(
            $"Creating Host service for appResourceId: {appId}, version: {version}, project: {projectId}");

        await CreateHostSiloAsync(appId, version, _HostDeployOptions.HostSiloImageName, projectId);
        await CreatePodAsync(appId, version, _HostDeployOptions.HostClientImageName,
            GetHostClientConfigContent(appId, version, KubernetesConstants.HostClientSettingTemplateFilePath, corsUrls,
                projectId),
            KubernetesConstants.HostClientCommand, _kubernetesOptions.DeveloperHostName);
        return "";
    }

    private string GetHostName(string appId, string appType) => $"{appId}-{appType}";

    private async Task CreateHostSiloAsync(string appId, string version, string imageName, Guid projectId)
    {
        var hostName = GetHostName(appId, KubernetesConstants.HostSilo);
        var configFiles = GetHostSiloConfigFiles(appId, version, projectId);
        await EnsureConfigMapAsync(
            hostName,
            version,
            ConfigMapHelper.GetAppSettingConfigMapName,
            configFiles,
            ConfigMapHelper.CreateAppSettingConfigMapDefinition);

        await EnsureConfigMapAsync(
            hostName,
            version,
            ConfigMapHelper.GetAppFileBeatConfigMapName,
            new Dictionary<string, string>
            {
                {
                    KubernetesConstants.FileBeatConfigFileName,
                    GetHostSiloConfigContent(appId, version, KubernetesConstants.HostFileBeatConfigTemplateFilePath,
                        projectId)
                }
            },
            ConfigMapHelper.CreateFileBeatConfigMapDefinition);

        // Ensure Deployment is created
        var deploymentName = DeploymentHelper.GetAppDeploymentName(hostName, version);
        var deploymentLabelName = DeploymentHelper.GetAppDeploymentLabelName(hostName, version);
        var containerName = ContainerHelper.GetAppContainerName(hostName, version);
        await EnsureDeploymentAsync(
            hostName, version, imageName,
            deploymentName, deploymentLabelName, containerName,
            KubernetesConstants.HostSiloCommand,
            _kubernetesOptions.AppPodReplicas,
            KubernetesConstants.SiloContainerTargetPort,
            KubernetesConstants.QueryPodMaxSurge,
            KubernetesConstants.QueryPodMaxUnavailable,
            "", true);

        // Ensure Service is created
        var serviceName = ServiceHelper.GetAppServiceName(hostName, version);
        await EnsureServiceAsync(
            hostName, version, serviceName,
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
        var deployment = await _kubernetesClientAdapter.ReadNamespacedDeploymentAsync(deploymentName,
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

    public async Task UpdateHostAsync(string appId, string version, string corsUrls, Guid projectId)
    {
        _logger.LogInformation(
            $"Updating service configuration for: {appId} with CORS URLs: {corsUrls}, project: {projectId}");

        try
        {
            await UpdateHostSiloConfigMapAsync(appId, version, projectId);
            await UpdateHostClientConfigMapAsync(appId, version, corsUrls, projectId);

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

    private async Task UpdateHostSiloConfigMapAsync(string appId, string version, Guid projectId)
    {
        _logger.LogInformation(
            $"[KubernetesHostManager] Starting to update Host Silo ConfigMap for appResourceId: {appId}, version: {version}, projectId: {projectId}");

        var hostSiloId = GetHostName(appId, KubernetesConstants.HostSilo);
        var configFiles = GetHostSiloConfigFiles(hostSiloId, version, projectId);

        // Log ConfigMap content
        _logger.LogInformation($"[KubernetesHostManager] Host Silo ConfigMap content for {hostSiloId}:");
        foreach (var configFile in configFiles)
        {
            _logger.LogInformation($"[KubernetesHostManager] File: {configFile.Key}, Content: {configFile.Value}");
        }

        await EnsureConfigMapAsync(
            hostSiloId,
            version,
            ConfigMapHelper.GetAppSettingConfigMapName,
            configFiles,
            ConfigMapHelper.CreateAppSettingConfigMapDefinition);

        _logger.LogInformation(
            $"[KubernetesHostManager] Successfully updated Host Silo ConfigMap for appResourceId: {appId}, version: {version}, projectId: {projectId}");
    }

    private async Task UpdateHostClientConfigMapAsync(string appId, string version, string corsUrls, Guid projectId)
    {
        _logger.LogInformation(
            $"[KubernetesHostManager] Starting to update Host Client ConfigMap for appResourceId: {appId}, version: {version}, corsUrls: {corsUrls}, projectId: {projectId}");

        var hostClientId = GetHostName(appId, KubernetesConstants.HostClient);
        var hostClientConfigContent = GetHostClientConfigContent(hostClientId, version,
            KubernetesConstants.HostClientSettingTemplateFilePath, corsUrls, projectId);

        var configFiles = new Dictionary<string, string>
        {
            { KubernetesConstants.AppSettingFileName, hostClientConfigContent }
        };

        // Log ConfigMap content
        _logger.LogInformation($"[KubernetesHostManager] Host Client ConfigMap content for {hostClientId}:");
        foreach (var configFile in configFiles)
        {
            _logger.LogInformation($"[KubernetesHostManager] File: {configFile.Key}, Content: {configFile.Value}");
        }

        await EnsureConfigMapAsync(
            hostClientId,
            version,
            ConfigMapHelper.GetAppSettingConfigMapName,
            configFiles,
            ConfigMapHelper.CreateAppSettingConfigMapDefinition);

        _logger.LogInformation(
            $"[KubernetesHostManager] Successfully updated Host Client ConfigMap for appResourceId: {appId}, version: {version}, corsUrls: {corsUrls}, projectId: {projectId}");
    }

    private Dictionary<string, string> GetHostSiloConfigFiles(string appId, string version, Guid projectId)
    {
        var hostSiloConfigContent =
            GetHostSiloConfigContent(appId, version, KubernetesConstants.HostSiloSettingTemplateFilePath,
                projectId);

        return new Dictionary<string, string>
        {
            { KubernetesConstants.AppSettingFileName, hostSiloConfigContent },
            {
                KubernetesConstants.AppSettingSharedFileName,
                GetHostSiloConfigContent(appId, version, KubernetesConstants.AppSettingSharedFileName, projectId)
            },
            {
                KubernetesConstants.AppSettingHttpApiHostSharedFileName,
                GetHostSiloConfigContent(appId, version, KubernetesConstants.AppSettingHttpApiHostSharedFileName,
                    projectId)
            },
            {
                KubernetesConstants.AppSettingSiloSharedFileName,
                GetHostSiloConfigContent(appId, version, KubernetesConstants.AppSettingSiloSharedFileName,
                    projectId)
            }
        };
    }
}