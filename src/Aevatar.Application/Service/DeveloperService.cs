using System;
using System.Collections.Generic;
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
    Task<RestartConfigResponseDto> RestartAsync(Guid projectId, string clientId);
}

public class DeveloperService : ApplicationService, IDeveloperService
{
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
        await _hostDeployManager.CreateHostAsync(HostId, version, corsUrls);
    }

    public async Task DestroyHostAsync(string inputHostId, string inputVersion)
    {
        await _hostDeployManager.DestroyHostAsync(inputHostId, inputVersion);
    }

    public async Task UpdateDockerImageAsync(string appId, string version, string newImage)
    {
        await _hostDeployManager.UpdateDockerImageAsync(appId, version, newImage);
    }

    public async Task<RestartConfigResponseDto> RestartAsync(Guid projectId, string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new UserFriendlyException("ClientId cannot be null or empty");
        }

        _logger.LogInformation($"Starting business service restart for client: {clientId}");

        try
        {
            var hostServiceExists = await DetermineIfHostServiceExistsAsync(clientId, "1");
            if (!hostServiceExists)
            {
                _logger.LogWarning($"No Host service found for client: {clientId}");
                throw new UserFriendlyException($"No Host service found to restart for client: {clientId}");
            }

            var corsUrls = await _projectCorsOriginService.GetListAsync(projectId);
            var corsUrlsString = string.Join(",", corsUrls.Items.Select(x => x.Domain));
            await _hostDeployManager.UpdateHostAsync(clientId, "1", corsUrlsString);

            _logger.LogInformation($"Business service restart completed successfully for client: {clientId}");
            return new RestartConfigResponseDto
            {
                IsSuccess = true,
                Message = "Service is restarting to apply configuration changes"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to restart service for client: {clientId}");
            return new RestartConfigResponseDto
            {
                IsSuccess = false,
                Message = $"Service restart failed: {ex.Message}"
            };
        }
    }

    private async Task<bool> DetermineIfHostServiceExistsAsync(string clientId, string version)
    {
        try
        {
            var deployments = await _kubernetesClientAdapter.ListDeploymentAsync(KubernetesConstants.AppNameSpace);

            // 检查Host服务是否存在（Silo或Client任一存在即可）
            var hostSiloExists = deployments.Items.Any(d =>
                d.Metadata.Name == DeploymentHelper.GetAppDeploymentName($"{clientId}-silo", version));
            var hostClientExists = deployments.Items.Any(d =>
                d.Metadata.Name == DeploymentHelper.GetAppDeploymentName($"{clientId}-client", version));

            return hostSiloExists || hostClientExists;
        }
        catch
        {
            return false;
        }
    }
}