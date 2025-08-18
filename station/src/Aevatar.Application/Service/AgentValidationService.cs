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
    
    // Cache for agent type to config type mapping
    private readonly Dictionary<string, Type> _agentToConfigTypeCache = new();
    private readonly object _cacheLock = new();
    private bool _isInitialized = false;

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
        _logger.LogInformation("🔍 Starting validation for GAgent: {GAgentNamespace}", request.GAgentNamespace);
        
        // Validate request parameters
        if (string.IsNullOrWhiteSpace(request.GAgentNamespace))
        {
            return ConfigValidationResultDto.Failure(
                new[] { new ValidationErrorDto { PropertyName = nameof(request.GAgentNamespace), Message = "Complete GAgent Namespace is required" } },
                "Missing required GAgent Namespace");
        }

        if (string.IsNullOrWhiteSpace(request.ConfigJson))
        {
            return ConfigValidationResultDto.Failure(
                new[] { new ValidationErrorDto { PropertyName = nameof(request.ConfigJson), Message = "Configuration JSON is required" } },
                "Missing required Configuration JSON");
        }

        try
        {
            // Ensure cache is initialized
            await EnsureCacheInitializedAsync();

            // Find configuration type by GAgent namespace
            if (!_agentToConfigTypeCache.TryGetValue(request.GAgentNamespace, out var configType))
            {
                return ConfigValidationResultDto.Failure(
                    new[] { new ValidationErrorDto { PropertyName = nameof(request.GAgentNamespace), Message = $"Unknown GAgent type: {request.GAgentNamespace}" } },
                    $"GAgent type '{request.GAgentNamespace}' not found or no corresponding configuration type available");
            }

            // Validate configuration JSON
            return await ValidateConfigByTypeAsync(configType, request.ConfigJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error validating configuration for GAgent: {GAgentNamespace}", request.GAgentNamespace);
            return ConfigValidationResultDto.Failure(
                new[] { new ValidationErrorDto { PropertyName = "System", Message = "Internal validation error occurred" } },
                "An error occurred during validation");
        }
    }

    public async Task<List<string>> GetAvailableAgentTypesAsync()
    {
        await EnsureCacheInitializedAsync();
        return _agentToConfigTypeCache.Keys.ToList();
    }

    private async Task EnsureCacheInitializedAsync()
    {
        if (_isInitialized) return;

        lock (_cacheLock)
        {
            if (_isInitialized) return;

            try
            {
                _logger.LogInformation("🔍 Initializing Agent->Config type mapping...");
                InitializeAgentConfigMapping();
                _isInitialized = true;
                _logger.LogInformation("✅ Agent->Config mapping initialized successfully with {Count} mappings", _agentToConfigTypeCache.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to initialize Agent->Config type mapping");
                throw;
            }
        }
    }

    private void InitializeAgentConfigMapping()
    {
        var systemAgents = _agentOptions.CurrentValue.SystemAgentList;
        var availableGAgents = _gAgentManager.GetAvailableGAgentTypes();
        
        // Filter out Orleans generated types and system agents
        var validAgents = availableGAgents.Where(a => !a.Namespace.StartsWith("OrleansCodeGen")).ToList();
        var businessAgentTypes = validAgents.Where(a => !systemAgents.Contains(a.Name)).ToList();

        _logger.LogInformation("🔍 Found {TotalCount} available agents, filtered to {BusinessCount} business agents", 
            availableGAgents.Count(), businessAgentTypes.Count);

        foreach (var agentType in businessAgentTypes)
        {
            try
            {
                var configType = FindConfigTypeInAgentAssembly(agentType);
                if (configType != null)
                {
                    _agentToConfigTypeCache[agentType.FullName] = configType;
                    _logger.LogDebug("✅ Mapped: {AgentType} -> {ConfigType}", agentType.FullName, configType.FullName);
                }
                else
                {
                    _logger.LogWarning("⚠️  No config type found for agent: {AgentType}", agentType.FullName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️  Failed to process agent type {AgentType}: {ErrorMessage}", 
                    agentType.FullName, ex.Message);
            }
        }
    }

    private Type? FindConfigTypeInAgentAssembly(Type agentType)
    {
        var agentAssembly = agentType.Assembly;
        var expectedConfigTypeName = $"{agentType.FullName}Config";
        
        try
        {
            var configType = agentAssembly.GetTypes()
                .FirstOrDefault(type => 
                    type.FullName == expectedConfigTypeName &&
                    type.IsClass && 
                    !type.IsAbstract &&
                    typeof(IValidatableObject).IsAssignableFrom(type));
            
            return configType;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️  Error scanning assembly {AssemblyName}: {ErrorMessage}", 
                agentAssembly.GetName().Name, ex.Message);
            return null;
        }
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