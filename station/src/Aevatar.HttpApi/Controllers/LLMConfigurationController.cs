using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Configuration;
using Aevatar.GAgents.AI.Abstractions.Configuration;
using Aevatar.GAgents.AI.Options;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace Aevatar.Controllers;

/// <summary>
/// Controller for managing LLM configurations (Providers, Models, and API Keys)
/// Provides REST API endpoints for dynamic configuration management
/// </summary>
[RemoteService]
[ControllerName("LLMConfiguration")]
[Route("api/llm-config")]
[Authorize]
public class LLMConfigurationController : AevatarController
{
    private readonly IConfigurationProvider _configurationProvider;
    private readonly IApiKeyManager _apiKeyManager;
    private readonly ILLMConfigurationService _llmConfigurationService;
    private readonly ILogger<LLMConfigurationController> _logger;

    public LLMConfigurationController(
        IConfigurationProvider configurationProvider,
        IApiKeyManager apiKeyManager,
        ILLMConfigurationService llmConfigurationService,
        ILogger<LLMConfigurationController> logger)
    {
        _configurationProvider = configurationProvider;
        _apiKeyManager = apiKeyManager;
        _llmConfigurationService = llmConfigurationService;
        _logger = logger;
    }

    #region Provider Configuration Management

    /// <summary>
    /// Set or update LLM Provider configuration
    /// </summary>
    [HttpPost("providers")]
    public async Task<ProviderConfigResponse> SetProviderConfigAsync([FromBody] SetProviderConfigRequest request)
    {
        _logger.LogInformation("Setting provider configuration for {ProviderType}", request.ProviderType);

        var configKey = $"llm-provider-{request.ProviderType.ToString().ToLower()}";
        
        await _configurationProvider.SetAsync(
            configKey,
            request.Settings,
            GetConfigurationScope(request.Context),
            GetScopeIdentifier(request.Context));

        return new ProviderConfigResponse
        {
            ProviderType = request.ProviderType,
            Settings = request.Settings,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Context = request.Context
        };
    }

    /// <summary>
    /// Get LLM Provider configuration
    /// </summary>
    [HttpGet("providers/{providerType}")]
    public async Task<ProviderConfigResponse?> GetProviderConfigAsync(
        LLMProviderEnum providerType,
        [FromQuery] string? primaryScope = null,
        [FromQuery] string? secondaryScope = null,
        [FromQuery] string? tertiaryScope = null)
    {
        var context = new ConfigurationContext
        {
            PrimaryScope = primaryScope,
            SecondaryScope = secondaryScope,
            TertiaryScope = tertiaryScope
        };

        var configKey = $"llm-provider-{providerType.ToString().ToLower()}";
        
        var settings = await _configurationProvider.GetConfigurationWithCascadeAsync<Dictionary<string, object>>(
            configKey, context.TertiaryScope, context.SecondaryScope, context.PrimaryScope);

        if (settings == null)
        {
            return null;
        }

        return new ProviderConfigResponse
        {
            ProviderType = providerType,
            Settings = settings,
            CreatedAt = DateTime.UtcNow, // TODO: Get actual timestamps from storage
            UpdatedAt = DateTime.UtcNow,
            Context = context
        };
    }

    /// <summary>
    /// Get all configured LLM Providers
    /// </summary>
    [HttpGet("providers")]
    public async Task<List<ProviderConfigResponse>> GetAllProvidersAsync(
        [FromQuery] string? primaryScope = null,
        [FromQuery] string? secondaryScope = null,
        [FromQuery] string? tertiaryScope = null)
    {
        var context = new ConfigurationContext
        {
            PrimaryScope = primaryScope,
            SecondaryScope = secondaryScope,
            TertiaryScope = tertiaryScope
        };

        var providers = new List<ProviderConfigResponse>();
        
        foreach (var providerType in Enum.GetValues<LLMProviderEnum>())
        {
            var configKey = $"llm-provider-{providerType.ToString().ToLower()}";
            var settings = await _configurationProvider.GetConfigurationWithCascadeAsync<Dictionary<string, object>>(
                configKey, context.TertiaryScope, context.SecondaryScope, context.PrimaryScope);

            if (settings != null)
            {
                providers.Add(new ProviderConfigResponse
                {
                    ProviderType = providerType,
                    Settings = settings,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Context = context
                });
            }
        }

        return providers;
    }

