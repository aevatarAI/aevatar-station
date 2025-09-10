// ABOUTME: This file implements the Schema Configuration Agent
// ABOUTME: Provides dynamic dropdown context from silo's SystemLLMConfigOptions

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Aevatar.Options;
using Aevatar.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Providers;

namespace Aevatar.Application.Grains.Agents.Configuration;

/// <summary>
/// Schema configuration grain that provides dynamic dropdown context from silo's SystemLLMConfigOptions
/// This grain runs in the silo and can access silo configuration directly
/// </summary>
[Description("Schema Configuration Agent for Dynamic Dropdown Context")]
public class SchemaConfigurationGAgent : GAgentBase<SchemaConfigurationGAgentState, SchemaConfigurationGEvent>, ISchemaConfigurationGAgent
{
    private readonly ILogger<SchemaConfigurationGAgent> _logger;

    public SchemaConfigurationGAgent(ILogger<SchemaConfigurationGAgent> logger)
    {
        _logger = logger;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "Schema Configuration Agent for providing dynamic dropdown context from silo's SystemLLMConfigOptions.");
    }

    /// <summary>
    /// Get dynamic dropdown context containing AI model configurations from silo's SystemLLMConfigOptions
    /// </summary>
    /// <returns>DynamicDropDownContext with AI model configurations</returns>
    public async Task<DynamicDropDownContext> GetSchemaContextAsync()
    {
        var configurationSource = "Silo";
        var aiModelConfigs = new List<SystemLLMConfigDto>();

        try
        {
            // Get SystemLLMConfigOptions from silo's service provider (similar to AIGAgentBase pattern)
            var systemLLMConfigOptions = ServiceProvider.GetRequiredService<IOptions<SystemLLMConfigOptions>>();
            
            if (systemLLMConfigOptions.Value.SystemLLMConfigs != null)
            {
                // Convert from Dictionary<string, LLMConfig> to List<SystemLLMConfigDto>
                foreach (var kvp in systemLLMConfigOptions.Value.SystemLLMConfigs)
                {
                    var config = kvp.Value;
                    var configDto = new SystemLLMConfigDto
                    {
                        Name = kvp.Key,
                        Provider = config.ProviderEnum.ToString(),
                        Type = config.ModelName,
                        // Map additional properties as needed
                        Strengths = new List<string> { $"Provider: {config.ProviderEnum}", $"Model: {config.ModelIdEnum}" },
                        BestFor = new List<string> { "AI chat functionality", "Model inference" },
                        Speed = "Variable" // Default value
                    };
                    aiModelConfigs.Add(configDto);
                }
            }

            // If no configurations found, provide default ones similar to SystemLLMMetaInfoOptions
            if (!aiModelConfigs.Any())
            {
                _logger.LogWarning("No SystemLLMConfigs found in silo configuration, using default configurations");
                aiModelConfigs = GetDefaultSystemLLMConfigs();
                configurationSource = "Default";
            }

            _logger.LogInformation("Retrieved schema context with {ConfigCount} AI model configurations from {Source}", 
                aiModelConfigs.Count, configurationSource);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve schema context from silo configuration");
            
            // Use default context as fallback
            aiModelConfigs = GetDefaultSystemLLMConfigs();
            configurationSource = "Fallback";
        }

        // Raise event to track schema context retrieval
        var retrievalEvent = new SchemaContextRetrievedGEvent
        {
            Ctime = DateTime.UtcNow,
            ConfigurationCount = aiModelConfigs.Count,
            ConfigurationSource = configurationSource
        };

        RaiseEvent(retrievalEvent);
        await ConfirmEvents();

        var context = new DynamicDropDownContext
        {
            AIModelConfigs = aiModelConfigs
        };

        return context;
    }

    /// <summary>
    /// Provides default AI model configurations as fallback
    /// </summary>
    private static List<SystemLLMConfigDto> GetDefaultSystemLLMConfigs()
    {
        return new List<SystemLLMConfigDto>
        {
            new()
            {
                Name = "OpenAI",
                Provider = "OpenAI",
                Type = "GPT-4",
                Strengths = new List<string> { "Multi-modal capabilities", "Advanced reasoning", "Code generation" },
                BestFor = new List<string> { "Complex conversations", "Programming tasks", "Creative writing" },
                Speed = "Fast"
            },
            new()
            {
                Name = "Azure",
                Provider = "Azure",
                Type = "Azure OpenAI",
                Strengths = new List<string> { "Enterprise security", "Compliance", "Integration with Azure services" },
                BestFor = new List<string> { "Enterprise applications", "Secure environments", "Azure ecosystem" },
                Speed = "Fast"
            }
        };
    }
}
