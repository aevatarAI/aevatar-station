using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MCP.Core.Extensions;
using Aevatar.GAgents.MCP.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aevatar.GAgents.MCP.Services;

/// <summary>
/// MCP whitelist initialization service responsible for automatically initializing the whitelist from configuration files at application startup
/// </summary>
public class MCPWhitelistInitializationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MCPWhitelistInitializationService> _logger;
    private readonly MCPServerOptions _options;

    public MCPWhitelistInitializationService(
        IServiceProvider serviceProvider,
        ILogger<MCPWhitelistInitializationService> logger,
        IOptions<MCPServerOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting MCP whitelist initialization...");

            // Check if whitelist reset is needed
            if (_options.ResetWhitelistEverytime)
            {
                _logger.LogInformation("ResetWhitelistEverytime is enabled, will update whitelist from configuration");
                await InitializeWhitelistAsync();
            }
            else
            {
                _logger.LogDebug("ResetWhitelistEverytime is disabled, skipping whitelist initialization");
            }

            _logger.LogInformation("MCP whitelist initialization completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize MCP whitelist");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Initialize whitelist configuration
    /// </summary>
    private async Task InitializeWhitelistAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var gAgentFactory = scope.ServiceProvider.GetRequiredService<IGAgentFactory>();
            var mcpServerRegistry = scope.ServiceProvider.GetRequiredService<IMCPServerRegistry>();

            // Get configuration manager GAgent
            var configManagerGAgent = await gAgentFactory.GetMCPServerConfigGAgent();

            // Get all registered server configurations
            var allServerConfigs = mcpServerRegistry.GetAllServerConfigs();

            if (allServerConfigs.Any())
            {
                // Update whitelist configuration
                var configResult = await configManagerGAgent.ConfigMCPWhitelistAsync(allServerConfigs);

                if (configResult)
                {
                    _logger.LogInformation("Successfully initialized MCP whitelist with {Count} servers",
                        allServerConfigs.Count);

                    // Log server list
                    foreach (var serverName in allServerConfigs.Keys)
                    {
                        _logger.LogDebug("Whitelisted MCP server: {ServerName}", serverName);
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to initialize MCP whitelist configuration");
                }
            }
            else
            {
                _logger.LogWarning("No MCP server configurations found to initialize whitelist");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during whitelist initialization");
            throw;
        }
    }
}