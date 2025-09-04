using System.ComponentModel.DataAnnotations;
namespace Aevatar.GAgents.MCP.Options;

// ReSharper disable InconsistentNaming
[GenerateSerializer]
public class MCPServerConfig
{
    [Id(0)] 
    [Required(ErrorMessage = "Server name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Server name must be between 1 and 100 characters")]
    public string ServerName { get; set; } = string.Empty;

    [Id(1)] 
    [Required(ErrorMessage = "Command is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Command must be between 1 and 200 characters")]
    public string Command { get; set; } = string.Empty;
    [Id(2)] public List<string> Args { get; set; } = [];
    [Id(3)] public Dictionary<string, string> Env { get; set; } = new();
    [Id(4)] 
    [StringLength(500, ErrorMessage = "Description must not exceed 500 characters")]
    public string Description { get; set; } = string.Empty;

    [Id(5)] 
    [StringLength(1000, ErrorMessage = "URL must not exceed 1000 characters")]
    [RegularExpression(@"^https?://[^\s/$.?#].[^\s]*$", ErrorMessage = "URL must start with http:// or https://")]
    public string? Url { get; set; }
    [Id(6)] public MCPServerType Type { get; set; }

    /// <summary>
    /// Whether to use MCP Gateway for this server connection
    /// </summary>
    [Id(7)] public bool UseGateway { get; set; } = true;

    /// <summary>
    /// Gateway adapter name for routing (required when UseGateway is true)
    /// </summary>
    [Id(8)] 
    [StringLength(100, ErrorMessage = "Gateway adapter name must not exceed 100 characters")]
    public string? GatewayAdapterName { get; set; }

    /// <summary>
    /// Session ID for session-aware routing
    /// </summary>
    [Id(9)]
    [StringLength(200, ErrorMessage = "Session ID must not exceed 200 characters")]
    public string? SessionId { get; set; }

    /// <summary>
    /// Priority for load balancing (higher values get more traffic)
    /// </summary>
    [Id(10)]
    [Range(1, 100, ErrorMessage = "Priority must be between 1 and 100")]
    public int Priority { get; set; } = 50;

    /// <summary>
    /// Tags for adapter categorization and filtering
    /// </summary>
    [Id(11)]
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Additional metadata for the adapter
    /// </summary>
    [Id(12)]
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// https://modelcontextprotocol.io/specification/2025-06-18/basic/transports
/// </summary>
[GenerateSerializer]
public enum MCPServerType
{
    Stdio,
    StreamableHttp,
    Gateway
}

/// <summary>
/// Tool definition for predefined tools in configuration
/// </summary>
[GenerateSerializer]
public class MCPToolDefinition
{
    [Id(0)]
    public string Name { get; set; } = string.Empty;
    
    [Id(1)]
    public string Description { get; set; } = string.Empty;
    
    [Id(2)]
    public Dictionary<string, MCPParameterDefinition>? Parameters { get; set; }
}

/// <summary>
/// Parameter definition for predefined tools
/// </summary>
[GenerateSerializer]
public class MCPParameterDefinition
{
    [Id(0)]
    public string Type { get; set; } = "string";
    
    [Id(1)]
    public string? Description { get; set; }
    
    [Id(2)]
    public bool Required { get; set; }
}

public static class MCPServerConfigExtensions
{
    public static bool IsValid(this MCPServerConfig config)
    {
        return !string.IsNullOrWhiteSpace(config.ServerName);
    }

    /// <summary>
    /// Validate configuration for gateway usage
    /// </summary>
    public static bool IsValidForGateway(this MCPServerConfig config, out List<string> errors)
    {
        errors = new List<string>();

        if (!config.IsValid())
        {
            errors.Add("Basic server configuration is invalid");
        }

        if (config.UseGateway)
        {
            if (string.IsNullOrWhiteSpace(config.GatewayAdapterName))
            {
                errors.Add("Gateway adapter name is required when UseGateway is true");
            }
            else if (config.GatewayAdapterName.Length > 100)
            {
                errors.Add("Gateway adapter name must not exceed 100 characters");
            }

            if (!string.IsNullOrEmpty(config.SessionId) && config.SessionId.Length > 200)
            {
                errors.Add("Session ID must not exceed 200 characters");
            }

            if (config.Priority < 1 || config.Priority > 100)
            {
                errors.Add("Priority must be between 1 and 100");
            }
        }
        else
        {
            // For direct connections, ensure we have either Command or URL
            if (string.IsNullOrWhiteSpace(config.Command) && string.IsNullOrWhiteSpace(config.Url))
            {
                errors.Add("Either Command or URL is required for direct connections");
            }
        }

        return errors.Count == 0;
    }

    /// <summary>
    /// Get the effective connection type
    /// </summary>
    public static MCPServerType GetEffectiveType(this MCPServerConfig config)
    {
        if (config.UseGateway)
        {
            return MCPServerType.Gateway;
        }

        return config.Type;
    }

    /// <summary>
    /// Generate a session ID if not provided
    /// </summary>
    public static string GetOrGenerateSessionId(this MCPServerConfig config)
    {
        if (!string.IsNullOrEmpty(config.SessionId))
        {
            return config.SessionId;
        }

        return $"{config.ServerName}-{Guid.NewGuid():N}";
    }
}