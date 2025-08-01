using System;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Kubernetes;
using Aevatar.Kubernetes.Adapter;
using Aevatar.Kubernetes.ResourceDefinition;
using Aevatar.Projects;
using Aevatar.WebHook.Deploy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace Aevatar.Service;

public interface IDeveloperService
{
    Task CreateServiceAsync(string HostId, string version, string corsUrls);
    Task DestroyServiceAsync(string inputHostId, string inputVersion);

    Task UpdateDockerImageAsync(string appId, string version, string newImage);

    Task RestartServiceAsync(DeveloperServiceOperationDto operationInput);
    Task CreateServiceAsync(string clientId, Guid projectId);
    Task DeleteServiceAsync(string clientId);
}

public class DeveloperService : ApplicationService, IDeveloperService
{
    private const string DefaultVersion = "1";
    private readonly IConfiguration _configuration;
    private readonly ILogger<DeveloperService> _logger;
    private readonly IHostDeployManager _hostDeployManager;
    private readonly IKubernetesClientAdapter _kubernetesClientAdapter;
    private readonly IProjectCorsOriginService _projectCorsOriginService;

    public DeveloperService(IHostDeployManager hostDeployManager, IKubernetesClientAdapter kubernetesClientAdapter,
        ILogger<DeveloperService> logger, IProjectCorsOriginService projectCorsOriginService,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _hostDeployManager = hostDeployManager;
        _kubernetesClientAdapter = kubernetesClientAdapter;
        _projectCorsOriginService = projectCorsOriginService;
    }

    public async Task CreateServiceAsync(string hostId, string version, string corsUrls)
    {
        using var scope = _logger.BeginScope("HostId: {HostId}, Version: {Version}", hostId, version);
        _logger.LogInformation("Creating service with CORS URLs: {CorsUrls}", corsUrls);
        await _hostDeployManager.CreateApplicationAsync(hostId, version, corsUrls, Guid.Empty);
        _logger.LogInformation("Service created successfully");
    }

    public async Task DestroyServiceAsync(string inputHostId, string inputVersion)
    {
        using var scope = _logger.BeginScope("HostId: {HostId}, Version: {Version}", inputHostId, inputVersion);
        _logger.LogInformation("Destroying service");
        await _hostDeployManager.DestroyApplicationAsync(inputHostId, inputVersion);
        _logger.LogInformation("Service destroyed successfully");
    }

    public async Task UpdateDockerImageAsync(string appId, string version, string newImage)
    {
        using var scope = _logger.BeginScope("AppId: {AppId}, Version: {Version}", appId, version);
        _logger.LogInformation("Updating Docker image to: {NewImage}", newImage);
        await _hostDeployManager.UpdateDeploymentImageAsync(appId, version, newImage);
        _logger.LogInformation("Docker image updated successfully");
    }

    public async Task RestartServiceAsync(DeveloperServiceOperationDto input)
    {
        using var scope = _logger.BeginScope("ClientDomain: {DomainName}, ProjectId: {ProjectId}", input.DomainName, input.ProjectId);
        _logger.LogInformation("Starting business service restart with operation input: {@OperationInput}", input);

        var hostServiceExists = await DetermineIfHostServiceExistsAsync(input.DomainName, DefaultVersion);
        if (!hostServiceExists)
        {
            _logger.LogWarning("No Host service found for client");
            throw new UserFriendlyException($"No Host service found to restart for client: {input.DomainName}");
        }

        var corsUrlsString = await GetCombinedCorsUrlsAsync(input.ProjectId);
        _logger.LogInformation("Processing combined CORS URLs: {CorsUrlsString}", corsUrlsString);
        
        await _hostDeployManager.UpgradeApplicationAsync(input.DomainName, DefaultVersion, corsUrlsString,
            input.ProjectId);

        _logger.LogInformation("Business service restart completed successfully");
    }

    public async Task CreateServiceAsync(string clientId, Guid projectId)
    {
        using var scope = _logger.BeginScope("ClientId: {ClientId}, ProjectId: {ProjectId}", clientId, projectId);
        
        if (string.IsNullOrWhiteSpace(clientId)) 
        {
            _logger.LogError("DomainName cannot be null or empty");
            throw new UserFriendlyException("DomainName cannot be null or empty");
        }

        _logger.LogInformation("Starting developer service creation");

        var canCreate = await CanCreateHostServiceAsync(clientId, DefaultVersion);
        if (!canCreate)
        {
            _logger.LogWarning("Host service partially or fully exists");
            throw new UserFriendlyException(
                $"Host service partially or fully exists for client: {clientId}. Please delete existing services first.");
        }

        var corsUrlsString = await GetCombinedCorsUrlsAsync(projectId);
        _logger.LogInformation("Processing combined CORS URLs: {CorsUrlsString}", corsUrlsString);
        
        await _hostDeployManager.CreateApplicationAsync(clientId, DefaultVersion, corsUrlsString, projectId);

        _logger.LogInformation("Developer service created successfully");
    }

