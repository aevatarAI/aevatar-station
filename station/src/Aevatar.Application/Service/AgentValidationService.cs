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
    public AgentValidationService(ILogger<AgentValidationService> logger, IGAgentManager gAgentManager, ISchemaProvider schemaProvider)
    {
        _logger = logger;
        _gAgentManager = gAgentManager;
        _schemaProvider = schemaProvider;
    }
    public async Task<ConfigValidationResultDto> ValidateConfigAsync(ValidationRequestDto request)
    {
        try
        {
            _logger.LogInformation("Validating {GAgentNamespace}", request?.GAgentNamespace ?? "null");
            if (request == null)
            {
                _logger.LogWarning("Null validation request");
                return ConfigValidationResultDto.Failure(
                    new[] { new ValidationErrorDto { PropertyName = "Request", Message = "Request body is required" } },
                    "Invalid request: Request body cannot be null");
            }
            if (string.IsNullOrWhiteSpace(request.GAgentNamespace))
            {
                _logger.LogWarning("Missing GAgent namespace in validation request");
                return ConfigValidationResultDto.Failure(
                    new[] { new ValidationErrorDto { PropertyName = nameof(request.GAgentNamespace), Message = "Complete GAgent Namespace is required" } },
                    "Missing required GAgent Namespace");
            }
            if (string.IsNullOrWhiteSpace(request.ConfigJson))
            {
                _logger.LogWarning("Missing configuration JSON");
                return ConfigValidationResultDto.Failure(
                    new[] { new ValidationErrorDto { PropertyName = nameof(request.ConfigJson), Message = "Configuration JSON is required" } },
                    "Missing required Configuration JSON");
            }
            var configType = FindConfigTypeByAgentNamespace(request.GAgentNamespace);
            if (configType == null)
            {
                _logger.LogWarning("Unknown GAgent type: {GAgentNamespace}", request.GAgentNamespace);
                return ConfigValidationResultDto.Failure(
                    new[] { new ValidationErrorDto { PropertyName = nameof(request.GAgentNamespace), Message = $"Unknown GAgent type: {request.GAgentNamespace}" } },
                    $"GAgent type '{request.GAgentNamespace}' not found or no corresponding configuration type available");
            }
            var result = await ValidateConfigByTypeAsync(configType, request.ConfigJson);
            _logger.LogInformation("Validation completed: {GAgentNamespace}, IsValid: {IsValid}", request.GAgentNamespace, result.IsValid);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation error for {GAgentNamespace}", request?.GAgentNamespace ?? "unknown");
            return ConfigValidationResultDto.Failure(
                new[] { new ValidationErrorDto { PropertyName = "System", Message = "Internal server error" } },
                "An unexpected error occurred during validation");
        }
    }
    private Type? FindConfigTypeByAgentNamespace(string agentNamespace)
    {
        var availableGAgents = _gAgentManager.GetAvailableGAgentTypes();
        var agentType = availableGAgents.FirstOrDefault(a => a.FullName == agentNamespace);
        if (agentType == null)
        {
            _logger.LogWarning("Agent type not found: {AgentNamespace}", agentNamespace);
            return null;
        }
        try { return FindConfigTypeInAgentAssembly(agentType); }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed to find config type for agent {AgentType}", agentType.FullName); return null; }
    }
    private Type? FindConfigTypeInAgentAssembly(Type agentType)
    {
        var configType = GetConfigurationTypeFromGAgent(agentType);
        if (configType != null) return configType;
        var configTypes = agentType.Assembly.GetTypes().Where(type => type.IsClass && !type.IsAbstract && IsConfigurationBase(type)).ToList();
        return configTypes.FirstOrDefault();
    }
    private Type? GetConfigurationTypeFromGAgent(Type agentType)
    {
        var currentType = agentType;
        while (currentType != null)
        {
            if (currentType.IsGenericType && currentType.GetGenericTypeDefinition().Name.StartsWith("GAgentBase") && currentType.GenericTypeArguments.Length >= 4)
            {
                var configurationType = currentType.GenericTypeArguments[3];
                if (IsConfigurationBase(configurationType)) return configurationType;
            }
            currentType = currentType.BaseType;
        }
        return null;
    }
    private bool IsConfigurationBase(Type type)
    {
        if (type == null) return false;
        var currentType = type.BaseType;
        while (currentType != null)
        {
            if (currentType.Name == "ConfigurationBase") return true;
            currentType = currentType.BaseType;
        }
        return false;
    }
    private async Task<ConfigValidationResultDto> ValidateConfigByTypeAsync(Type configType, string configJson)
    {
        try
        {
            var schema = _schemaProvider.GetTypeSchema(configType);
            var validationErrors = schema.Validate(configJson);
            if (validationErrors.Any())
            {
                var errorDict = _schemaProvider.ConvertValidateError(validationErrors);
                var errors = errorDict.Select(kvp => new ValidationErrorDto { PropertyName = kvp.Key, Message = kvp.Value }).ToList();
                return ConfigValidationResultDto.Failure(errors, "Configuration schema validation failed");
            }
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var config = JsonSerializer.Deserialize(configJson, configType, options);
                if (config != null)
                {
                    var validationContext = new ValidationContext(config);
                    var validationResults = new List<ValidationResult>();
                    System.ComponentModel.DataAnnotations.Validator.TryValidateObject(config, validationContext, validationResults, validateAllProperties: true);
                    if (config is IValidatableObject validatableConfig)
                    {
                        var customResults = validatableConfig.Validate(validationContext);
                        validationResults.AddRange(customResults);
                    }
                    if (validationResults.Any())
                    {
                        var additionalErrors = validationResults.Select(vr => new ValidationErrorDto { PropertyName = vr.MemberNames.FirstOrDefault() ?? "Unknown", Message = vr.ErrorMessage ?? "Validation error" }).ToList();
                        return ConfigValidationResultDto.Failure(additionalErrors, "Configuration validation failed");
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "JSON error for {ConfigType}", configType.Name);
                return ConfigValidationResultDto.Failure(new[] { new ValidationErrorDto { PropertyName = "ConfigJson", Message = "Invalid JSON format: " + ex.Message } }, "JSON format error");
            }
            return ConfigValidationResultDto.Success("Configuration validation passed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation error for {ConfigType}", configType.Name);
            return ConfigValidationResultDto.Failure(new[] { new ValidationErrorDto { PropertyName = "System", Message = "Schema validation system error" } }, "System validation error");
        }
    }
}