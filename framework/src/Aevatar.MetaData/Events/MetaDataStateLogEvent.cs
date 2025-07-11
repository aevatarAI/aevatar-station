// ABOUTME: This file defines the base event class for metadata state changes
// ABOUTME: Provides foundation for all metadata-related events in the system

using Aevatar.Core.Abstractions;

namespace Aevatar.MetaData.Events;

/// <summary>
/// Base class for all metadata state log events.
/// Inherits from StateLogEventBase to integrate with Orleans event sourcing.
/// </summary>
[GenerateSerializer]
public abstract class MetaDataStateLogEvent : StateLogEventBase<MetaDataStateLogEvent>
{
    /// <summary>
    /// The ID of the agent that triggered this event.
    /// </summary>
    [Id(10)]
    public Guid AgentId { get; set; }
    
    /// <summary>
    /// The user ID associated with the agent.
    /// </summary>
    [Id(11)]
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Optional reason or description for the event.
    /// </summary>
    [Id(12)]
    public string? Reason { get; set; }
}