    public async Task DeleteServiceAsync(string clientId)
    {
        using var scope = _logger.BeginScope("ClientId: {ClientId}", clientId);
        
        if (string.IsNullOrWhiteSpace(clientId)) 
        {
            _logger.LogError("DomainName cannot be null or empty");
            throw new UserFriendlyException("DomainName cannot be null or empty");
        }

        _logger.LogInformation("Starting developer service deletion");

        var hostServiceExists = await DetermineIfHostServiceExistsAsync(clientId, DefaultVersion);
        if (!hostServiceExists)
        {
            _logger.LogWarning("No Host service found to delete");
            throw new UserFriendlyException($"No Host service found to delete for client: {clientId}");
        }

        await _hostDeployManager.DestroyApplicationAsync(clientId, DefaultVersion);

        _logger.LogInformation("Developer service deleted successfully");
    }

    private async Task<(bool siloExists, bool clientExists)> GetHostServiceStatusAsync(string clientId, string version)
    {
        try
        {
            _logger.LogDebug("Checking Host service status for clientId: {ClientId}, version: {Version}", clientId, version);
            var deployments = await _kubernetesClientAdapter.ListDeploymentAsync(KubernetesConstants.AppNameSpace);

            var hostSiloExists = deployments.Items.Any(d =>
                d.Metadata.Name == DeploymentHelper.GetAppDeploymentName($"{clientId}-silo", version));
            var hostClientExists = deployments.Items.Any(d =>
                d.Metadata.Name == DeploymentHelper.GetAppDeploymentName($"{clientId}-client", version));

            _logger.LogDebug("Service status check result: {@ServiceStatus}", new { SiloExists = hostSiloExists, ClientExists = hostClientExists });
            return (hostSiloExists, hostClientExists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Host service status");
            return (false, false);
        }
    }

    private async Task<bool> DetermineIfHostServiceExistsAsync(string clientId, string version)
    {
        var (siloExists, clientExists) = await GetHostServiceStatusAsync(clientId, version);
        var exists = siloExists || clientExists;
        _logger.LogDebug("Host service existence determination: {Exists}", exists);
        return exists;
    }

    private async Task<bool> CanCreateHostServiceAsync(string clientId, string version)
    {
        var (siloExists, clientExists) = await GetHostServiceStatusAsync(clientId, version);

        var canCreate = !siloExists && !clientExists;

        if (siloExists || clientExists)
        {
            _logger.LogWarning(
                "Cannot create service - service components already exist: {@ServiceComponentStatus}", 
                new { SiloExists = siloExists, ClientExists = clientExists });
        }

        return canCreate;
    }

    private async Task<string> GetCombinedCorsUrlsAsync(Guid projectId)
    {
        using var scope = _logger.BeginScope("ProjectId: {ProjectId}", projectId);
        _logger.LogInformation("Getting combined CORS URLs");

        // Get default platform CORS URLs from configuration
        var defaultCorsUrls = GetConfigValue("App:DefaultCorsOrigins", "App:CorsOrigins");
        _logger.LogInformation(string.IsNullOrWhiteSpace(defaultCorsUrls)
            ? "No platform CORS origins configured"
            : "Using platform CORS URLs: {DefaultCorsUrls}", defaultCorsUrls);

        // Get business CORS URLs from project
        var businessCorsUrls = await _projectCorsOriginService.GetListAsync(projectId);
        var businessCorsUrlsString = string.Join(",", businessCorsUrls.Items.Select(x => x.Domain));
        _logger.LogInformation("Business CORS URLs: {@BusinessCorsUrls}", businessCorsUrls.Items);

        // Combine URLs
        var hasDefault = !string.IsNullOrWhiteSpace(defaultCorsUrls);
        var hasBusiness = !string.IsNullOrWhiteSpace(businessCorsUrlsString);

        string combinedCorsUrls;
        if (hasDefault && hasBusiness)
        {
            combinedCorsUrls = $"{defaultCorsUrls},{businessCorsUrlsString}";
        }
        else if (hasDefault)
        {
            combinedCorsUrls = defaultCorsUrls;
        }
        else if (hasBusiness)
        {
            combinedCorsUrls = businessCorsUrlsString;
        }
        else
        {
            combinedCorsUrls = string.Empty;
        }

        _logger.LogInformation("Combined CORS URLs result: {CombinedCorsUrls}", combinedCorsUrls);
        return combinedCorsUrls;
    }

    private string GetConfigValue(params string[] keys) =>
        keys.Select(key => _configuration[key]?.Trim())
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
}