    /// <summary>
    /// Delete LLM Provider configuration
    /// </summary>
    [HttpDelete("providers/{providerType}")]
    public async Task<IActionResult> DeleteProviderConfigAsync(
        LLMProviderEnum providerType,
        [FromQuery] string? primaryScope = null,
        [FromQuery] string? secondaryScope = null,
        [FromQuery] string? tertiaryScope = null)
    {
        var context = new ConfigurationContext
        {
            PrimaryScope = primaryScope,
            SecondaryScope = secondaryScope,
            TertiaryScope = tertiaryScope
        };

        var configKey = $"llm-provider-{providerType.ToString().ToLower()}";
        
        // Note: Current IConfigurationProvider doesn't have Delete method
        // Setting to null to effectively remove the configuration
        await _configurationProvider.SetAsync(
            configKey,
            null,
            GetConfigurationScope(context),
            GetScopeIdentifier(context));

        _logger.LogInformation("Deleted provider configuration for {ProviderType}", providerType);
        return Ok();
    }

    #endregion

    #region Model Configuration Management

    /// <summary>
    /// Set or update LLM Model configuration
    /// </summary>
    [HttpPost("models")]
    public async Task<ModelConfigResponse> SetModelConfigAsync([FromBody] SetModelConfigRequest request)
    {
        _logger.LogInformation("Setting model configuration for {ModelType}", request.ModelType);

        var configKey = $"llm-model-{request.ModelType.ToString().ToLower()}";
        
        await _configurationProvider.SetAsync(
            configKey,
            request.Settings,
            GetConfigurationScope(request.Context),
            GetScopeIdentifier(request.Context));

        return new ModelConfigResponse
        {
            ModelType = request.ModelType,
            Settings = request.Settings,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Context = request.Context
        };
    }

    /// <summary>
    /// Get LLM Model configuration
    /// </summary>
    [HttpGet("models/{modelType}")]
    public async Task<ModelConfigResponse?> GetModelConfigAsync(
        ModelIdEnum modelType,
        [FromQuery] string? primaryScope = null,
        [FromQuery] string? secondaryScope = null,
        [FromQuery] string? tertiaryScope = null)
    {
        var context = new ConfigurationContext
        {
            PrimaryScope = primaryScope,
            SecondaryScope = secondaryScope,
            TertiaryScope = tertiaryScope
        };

        var configKey = $"llm-model-{modelType.ToString().ToLower()}";
        
        var settings = await _configurationProvider.GetConfigurationWithCascadeAsync<Dictionary<string, object>>(
            configKey, context.TertiaryScope, context.SecondaryScope, context.PrimaryScope);

        if (settings == null)
        {
            return null;
        }

        return new ModelConfigResponse
        {
            ModelType = modelType,
            Settings = settings,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Context = context
        };
    }

    /// <summary>
    /// Get all configured LLM Models
    /// </summary>
    [HttpGet("models")]
    public async Task<List<ModelConfigResponse>> GetAllModelsAsync(
        [FromQuery] string? primaryScope = null,
        [FromQuery] string? secondaryScope = null,
        [FromQuery] string? tertiaryScope = null)
    {
        var context = new ConfigurationContext
        {
            PrimaryScope = primaryScope,
            SecondaryScope = secondaryScope,
            TertiaryScope = tertiaryScope
        };

        var models = new List<ModelConfigResponse>();
        
        foreach (var modelType in Enum.GetValues<ModelIdEnum>())
        {
            var configKey = $"llm-model-{modelType.ToString().ToLower()}";
            var settings = await _configurationProvider.GetConfigurationWithCascadeAsync<Dictionary<string, object>>(
                configKey, context.TertiaryScope, context.SecondaryScope, context.PrimaryScope);

            if (settings != null)
            {
                models.Add(new ModelConfigResponse
                {
                    ModelType = modelType,
                    Settings = settings,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Context = context
                });
            }
        }

        return models;
    }

