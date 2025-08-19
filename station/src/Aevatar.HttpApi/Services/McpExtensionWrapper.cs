using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Basic.BasicGAgents;
using Aevatar.GAgents.MCP.Core.Extensions;
using Aevatar.GAgents.MCP.Options;

namespace Aevatar.Services;

/// <summary>
/// Implementation of MCP extension wrapper that calls the actual extension methods
/// </summary>
public class McpExtensionWrapper : IMcpExtensionWrapper
{
    private readonly IGAgentFactory _gAgentFactory;

    public McpExtensionWrapper(IGAgentFactory gAgentFactory)
    {
        _gAgentFactory = gAgentFactory;
    }

    /// <summary>
    /// Get MCP whitelist configuration using extension method
    /// </summary>
    public async Task<Dictionary<string, MCPServerConfig>> GetMCPWhiteListAsync(
        IConfigManagerGAgent configManagerGAgent)
    {
        return await configManagerGAgent.GetMCPWhiteListAsync();
    }

    /// <summary>
    /// Configure MCP whitelist using extension method
    /// </summary>
    public async Task<bool> ConfigMCPWhitelistAsync(IConfigManagerGAgent configManagerGAgent, string configJson)
    {
        return await configManagerGAgent.ConfigMCPWhitelistAsync(configJson);
    }

    public async Task<IConfigManagerGAgent> GetMcpServerConfigManagerAsync()
    {
        return await _gAgentFactory.GetMCPServerConfigGAgent();
    }
}