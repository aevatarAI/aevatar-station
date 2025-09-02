using System.Text.Json;
using Aevatar.GAgents.MCP.Options;

namespace Aevatar.GAgents.MCP.Extensions;

/// <summary>
/// MCPServerConfig extension methods
/// </summary>
public static class MCPServerConfigExtensions
{
    /// <summary>
    /// Deep clone MCPServerConfig object
    /// </summary>
    /// <param name="config">Configuration to clone</param>
    /// <returns>Cloned configuration object</returns>
    public static MCPServerConfig Clone(this MCPServerConfig config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        // Use JSON serialization/deserialization for deep cloning
        var json = JsonSerializer.Serialize(config);
        return JsonSerializer.Deserialize<MCPServerConfig>(json) 
            ?? throw new InvalidOperationException("Failed to clone MCPServerConfig");
    }

    /// <summary>
    /// Merge two MCPServerConfig objects, target values take priority
    /// </summary>
    /// <param name="source">Source configuration</param>
    /// <param name="target">Target configuration</param>
    /// <returns>New merged configuration</returns>
    public static MCPServerConfig Merge(this MCPServerConfig source, MCPServerConfig target)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        if (target == null)
        {
            return source.Clone();
        }

        var merged = source.Clone();

        // Merge basic properties (target takes priority)
        if (!string.IsNullOrWhiteSpace(target.ServerName))
            merged.ServerName = target.ServerName;
        
        if (!string.IsNullOrWhiteSpace(target.Command))
            merged.Command = target.Command;
        
        if (target.Args != null && target.Args.Any())
            merged.Args = target.Args.ToList();
        
        if (!string.IsNullOrWhiteSpace(target.Url))
            merged.Url = target.Url;
        
        if (!string.IsNullOrWhiteSpace(target.Description))
            merged.Description = target.Description;
        
        if (target.Type != MCPServerType.Unknown)
            merged.Type = target.Type;

        // Merge environment variables
        if (target.Env.Count != 0)
        {
            merged.Env = merged.Env;
            foreach (var kvp in target.Env)
            {
                merged.Env[kvp.Key] = kvp.Value;
            }
        }

