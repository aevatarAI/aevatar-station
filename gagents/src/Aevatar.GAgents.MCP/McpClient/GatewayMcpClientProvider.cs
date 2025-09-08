using Aevatar.GAgents.MCP.Core.Options;
using Aevatar.GAgents.MCP.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using System.Reflection;

namespace Aevatar.GAgents.MCP.McpClient;

/// <summary>
/// MCP Client Provider for Microsoft MCP Gateway connections
/// </summary>
public class GatewayMcpClientProvider : IMcpClientProvider
{
    private readonly MCPGatewayConfig _gatewayConfig;
    private readonly ILogger<GatewayMcpClientProvider> _logger;

    public GatewayMcpClientProvider(
        IOptions<MCPGatewayConfig> gatewayConfig,
        ILogger<GatewayMcpClientProvider> logger)
    {
        _gatewayConfig = gatewayConfig.Value;
        _logger = logger;
    }

    public McpClientType ClientType => McpClientType.Gateway;

    /// <summary>
    /// Gateway-specific client cache with session-aware keys
    /// </summary>
    private readonly Dictionary<string, IMcpClient> _gatewayClients = new();

    protected IClientTransport CreateClientTransport(MCPServerConfig config)
    {
        ValidateGatewayConfig(config);

        var gatewayUrl = _gatewayConfig.GetAdapterUrl(
            config.GatewayAdapterName!, 
            "mcp");

        _logger.LogInformation(
            "Creating Gateway MCP transport for server {ServerName} to {GatewayUrl}",
            config.ServerName, gatewayUrl);

        // Create SSE transport for gateway connection
        // Note: Authentication will be handled at the HTTP client level
        return new SseClientTransport(new SseClientTransportOptions
        {
            Name = config.ServerName,
            Endpoint = new Uri(gatewayUrl)
        });
    }

    /// <summary>
    /// Validate gateway configuration before creating transport
    /// </summary>
    private void ValidateGatewayConfig(MCPServerConfig config)
    {
        if (!_gatewayConfig.IsValid(out var errors))
        {
            var errorMessage = $"Invalid gateway configuration: {string.Join(", ", errors)}";
            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        if (!config.IsValidForGateway(out var configErrors))
        {
            var errorMessage = $"Invalid server configuration for gateway: {string.Join(", ", configErrors)}";
            _logger.LogError(errorMessage);
            throw new ArgumentException(errorMessage, nameof(config));
        }

        if (string.IsNullOrEmpty(config.GatewayAdapterName))
        {
            throw new ArgumentException(
                "Gateway adapter name is required for gateway connections", 
                nameof(config));
        }
    }

    /// <summary>
    /// Build headers for gateway authentication and session management
    /// </summary>
    private Dictionary<string, string> BuildGatewayHeaders(MCPServerConfig config)
    {
        var sessionId = config.GetOrGenerateSessionId();
        var headers = _gatewayConfig.GetAuthenticatedHeaders(sessionId);

        // Add client identification headers
        headers["X-Client-Name"] = "Aevatar-MCPGAgent";
        headers["X-Client-Version"] = GetClientVersion();
        headers["X-Server-Name"] = config.ServerName;
        headers["X-Adapter-Name"] = config.GatewayAdapterName!;

        // Add priority for load balancing
        if (config.Priority > 0)
        {
            headers["X-Priority"] = config.Priority.ToString();
        }

        // Add tags for routing
        if (config.Tags.Any())
        {
            headers["X-Tags"] = string.Join(",", config.Tags);
        }

        // Add metadata
        foreach (var metadata in config.Metadata)
        {
            headers[$"X-Meta-{metadata.Key}"] = metadata.Value;
        }

        if (_gatewayConfig.EnableDetailedLogging)
        {
            _logger.LogDebug(
                "Gateway headers for {ServerName}: {@Headers}", 
                config.ServerName, headers);
        }

        return headers;
    }

    /// <summary>
    /// Get client version for identification
    /// </summary>
    private string GetClientVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "1.0.0";
    }

