using System;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Aevatar.GAgents.AI.Abstractions.Configuration;

/// <summary>
/// Configuration context that abstracts business-level identifiers from framework layer
/// Provides a generic way to pass scope information without coupling to specific business concepts
/// </summary>
public class ConfigurationContext
{
    /// <summary>
    /// Primary scope identifier (e.g., grain ID, session ID)
    /// </summary>
    public string? PrimaryScope { get; set; }
    
    /// <summary>
    /// Secondary scope identifier (e.g., tenant, project)
    /// </summary>
    public string? SecondaryScope { get; set; }
    
    /// <summary>
    /// Tertiary scope identifier (e.g., user, workflow)
    /// </summary>
    public string? TertiaryScope { get; set; }
    
    /// <summary>
    /// System-level scope (always "system" for framework level)
    /// </summary>
    public string SystemScope { get; set; } = "system";
    
    /// <summary>
    /// Default context with only system scope
    /// </summary>
    public static ConfigurationContext Default => new();
    
    /// <summary>
    /// Create context from grain-level information
    /// </summary>
    public static ConfigurationContext FromGrainContext(string grainId)
    {
        return new ConfigurationContext
        {
            PrimaryScope = grainId
        };
    }
    
    /// <summary>
    /// Create context for project-level configuration
    /// </summary>
    public static ConfigurationContext ForProject(string projectId)
    {
        return new ConfigurationContext
        {
            SecondaryScope = projectId
        };
    }
    
    /// <summary>
    /// Create context for user-level configuration
    /// </summary>
    public static ConfigurationContext ForUser(string userId)
    {
        return new ConfigurationContext
        {
            PrimaryScope = userId
        };
    }
    
    /// <summary>
    /// Create context for workflow-level configuration
    /// </summary>
    public static ConfigurationContext ForWorkflow(string workflowId)
    {
        return new ConfigurationContext
        {
            TertiaryScope = workflowId
        };
    }
    
    /// <summary>
    /// Fluent API for building configuration context
    /// </summary>
    public ConfigurationContext WithProject(string projectId)
    {
        SecondaryScope = projectId;
        return this;
    }
    
    public ConfigurationContext WithUser(string userId)
    {
        PrimaryScope = userId;
        return this;
    }
    
    public ConfigurationContext WithWorkflow(string workflowId)
    {
        TertiaryScope = workflowId;
        return this;
    }
}

/// <summary>
/// Service for resolving LLM configurations dynamically
/// Abstracts the complexity of Provider + Model resolution from individual AI agents
/// </summary>
public interface ILLMConfigurationService
{
    /// <summary>
    /// Resolves LLM service configuration based on provider and model enums
    /// Uses configuration context to abstract business-level scope information
    /// </summary>
    Task<LLMService> ResolveLLMServiceAsync(
        LLMProviderEnum provider, 
        ModelIdEnum model, 
        ConfigurationContext? context = null);

    /// <summary>
    /// Resolves LLM service from legacy SystemLLM string (backward compatibility)
    /// Uses configuration context to abstract business-level scope information
    /// </summary>
    Task<LLMService> ResolveLLMServiceFromSystemLLMAsync(
        string systemLLM,
        ConfigurationContext? context = null);
}

/// <summary>
/// Default implementation of LLM configuration service
/// </summary>
public class LLMConfigurationService : ILLMConfigurationService
{
    private readonly IConfigurationProvider _configurationProvider;
    private readonly IApiKeyManager _apiKeyManager;

    public LLMConfigurationService(
        IConfigurationProvider configurationProvider,
        IApiKeyManager apiKeyManager)
    {
        _configurationProvider = configurationProvider;
        _apiKeyManager = apiKeyManager;
    }

    public async Task<LLMService> ResolveLLMServiceAsync(
        LLMProviderEnum provider, 
        ModelIdEnum model, 
        ConfigurationContext? context = null)
    {
        context ??= ConfigurationContext.Default;
        
        // Create provider configuration using abstracted context
        var providerConfig = await CreateProviderConfigurationAsync(provider, context);
        
        // Create model configuration using abstracted context
        var modelConfig = await CreateModelConfigurationAsync(model, context);
        
        return new LLMService(providerConfig, modelConfig);
    }

    public async Task<LLMService> ResolveLLMServiceFromSystemLLMAsync(
        string systemLLM,
        ConfigurationContext? context = null)
    {
        // Parse SystemLLM string to Provider + Model
        var (provider, model) = ParseSystemLLM(systemLLM);
        
        return await ResolveLLMServiceAsync(provider, model, context);
    }

