using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MCP.Core.Extensions;
using Aevatar.GAgents.MCP.Options;

namespace Aevatar.Services;

/// <summary>
/// Implementation of MCP extension wrapper that calls the actual extension methods
/// </summary>
public class McpExtensionWrapper : IMcpExtensionWrapper
{
    /// <summary>
    /// Get MCP whitelist configuration using extension method
    /// </summary>
    public async Task<Dictionary<string, MCPServerConfig>> GetMCPWhiteListAsync(IGAgentFactory gAgentFactory)
    {
        return await gAgentFactory.GetMCPWhiteListAsync();
    }
    
    /// <summary>
    /// Configure MCP whitelist using extension method
    /// </summary>
    public async Task<bool> ConfigMCPWhitelistAsync(IGAgentFactory gAgentFactory, string configJson)
    {
        return await gAgentFactory.ConfigMCPWhitelistAsync(configJson);
    }
}