    /// <summary>
    /// Delete LLM Model configuration
    /// </summary>
    [HttpDelete("models/{modelType}")]
    public async Task<IActionResult> DeleteModelConfigAsync(
        ModelIdEnum modelType,
        [FromQuery] string? primaryScope = null,
        [FromQuery] string? secondaryScope = null,
        [FromQuery] string? tertiaryScope = null)
    {
        var context = new ConfigurationContext
        {
            PrimaryScope = primaryScope,
            SecondaryScope = secondaryScope,
            TertiaryScope = tertiaryScope
        };

        var configKey = $"llm-model-{modelType.ToString().ToLower()}";
        
        await _configurationProvider.SetAsync(
            configKey,
            null,
            GetConfigurationScope(context),
            GetScopeIdentifier(context));

        _logger.LogInformation("Deleted model configuration for {ModelType}", modelType);
        return Ok();
    }

    #endregion

    #region API Key Management

    /// <summary>
    /// Set or update API Key
    /// </summary>
    [HttpPost("api-keys")]
    public async Task<ApiKeyResponse> SetApiKeyAsync([FromBody] SetApiKeyRequest request)
    {
        _logger.LogInformation("Setting API key: {KeyName}", request.KeyName);

        await _apiKeyManager.SetKeyAsync(
            request.KeyName,
            request.KeyValue,
            GetConfigurationScope(request.Context),
            GetScopeIdentifier(request.Context));

        return new ApiKeyResponse
        {
            KeyName = request.KeyName,
            MaskedValue = MaskApiKey(request.KeyValue),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Context = request.Context
        };
    }

    /// <summary>
    /// Get API Key (masked for security)
    /// </summary>
    [HttpGet("api-keys/{keyName}")]
    public async Task<ApiKeyResponse?> GetApiKeyAsync(
        string keyName,
        [FromQuery] string? primaryScope = null,
        [FromQuery] string? secondaryScope = null,
        [FromQuery] string? tertiaryScope = null)
    {
        var context = new ConfigurationContext
        {
            PrimaryScope = primaryScope,
            SecondaryScope = secondaryScope,
            TertiaryScope = tertiaryScope
        };

        var keyValue = await _apiKeyManager.GetKeyWithCascadeAsync(
            keyName, context.TertiaryScope, context.SecondaryScope, context.PrimaryScope);

        if (keyValue == null)
        {
            return null;
        }

        return new ApiKeyResponse
        {
            KeyName = keyName,
            MaskedValue = MaskApiKey(keyValue),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Context = context
        };
    }

    /// <summary>
    /// Delete API Key
    /// </summary>
    [HttpDelete("api-keys/{keyName}")]
    public async Task<IActionResult> DeleteApiKeyAsync(
        string keyName,
        [FromQuery] string? primaryScope = null,
        [FromQuery] string? secondaryScope = null,
        [FromQuery] string? tertiaryScope = null)
    {
        var context = new ConfigurationContext
        {
            PrimaryScope = primaryScope,
            SecondaryScope = secondaryScope,
            TertiaryScope = tertiaryScope
        };

        // Note: Current IApiKeyManager doesn't have Delete method
        // Setting to empty string to effectively remove the key
        await _apiKeyManager.SetKeyAsync(
            keyName,
            string.Empty,
            GetConfigurationScope(context),
            GetScopeIdentifier(context));

        _logger.LogInformation("Deleted API key: {KeyName}", keyName);
        return Ok();
    }

    #endregion

    #region Configuration Testing and Validation

    /// <summary>
    /// Test LLM configuration by making a test call
    /// </summary>
    [HttpPost("test")]
    public async Task<TestLLMConfigResponse> TestLLMConfigAsync([FromBody] TestLLMConfigRequest request)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            _logger.LogInformation("Testing LLM configuration: {Provider} + {Model}", 
                request.ProviderType, request.ModelType);

            // Resolve LLM service using the configuration
            var llmService = await _llmConfigurationService.ResolveLLMServiceAsync(
                request.ProviderType, request.ModelType, request.Context);

