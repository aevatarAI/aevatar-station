using System;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Kubernetes;
using Aevatar.Kubernetes.Adapter;
using Aevatar.Kubernetes.ResourceDefinition;
using Aevatar.Projects;
using Aevatar.WebHook.Deploy;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace Aevatar.Service;

public interface IDeveloperService
{
    Task CreateHostAsync(string HostId, string version, string corsUrls);
    Task DestroyHostAsync(string inputHostId, string inputVersion);

    Task UpdateDockerImageAsync(string appId, string version, string newImage);

    Task RestartAsync(string clientId, Guid projectId);
    Task CreateAsync(string clientId, Guid projectId);
    Task DeleteAsync(string clientId);
}

public class DeveloperService : ApplicationService, IDeveloperService
{
    private const string DefaultVersion = "1";
    private readonly ILogger<DeveloperService> _logger;
    private readonly IHostDeployManager _hostDeployManager;
    private readonly IKubernetesClientAdapter _kubernetesClientAdapter;
    private readonly IProjectCorsOriginService _projectCorsOriginService;

    public DeveloperService(IHostDeployManager hostDeployManager, IKubernetesClientAdapter kubernetesClientAdapter,
        ILogger<DeveloperService> logger, IProjectCorsOriginService projectCorsOriginService)
    {
        _logger = logger;
        _hostDeployManager = hostDeployManager;
        _kubernetesClientAdapter = kubernetesClientAdapter;
        _projectCorsOriginService = projectCorsOriginService;
    }

    public async Task CreateHostAsync(string HostId, string version, string corsUrls)
    {
        await _hostDeployManager.CreateHostAsync(HostId, version, corsUrls, Guid.Empty);
    }

    public async Task DestroyHostAsync(string inputHostId, string inputVersion)
    {
        await _hostDeployManager.DestroyHostAsync(inputHostId, inputVersion);
    }

    public async Task UpdateDockerImageAsync(string appId, string version, string newImage)
    {
        await _hostDeployManager.UpdateDockerImageAsync(appId, version, newImage);
    }

    public async Task RestartAsync(string clientId, Guid projectId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new UserFriendlyException("ClientId cannot be null or empty");
        }

        _logger.LogInformation($"Starting business service restart for client: {clientId} in project: {projectId}");

        var hostServiceExists = await DetermineIfHostServiceExistsAsync(clientId, DefaultVersion);
        if (!hostServiceExists)
        {
            _logger.LogWarning($"No Host service found for client: {clientId}");
            throw new UserFriendlyException($"No Host service found to restart for client: {clientId}");
        }

        var corsUrls = await _projectCorsOriginService.GetListAsync(projectId);
        var corsUrlsString = string.Join(",", corsUrls.Items.Select(x => x.Domain));
        await _hostDeployManager.UpdateHostAsync(clientId, DefaultVersion, corsUrlsString, projectId);

        _logger.LogInformation($"Business service restart completed successfully for client: {clientId}");
    }

    public async Task CreateAsync(string clientId, Guid projectId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new UserFriendlyException("ClientId cannot be null or empty");
        }

        _logger.LogInformation($"Starting developer service creation for client: {clientId} in project: {projectId}");

        var canCreate = await CanCreateHostServiceAsync(clientId, DefaultVersion);
        if (!canCreate)
        {
            _logger.LogWarning($"Host service partially or fully exists for client: {clientId}");
            throw new UserFriendlyException(
                $"Host service partially or fully exists for client: {clientId}. Please delete existing services first.");
        }

        var corsUrls = await _projectCorsOriginService.GetListAsync(projectId);
        var corsUrlsString = string.Join(",", corsUrls.Items.Select(x => x.Domain));
        await _hostDeployManager.CreateHostAsync(clientId, DefaultVersion, corsUrlsString, projectId);

        _logger.LogInformation($"Developer service created successfully for client: {clientId}");
    }

    public async Task DeleteAsync(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new UserFriendlyException("ClientId cannot be null or empty");
        }

        var hostServiceExists = await DetermineIfHostServiceExistsAsync(clientId, DefaultVersion);
        if (!hostServiceExists)
        {
            _logger.LogWarning($"No Host service found for client: {clientId}");
            throw new UserFriendlyException($"No Host service found to delete for client: {clientId}");
        }

        await _hostDeployManager.DestroyHostAsync(clientId, DefaultVersion);

        _logger.LogInformation($"Developer service deleted successfully for client: {clientId}");
    }

    private async Task<(bool siloExists, bool clientExists)> GetHostServiceStatusAsync(string clientId, string version)
    {
        try
        {
            var deployments = await _kubernetesClientAdapter.ListDeploymentAsync(KubernetesConstants.AppNameSpace);

            var hostSiloExists = deployments.Items.Any(d =>
                d.Metadata.Name == DeploymentHelper.GetAppDeploymentName($"{clientId}-silo", version));
            var hostClientExists = deployments.Items.Any(d =>
                d.Metadata.Name == DeploymentHelper.GetAppDeploymentName($"{clientId}-client", version));

            return (hostSiloExists, hostClientExists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking Host service status for client: {clientId}");
            return (false, false);
        }
    }

    private async Task<bool> DetermineIfHostServiceExistsAsync(string clientId, string version)
    {
        var (siloExists, clientExists) = await GetHostServiceStatusAsync(clientId, version);
        return siloExists || clientExists;
    }

    private async Task<bool> CanCreateHostServiceAsync(string clientId, string version)
    {
        var (siloExists, clientExists) = await GetHostServiceStatusAsync(clientId, version);

        var canCreate = !siloExists && !clientExists;

        if (siloExists || clientExists)
        {
            _logger.LogWarning(
                $"Cannot create service for client {clientId}: Silo exists={siloExists}, Client exists={clientExists}");
        }

        return canCreate;
    }

    // private async Task<List<string>> GetCorsUrlsForClientAsync(string clientId)
    // {
    //     _logger.LogInformation($"Getting CORS URLs for client: {clientId}");
    //
    //     var mockCorsUrls = new List<string>
    //     {
    //         "https://api.example.com",
    //         "https://app.test.com",
    //         "https://webhook.demo.org",
    //         "http://localhost:3000",
    //         "https://staging.myapp.com"
    //     };
    //
    //     _logger.LogInformation($"Retrieved {mockCorsUrls.Count} CORS URLs for client: {clientId}");
    //
    //     await Task.CompletedTask;
    //     return mockCorsUrls;
    // }
}