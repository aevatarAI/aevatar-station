using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MCP.Core;
using Aevatar.GAgents.MCP.Options;
using Aevatar.GAgents.MCP.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Aevatar.GAgents.MCP.Extensions;

/// <summary>
/// Unified MCPGAgent creation extension methods supporting multiple creation approaches
/// </summary>
public static class UnifiedGAgentFactoryExtensions
{
    /// <summary>
    /// Creates MCPGAgent through DefaultMCPServer enum (recommended for core servers)
    /// </summary>
    /// <param name="gAgentFactory">GAgent factory</param>
    /// <param name="defaultServer">Default server enum</param>
    /// <param name="env">Environment variables (optional, for servers requiring authentication)</param>
    /// <param name="customArgs">Custom arguments (optional, will override default arguments)</param>
    /// <returns>MCPGAgent instance, throws exception if configuration not found</returns>
    /// <exception cref="MCPServerConfigNotFoundException">Server configuration not found</exception>
    public static async Task<IMCPGAgent> GetMCPGAgentAsync(
        this IGAgentFactory gAgentFactory,
        DefaultMCPServer defaultServer,
        Dictionary<string, string>? env = null,
        string[]? customArgs = null)
    {
        var serviceProvider = ((dynamic)gAgentFactory).ServiceProvider as IServiceProvider
            ?? throw new InvalidOperationException("Cannot access ServiceProvider from GAgentFactory");
        
        var registry = serviceProvider.GetRequiredService<IMCPServerRegistry>();
        var serverName = defaultServer.ToMCPServerName();
        
        var config = registry.GetServerConfig(defaultServer);
        if (config == null)
        {
            throw new MCPServerConfigNotFoundException($"MCP Server config for {serverName} not found in registry");
        }

        // Clone configuration to avoid modifying original
        var workingConfig = config.Clone();

        // Set environment variables (for servers requiring authentication)
        if ((int)defaultServer > 1000)
        {
            if (env == null || !env.Any())
            {
                throw new MCPServerConfigNotFoundException(
                    $"Environment variables required for MCP server {serverName} but not provided");
            }
            workingConfig.Env = env;
        }

        // Use custom arguments (if provided)
        if (customArgs != null)
        {
            workingConfig.Args = customArgs.ToList();
        }

        return await gAgentFactory.GetGAgentAsync<IMCPGAgent>(new MCPGAgentConfig
        {
            MemberName = serverName,
            ServerConfig = workingConfig
        });
    }

    /// <summary>
    /// Creates MCPGAgent by server name (supports servers defined in configuration files)
    /// </summary>
    /// <param name="gAgentFactory">GAgent factory</param>
    /// <param name="serverName">Server name</param>
    /// <param name="env">Environment variables (optional, will merge with configuration environment variables)</param>
    /// <param name="customArgs">Custom arguments (optional, will override configuration arguments)</param>
    /// <returns>MCPGAgent instance, returns null if configuration not found</returns>
    public static async Task<IMCPGAgent?> GetMCPGAgentAsync(
        this IGAgentFactory gAgentFactory,
        string serverName,
        Dictionary<string, string>? env = null,
        string[]? customArgs = null)
    {
        if (string.IsNullOrWhiteSpace(serverName))
        {
            return null;
        }

        var serviceProvider = ((dynamic)gAgentFactory).ServiceProvider as IServiceProvider
            ?? throw new InvalidOperationException("Cannot access ServiceProvider from GAgentFactory");
        
        var registry = serviceProvider.GetRequiredService<IMCPServerRegistry>();
        
        var config = registry.GetServerConfig(serverName);
        if (config == null)
        {
            return null;
        }

        // Clone configuration to avoid modifying original
        var workingConfig = config.Clone();

        // Merge environment variables
        if (env != null && env.Any())
        {
            workingConfig.Env = workingConfig.Env ?? new Dictionary<string, string>();
            foreach (var kvp in env)
            {
                workingConfig.Env[kvp.Key] = kvp.Value;
            }
        }

        // Use custom arguments (if provided)
        if (customArgs != null)
        {
            workingConfig.Args = customArgs.ToList();
        }

        return await gAgentFactory.GetGAgentAsync<IMCPGAgent>(new MCPGAgentConfig
        {
            MemberName = serverName,
            ServerConfig = workingConfig
        });
    }

