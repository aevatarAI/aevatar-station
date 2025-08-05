using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MCP.Options;

namespace Aevatar.Services;

/// <summary>
/// Wrapper interface for MCP extension methods to enable unit testing
/// </summary>
public interface IMcpExtensionWrapper
{
    /// <summary>
    /// Get MCP whitelist configuration
    /// </summary>
    /// <param name="gAgentFactory">The agent factory</param>
    /// <returns>Dictionary of server configurations</returns>
    Task<Dictionary<string, MCPServerConfig>> GetMCPWhiteListAsync(IGAgentFactory gAgentFactory);
    
    /// <summary>
    /// Configure MCP whitelist
    /// </summary>
    /// <param name="gAgentFactory">The agent factory</param>
    /// <param name="configJson">Configuration JSON string</param>
    /// <returns>True if successful</returns>
    Task<bool> ConfigMCPWhitelistAsync(IGAgentFactory gAgentFactory, string configJson);
}