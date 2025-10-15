namespace Aevatar.Core.Abstractions.Communication;

/// <summary>
/// Interface for layered communication functionality with broadcasting-enhanced methods.
/// Handles event publishing, forwarding, and stream management for parent-child agent communication.
/// </summary>
public interface ILayeredCommunication
{
    /// <summary>
    /// Publishes an event directly based on its direction property using stream-based broadcasting
    /// </summary>
    /// <typeparam name="T">The event type</typeparam>
    /// <param name="event">The event to publish</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task PublishEventByDirectionAsync<T>(T @event) where T : EventBase;
}
