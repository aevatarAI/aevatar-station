namespace Aevatar.Core.Abstractions;

/// <summary>
/// Interface for core agent functionality without layered communication.
/// Provides basic agent operations like activation, configuration, and event subscription discovery.
/// </summary>
public interface IGAgentBase : IGrainWithGuidKey, IActivatable, IConfigurable
{
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
} 