    /// <summary>
    /// Convenience method for creating filesystem MCPGAgent
    /// </summary>
    /// <param name="gAgentFactory">GAgent factory</param>
    /// <param name="paths">List of allowed access paths</param>
    /// <returns>Filesystem MCPGAgent instance</returns>
    public static async Task<IMCPGAgent> GetFilesystemMCPGAgentAsync(
        this IGAgentFactory gAgentFactory,
        params string[] paths)
    {
        var args = new List<string> { "-y", "@modelcontextprotocol/server-filesystem" };
        if (paths.Any())
        {
            args.AddRange(paths);
        }

        return await gAgentFactory.GetMCPGAgentAsync(
            DefaultMCPServer.Filesystem,
            customArgs: args.ToArray());
    }

    /// <summary>
    /// Convenience method for creating StreamableHttp (SSE) MCPGAgent
    /// </summary>
    /// <param name="gAgentFactory">GAgent factory</param>
    /// <param name="serverName">Server name</param>
    /// <param name="url">SSE server URL</param>
    /// <param name="headers">HTTP headers (optional)</param>
    /// <param name="description">Server description (optional)</param>
    /// <returns>StreamableHttp MCPGAgent instance</returns>
    public static async Task<IMCPGAgent> GetStreamableHttpMCPGAgentAsync(
        this IGAgentFactory gAgentFactory,
        string serverName,
        string url,
        Dictionary<string, string>? headers = null,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(serverName))
        {
            throw new ArgumentException("Server name cannot be null or empty", nameof(serverName));
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL cannot be null or empty", nameof(url));
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || 
            (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            throw new ArgumentException("URL must be a valid HTTP or HTTPS URL", nameof(url));
        }

        var config = new MCPServerConfig
        {
            ServerName = serverName,
            Url = url,
            Headers = headers ?? new Dictionary<string, string>(),
            Description = description ?? $"StreamableHttp MCP server at {url}",
            Type = MCPServerType.StreamableHttp
        };

        return await gAgentFactory.GetGAgentAsync<IMCPGAgent>(new MCPGAgentConfig
        {
            MemberName = serverName,
            ServerConfig = config
        });
    }

    /// <summary>
    /// Convenience method for creating StreamableHttp MCPGAgent with authentication headers
    /// </summary>
    /// <param name="gAgentFactory">GAgent factory</param>
    /// <param name="serverName">Server name</param>
    /// <param name="url">SSE server URL</param>
    /// <param name="bearerToken">Bearer authentication token</param>
    /// <param name="additionalHeaders">Additional HTTP headers (optional)</param>
    /// <param name="description">Server description (optional)</param>
    /// <returns>Authenticated StreamableHttp MCPGAgent instance</returns>
    public static async Task<IMCPGAgent> GetStreamableHttpMCPGAgentWithAuthAsync(
        this IGAgentFactory gAgentFactory,
        string serverName,
        string url,
        string bearerToken,
        Dictionary<string, string>? additionalHeaders = null,
        string? description = null)
    {
        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = $"Bearer {bearerToken}"
        };

        if (additionalHeaders != null)
        {
            foreach (var kvp in additionalHeaders)
            {
                headers[kvp.Key] = kvp.Value;
            }
        }

