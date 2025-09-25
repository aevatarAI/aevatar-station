using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Aevatar.GAgents.AI.Abstractions.Configuration;
using Aevatar.GAgents.AI.Options;

namespace Aevatar.Configuration;

/// <summary>
/// Request DTO for setting LLM Provider configuration
/// </summary>
public class SetProviderConfigRequest
{
    [Required]
    public LLMProviderEnum ProviderType { get; set; }
    
    [Required]
    public ConfigurationContext Context { get; set; } = new();
    
    public Dictionary<string, object> Settings { get; set; } = new();
}

/// <summary>
/// Request DTO for setting LLM Model configuration
/// </summary>
public class SetModelConfigRequest
{
    [Required]
    public ModelIdEnum ModelType { get; set; }
    
    [Required]
    public ConfigurationContext Context { get; set; } = new();
    
    public Dictionary<string, object> Settings { get; set; } = new();
}

/// <summary>
/// Request DTO for setting API Key
/// </summary>
public class SetApiKeyRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string KeyName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string KeyValue { get; set; } = string.Empty;
    
    [Required]
    public ConfigurationContext Context { get; set; } = new();
}

/// <summary>
/// Response DTO for LLM Provider configuration
/// </summary>
public class ProviderConfigResponse
{
    public LLMProviderEnum ProviderType { get; set; }
    public Dictionary<string, object> Settings { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ConfigurationContext Context { get; set; } = new();
}

/// <summary>
/// Response DTO for LLM Model configuration
/// </summary>
public class ModelConfigResponse
{
    public ModelIdEnum ModelType { get; set; }
    public Dictionary<string, object> Settings { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ConfigurationContext Context { get; set; } = new();
}

/// <summary>
/// Response DTO for API Key (with masked value for security)
/// </summary>
public class ApiKeyResponse
{
    public string KeyName { get; set; } = string.Empty;
    public string MaskedValue { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ConfigurationContext Context { get; set; } = new();
}

/// <summary>
/// Request DTO for testing LLM configuration
/// </summary>
public class TestLLMConfigRequest
{
    [Required]
    public LLMProviderEnum ProviderType { get; set; }
    
    [Required]
    public ModelIdEnum ModelType { get; set; }
    
    public ConfigurationContext Context { get; set; } = new();
    
    public string TestPrompt { get; set; } = "Hello, this is a test message.";
}

/// <summary>
/// Response DTO for LLM configuration test
/// </summary>
public class TestLLMConfigResponse
{
    public bool IsSuccessful { get; set; }
    public string? Response { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Request DTO for validating configuration completeness
/// </summary>
public class ValidateConfigRequest
{
    public ConfigurationContext Context { get; set; } = new();
    public List<LLMProviderEnum> RequiredProviders { get; set; } = new();
    public List<ModelIdEnum> RequiredModels { get; set; } = new();
    public List<string> RequiredApiKeys { get; set; } = new();
}

/// <summary>
/// Response DTO for configuration validation
/// </summary>
public class ValidateConfigResponse
{
    public bool IsValid { get; set; }
    public List<string> MissingProviders { get; set; } = new();
    public List<string> MissingModels { get; set; } = new();
    public List<string> MissingApiKeys { get; set; } = new();
    public List<string> ConfigurationErrors { get; set; } = new();
    public Dictionary<string, object> ValidationDetails { get; set; } = new();
}
