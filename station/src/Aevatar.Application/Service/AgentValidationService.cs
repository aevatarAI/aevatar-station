using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Aevatar.AgentValidation;
using Aevatar.Core.Abstractions;
using Aevatar.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Runtime;
using Orleans.Metadata;
using Volo.Abp.Application.Services;
using Volo.Abp;

namespace Aevatar.Service;

[RemoteService(IsEnabled = false)]
public class AgentValidationService : ApplicationService, IAgentValidationService
{
    private readonly ILogger<AgentValidationService> _logger;
    private readonly IGAgentFactory _gAgentFactory;
    private readonly IGAgentManager _gAgentManager;
    private readonly IOptionsMonitor<AgentOptions> _agentOptions;
    private readonly GrainTypeResolver _grainTypeResolver;

    public AgentValidationService(
        ILogger<AgentValidationService> logger,
        IGAgentFactory gAgentFactory,
        IGAgentManager gAgentManager,
        IOptionsMonitor<AgentOptions> agentOptions,
        GrainTypeResolver grainTypeResolver)
    {
        _logger = logger;
        _gAgentFactory = gAgentFactory;
        _gAgentManager = gAgentManager;
        _agentOptions = agentOptions;
        _grainTypeResolver = grainTypeResolver;
    }

    public async Task<ConfigValidationResultDto> ValidateConfigAsync(ValidationRequestDto request)
    {
        try
        {
            _logger.LogInformation("Starting validation for GAgent: {GAgentNamespace}", 
                request?.GAgentNamespace ?? "null");
            
            // Handle null request - this is now handled in service layer
            if (request == null)
            {
                _logger.LogWarning("Received null validation request");
                return ConfigValidationResultDto.Failure(
                    new[] { new ValidationErrorDto { PropertyName = "Request", Message = "Request body is required" } },
                    "Invalid request: Request body cannot be null");
            }
            
            // Validate request parameters
            if (string.IsNullOrWhiteSpace(request.GAgentNamespace))
            {
                _logger.LogWarning("Missing GAgent namespace in validation request");
                return ConfigValidationResultDto.Failure(
                    new[] { new ValidationErrorDto { PropertyName = nameof(request.GAgentNamespace), Message = "Complete GAgent Namespace is required" } },
                    "Missing required GAgent Namespace");
            }

            if (string.IsNullOrWhiteSpace(request.ConfigJson))
            {
                _logger.LogWarning("Missing configuration JSON in validation request");
                return ConfigValidationResultDto.Failure(
                    new[] { new ValidationErrorDto { PropertyName = nameof(request.ConfigJson), Message = "Configuration JSON is required" } },
                    "Missing required Configuration JSON");
            }

            // Find configuration type by GAgent namespace (real-time lookup)
            var configType = FindConfigTypeByAgentNamespace(request.GAgentNamespace);
            if (configType == null)
            {
                _logger.LogWarning("Unknown GAgent type: {GAgentNamespace}", request.GAgentNamespace);
                return ConfigValidationResultDto.Failure(
                    new[] { new ValidationErrorDto { PropertyName = nameof(request.GAgentNamespace), Message = $"Unknown GAgent type: {request.GAgentNamespace}" } },
                    $"GAgent type '{request.GAgentNamespace}' not found or no corresponding configuration type available");
            }

            // Validate configuration JSON
            var result = await ValidateConfigByTypeAsync(configType, request.ConfigJson);
            
            _logger.LogInformation("Validation completed for GAgent: {GAgentNamespace}, IsValid: {IsValid}", 
                request.GAgentNamespace, result.IsValid);
                
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during validation for GAgent: {GAgentNamespace}", 
                request?.GAgentNamespace ?? "unknown");
            return ConfigValidationResultDto.Failure(
                new[] { new ValidationErrorDto { PropertyName = "System", Message = "Internal server error" } },
                "An unexpected error occurred during validation");
        }
    }

    private Type? FindConfigTypeByAgentNamespace(string agentNamespace)
    {
        _logger.LogDebug("Finding config type for agent: {AgentNamespace}", agentNamespace);
        
        var availableGAgents = _gAgentManager.GetAvailableGAgentTypes();
        
        // Find the specific agent type by namespace
        var agentType = availableGAgents.FirstOrDefault(a => a.FullName == agentNamespace);
        if (agentType == null)
        {
            _logger.LogWarning("Agent type not found: {AgentNamespace}", agentNamespace);
            return null;
        }
        
        // Skip Orleans generated types only
        if (agentType.Namespace.StartsWith("OrleansCodeGen"))
        {
            _logger.LogWarning("Agent type is Orleans generated: {AgentNamespace}", agentNamespace);
            return null;
        }
        
        try
        {
            var configType = FindConfigTypeInAgentAssembly(agentType);
            if (configType != null)
            {
                _logger.LogDebug("Found config type: {AgentType} -> {ConfigType}", agentType.FullName, configType.FullName);
            }
            else
            {
                _logger.LogWarning("No config type found for agent: {AgentType}", agentType.FullName);
            }
            return configType;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to find config type for agent {AgentType}: {ErrorMessage}", 
                agentType.FullName, ex.Message);
            return null;
        }
    }

    private Type? FindConfigTypeInAgentAssembly(Type agentType)
    {
        try
        {
            // Try to get configuration type from GAgent generic parameters
            var configType = GetConfigurationTypeFromGAgent(agentType);
            if (configType != null)
            {
                _logger.LogDebug("Found config type from GAgent generics: {ConfigType}", configType.FullName);
                return configType;
            }

            // Fallback: find ConfigurationBase-derived types in same assembly
            var configTypes = agentType.Assembly.GetTypes()
                .Where(type => type.IsClass && 
                              !type.IsAbstract && 
                              IsConfigurationBase(type))
                .ToList();

            if (configTypes.Count == 1)
            {
                _logger.LogDebug("Found single config type in assembly: {ConfigType}", configTypes[0].FullName);
                return configTypes[0];
            }

            _logger.LogDebug("Found {Count} potential config types for agent {AgentType}", 
                configTypes.Count, agentType.FullName);
            return configTypes.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error finding config type for agent {AgentType}: {ErrorMessage}", 
                agentType.FullName, ex.Message);
            return null;
        }
    }

    private Type? GetConfigurationTypeFromGAgent(Type agentType)
    {
        // Walk up the inheritance chain to find GAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>
        var currentType = agentType;
        while (currentType != null)
        {
            if (currentType.IsGenericType && 
                currentType.GetGenericTypeDefinition().Name.StartsWith("GAgentBase") &&
                currentType.GenericTypeArguments.Length >= 4)
            {
                var configurationType = currentType.GenericTypeArguments[3]; // TConfiguration is 4th parameter
                if (IsConfigurationBase(configurationType))
                {
                    return configurationType;
                }
            }
            currentType = currentType.BaseType;
        }
        return null;
    }

    private bool IsConfigurationBase(Type type)
    {
        if (type == null) return false;
        
        // Walk up the inheritance chain to find ConfigurationBase
        var currentType = type.BaseType;
        while (currentType != null)
        {
            if (currentType.Name == "ConfigurationBase")
            {
                return true;
            }
            currentType = currentType.BaseType;
        }
        
        return false;
    }

    private async Task<ConfigValidationResultDto> ValidateConfigByTypeAsync(Type configType, string configJson)
    {
        try
        {
            // JSON deserialzation with case-insensitive options
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var config = JsonSerializer.Deserialize(configJson, configType, options);
            if (config == null)
            {
                return ConfigValidationResultDto.Failure(
                    new[] { new ValidationErrorDto { PropertyName = "ConfigJson", Message = "Failed to deserialize configuration JSON" } },
                    "Invalid JSON format");
            }

            // DataAnnotations validation
            var validationContext = new ValidationContext(config);
            var validationResults = new List<ValidationResult>();
            
            var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(config, validationContext, validationResults, validateAllProperties: true);

            // IValidatableObject custom validation
            if (config is IValidatableObject validatableConfig)
            {
                var customResults = validatableConfig.Validate(validationContext);
                validationResults.AddRange(customResults);
            }

            // Convert validation results to DTOs
            var errors = validationResults.Select(vr => new ValidationErrorDto
            {
                PropertyName = vr.MemberNames.FirstOrDefault() ?? "Unknown",
                Message = vr.ErrorMessage ?? "Validation error"
            }).ToList();

            return errors.Any() 
                ? ConfigValidationResultDto.Failure(errors, "Configuration validation failed")
                : ConfigValidationResultDto.Success("Configuration validation passed");
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "JSON deserialization error for config type {ConfigType}", configType.Name);
            return ConfigValidationResultDto.Failure(
                new[] { new ValidationErrorDto { PropertyName = "ConfigJson", Message = "Invalid JSON format: " + ex.Message } },
                "JSON format error");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during validation for config type {ConfigType}", configType.Name);
            return ConfigValidationResultDto.Failure(
                new[] { new ValidationErrorDto { PropertyName = "System", Message = "Validation system error" } },
                "System validation error");
        }
    }
}