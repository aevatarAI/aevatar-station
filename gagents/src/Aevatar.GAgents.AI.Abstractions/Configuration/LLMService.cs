using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Options;
using Orleans;

namespace Aevatar.GAgents.AI.Abstractions.Configuration;

/// <summary>
/// Service for combining provider and model configurations
/// Uses existing LLMProviderEnum and ModelIdEnum for compatibility
/// </summary>
[GenerateSerializer]
public class LLMService
{
    /// <summary>
    /// Provider configuration (connection and authentication)
    /// </summary>
    [Id(0)] public ILLMProviderConfig Provider { get; set; } = null!;
    
    /// <summary>
    /// Model configuration (parameters and behavior)
    /// </summary>
    [Id(1)] public ILLMModelConfig Model { get; set; } = null!;
    
    /// <summary>
    /// Service description
    /// </summary>
    [Id(2)] public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Creates a new LLM service with provider and model configurations
    /// </summary>
    public LLMService(ILLMProviderConfig provider, ILLMModelConfig model)
    {
        Provider = provider;
        Model = model;
        Description = $"{provider.Provider}-{model.Model}";
    }
    
    /// <summary>
    /// Parameterless constructor for Orleans serialization
    /// </summary>
    public LLMService() { }
    
    /// <summary>
    /// Gets the complete configuration for API calls
    /// </summary>
    public async Task<LLMApiConfiguration> GetApiConfigurationAsync(string scopeId)
    {
        var endpoint = await Provider.GetEndpointAsync();
        var authHeaders = await Provider.GetAuthHeadersAsync(scopeId);
        var modelParameters = Model.GetParameters();
        var modelId = Model.GetModelIdentifier();
        
        return new LLMApiConfiguration
        {
            Endpoint = endpoint,
            AuthHeaders = authHeaders,
            ModelId = modelId,
            ModelParameters = modelParameters,
            Provider = Provider.Provider,
            Model = Model.Model
        };
    }
    
    /// <summary>
    /// Validates that provider and model are compatible and configured correctly
    /// </summary>
    public bool IsValid()
    {
        if (!Provider.IsValid() || !Model.IsValid())
            return false;
            
        // Check provider-model compatibility using existing enums
        return (Provider.Provider, Model.Model) switch
        {
            // Azure can host OpenAI models
            (LLMProviderEnum.Azure, ModelIdEnum.OpenAI) => true,
            
            // OpenAI native
            (LLMProviderEnum.OpenAI, ModelIdEnum.OpenAI) => true,
            (LLMProviderEnum.OpenAI, ModelIdEnum.OpenAITextToImage) => true,
            
            // DeepSeek native
            (LLMProviderEnum.DeepSeek, ModelIdEnum.DeepSeek) => true,
            
            // Google native
            (LLMProviderEnum.Google, ModelIdEnum.Gemini) => true,
            
            // BytePlus native
            (LLMProviderEnum.BytePlus, ModelIdEnum.BytePlusVideoGeneration) => true,
            
            _ => false // Unsupported provider-model combination
        };
    }
}

/// <summary>
/// Complete API configuration ready for HTTP calls
/// </summary>
[GenerateSerializer]
public class LLMApiConfiguration
{
    /// <summary>
    /// API endpoint URL
    /// </summary>
    [Id(0)] public string Endpoint { get; set; } = string.Empty;
    
    /// <summary>
    /// Authentication headers
    /// </summary>
    [Id(1)] public Dictionary<string, string> AuthHeaders { get; set; } = new();
    
    /// <summary>
    /// Provider-specific model identifier
    /// </summary>
    [Id(2)] public string ModelId { get; set; } = string.Empty;
    
    /// <summary>
    /// Model parameters for API calls
    /// </summary>
    [Id(3)] public Dictionary<string, object> ModelParameters { get; set; } = new();
    
    /// <summary>
    /// Provider type (using existing enum)
    /// </summary>
    [Id(4)] public LLMProviderEnum Provider { get; set; }
    
    /// <summary>
    /// Model type (using existing enum)
    /// </summary>
    [Id(5)] public ModelIdEnum Model { get; set; }
}

/// <summary>
/// Factory for creating common LLM service configurations using existing enums
/// </summary>
public static class LLMServiceFactory
{
    /// <summary>
    /// Creates Azure + OpenAI service
    /// </summary>
    public static LLMService CreateAzureOpenAI(string resourceName)
    {
        ILLMProviderConfig provider = new AzureProviderConfiguration
        {
            ResourceName = resourceName,
            Description = $"Azure OpenAI - {resourceName}"
        };
        
        ILLMModelConfig model = new OpenAIModelConfiguration
        {
            ModelName = "gpt-4",
            Description = "GPT-4 on Azure"
        };
        
        return new LLMService(provider, model);
    }
    
    /// <summary>
    /// Creates OpenAI native service
    /// </summary>
    public static LLMService CreateOpenAI(string modelName = "gpt-4")
    {
        var provider = new OpenAIProviderConfiguration
        {
            Description = "OpenAI Native"
        };
        
        var model = new OpenAIModelConfiguration
        {
            ModelName = modelName,
            Description = $"OpenAI {modelName}"
        };
        
        return new LLMService(provider, model);
    }
    
    /// <summary>
    /// Creates DeepSeek service
    /// </summary>
    public static LLMService CreateDeepSeek()
    {
        var provider = new DeepSeekProviderConfiguration
        {
            Description = "DeepSeek AI"
        };
        
        var model = new DeepSeekModelConfiguration
        {
            Description = "DeepSeek Chat"
        };
        
        return new LLMService(provider, model);
    }
    
    /// <summary>
    /// Creates Google Gemini service
    /// </summary>
    public static LLMService CreateGoogleGemini()
    {
        var provider = new GoogleProviderConfiguration
        {
            Description = "Google AI"
        };
        
        var model = new GeminiModelConfiguration
        {
            Description = "Gemini Pro"
        };
        
        return new LLMService(provider, model);
    }
    
    /// <summary>
    /// Creates BytePlus video generation service
    /// </summary>
    public static LLMService CreateBytePlusVideo()
    {
        var provider = new BytePlusProviderConfiguration
        {
            Description = "BytePlus AI"
        };
        
        var model = new BytePlusVideoGenerationModelConfiguration
        {
            Description = "BytePlus Video Generation"
        };
        
        return new LLMService(provider, model);
    }
}