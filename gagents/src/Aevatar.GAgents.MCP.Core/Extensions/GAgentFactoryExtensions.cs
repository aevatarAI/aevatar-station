using System.Text.Json;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Basic.BasicGAgents;
using Aevatar.GAgents.Basic.BasicGEvent;
using Aevatar.GAgents.MCP.Options;

namespace Aevatar.GAgents.MCP.Core.Extensions;

// ReSharper disable InconsistentNaming
public static class GAgentFactoryExtensions
{
    public static async Task<IConfigManagerGAgent> GetMCPServerConfigGAgent(this IGAgentFactory gAgentFactory)
    {
        return await gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(MCPServerConfigManagerGAgentExtensions
            .MCPWhitelistConfigGuid);
    }

    public static async Task<IMCPGAgent?> GetMCPGAgentAsync(this IGAgentFactory gAgentFactory, string mcpServerName)
    {
        var configManagerGAgent = await gAgentFactory.GetMCPServerConfigGAgent();
        var configResponseEvent = await configManagerGAgent.RequestConfigAsync(new ConfigRequestEvent
        {
            ConfigType = MCPServerConfigManagerGAgentExtensions.MCPWhitelistConfigTypeFullName,
            ConfigKey = mcpServerName
        });
        if (!configResponseEvent.Success)
        {
            return null;
        }

        try
        {
            var config = JsonSerializer.Deserialize<MCPServerConfig>(configResponseEvent.ConfigJson);
            if (config != null && config.IsValid())
            {
                return await gAgentFactory.GetGAgentAsync<IMCPGAgent>(new MCPGAgentConfig
                {
                    MemberName = mcpServerName,
                    ServerConfig = config
                });
            }
        }
        catch (Exception ex)
        {
            throw new ArgumentException(
                $"Failed to deserialize config {configResponseEvent.ConfigJson} to type MCPServerConfig.", ex);
        }

        return null;
    }

    public static async Task<IMCPGAgent?> GetMCPGAgentAsync(this IGAgentFactory gAgentFactory,
        string url, Dictionary<string, string> env, string? serverName = null, string? description = null)
    {
        serverName ??= Guid.NewGuid().ToString();
        return await gAgentFactory.GetGAgentAsync<IMCPGAgent>(new MCPGAgentConfig
        {
            MemberName = serverName,
            ServerConfig = new MCPServerConfig
            {
                ServerName = serverName,
                Url = url,
                Env = env,
                Description = description ?? string.Empty,
                Type = MCPServerType.StreamableHttp
            }
        });
    }
}

/// <summary>
/// Result of MCP server whitelist validation
/// </summary>
public class MCPWhitelistValidationResult
{
    /// <summary>
    /// Whether the validation passed
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Error message if validation failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}