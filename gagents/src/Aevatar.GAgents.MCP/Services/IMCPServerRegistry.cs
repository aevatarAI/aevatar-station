using Aevatar.GAgents.MCP.Options;

namespace Aevatar.GAgents.MCP.Services;

/// <summary>
/// MCP server registry interface providing unified server configuration management
/// </summary>
public interface IMCPServerRegistry
{
    /// <summary>
    /// Gets the MCP server configuration for the specified name
    /// </summary>
    /// <param name="serverName">Server name</param>
    /// <returns>Server configuration, or null if not found</returns>
    MCPServerConfig? GetServerConfig(string serverName);

    /// <summary>
    /// Gets the configuration corresponding to the DefaultMCPServer enum
    /// </summary>
    /// <param name="defaultServer">Default server enum</param>
    /// <returns>Server configuration, or null if not found</returns>
    MCPServerConfig? GetServerConfig(DefaultMCPServer defaultServer);

    /// <summary>
    /// Registers a server configuration
    /// </summary>
    /// <param name="serverName">Server name</param>
    /// <param name="config">Server configuration</param>
    void RegisterServer(string serverName, MCPServerConfig config);

    /// <summary>
    /// Registers multiple server configurations in batch
    /// </summary>
    /// <param name="servers">Dictionary of server configurations</param>
    void RegisterServers(Dictionary<string, MCPServerConfig> servers);

    /// <summary>
    /// Gets all registered server configurations
    /// </summary>
    /// <returns>Dictionary of all server configurations</returns>
    Dictionary<string, MCPServerConfig> GetAllServerConfigs();

    /// <summary>
    /// Checks if a server is registered
    /// </summary>
    /// <param name="serverName">Server name</param>
    /// <returns>Whether the server is registered</returns>
    bool IsServerRegistered(string serverName);

    /// <summary>
    /// Gets all registered server names
    /// </summary>
    /// <returns>List of server names</returns>
    IEnumerable<string> GetRegisteredServerNames();

    /// <summary>
    /// Removes a server configuration
    /// </summary>
    /// <param name="serverName">Server name</param>
    /// <returns>Whether the removal was successful</returns>
    bool UnregisterServer(string serverName);

    /// <summary>
    /// Clears all server configurations
    /// </summary>
    void ClearAll();
}