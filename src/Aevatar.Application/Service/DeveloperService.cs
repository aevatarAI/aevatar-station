using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Kubernetes;
using Aevatar.Kubernetes.Adapter;
using Aevatar.Kubernetes.ResourceDefinition;
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
    Task<RestartConfigResponseDto> RestartAsync(string clientId);
}

public class DeveloperService : ApplicationService, IDeveloperService
{
    private readonly ILogger<DeveloperService> _logger;
    private readonly IHostDeployManager _hostDeployManager;
    private readonly IKubernetesClientAdapter _kubernetesClientAdapter;

    public DeveloperService(IHostDeployManager hostDeployManager, IKubernetesClientAdapter kubernetesClientAdapter,
        ILogger<DeveloperService> logger)
    {
        _logger = logger;
        _hostDeployManager = hostDeployManager;
        _kubernetesClientAdapter = kubernetesClientAdapter;
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

    public async Task<RestartConfigResponseDto> RestartAsync(string clientId)
    {
        // if (string.IsNullOrWhiteSpace(clientId))
        // {
        //     throw new UserFriendlyException("ClientId cannot be null or empty");
        // }

        const string version = "1"; // 版本由后端控制
        _logger.LogInformation($"Starting business service restart for client: {clientId}");

        try
        {
            // var hostServiceExists = await DetermineIfHostServiceExistsAsync(clientId, version);
            // if (!hostServiceExists)
            // {
            //     _logger.LogWarning($"No Host service found for client: {clientId}");
            //     throw new UserFriendlyException($"No Host service found to restart for client: {clientId}");
            // }

            var corsUrls = await GetCorsUrlsForClientAsync(clientId);
            var corsUrlsString = string.Join(",", corsUrls);
            await _hostDeployManager.UpdateHostAsync(clientId, version, corsUrlsString);

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


    private async Task UpdateServiceConfigurationsAsync(string clientId, string version, List<string> corsUrls)
    {
        _logger.LogInformation($"Updating configurations for client: {clientId} with {corsUrls.Count} CORS URLs");

        // 像CreateHostAsync那样，只传递业务配置
        var corsUrlsString = string.Join(",", corsUrls);

        try
        {
            await _hostDeployManager.UpdateHostAsync(clientId, version, corsUrlsString);
            _logger.LogInformation($"Configuration updated for client: {clientId}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to update configuration for client: {clientId}");
        }
    }

    private async Task ExecuteHostServiceRestartAsync(string clientId, string version)
    {
        _logger.LogInformation($"Executing Host service restart for client: {clientId}");

        try
        {
            await _hostDeployManager.RestartHostAsync(clientId, version);
            _logger.LogInformation($"Host service restarted for client: {clientId}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Host service restart failed for client: {clientId}");
            throw;
        }
    }

    /// <summary>
    /// 获取指定客户端的CORS URLs配置
    /// 当前使用Mock数据进行测试
    /// </summary>
    private async Task<List<string>> GetCorsUrlsForClientAsync(string clientId)
    {
        _logger.LogInformation($"Getting CORS URLs for client: {clientId}");

        // Mock数据用于测试
        var mockCorsUrls = new List<string>
        {
            "https://api.example.com",
            "https://app.test.com",
            "https://webhook.demo.org",
            "http://localhost:3000",
            "https://staging.myapp.com"
        };

        _logger.LogInformation($"Retrieved {mockCorsUrls.Count} CORS URLs for client: {clientId}");

        await Task.CompletedTask;
        return mockCorsUrls;
    }
}