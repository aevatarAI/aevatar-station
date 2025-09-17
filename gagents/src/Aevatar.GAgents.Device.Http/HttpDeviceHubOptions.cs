namespace Aevatar.GAgents.Device.Http;

/// <summary>
/// Virtual Device Hub API configuration options
/// </summary>
[GenerateSerializer]
public class HttpDeviceHubOptions
{
    /// <summary>
    /// Base URL of the Virtual Device Hub API
    /// </summary>
    [Id(0)] public string BaseUrl { get; set; } = "https://localhost:9001";
    
    /// <summary>
    /// API version
    /// </summary>
    [Id(1)] public string ApiVersion { get; set; } = "v1";
    
    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    [Id(2)] public int TimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// Number of retry attempts for failed requests
    /// </summary>
    [Id(3)] public int RetryAttempts { get; set; } = 3;
    
    /// <summary>
    /// Delay between retry attempts in seconds
    /// </summary>
    [Id(4)] public int RetryDelaySeconds { get; set; } = 2;
    
    /// <summary>
    /// API key for authentication (if required)
    /// </summary>
    [Id(5)] public string? ApiKey { get; set; }
    
    /// <summary>
    /// Whether to use HTTPS
    /// </summary>
    [Id(6)] public bool UseHttps { get; set; } = true;
    
    /// <summary>
    /// Whether to validate SSL certificates
    /// </summary>
    [Id(7)] public bool ValidateSslCertificate { get; set; } = true;
}
