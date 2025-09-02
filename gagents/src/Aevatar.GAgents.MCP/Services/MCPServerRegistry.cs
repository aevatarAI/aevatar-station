using System.Collections.Concurrent;
using Aevatar.GAgents.MCP.Core.Options;
using Aevatar.GAgents.MCP.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aevatar.GAgents.MCP.Services;

/// <summary>
/// MCP server registry implementation providing thread-safe server configuration management
/// </summary>
public class MCPServerRegistry : IMCPServerRegistry
{
    private readonly ConcurrentDictionary<string, MCPServerConfig> _serverConfigs = new();
    private readonly ILogger<MCPServerRegistry> _logger;
    private readonly MCPServerOptions _options;

    public MCPServerRegistry(ILogger<MCPServerRegistry> logger, IOptions<MCPServerOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        InitializeDefaultServers();
        LoadConfigurationServers();
    }

    public MCPServerConfig? GetServerConfig(string serverName)
    {
        if (string.IsNullOrWhiteSpace(serverName))
        {
            return null;
        }

        _serverConfigs.TryGetValue(serverName, out var config);
        return config;
    }

    public MCPServerConfig? GetServerConfig(DefaultMCPServer defaultServer)
    {
        var serverName = defaultServer.ToMCPServerName();
        return GetServerConfig(serverName);
    }

    public void RegisterServer(string serverName, MCPServerConfig config)
    {
        if (string.IsNullOrWhiteSpace(serverName) || config == null)
        {
            _logger.LogWarning("Invalid server registration attempt: {ServerName}", serverName);
            return;
        }

        // Ensure the ServerName in config matches the registration name
        config.ServerName = serverName;

        _serverConfigs.AddOrUpdate(serverName, config, (key, existingConfig) =>
        {
            _logger.LogDebug("Updating existing server configuration: {ServerName}", serverName);
            return config;
        });

        _logger.LogDebug("Registered MCP server: {ServerName}", serverName);
    }

    public void RegisterServers(Dictionary<string, MCPServerConfig> servers)
    {
        if (servers == null || !servers.Any())
        {
            return;
        }

        foreach (var kvp in servers)
        {
            RegisterServer(kvp.Key, kvp.Value);
        }

        _logger.LogInformation("Registered {Count} MCP servers", servers.Count);
    }

    public Dictionary<string, MCPServerConfig> GetAllServerConfigs()
    {
        return new Dictionary<string, MCPServerConfig>(_serverConfigs);
    }

    public bool IsServerRegistered(string serverName)
    {
        return !string.IsNullOrWhiteSpace(serverName) && _serverConfigs.ContainsKey(serverName);
    }

    public IEnumerable<string> GetRegisteredServerNames()
    {
        return _serverConfigs.Keys.ToList();
    }

    public bool UnregisterServer(string serverName)
    {
        if (string.IsNullOrWhiteSpace(serverName))
        {
            return false;
        }

        var removed = _serverConfigs.TryRemove(serverName, out _);
        if (removed)
        {
            _logger.LogDebug("Unregistered MCP server: {ServerName}", serverName);
        }

        return removed;
    }

    public void ClearAll()
    {
        var count = _serverConfigs.Count;
        _serverConfigs.Clear();
        _logger.LogInformation("Cleared all {Count} MCP server configurations", count);
    }

    /// <summary>
    /// Initialize default server configurations (from DefaultMCPServers.cs)
    /// </summary>
    private void InitializeDefaultServers()
    {
        try
        {
            // Register default server configurations
            RegisterServers(DefaultMCPServers.Configs);
            _logger.LogInformation("Initialized {Count} default MCP servers from DefaultMCPServers.Configs",
                DefaultMCPServers.Configs.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize default MCP servers");
        }
    }

    /// <summary>
    /// Load server configurations from configuration file
    /// </summary>
    private void LoadConfigurationServers()
    {
        try
        {
            if (_options.MCPServers != null && _options.MCPServers.Any())
            {
                RegisterServers(_options.MCPServers);
                _logger.LogInformation("Loaded {Count} MCP servers from configuration",
                    _options.MCPServers.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load MCP servers from configuration");
        }
    }
}