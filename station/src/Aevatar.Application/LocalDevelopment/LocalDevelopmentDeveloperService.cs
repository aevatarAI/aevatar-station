using System;
using System.Threading.Tasks;
using Aevatar.Enum;
using Aevatar.Service;
using Microsoft.Extensions.Logging;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;

namespace Aevatar.LocalDevelopment;

public class LocalDevelopmentDeveloperService : ApplicationService, IDeveloperService, ITransientDependency
{
    private readonly ILogger<LocalDevelopmentDeveloperService> _logger;

    public LocalDevelopmentDeveloperService(ILogger<LocalDevelopmentDeveloperService> logger)
    {
        _logger = logger;
    }

    public Task CreateServiceAsync(string HostId, string version, string corsUrls)
    {
        _logger.LogInformation("[LOCAL DEV] CreateServiceAsync called - HostId: {HostId}, Version: {Version}, CorsUrls: {CorsUrls}", 
            HostId, version, corsUrls);
        return Task.CompletedTask;
    }

    public Task UpdateDockerImageAsync(string appId, string version, string newImage)
    {
        _logger.LogInformation("[LOCAL DEV] UpdateDockerImageAsync called - AppId: {AppId}, Version: {Version}, NewImage: {NewImage}", 
            appId, version, newImage);
        return Task.CompletedTask;
    }

    public Task RestartServiceAsync(DeveloperServiceOperationDto operationInput)
    {
        _logger.LogInformation("[LOCAL DEV] RestartServiceAsync called - ProjectId: {ProjectId}, DomainName: {DomainName}", 
            operationInput.ProjectId, operationInput.DomainName);
        return Task.CompletedTask;
    }

    public Task CreateServiceAsync(string clientId, Guid projectId)
    {
        _logger.LogInformation("[LOCAL DEV] CreateServiceAsync called - ClientId: {ClientId}, ProjectId: {ProjectId}", 
            clientId, projectId);
        return Task.CompletedTask;
    }

    public Task DeleteServiceAsync(string clientId)
    {
        _logger.LogInformation("[LOCAL DEV] DeleteServiceAsync called - ClientId: {ClientId}", clientId);
        return Task.CompletedTask;
    }

    public Task UpdateBusinessConfigurationAsync(string hostId, string version, HostTypeEnum hostType)
    {
        _logger.LogInformation("[LOCAL DEV] UpdateBusinessConfigurationAsync called - HostId: {HostId}, Version: {Version}, HostType: {HostType}", 
            hostId, version, hostType);
        return Task.CompletedTask;
    }

    public Task CopyHostAsync(string sourceClientId, string newClientId, string version)
    {
        _logger.LogInformation("[LOCAL DEV] CopyHostAsync called - SourceClientId: {SourceClientId}, NewClientId: {NewClientId}, Version: {Version}", 
            sourceClientId, newClientId, version);
        return Task.CompletedTask;
    }

    public Task CopyDeploymentWithPatternAsync(string clientId, string sourceVersion, string targetVersion, string siloNamePattern)
    {
        _logger.LogInformation("[LOCAL DEV] CopyDeploymentWithPatternAsync called - ClientId: {ClientId}, SourceVersion: {SourceVersion}, TargetVersion: {TargetVersion}, Pattern: {Pattern}", 
            clientId, sourceVersion, targetVersion, siloNamePattern);
        return Task.CompletedTask;
    }
}