        return await gAgentFactory.GetStreamableHttpMCPGAgentAsync(
            serverName, url, headers, description);
    }

    /// <summary>
    /// Convenience method for creating StreamableHttp MCPGAgent with API Key authentication
    /// </summary>
    /// <param name="gAgentFactory">GAgent factory</param>
    /// <param name="serverName">Server name</param>
    /// <param name="url">SSE server URL</param>
    /// <param name="apiKey">API key</param>
    /// <param name="apiKeyHeader">API key header name (defaults to "X-API-Key")</param>
    /// <param name="additionalHeaders">Additional HTTP headers (optional)</param>
    /// <param name="description">Server description (optional)</param>
    /// <returns>API Key authenticated StreamableHttp MCPGAgent instance</returns>
    public static async Task<IMCPGAgent> GetStreamableHttpMCPGAgentWithApiKeyAsync(
        this IGAgentFactory gAgentFactory,
        string serverName,
        string url,
        string apiKey,
        string apiKeyHeader = "X-API-Key",
        Dictionary<string, string>? additionalHeaders = null,
        string? description = null)
    {
        var headers = new Dictionary<string, string>
        {
            [apiKeyHeader] = apiKey
        };

        if (additionalHeaders != null)
        {
            foreach (var kvp in additionalHeaders)
            {
                headers[kvp.Key] = kvp.Value;
            }
        }

        return await gAgentFactory.GetStreamableHttpMCPGAgentAsync(
            serverName, url, headers, description);
    }

    /// <summary>
    /// Convenience method for creating StreamableHttp MCPGAgent with OAuth authentication
    /// </summary>
    /// <param name="gAgentFactory">GAgent factory</param>
    /// <param name="serverName">Server name</param>
    /// <param name="url">SSE server URL</param>
    /// <param name="oauthConfig">OAuth configuration</param>
    /// <param name="additionalHeaders">Additional HTTP headers (optional)</param>
    /// <param name="description">Server description (optional)</param>
    /// <returns>OAuth authenticated StreamableHttp MCPGAgent instance</returns>
    public static async Task<IMCPGAgent> GetStreamableHttpMCPGAgentWithOAuthAsync(
        this IGAgentFactory gAgentFactory,
        string serverName,
        string url,
        MCPOAuthConfig oauthConfig,
        Dictionary<string, string>? additionalHeaders = null,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(serverName))
            throw new ArgumentException("Server name cannot be null or empty", nameof(serverName));
        
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be null or empty", nameof(url));
        
        if (oauthConfig == null)
            throw new ArgumentNullException(nameof(oauthConfig));

        var headers = additionalHeaders ?? new Dictionary<string, string>();

        var config = new MCPServerConfig
        {
            ServerName = serverName,
            Url = url,
            Headers = headers,
            OAuth = oauthConfig,
            Description = description ?? $"StreamableHttp MCP server at {url} with OAuth authentication",
            Type = MCPServerType.StreamableHttp
        };

        return await gAgentFactory.GetGAgentAsync<IMCPGAgent>(new MCPGAgentConfig
        {
            MemberName = serverName,
            ServerConfig = config
        });
    }

    /// <summary>
    /// Gets all registered MCP server names
    /// </summary>
    /// <param name="gAgentFactory">GAgent factory</param>
    /// <returns>List of server names</returns>
    public static IEnumerable<string> GetRegisteredMCPServerNames(this IGAgentFactory gAgentFactory)
    {
        var serviceProvider = ((dynamic)gAgentFactory).ServiceProvider as IServiceProvider
            ?? throw new InvalidOperationException("Cannot access ServiceProvider from GAgentFactory");
        
        var registry = serviceProvider.GetRequiredService<IMCPServerRegistry>();
        return registry.GetRegisteredServerNames();
    }

    /// <summary>
    /// Checks if the specified MCP server is registered
    /// </summary>
    /// <param name="gAgentFactory">GAgent factory</param>
    /// <param name="serverName">Server name</param>
    /// <returns>Whether the server is registered</returns>
    public static bool IsMCPServerRegistered(this IGAgentFactory gAgentFactory, string serverName)
    {
        var serviceProvider = ((dynamic)gAgentFactory).ServiceProvider as IServiceProvider
            ?? throw new InvalidOperationException("Cannot access ServiceProvider from GAgentFactory");
        
        var registry = serviceProvider.GetRequiredService<IMCPServerRegistry>();
        return registry.IsServerRegistered(serverName);
    }

    /// <summary>
    /// Gets the configuration information for the specified MCP server
    /// </summary>
    /// <param name="gAgentFactory">GAgent factory</param>
    /// <param name="serverName">Server name</param>
    /// <returns>Server configuration, or null if not found</returns>
    public static MCPServerConfig? GetMCPServerConfig(this IGAgentFactory gAgentFactory, string serverName)
    {
        var serviceProvider = ((dynamic)gAgentFactory).ServiceProvider as IServiceProvider
            ?? throw new InvalidOperationException("Cannot access ServiceProvider from GAgentFactory");
        
        var registry = serviceProvider.GetRequiredService<IMCPServerRegistry>();
        return registry.GetServerConfig(serverName);
    }
}
