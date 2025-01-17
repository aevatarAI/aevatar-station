using Aevatar.Kubernetes.Adapter;
using Aevatar.Kubernetes.ResourceDefinition;
using Aevatar.Options;
using Aevatar.WebHook.Deploy;
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

    public KubernetesWebhookManager(ILogger<KubernetesWebhookManager> logger,
        IKubernetesClientAdapter kubernetesClientAdapter,
        IOptionsSnapshot<KubernetesOptions> kubernetesOptions)
    {
        _logger = logger;
        _kubernetesClientAdapter = kubernetesClientAdapter;
        _kubernetesOptions = kubernetesOptions.Value;
    }

    public async Task<string> CreateNewWebHookAsync(string appId, string version, string imageName)
    {
        return await CreatePodAsync(appId, version, imageName);
    }

    public async Task DestroyWebHookAsync(string appId, string version)
    {
        await DestroyPodsAsync(appId, version);
    }


    private async Task<string> CreatePodAsync(string appId, string version, string imageName)
    {
        //Create query app appsetting config map
        var configMapName =
            ConfigMapHelper.GetAppSettingConfigMapName(appId, version, KubernetesConstants.AppClientTypeQuery, null);
        var appSettingsContent = File.ReadAllText(KubernetesConstants.AppSettingTemplateFilePath);
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderAppId, appId);
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderVersion, version);
      
        var configMaps = await _kubernetesClientAdapter.ListConfigMapAsync(KubernetesConstants.AppNameSpace);
        var configMapExists = configMaps.Items.Any(configMap => configMap.Metadata.Name == configMapName);
        if (!configMapExists)
        {
            var configMap =
                ConfigMapHelper.CreateAppSettingConfigMapDefinition(configMapName, appSettingsContent);
            // Submit the ConfigMap to the cluster
            await _kubernetesClientAdapter.CreateConfigMapAsync(configMap, KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]ConfigMap {configMapName} created", configMapName);
        }

        //Create query app filebeat config map
        var sideCarConfigName =
            ConfigMapHelper.GetAppFileBeatConfigMapName(appId, version, KubernetesConstants.AppClientTypeQuery, null);
        var sideCarConfigContent = File.ReadAllText(KubernetesConstants.AppFileBeatConfigTemplateFilePath);
        sideCarConfigContent = sideCarConfigContent.Replace(KubernetesConstants.PlaceHolderAppId, appId.ToLower());
        sideCarConfigContent = sideCarConfigContent.Replace(KubernetesConstants.PlaceHolderVersion, version.ToLower());
        sideCarConfigContent = sideCarConfigContent.Replace(KubernetesConstants.PlaceHolderNameSpace,
            KubernetesConstants.AppNameSpace.ToLower());
        var sideCarConfigMapExists = configMaps.Items.Any(configMap => configMap.Metadata.Name == sideCarConfigName);
        if (!sideCarConfigMapExists)
        {
            var sideCarConfigMap =
                ConfigMapHelper.CreateFileBeatConfigMapDefinition(sideCarConfigName, sideCarConfigContent);
            // Submit the ConfigMap to the cluster
            await _kubernetesClientAdapter.CreateConfigMapAsync(sideCarConfigMap, KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]ConfigMap {sideCarConfigName} created", sideCarConfigName);
        }

        //Create query app deployment
        var deploymentName =
            DeploymentHelper.GetAppDeploymentName(appId, version, KubernetesConstants.AppClientTypeQuery, null);
        var deploymentLabelName =
            DeploymentHelper.GetAppDeploymentLabelName(version, KubernetesConstants.AppClientTypeQuery, null);
        var containerName =
            ContainerHelper.GetAppContainerName(appId, version, KubernetesConstants.AppClientTypeQuery, null);
        var targetPort = KubernetesConstants.AppContainerTargetPort;
        var maxSurge = KubernetesConstants.QueryPodMaxSurge;
        var maxUnavailable = KubernetesConstants.QueryPodMaxUnavailable;
        var deployments = await _kubernetesClientAdapter.ListDeploymentAsync(KubernetesConstants.AppNameSpace);
        var deploymentExists = deployments.Items.Any(item => item.Metadata.Name == deploymentName);
        if (!deploymentExists)
        { 
            var healthPath = GethealthPath(appId, version);
            var deployment = DeploymentHelper.CreateAppDeploymentWithFileBeatSideCarDefinition(appId, version,
                KubernetesConstants.AppClientTypeQuery, string.Empty, imageName, deploymentName, deploymentLabelName,
                1, containerName, targetPort, configMapName, sideCarConfigName, "", "", 
                maxSurge, maxUnavailable, healthPath);
            // Create Deployment
            await _kubernetesClientAdapter.CreateDeploymentAsync(deployment, KubernetesConstants.AppNameSpace);
           
        }

        //Create query app service
        var serviceName = ServiceHelper.GetAppServiceName(appId, version);
        var serviceLabelName = ServiceHelper.GetAppServiceLabelName(appId, version);
        var servicePortName = ServiceHelper.GetAppServicePortName(version);
        var services = await _kubernetesClientAdapter.ListServiceAsync(KubernetesConstants.AppNameSpace);
        var serviceExists = services.Items.Any(item => item.Metadata.Name == serviceName);
        if (!serviceExists)
        {
            var service =
                ServiceHelper.CreateAppClusterIPServiceDefinition(appId, serviceName, serviceLabelName,
                    deploymentLabelName, servicePortName, targetPort);
            // Create Service
            await _kubernetesClientAdapter.CreateServiceAsync(service, KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]Service {serviceName} created", serviceName);
        }

        //Create query app ingress
        var ingressName = IngressHelper.GetAppIngressName(appId, version);
        var hostName = _kubernetesOptions.HostName;
        // string rulePath = $"/{appId}";
        var rulePath = $"/{appId}/{version}";
        var ingresses = await _kubernetesClientAdapter.ListIngressAsync(KubernetesConstants.AppNameSpace);
        var ingressExists = ingresses.Items.Any(item => item.Metadata.Name == ingressName);
        if (!ingressExists)
        {
            var ingress =
                IngressHelper.CreateAppIngressDefinition(ingressName, hostName,
                    rulePath, serviceName, targetPort);
            // Submit the Ingress to the cluster
            await _kubernetesClientAdapter.CreateIngressAsync(ingress, KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]Ingress {ingressName} created", ingressName);
        }

        return hostName + rulePath;
    }
  
    
    private string GethealthPath(string appId, string version)
    {
        return $"/{appId}/{version}/index.html";
    }


    private async Task DestroyPodsAsync(string appId, string version)
    {
        var deployments = await _kubernetesClientAdapter.ListDeploymentAsync(KubernetesConstants.AppNameSpace);
        var configMaps = await _kubernetesClientAdapter.ListConfigMapAsync(KubernetesConstants.AppNameSpace);

        //Delete query app deployment
        var queryTypeAppDeploymentName =
            DeploymentHelper.GetAppDeploymentName(appId, version, KubernetesConstants.AppClientTypeQuery, null);
        var queryTypeAppDeploymentExists =
            deployments.Items.Any(item => item.Metadata.Name == queryTypeAppDeploymentName);
        if (queryTypeAppDeploymentExists)
        {
            // Delete the existing Deployment
            await _kubernetesClientAdapter.DeleteDeploymentAsync(
                queryTypeAppDeploymentName,
                KubernetesConstants.AppNameSpace
            );
            _logger.LogInformation("[KubernetesAppManager]Deployment {queryTypeAppDeploymentName} deleted.",
                queryTypeAppDeploymentName);
        }

        //Delete query app appsetting config map
        var queryTypeAppConfigMapName =
            ConfigMapHelper.GetAppSettingConfigMapName(appId, version, KubernetesConstants.AppClientTypeQuery, null);
        var queryTypeAppConfigMapExists =
            configMaps.Items.Any(configMap => configMap.Metadata.Name == queryTypeAppConfigMapName);
        if (queryTypeAppConfigMapExists)
        {
            await _kubernetesClientAdapter.DeleteConfigMapAsync(queryTypeAppConfigMapName,
                KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]ConfigMap {queryTypeAppConfigMapName} deleted.",
                queryTypeAppConfigMapName);
        }

        //Delete query app filebeat config map
        var queryTypeAppSideCarConfigName =
            ConfigMapHelper.GetAppFileBeatConfigMapName(appId, version, KubernetesConstants.AppClientTypeQuery, null);
        var queryTypeAppSideCarConfigExists =
            configMaps.Items.Any(configMap => configMap.Metadata.Name == queryTypeAppSideCarConfigName);
        if (queryTypeAppSideCarConfigExists)
        {
            await _kubernetesClientAdapter.DeleteConfigMapAsync(queryTypeAppSideCarConfigName,
                KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]ConfigMap {queryTypeAppSideCarConfigName} deleted.",
                queryTypeAppSideCarConfigName);
        }

        //Delete query app service
        var serviceName = ServiceHelper.GetAppServiceName(appId, version);
        var services = await _kubernetesClientAdapter.ListServiceAsync(KubernetesConstants.AppNameSpace);
        var sericeExists = services.Items.Any(item => item.Metadata.Name == serviceName);
        if (sericeExists)
        {
            await _kubernetesClientAdapter.DeleteServiceAsync(serviceName, KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]Service {serviceName} deleted.", serviceName);
        }

        //Delete query app ingress
        var ingressName = IngressHelper.GetAppIngressName(appId, version);
        var ingresses = await _kubernetesClientAdapter.ListIngressAsync(KubernetesConstants.AppNameSpace);
        var ingressExists = ingresses.Items.Any(item => item.Metadata.Name == ingressName);
        if (ingressExists)
        {
            await _kubernetesClientAdapter.DeleteIngressAsync(ingressName, KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]Ingress {ingressName} deleted.", ingressName);
        }
    }

    public async Task RestartWebHookAsync(string appId, string version)
    {
        //Restart Query Client Type App Pod
        await RestartAppQueryPodsAsync(appId, version);
    }

    public async Task RestartAppQueryPodsAsync(string appId, string version)
    {
        var queryClientDeploymentName =
            DeploymentHelper.GetAppDeploymentName(appId, version, KubernetesConstants.AppClientTypeQuery, null);
        var deployments = await _kubernetesClientAdapter.ListDeploymentAsync(KubernetesConstants.AppNameSpace);
        var queryClientDeploymentExists =
            deployments.Items.Any(item => item.Metadata.Name == queryClientDeploymentName);
        if (queryClientDeploymentExists)
        {
            var deployment =
                await _kubernetesClientAdapter.ReadNamespacedDeploymentAsync(queryClientDeploymentName,
                    KubernetesConstants.AppNameSpace);
            // Add or update annotations to trigger rolling updates
            var annotations = deployment.Spec.Template.Metadata.Annotations ?? new Dictionary<string, string>();
            annotations["kubectl.kubernetes.io/restartedAt"] = DateTime.UtcNow.ToString("s");
            deployment.Spec.Template.Metadata.Annotations = annotations;

            // Update Deployment
            await _kubernetesClientAdapter.ReplaceNamespacedDeploymentAsync(deployment, queryClientDeploymentName,
                KubernetesConstants.AppNameSpace);
        }
        else
        {
            _logger.LogError($"Deployment {queryClientDeploymentName} is not exists!");
        }
    }
   
}