            // TODO: Implement actual LLM test call
            // For now, just validate that the service can be resolved
            if (llmService.IsValid())
            {
                return new TestLLMConfigResponse
                {
                    IsSuccessful = true,
                    Response = "Configuration test successful - LLM service resolved correctly",
                    ResponseTime = DateTime.UtcNow - startTime,
                    Metadata = new Dictionary<string, object>
                    {
                        ["provider"] = request.ProviderType.ToString(),
                        ["model"] = request.ModelType.ToString(),
                        ["description"] = llmService.Description
                    }
                };
            }
            else
            {
                return new TestLLMConfigResponse
                {
                    IsSuccessful = false,
                    ErrorMessage = "LLM service configuration is invalid",
                    ResponseTime = DateTime.UtcNow - startTime
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing LLM configuration");
            return new TestLLMConfigResponse
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                ResponseTime = DateTime.UtcNow - startTime
            };
        }
    }

    /// <summary>
    /// Validate configuration completeness
    /// </summary>
    [HttpPost("validate")]
    public async Task<ValidateConfigResponse> ValidateConfigAsync([FromBody] ValidateConfigRequest request)
    {
        var response = new ValidateConfigResponse();
        
        try
        {
            // Check required providers
            foreach (var provider in request.RequiredProviders)
            {
                var configKey = $"llm-provider-{provider.ToString().ToLower()}";
                var config = await _configurationProvider.GetConfigurationWithCascadeAsync<Dictionary<string, object>>(
                    configKey, request.Context.TertiaryScope, request.Context.SecondaryScope, request.Context.PrimaryScope);
                
                if (config == null)
                {
                    response.MissingProviders.Add(provider.ToString());
                }
            }

            // Check required models
            foreach (var model in request.RequiredModels)
            {
                var configKey = $"llm-model-{model.ToString().ToLower()}";
                var config = await _configurationProvider.GetConfigurationWithCascadeAsync<Dictionary<string, object>>(
                    configKey, request.Context.TertiaryScope, request.Context.SecondaryScope, request.Context.PrimaryScope);
                
                if (config == null)
                {
                    response.MissingModels.Add(model.ToString());
                }
            }

            // Check required API keys
            foreach (var keyName in request.RequiredApiKeys)
            {
                var keyValue = await _apiKeyManager.GetKeyWithCascadeAsync(
                    keyName, request.Context.TertiaryScope, request.Context.SecondaryScope, request.Context.PrimaryScope);
                
                if (string.IsNullOrEmpty(keyValue))
                {
                    response.MissingApiKeys.Add(keyName);
                }
            }

            response.IsValid = response.MissingProviders.Count == 0 && 
                              response.MissingModels.Count == 0 && 
                              response.MissingApiKeys.Count == 0;

            response.ValidationDetails["totalChecked"] = request.RequiredProviders.Count + request.RequiredModels.Count + request.RequiredApiKeys.Count;
            response.ValidationDetails["totalMissing"] = response.MissingProviders.Count + response.MissingModels.Count + response.MissingApiKeys.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating configuration");
            response.ConfigurationErrors.Add(ex.Message);
            response.IsValid = false;
        }

        return response;
    }

    #endregion

    #region Helper Methods

    private ConfigurationScope GetConfigurationScope(ConfigurationContext context)
    {
        // Map context to ConfigurationScope based on which scopes are provided
        if (!string.IsNullOrEmpty(context.TertiaryScope))
            return ConfigurationScope.Workflow;
        if (!string.IsNullOrEmpty(context.SecondaryScope))
            return ConfigurationScope.Project;
        if (!string.IsNullOrEmpty(context.PrimaryScope))
            return ConfigurationScope.User;
        return ConfigurationScope.System;
    }

    private string GetScopeIdentifier(ConfigurationContext context)
    {
        // Return the most specific scope identifier available
        if (!string.IsNullOrEmpty(context.TertiaryScope))
            return context.TertiaryScope;
        if (!string.IsNullOrEmpty(context.SecondaryScope))
            return context.SecondaryScope;
        if (!string.IsNullOrEmpty(context.PrimaryScope))
            return context.PrimaryScope;
        return "system";
    }

    private static string MaskApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey) || apiKey.Length <= 8)
        {
            return "***";
        }

        var start = apiKey.Substring(0, 4);
        var end = apiKey.Substring(apiKey.Length - 4);
        return $"{start}***{end}";
    }

    #endregion
}
