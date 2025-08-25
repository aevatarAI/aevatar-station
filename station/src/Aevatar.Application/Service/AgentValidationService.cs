using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Aevatar.AgentValidation;
using Aevatar.Core.Abstractions;
using Aevatar.Schema;
using Microsoft.Extensions.Logging;
using Volo.Abp.Application.Services;
using Volo.Abp;

namespace Aevatar.Service;

[RemoteService(IsEnabled = false)]
public class AgentValidationService : ApplicationService, IAgentValidationService
{
    private readonly ILogger<AgentValidationService> _logger;
    private readonly IGAgentManager _gAgentManager;
    private readonly ISchemaProvider _schemaProvider;

    public AgentValidationService(
        ILogger<AgentValidationService> logger,
        IGAgentManager gAgentManager,
        ISchemaProvider schemaProvider)
    {
        _logger = logger;
        _gAgentManager = gAgentManager;
        _schemaProvider = schemaProvider;
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
        var availableGAgents = _gAgentManager.GetAvailableGAgentTypes();
        
        // Find the specific agent type by namespace
        var agentType = availableGAgents.FirstOrDefault(a => a.FullName == agentNamespace);
        if (agentType == null)
        {
            _logger.LogWarning("Agent type not found: {AgentNamespace}", agentNamespace);
            return null;
        }
        
        try
        {
            return FindConfigTypeInAgentAssembly(agentType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to find config type for agent {AgentType}", agentType.FullName);
            return null;
        }
    }

    private Type? FindConfigTypeInAgentAssembly(Type agentType)
    {
        // Try to get configuration type from GAgent generic parameters
        var configType = GetConfigurationTypeFromGAgent(agentType);
        if (configType != null)
        {
            return configType;
        }

        // Fallback: find ConfigurationBase-derived types in same assembly
        var configTypes = agentType.Assembly.GetTypes()
            .Where(type => type.IsClass && 
                          !type.IsAbstract && 
                          IsConfigurationBase(type))
            .ToList();

        return configTypes.FirstOrDefault();
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
            // Get JSON Schema for the configuration type using SchemaProvider
            var schema = _schemaProvider.GetTypeSchema(configType);
            
            // Validate JSON against schema
            var validationErrors = schema.Validate(configJson);
            
            if (validationErrors.Any())
            {
                // Convert schema validation errors to our DTO format
                var errorDict = _schemaProvider.ConvertValidateError(validationErrors);
                var errors = errorDict.Select(kvp => new ValidationErrorDto
                {
                    PropertyName = kvp.Key,
                    Message = kvp.Value
                }).ToList();
                
                return ConfigValidationResultDto.Failure(errors, "Configuration schema validation failed");
            }

            // Additional DataAnnotations validation if needed
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                var config = JsonSerializer.Deserialize(configJson, configType, options);
                if (config != null)
                {
                    var validationContext = new ValidationContext(config);
                    var validationResults = new List<ValidationResult>();
                    
                    System.ComponentModel.DataAnnotations.Validator.TryValidateObject(config, validationContext, validationResults, validateAllProperties: true);

                    // IValidatableObject custom validation
                    if (config is IValidatableObject validatableConfig)
                    {
                        var customResults = validatableConfig.Validate(validationContext);
                        validationResults.AddRange(customResults);
                    }

                    if (validationResults.Any())
                    {
                        var additionalErrors = validationResults.Select(vr => new ValidationErrorDto
                        {
                            PropertyName = vr.MemberNames.FirstOrDefault() ?? "Unknown",
                            Message = vr.ErrorMessage ?? "Validation error"
                        }).ToList();
                        
                        return ConfigValidationResultDto.Failure(additionalErrors, "Configuration validation failed");
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "JSON deserialization error during validation for {ConfigType}", configType.Name);
                return ConfigValidationResultDto.Failure(
                    new[] { new ValidationErrorDto { PropertyName = "ConfigJson", Message = "Invalid JSON format: " + ex.Message } },
                    "JSON format error");
            }

            return ConfigValidationResultDto.Success("Configuration validation passed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during validation for config type {ConfigType}", configType.Name);
            return ConfigValidationResultDto.Failure(
                new[] { new ValidationErrorDto { PropertyName = "System", Message = "Schema validation system error" } },
                "System validation error");
        }
    }
}