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
    
    [Id(7)] public Dictionary<string, string> Headers { get; set; } = new();
    
    /// <summary>
    /// OAuth configuration for StreamableHttp transport
    /// </summary>
    [Id(8)] public MCPOAuthConfig? OAuth { get; set; }
    
    public bool IsValid()
    {
        return !Command.IsNullOrWhiteSpace() || !Url.IsNullOrWhiteSpace();
    }
}

/// <summary>
/// OAuth configuration for MCP server authentication
/// </summary>
[GenerateSerializer]
public class MCPOAuthConfig
{
    /// <summary>
    /// OAuth provider type (e.g., "oauth2", "bearer", "basic")
    /// </summary>
    [Id(0)]
    public string ProviderType { get; set; } = string.Empty;
    
    /// <summary>
    /// Client ID for OAuth2
    /// </summary>
    [Id(1)]
    public string? ClientId { get; set; }
    
    /// <summary>
    /// Client Secret for OAuth2
    /// </summary>
    [Id(2)]
    public string? ClientSecret { get; set; }
    
    /// <summary>
    /// Access Token for Bearer authentication
    /// </summary>
    [Id(3)]
    public string? AccessToken { get; set; }
    
    /// <summary>
    /// Refresh Token for OAuth2
    /// </summary>
    [Id(4)]
    public string? RefreshToken { get; set; }
    
    /// <summary>
    /// Authorization URL for OAuth2
    /// </summary>
    [Id(5)]
    public string? AuthorizationUrl { get; set; }
    
    /// <summary>
    /// Token URL for OAuth2
    /// </summary>
    [Id(6)]
    public string? TokenUrl { get; set; }
    
    /// <summary>
    /// OAuth scopes
    /// </summary>
    [Id(7)]
    public List<string> Scopes { get; set; } = new();
    
    /// <summary>
    /// Additional OAuth parameters
    /// </summary>
    [Id(8)]
    public Dictionary<string, string> AdditionalParameters { get; set; } = new();
}

/// <summary>
/// https://modelcontextprotocol.io/specification/2025-06-18/basic/transports
/// </summary>
[GenerateSerializer]
public enum MCPServerType
{
    Unknown,
    Stdio,
    StreamableHttp
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
}