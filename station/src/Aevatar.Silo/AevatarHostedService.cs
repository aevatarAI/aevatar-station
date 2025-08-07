using System.Text.Json;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MCP.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Volo.Abp;

namespace Aevatar.Silo;

public class AevatarHostedService : IHostedService
{
    private readonly IAbpApplicationWithExternalServiceProvider _application;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AevatarHostedService> _logger;

    public AevatarHostedService(
        IAbpApplicationWithExternalServiceProvider application,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<AevatarHostedService> logger)
    {
        _application = application;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _application.InitializeAsync(_serviceProvider);
        try
        {
            var mcpServers = _configuration["MCPServers"];
            if (!string.IsNullOrEmpty(mcpServers))
            {
                JsonDocument.Parse(mcpServers);
                var gAgentFactory = _serviceProvider.GetRequiredService<IGAgentFactory>();
                var configManagerGAgent = await gAgentFactory.GetMCPServerConfigGAgent();
                if (await configManagerGAgent.ConfigMCPWhitelistAsync(mcpServers))
                {
                    _logger.LogInformation("MCPServers configuration updated successfully.");
                }
                else
                {
                    _logger.LogError("Failed to config MCPServers.");
                }
            }
            else
            {
                _logger.LogWarning("MCPServers configuration is missing or empty.");
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid MCPServers configuration JSON.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while starting AevatarHostedService.");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _application.Shutdown();
        return Task.CompletedTask;
    }
}