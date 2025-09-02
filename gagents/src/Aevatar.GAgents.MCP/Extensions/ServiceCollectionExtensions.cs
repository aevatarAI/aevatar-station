using Aevatar.GAgents.MCP.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Aevatar.GAgents.MCP.Extensions;

/// <summary>
/// Service collection extension methods for registering MCP-related services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register MCP server registry and related services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddMCPServerRegistry(this IServiceCollection services)
    {
        // Register MCP server registry as singleton
        services.AddSingleton<IMCPServerRegistry, MCPServerRegistry>();
        
        // Register whitelist initialization background service
        services.AddHostedService<MCPWhitelistInitializationService>();
        
        return services;
    }

    /// <summary>
    /// Register MCP server registry only (without background services)
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddMCPServerRegistryOnly(this IServiceCollection services)
    {
        services.AddSingleton<IMCPServerRegistry, MCPServerRegistry>();
        return services;
    }
}