        return merged;
    }

    /// <summary>
    /// Validate configuration integrity and validity
    /// </summary>
    /// <param name="config">Configuration to validate</param>
    /// <returns>Validation result</returns>
    public static ConfigValidationResult Validate(this MCPServerConfig? config)
    {
        if (config == null)
        {
            return new ConfigValidationResult
            {
                IsValid = false,
                ErrorMessages = ["Configuration is null"]
            };
        }

        var errors = new List<string>();

        // Validate server name
        if (string.IsNullOrWhiteSpace(config.ServerName))
        {
            errors.Add("ServerName is required");
        }

        // Validate transport type configuration
        switch (config.Type)
        {
            case MCPServerType.Stdio:
                if (string.IsNullOrWhiteSpace(config.Command))
                {
                    errors.Add("Command is required for Stdio transport");
                }
                if (config.Args == null)
                {
                    errors.Add("Args list should be initialized (can be empty) for Stdio transport");
                }
                // For Stdio, URL should be empty or not set
                if (!string.IsNullOrWhiteSpace(config.Url))
                {
                    errors.Add("Url should not be set for Stdio transport (use Command and Args instead)");
                }
                break;
            
            case MCPServerType.StreamableHttp:
                if (string.IsNullOrWhiteSpace(config.Url))
                {
                    errors.Add("Url is required for StreamableHttp transport");
                }
                else 
                {
                    if (!Uri.TryCreate(config.Url, UriKind.Absolute, out var uri))
                    {
                        errors.Add("Url must be a valid absolute URI for StreamableHttp transport");
                    }
                    else if (uri.Scheme != "http" && uri.Scheme != "https")
                    {
                        errors.Add("Url must use http or https scheme for StreamableHttp transport");
                    }
                }
                // For StreamableHttp, Command should be empty or not set
                if (!string.IsNullOrWhiteSpace(config.Command))
                {
                    errors.Add("Command should not be set for StreamableHttp transport (use Url instead)");
                }
                // Headers should be initialized
                if (config.Headers == null)
                {
                    errors.Add("Headers dictionary should be initialized (can be empty) for StreamableHttp transport");
                }
                
                // Validate OAuth configuration if present
                if (config.OAuth != null)
                {
                    var oauthErrors = ValidateOAuthConfig(config.OAuth);
                    errors.AddRange(oauthErrors);
                }
                break;
            
            case MCPServerType.Unknown:
                // Try to auto-detect transport type
                if (!string.IsNullOrWhiteSpace(config.Url) && string.IsNullOrWhiteSpace(config.Command))
                {
                    errors.Add("Transport type should be set to StreamableHttp when Url is provided");
                }
                else if (!string.IsNullOrWhiteSpace(config.Command) && string.IsNullOrWhiteSpace(config.Url))
                {
                    errors.Add("Transport type should be set to Stdio when Command is provided");
                }
                else
                {
                    errors.Add("Transport type must be specified, or provide either Command (for Stdio) or Url (for StreamableHttp)");
                }
                break;
        }

        // Validate environment variables
        if (config.Env?.Any(kvp => string.IsNullOrWhiteSpace(kvp.Key)) == true)
        {
            errors.Add("Environment variable keys cannot be empty");
        }

        return new ConfigValidationResult
        {
            IsValid = !errors.Any(),
            ErrorMessages = errors
        };
    }

    /// <summary>
    /// Check if configuration contains sensitive information (for logging purposes)
    /// </summary>
    /// <param name="config">Configuration object</param>
    /// <returns>Configuration copy with sensitive information cleaned</returns>
    public static MCPServerConfig WithoutSensitiveInfo(this MCPServerConfig config)
    {
        if (config == null)
        {
            return config;
        }

        var sanitized = config.Clone();

        // Clean sensitive information from environment variables
        if (sanitized.Env != null)
        {
            var sensitiveKeys = new[]
            {
                "TOKEN", "KEY", "SECRET", "PASSWORD", "PASS", "PWD", "CREDENTIAL", "AUTH"
            };

            var sanitizedEnv = new Dictionary<string, string>();
            foreach (var kvp in sanitized.Env)
            {
                var isSensitive = sensitiveKeys.Any(key => 
                    kvp.Key.ToUpperInvariant().Contains(key));
                
                sanitizedEnv[kvp.Key] = isSensitive ? "***REDACTED***" : kvp.Value;
            }

            sanitized.Env = sanitizedEnv;
        }

        return sanitized;
    }

    /// <summary>
    /// Validate OAuth configuration
    /// </summary>
    /// <param name="oauthConfig">OAuth configuration to validate</param>
    /// <returns>List of validation errors</returns>
    private static List<string> ValidateOAuthConfig(MCPOAuthConfig oauthConfig)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(oauthConfig.ProviderType))
        {
            errors.Add("OAuth ProviderType is required");
            return errors;
        }

        switch (oauthConfig.ProviderType.ToLowerInvariant())
        {
            case "bearer":
                if (string.IsNullOrWhiteSpace(oauthConfig.AccessToken))
                {
                    errors.Add("AccessToken is required for Bearer authentication");
                }
                break;

            case "oauth2":
                if (string.IsNullOrWhiteSpace(oauthConfig.ClientId))
                {
                    errors.Add("ClientId is required for OAuth2 authentication");
                }
                if (string.IsNullOrWhiteSpace(oauthConfig.ClientSecret))
                {
                    errors.Add("ClientSecret is required for OAuth2 authentication");
                }
                if (!string.IsNullOrWhiteSpace(oauthConfig.AuthorizationUrl) && 
                    !Uri.TryCreate(oauthConfig.AuthorizationUrl, UriKind.Absolute, out _))
                {
                    errors.Add("AuthorizationUrl must be a valid absolute URI");
                }
                if (!string.IsNullOrWhiteSpace(oauthConfig.TokenUrl) && 
                    !Uri.TryCreate(oauthConfig.TokenUrl, UriKind.Absolute, out _))
                {
                    errors.Add("TokenUrl must be a valid absolute URI");
                }
                break;

            case "basic":
                if (!oauthConfig.AdditionalParameters.ContainsKey("username") || 
                    string.IsNullOrWhiteSpace(oauthConfig.AdditionalParameters["username"]))
                {
                    errors.Add("Username is required in AdditionalParameters for Basic authentication");
                }
                if (!oauthConfig.AdditionalParameters.ContainsKey("password") || 
                    string.IsNullOrWhiteSpace(oauthConfig.AdditionalParameters["password"]))
                {
                    errors.Add("Password is required in AdditionalParameters for Basic authentication");
                }
                break;

            case "custom":
                if (oauthConfig.AdditionalParameters == null || !oauthConfig.AdditionalParameters.Any())
                {
                    errors.Add("AdditionalParameters are required for Custom authentication");
                }
                break;

            default:
                errors.Add($"Unsupported OAuth ProviderType: {oauthConfig.ProviderType}. Supported types: bearer, oauth2, basic, custom");
                break;
        }

        return errors;
    }
}

/// <summary>
/// Configuration validation result
/// </summary>
public class ConfigValidationResult
{
    /// <summary>
    /// Whether the configuration is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of error messages
    /// </summary>
    public List<string> ErrorMessages { get; set; } = new();

    /// <summary>
    /// Get merged error messages
    /// </summary>
    public string GetErrorSummary() => string.Join("; ", ErrorMessages);
}
