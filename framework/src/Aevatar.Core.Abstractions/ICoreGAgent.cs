namespace Aevatar.Core.Abstractions;

/// <summary>
/// Interface for core agent functionality without layered communication.
/// Provides basic agent operations like activation, configuration, and event subscription discovery.
/// </summary>
public interface ICoreGAgent : IGrainWithGuidKey
{
    /// <summary>
    /// Activates the agent and initializes its core functionality.
    /// </summary>
    /// <returns>Task representing the async operation</returns>
    Task ActivateAsync();

    /// <summary>
    /// Get GAgent description.
    /// </summary>
    /// <returns>A descriptive string about this agent</returns>
    Task<string> GetDescriptionAsync();

    /// <summary>
    /// Get all subscribed events of current GAgent.
    /// </summary>
    /// <param name="includeBaseHandlers">Whether to include base handlers</param>
    /// <returns>List of event types this agent can handle</returns>
    Task<List<Type>?> GetAllSubscribedEventsAsync(bool includeBaseHandlers = false);

    /// <summary>
    /// Get the type of GAgent initialization event.
    /// </summary>
    /// <returns>The type of configuration this agent uses</returns>
    Task<Type?> GetConfigurationTypeAsync();

    /// <summary>
    /// Configures the agent with the provided configuration.
    /// </summary>
    /// <param name="configuration">The configuration object to apply</param>
    /// <returns>Task representing the async operation</returns>
    Task ConfigAsync(ConfigurationBase configuration);
} 