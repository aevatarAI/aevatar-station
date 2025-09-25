using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Options;
using Orleans;

namespace Aevatar.GAgents.AI.Abstractions.Configuration;

/// <summary>
/// Base class for AI service provider configurations
/// Focuses solely on connection and authentication concerns
/// Uses existing LLMProviderEnum for compatibility
/// </summary>
[GenerateSerializer]
public abstract class ProviderConfiguration : ILLMProviderConfig
{
    /// <summary>
    /// The AI service provider type (uses existing enum for compatibility)
    /// </summary>
    public abstract LLMProviderEnum Provider { get; }
    
    /// <summary>
    /// Human-readable description of this provider configuration
    /// </summary>
    [Id(0)] public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets the endpoint URL for this provider
    /// </summary>
    public abstract Task<string> GetEndpointAsync();
    
    /// <summary>
    /// Gets authentication headers required by this provider
    /// </summary>
    public abstract Task<Dictionary<string, string>> GetAuthHeadersAsync(string scopeId);
    
    /// <summary>
    /// Validates provider-specific connection configuration
    /// </summary>
    public abstract bool IsValid();
}

/// <summary>
/// Azure provider configuration (LLMProviderEnum.Azure)
/// </summary>
[GenerateSerializer]
public class AzureProviderConfiguration : ProviderConfiguration
{
    public override LLMProviderEnum Provider => LLMProviderEnum.Azure;
    
    /// <summary>
    /// Azure resource name
    /// </summary>
    [Id(1)] public string ResourceName { get; set; } = string.Empty;
    
    /// <summary>
    /// Azure API version
    /// </summary>
    [Id(2)] public string ApiVersion { get; set; } = "2024-02-01";
    
    public override Task<string> GetEndpointAsync()
    {
        var endpoint = string.IsNullOrEmpty(ResourceName) ? "" : $"https://{ResourceName}.openai.azure.com/";
        return Task.FromResult(endpoint);
    }
    
    public override Task<Dictionary<string, string>> GetAuthHeadersAsync(string scopeId)
    {
        // Note: In actual implementation, this would use IApiKeyManager to get the key
        // For now, return placeholder headers
        var headers = new Dictionary<string, string>
        {
            ["api-key"] = "placeholder-key", // Will be replaced by actual key from IApiKeyManager
            ["api-version"] = ApiVersion
        };
        return Task.FromResult(headers);
    }
    
    public override bool IsValid()
    {
        return !string.IsNullOrEmpty(ResourceName);
    }
}

/// <summary>
/// OpenAI provider configuration (LLMProviderEnum.OpenAI)
/// </summary>
[GenerateSerializer]
public class OpenAIProviderConfiguration : ProviderConfiguration
{
    public override LLMProviderEnum Provider => LLMProviderEnum.OpenAI;
    
    public override Task<string> GetEndpointAsync()
    {
        return Task.FromResult("https://api.openai.com/v1/");
    }
    
    public override Task<Dictionary<string, string>> GetAuthHeadersAsync(string scopeId)
    {
        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer placeholder-key" // Will be replaced by actual key
        };
        return Task.FromResult(headers);
    }
    
    public override bool IsValid()
    {
        return true;
    }
}

/// <summary>
/// DeepSeek provider configuration (LLMProviderEnum.DeepSeek)
/// </summary>
[GenerateSerializer]
public class DeepSeekProviderConfiguration : ProviderConfiguration
{
    public override LLMProviderEnum Provider => LLMProviderEnum.DeepSeek;
    
    public override Task<string> GetEndpointAsync()
    {
        return Task.FromResult("https://api.deepseek.com/v1/");
    }
    
    public override Task<Dictionary<string, string>> GetAuthHeadersAsync(string scopeId)
    {
        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer placeholder-key"
        };
        return Task.FromResult(headers);
    }
    
    public override bool IsValid()
    {
        return true;
    }
}

/// <summary>
/// Google provider configuration (LLMProviderEnum.Google)
/// </summary>
[GenerateSerializer]
public class GoogleProviderConfiguration : ProviderConfiguration
{
    public override LLMProviderEnum Provider => LLMProviderEnum.Google;
    
    public override Task<string> GetEndpointAsync()
    {
        return Task.FromResult("https://generativelanguage.googleapis.com/v1/");
    }
    
    public override Task<Dictionary<string, string>> GetAuthHeadersAsync(string scopeId)
    {
        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer placeholder-key"
        };
        return Task.FromResult(headers);
    }
    
    public override bool IsValid()
    {
        return true;
    }
}

/// <summary>
/// BytePlus provider configuration (LLMProviderEnum.BytePlus)
/// </summary>
[GenerateSerializer]
public class BytePlusProviderConfiguration : ProviderConfiguration
{
    public override LLMProviderEnum Provider => LLMProviderEnum.BytePlus;
    
    public override Task<string> GetEndpointAsync()
    {
        return Task.FromResult("https://ark.cn-beijing.volces.com/api/v3/");
    }
    
    public override Task<Dictionary<string, string>> GetAuthHeadersAsync(string scopeId)
    {
        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer placeholder-key"
        };
        return Task.FromResult(headers);
    }
    
    public override bool IsValid()
    {
        return true;
    }
}