    private async Task<ILLMProviderConfig> CreateProviderConfigurationAsync(
        LLMProviderEnum provider,
        ConfigurationContext context)
    {
        // Get provider-specific configuration from dynamic config using abstracted context
        var providerConfigKey = $"llm-provider-{provider.ToString().ToLower()}";
        var dynamicConfig = await _configurationProvider.GetConfigurationWithCascadeAsync<dynamic>(
            providerConfigKey, context.TertiaryScope, context.SecondaryScope, context.PrimaryScope);

        return provider switch
        {
            LLMProviderEnum.Azure => new AzureProviderConfiguration
            {
                ResourceName = GetConfigValue(dynamicConfig, "ResourceName", "default-resource"),
                ApiVersion = GetConfigValue(dynamicConfig, "ApiVersion", "2024-02-01"),
                Description = $"Azure OpenAI - Dynamic Config"
            },
            LLMProviderEnum.OpenAI => new OpenAIProviderConfiguration
            {
                Description = "OpenAI - Dynamic Config"
            },
            LLMProviderEnum.DeepSeek => new DeepSeekProviderConfiguration
            {
                Description = "DeepSeek - Dynamic Config"
            },
            LLMProviderEnum.Google => new GoogleProviderConfiguration
            {
                Description = "Google AI - Dynamic Config"
            },
            LLMProviderEnum.BytePlus => new BytePlusProviderConfiguration
            {
                Description = "BytePlus - Dynamic Config"
            },
            _ => new OpenAIProviderConfiguration { Description = "Default OpenAI" }
        };
    }

    private async Task<ILLMModelConfig> CreateModelConfigurationAsync(
        ModelIdEnum model,
        ConfigurationContext context)
    {
        // Get model-specific configuration from dynamic config using abstracted context
        var modelConfigKey = $"llm-model-{model.ToString().ToLower()}";
        var dynamicConfig = await _configurationProvider.GetConfigurationWithCascadeAsync<dynamic>(
            modelConfigKey, context.TertiaryScope, context.SecondaryScope, context.PrimaryScope);

        return model switch
        {
            ModelIdEnum.OpenAI => new OpenAIModelConfiguration
            {
                ModelName = GetConfigValue(dynamicConfig, "ModelName", "gpt-4"),
                Temperature = GetConfigValue(dynamicConfig, "Temperature", 0.7),
                MaxTokens = GetConfigValue(dynamicConfig, "MaxTokens", 4000),
                Description = "OpenAI - Dynamic Config"
            },
            ModelIdEnum.DeepSeek => new DeepSeekModelConfiguration
            {
                Temperature = GetConfigValue(dynamicConfig, "Temperature", 0.7),
                MaxTokens = GetConfigValue(dynamicConfig, "MaxTokens", 4000),
                Description = "DeepSeek - Dynamic Config"
            },
            ModelIdEnum.Gemini => new GeminiModelConfiguration
            {
                Temperature = GetConfigValue(dynamicConfig, "Temperature", 0.7),
                MaxTokens = GetConfigValue(dynamicConfig, "MaxTokens", 4000),
                Description = "Gemini - Dynamic Config"
            },
            ModelIdEnum.OpenAITextToImage => new OpenAITextToImageModelConfiguration
            {
                Size = GetConfigValue(dynamicConfig, "Size", "1024x1024"),
                NumberOfImages = GetConfigValue(dynamicConfig, "NumberOfImages", 1),
                Description = "OpenAI Text-to-Image - Dynamic Config"
            },
            ModelIdEnum.BytePlusVideoGeneration => new BytePlusVideoGenerationModelConfiguration
            {
                Duration = GetConfigValue(dynamicConfig, "Duration", 10),
                Resolution = GetConfigValue(dynamicConfig, "Resolution", "720p"),
                Description = "BytePlus Video Generation - Dynamic Config"
            },
            _ => new OpenAIModelConfiguration { Description = "Default OpenAI Model" }
        };
    }

    private static (LLMProviderEnum, ModelIdEnum) ParseSystemLLM(string systemLLM)
    {
        return systemLLM.ToLowerInvariant() switch
        {
            "openai" => (LLMProviderEnum.OpenAI, ModelIdEnum.OpenAI),
            "azure" => (LLMProviderEnum.Azure, ModelIdEnum.OpenAI),
            "deepseek" => (LLMProviderEnum.DeepSeek, ModelIdEnum.DeepSeek),
            "google" => (LLMProviderEnum.Google, ModelIdEnum.Gemini),
            "gemini" => (LLMProviderEnum.Google, ModelIdEnum.Gemini),
            "byteplus" => (LLMProviderEnum.BytePlus, ModelIdEnum.BytePlusVideoGeneration),
            _ => (LLMProviderEnum.OpenAI, ModelIdEnum.OpenAI) // Default fallback
        };
    }

    private static T GetConfigValue<T>(dynamic? config, string key, T defaultValue)
    {
        if (config == null) return defaultValue;
        
        try
        {
            // Try to get value from dynamic config
            var value = ((object)config).GetType().GetProperty(key)?.GetValue(config);
            if (value != null && value is T)
                return (T)value;
        }
        catch
        {
            // Ignore errors and use default
        }
        
        return defaultValue;
    }
}