    /// <summary>
    /// Gateway-specific connection handling
    /// </summary>
    public async Task<IMcpClient> GetOrCreateClientAsync(MCPServerConfig config)
    {
        var clientKey = GetClientKey(config);
        
        // Check if we already have a client for this specific gateway configuration
        if (_gatewayClients.TryGetValue(clientKey, out var existingClient))
        {
            return existingClient;
        }
        
        try
        {
            _logger.LogInformation(
                "Attempting gateway connection for server {ServerName} via adapter {AdapterName}",
                config.ServerName, config.GatewayAdapterName);

            var clientTransport = CreateClientTransport(config);
            var client = await McpClientFactory.CreateAsync(clientTransport);
            _gatewayClients[clientKey] = client;
            
            _logger.LogInformation(
                "Successfully connected to gateway for server {ServerName}",
                config.ServerName);
                
            return client;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to connect to gateway for server {ServerName}: {Error}",
                config.ServerName, ex.Message);

            // Implement retry logic based on gateway configuration
            if (_gatewayConfig.MaxRetryAttempts > 0)
            {
                return await RetryConnectionAsync(config, ex);
            }

            throw;
        }
    }

    /// <summary>
    /// Implement retry logic for gateway connections
    /// </summary>
    private async Task<IMcpClient> RetryConnectionAsync(MCPServerConfig config, Exception lastException)
    {
        for (int attempt = 1; attempt <= _gatewayConfig.MaxRetryAttempts; attempt++)
        {
            try
            {
                _logger.LogWarning(
                    "Retrying gateway connection for {ServerName}, attempt {Attempt}/{MaxAttempts}",
                    config.ServerName, attempt, _gatewayConfig.MaxRetryAttempts);

                await Task.Delay(_gatewayConfig.RetryDelay);
                
                // Force create new transport for retry
                var clientTransport = CreateClientTransport(config);
                var client = await McpClientFactory.CreateAsync(clientTransport);
                
                var clientKey = GetClientKey(config);
                _gatewayClients[clientKey] = client;

                _logger.LogInformation(
                    "Successfully connected to gateway for server {ServerName} on retry attempt {Attempt}",
                    config.ServerName, attempt);

                return client;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Retry attempt {Attempt} failed for server {ServerName}: {Error}",
                    attempt, config.ServerName, ex.Message);

                if (attempt == _gatewayConfig.MaxRetryAttempts)
                {
                    _logger.LogError(
                        "All retry attempts exhausted for server {ServerName}. Last error: {Error}",
                        config.ServerName, ex.Message);
                    throw new InvalidOperationException(
                        $"Failed to connect to gateway after {_gatewayConfig.MaxRetryAttempts} attempts", 
                        ex);
                }
            }
        }

        throw lastException;
    }

    /// <summary>
    /// Get unique client key for caching
    /// </summary>
    private string GetClientKey(MCPServerConfig config)
    {
        // Include adapter name and session ID in key for gateway connections
        return $"{config.ServerName}:{config.GatewayAdapterName}:{config.SessionId}";
    }

    /// <summary>
    /// Test gateway connection health
    /// </summary>
    public async Task<bool> TestConnectionAsync(MCPServerConfig config)
    {
        try
        {
            var client = await GetOrCreateClientAsync(config);
            await client.PingAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Gateway connection test failed for server {ServerName}: {Error}",
                config.ServerName, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Get connection statistics
    /// </summary>
    public Dictionary<string, object> GetConnectionStats()
    {
        return new Dictionary<string, object>
        {
            ["ActiveConnections"] = _gatewayClients.Count,
            ["GatewayUrl"] = _gatewayConfig.GatewayBaseUrl,
            ["MaxRetryAttempts"] = _gatewayConfig.MaxRetryAttempts,
            ["RequestTimeout"] = _gatewayConfig.RequestTimeout.TotalSeconds,
            ["SessionAffinity"] = _gatewayConfig.EnableSessionAffinity
        };
    }

    /// <summary>
    /// Gateway-specific disconnect handling
    /// </summary>
    public async Task DisconnectClientAsync(string serverName)
    {
        var clientsToRemove = _gatewayClients
            .Where(kvp => kvp.Key.StartsWith($"{serverName}:"))
            .ToList();

        foreach (var (key, client) in clientsToRemove)
        {
            try
            {
                await client.DisposeAsync();
                _gatewayClients.Remove(key);
                _logger.LogInformation("Disconnected gateway client for server {ServerName}", serverName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disconnecting gateway client for server {ServerName}", serverName);
            }
        }

    }

    /// <summary>
    /// Gateway-specific connection check
    /// </summary>
    public async Task<bool> IsConnectedAsync(string serverName)
    {
        var gatewayClients = _gatewayClients
            .Where(kvp => kvp.Key.StartsWith($"{serverName}:"))
            .Select(kvp => kvp.Value)
            .ToList();

        if (!gatewayClients.Any())
        {
            return false;
        }

        // Check if any gateway client for this server is connected
        foreach (var client in gatewayClients)
        {
            try
            {
                await client.PingAsync();
                return true;
            }
            catch
            {
                // Continue checking other clients
            }
        }

        return false;
    }

    /// <summary>
    /// Dispose all gateway connections
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        var disposeTasks = _gatewayClients.Values.Select(client => client.DisposeAsync().AsTask());
        await Task.WhenAll(disposeTasks);
        _gatewayClients.Clear();
    }
}
