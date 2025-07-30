using Orleans.Streams;

namespace Aevatar.Core.Abstractions.Communication;

/// <summary>
/// Interface for layered communication functionality with broadcasting-enhanced methods.
/// Handles event publishing, forwarding, and stream management for parent-child agent communication.
/// </summary>
public interface ILayeredCommunication
{
    /// <summary>
    /// Publishes an event and returns the event ID. Uses internal State.Parent and State.Children for routing.
    /// </summary>
    /// <typeparam name="T">Event type that inherits from EventBase</typeparam>
    /// <param name="event">The event to publish</param>
    /// <returns>Event ID of the published event</returns>
    Task<Guid> PublishAsync<T>(T @event) where T : EventBase;

    /// <summary>
    /// Publishes an event wrapper using internal State.Parent and State.Children for routing.
    /// </summary>
    /// <typeparam name="T">Event type that inherits from EventBase</typeparam>
    /// <param name="eventWrapper">The event wrapper to publish</param>
    /// <returns>Task representing the async operation</returns>
    Task PublishAsync<T>(EventWrapper<T> eventWrapper) where T : EventBase;

    /// <summary>
    /// Publishes an event upwards to parent with a specific event ID.
    /// </summary>
    /// <typeparam name="T">Event type that inherits from EventBase</typeparam>
    /// <param name="event">The event to publish</param>
    /// <param name="eventId">The event ID</param>
    /// <returns>Task representing the async operation</returns>
    Task PublishEventUpwardsAsync<T>(T @event, Guid eventId) where T : EventBase;

    /// <summary>
    /// Sends an event upwards to the parent agent.
    /// </summary>
    /// <typeparam name="T">Event type that inherits from EventBase</typeparam>
    /// <param name="eventWrapper">The event wrapper to send</param>
    /// <returns>Task representing the async operation</returns>
    Task SendEventUpwardsAsync<T>(EventWrapper<T> eventWrapper) where T : EventBase;

    /// <summary>
    /// Sends an event downwards to children agents using broadcasting.
    /// </summary>
    /// <typeparam name="T">Event type that inherits from EventBase</typeparam>
    /// <param name="eventWrapper">The event wrapper to send</param>
    /// <returns>Task representing the async operation</returns>
    Task SendEventDownwardsAsync<T>(EventWrapper<T> eventWrapper) where T : EventBase;

    /// <summary>
    /// Sends an event to the same agent (self).
    /// </summary>
    /// <typeparam name="T">Event type that inherits from EventBase</typeparam>
    /// <param name="eventWrapper">The event wrapper to send</param>
    /// <returns>Task representing the async operation</returns>
    Task SendEventToSelfAsync<T>(EventWrapper<T> eventWrapper) where T : EventBase;

    /// <summary>
    /// Forwards an event to specified children using broadcasting.
    /// </summary>
    /// <param name="eventWrapper">The event wrapper to forward</param>
    /// <param name="childrenIds">List of children IDs to forward to</param>
    /// <returns>Task representing the async operation</returns>
    Task ForwardEventAsync(EventWrapperBase eventWrapper, List<GrainId> childrenIds);
} 