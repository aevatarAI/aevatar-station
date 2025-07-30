using Orleans;

namespace Aevatar.Core.Abstractions.Communication;

/// <summary>
/// Interface for agents that can send response events.
/// Provides a contract for response handling that works with both point-to-point and broadcast communication.
/// </summary>
public interface IResponseHandler
{
    /// <summary>
    /// Sends a response event back to the originating agent.
    /// Implementation may vary: CoreGAgentBase uses SendEventToAgentAsync for point-to-point,
    /// while GAgentBase uses PublishAsync for broadcast.
    /// </summary>
    /// <param name="responseEvent">The response event to send</param>
    /// <param name="targetGrainId">The grain ID to send the response to</param>
    /// <returns>Task representing the async operation</returns>
    Task SendResponseAsync<T>(EventWrapper<T> responseEvent, GrainId targetGrainId) where T : EventBase;
} 

/// <summary>
/// Interface for agents that can send exception events.
/// Provides a contract for exception handling that works with both point-to-point and broadcast communication.
/// </summary>
public interface IExceptionHandler
{
    /// <summary>
    /// Sends an exception event back to the originating agent.
    /// Implementation may vary: CoreGAgentBase uses SendEventToAgentAsync for point-to-point,
    /// while GAgentBase uses PublishAsync for broadcast.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="responseEvent"></param>
    /// <param name="targetGrainId"></param>
    /// <returns></returns>
    Task SendExceptionAsync<T>(T responseEvent, GrainId targetGrainId) where T : EventBase;
} 