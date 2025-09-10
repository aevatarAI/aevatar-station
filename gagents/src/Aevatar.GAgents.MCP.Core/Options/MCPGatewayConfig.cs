using System.ComponentModel.DataAnnotations;

namespace Aevatar.GAgents.MCP.Core.Options;

/// <summary>
/// Configuration for MCP Gateway integration
/// </summary>
[GenerateSerializer]
public class MCPGatewayConfig
{
    /// <summary>
    /// Base URL of the MCP Gateway service
    /// </summary>
    [Id(0)]
    [Required(ErrorMessage = "Gateway base URL is required")]
    [Url(ErrorMessage = "Gateway base URL must be a valid URL")]
    public string GatewayBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Authentication token for MCP Gateway API access
    /// </summary>
    [Id(1)]
    [Required(ErrorMessage = "Auth token is required")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "Auth token must be between 10 and 1000 characters")]
    public string AuthToken { get; set; } = string.Empty;

    /// <summary>
    /// Request timeout for gateway operations
    /// </summary>
    [Id(2)]
    [Range(typeof(TimeSpan), "00:00:01", "00:10:00",
        ErrorMessage = "Request timeout must be between 1 second and 10 minutes")]
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Enable session affinity for stateful routing
    /// </summary>
    [Id(3)]
    public bool EnableSessionAffinity { get; set; } = true;

    /// <summary>
    /// Default headers to include in all gateway requests
    /// </summary>
    [Id(4)]
    public Dictionary<string, string> DefaultHeaders { get; set; } = new();

    /// <summary>
    /// Maximum number of retry attempts for failed requests
    /// </summary>
    [Id(5)]
    [Range(0, 10, ErrorMessage = "Max retry attempts must be between 0 and 10")]
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts
    /// </summary>
    [Id(6)]
    [Range(typeof(TimeSpan), "00:00:00.100", "00:01:00",
        ErrorMessage = "Retry delay must be between 100ms and 1 minute")]
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Enable detailed logging for gateway operations
    /// </summary>
    [Id(7)]
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Health check endpoint path
    /// </summary>
    [Id(8)]
    public string HealthCheckPath { get; set; } = "/health";

    /// <summary>
    /// Validate the configuration
    /// </summary>
    public bool IsValid(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(GatewayBaseUrl))
        {
            errors.Add("Gateway base URL is required");
        }
        else if (!Uri.TryCreate(GatewayBaseUrl, UriKind.Absolute, out var uri) ||
                 (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            errors.Add("Gateway base URL must be a valid HTTP/HTTPS URL");
        }

        if (string.IsNullOrWhiteSpace(AuthToken))
        {
            errors.Add("Auth token is required");
        }
        else if (AuthToken.Length < 10 || AuthToken.Length > 1000)
        {
            errors.Add("Auth token must be between 10 and 1000 characters");
        }

        if (RequestTimeout < TimeSpan.FromSeconds(1) || RequestTimeout > TimeSpan.FromMinutes(10))
        {
            errors.Add("Request timeout must be between 1 second and 10 minutes");
        }

        if (MaxRetryAttempts < 0 || MaxRetryAttempts > 10)
        {
            errors.Add("Max retry attempts must be between 0 and 10");
        }

        if (RetryDelay < TimeSpan.FromMilliseconds(100) || RetryDelay > TimeSpan.FromMinutes(1))
        {
            errors.Add("Retry delay must be between 100ms and 1 minute");
        }

        return errors.Count == 0;
    }

    /// <summary>
    /// Get the full URL for a specific adapter endpoint
    /// </summary>
    public string GetAdapterUrl(string adapterName, string endpoint = "mcp")
    {
        var baseUri = new Uri(GatewayBaseUrl.TrimEnd('/'));
        return new Uri(baseUri, $"adapters/{adapterName}/{endpoint}").ToString();
    }

    /// <summary>
    /// Get headers with authentication
    /// </summary>
    public Dictionary<string, string> GetAuthenticatedHeaders(string? sessionId = null)
    {
        var headers = new Dictionary<string, string>(DefaultHeaders)
        {
            ["Authorization"] = AuthToken.StartsWith("Bearer ") ? AuthToken : $"Bearer {AuthToken}",
            ["User-Agent"] = "Aevatar-MCP-Gateway-Client/1.0",
            ["Accept"] = "application/json"
        };

        if (!string.IsNullOrEmpty(sessionId))
        {
            headers["X-Session-ID"] = sessionId;
        }

        return headers;
    }
}