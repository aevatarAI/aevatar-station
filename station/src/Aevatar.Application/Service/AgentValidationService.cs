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

    public AgentValidationService(ILogger<AgentValidationService> logger, IGAgentManager gAgentManager,
        ISchemaProvider schemaProvider)
    {
        _logger = logger;
        _gAgentManager = gAgentManager;
        _schemaProvider = schemaProvider;
    }

    public async Task<ConfigValidationResultDto> ValidateConfigAsync(ValidationRequestDto request)
    {
        _logger.LogInformation("Validating {GAgentNamespace}", request.GAgentNamespace);

        var configType = FindConfigTypeByAgentNamespace(request.GAgentNamespace);
        if (configType == null)
        {
            _logger.LogWarning("Unknown GAgent type: {GAgentNamespace}", request.GAgentNamespace);
            return ConfigValidationResultDto.Failure();
        }

        var result = await ValidateConfigByTypeAsync(configType, request.ConfigJson);
        _logger.LogInformation("Validation completed: {GAgentNamespace}, IsValid: {IsValid}", request.GAgentNamespace,
            result.IsValid);
        return result;
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

        return FindConfigTypeInAgentAssembly(agentType);
    }

    private Type? FindConfigTypeInAgentAssembly(Type agentType)
    {
        var configType = GetConfigurationTypeFromGAgent(agentType);
        if (configType != null) return configType;
        var configTypes = agentType.Assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && IsConfigurationBase(type)).ToList();
        return configTypes.FirstOrDefault();
    }

    private Type? GetConfigurationTypeFromGAgent(Type agentType)
    {
        var currentType = agentType;
        while (currentType != null)
        {
            if (currentType.IsGenericType && currentType.GetGenericTypeDefinition().Name.StartsWith("GAgentBase") &&
                currentType.GenericTypeArguments.Length >= 4)
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
                _logger.LogInformation("Schema validation failed for {ConfigType}", configType.Name);
                return ConfigValidationResultDto.Failure();
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var config = JsonSerializer.Deserialize(configJson, configType, options);

            if (config == null) return ConfigValidationResultDto.Failure();

            var validationContext = new ValidationContext(config);
            if (config is not IValidatableObject validatableConfig)
                return ConfigValidationResultDto.Success("Configuration validation passed");
            var customResults = validatableConfig.Validate(validationContext).ToList();
            return !customResults.Any() ? ConfigValidationResultDto.Success("Configuration validation passed") : ConfigValidationResultDto.Failure();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error during config validation for {ConfigType}", configType.Name);
            return ConfigValidationResultDto.Failure();
        }
    }
}