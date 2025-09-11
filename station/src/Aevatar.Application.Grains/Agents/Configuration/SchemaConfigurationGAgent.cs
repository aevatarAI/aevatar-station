// ABOUTME: This file implements the Schema Configuration Agent
// ABOUTME: Provides SystemLLMConfigOptions from silo configuration (decoupled from presentation layer)
// ABOUTME: Contains all related types: interface, state, events, and implementation

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Providers;

namespace Aevatar.Application.Grains.Agents.Configuration;

/// <summary>
/// Schema configuration grain interface for providing configuration options from silo
/// </summary>
public interface ISchemaConfigurationGAgent : IStateGAgent<SchemaConfigurationGAgentState>, IGrainWithStringKey
{
    /// <summary>
    /// Get configuration options from silo configuration using generic type
    /// </summary>
    /// <typeparam name="T">The configuration type to retrieve</typeparam>
    /// <returns>Configuration options of type T</returns>
    Task<T> GetConfigOptionsAsync<T>() where T : class;

    /// <summary>
    /// Get SystemLLM configuration options from silo configuration
    /// </summary>
    /// <returns>SystemLLMConfigOptions containing AI model configurations</returns>
    Task<SystemLLMConfigOptions> GetSystemLLMConfigOptionsAsync();
}

/// <summary>
/// State for Schema Configuration Agent
/// Simple state for configuration reading agent - minimal state required
/// </summary>
[GenerateSerializer]
public class SchemaConfigurationGAgentState : StateBase
{
    // This agent only reads configuration, no complex state needed
}

/// <summary>
/// Minimal event class for SchemaConfigurationGAgent - required by GAgentBase but unused
/// </summary>
[GenerateSerializer]
public abstract class SchemaConfigurationGEvent : StateLogEventBase<SchemaConfigurationGEvent>
{
    [Id(0)] public override Guid Id { get; set; } = Guid.NewGuid();
}


/// <summary>
/// Schema configuration grain that provides SystemLLMConfigOptions from silo configuration
/// This grain runs in the silo and can access silo configuration directly
/// </summary>
[Description("Schema Configuration Agent for Options")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
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
            "Schema Configuration Agent for providing SystemLLMConfigOptions from silo configuration.");
    }

    /// <summary>
    /// Get configuration options from silo configuration using generic type
    /// </summary>
    /// <typeparam name="T">The configuration type to retrieve</typeparam>
    /// <returns>Configuration options of type T</returns>
    public Task<T> GetConfigOptionsAsync<T>() where T : class
    {
        try
        {
            // Get configuration options from silo's service provider using generic type
            var configOptions = ServiceProvider.GetRequiredService<IOptions<T>>();
            
            _logger.LogInformation("Retrieved configuration options of type {ConfigType} from silo", typeof(T).Name);

            return Task.FromResult(configOptions.Value);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve configuration options of type {ConfigType} from silo configuration", typeof(T).Name);
            
            // Return default instance as fallback
            var defaultInstance = Activator.CreateInstance<T>();
            _logger.LogWarning("Returning default instance of {ConfigType} as fallback", typeof(T).Name);
            return Task.FromResult(defaultInstance);
        }
    }

    /// <summary>
    /// Get SystemLLM configuration options from silo configuration
    /// </summary>
    /// <returns>SystemLLMConfigOptions containing AI model configurations</returns>
    public Task<SystemLLMConfigOptions> GetSystemLLMConfigOptionsAsync()
    {
        // Use the generic method for backward compatibility
        return GetConfigOptionsAsync<SystemLLMConfigOptions>();
    }

}
