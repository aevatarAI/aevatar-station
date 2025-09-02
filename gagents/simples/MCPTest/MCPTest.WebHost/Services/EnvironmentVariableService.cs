using System.Text.RegularExpressions;

namespace MCPTest.WebHost.Services;

public class EnvironmentVariableService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EnvironmentVariableService> _logger;

    public EnvironmentVariableService(IConfiguration configuration, ILogger<EnvironmentVariableService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Resolves environment variables in a dictionary of environment settings
    /// Supports patterns like ${VAR_NAME} in values
    /// </summary>
    public Dictionary<string, string> ResolveEnvironmentVariables(Dictionary<string, string> envDict)
    {
        var resolved = new Dictionary<string, string>();

        foreach (var (key, value) in envDict)
        {
            var resolvedValue = ResolveVariableValue(value);
            resolved[key] = resolvedValue;
            
            _logger.LogDebug("Resolved environment variable {Key}: {Original} -> {Resolved}", 
                key, value, resolvedValue);
        }

        return resolved;
    }

    /// <summary>
    /// Resolves a single value that may contain environment variable references
    /// </summary>
    private string ResolveVariableValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // Pattern to match ${VAR_NAME} or $VAR_NAME
        var pattern = @"\$\{([^}]+)\}|\$([A-Za-z_][A-Za-z0-9_]*)";
        
        return Regex.Replace(value, pattern, match =>
        {
            // Get the variable name from either capture group
            var varName = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
            
            // First try to get from appsettings EnvironmentVariables section
            var configValue = _configuration[$"EnvironmentVariables:{varName}"];
            if (!string.IsNullOrEmpty(configValue))
            {
                _logger.LogDebug("Found environment variable {VarName} in configuration", varName);
                return configValue;
            }
            
            // Then try system environment variables
            var envValue = Environment.GetEnvironmentVariable(varName);
            if (!string.IsNullOrEmpty(envValue))
            {
                _logger.LogDebug("Found environment variable {VarName} in system environment", varName);
                return envValue;
            }
            
            // Log warning and return original placeholder
            _logger.LogWarning("Environment variable {VarName} not found, keeping original value", varName);
            return match.Value;
        });
    }

    /// <summary>
    /// Gets all available environment variables for debugging
    /// </summary>
    public Dictionary<string, string> GetAvailableEnvironmentVariables()
    {
        var result = new Dictionary<string, string>();
        
        // Add configuration-based environment variables
        var configSection = _configuration.GetSection("EnvironmentVariables");
        foreach (var child in configSection.GetChildren())
        {
            result[$"Config:{child.Key}"] = child.Value ?? "";
        }
        
        // Add some system environment variables (filtered for security)
        var allowedSystemVars = new[] { "PATH", "HOME", "USER", "NODE_ENV", "PYTHON_PATH" };
        foreach (var varName in allowedSystemVars)
        {
            var value = Environment.GetEnvironmentVariable(varName);
            if (!string.IsNullOrEmpty(value))
            {
                result[$"System:{varName}"] = value;
            }
        }
        
        return result;
    }
}
