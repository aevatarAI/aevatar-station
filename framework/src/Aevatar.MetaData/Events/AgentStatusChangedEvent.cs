// ABOUTME: This file defines the AgentStatusChangedEvent for agent lifecycle tracking
// ABOUTME: Captures status transitions during agent operation

using Aevatar.MetaData.Enums;

namespace Aevatar.MetaData.Events;

/// <summary>
/// Event raised when an agent's status changes.
/// </summary>
[GenerateSerializer]
public class AgentStatusChangedEvent : MetaDataStateLogEvent
{
    /// <summary>
    /// The previous status of the agent.
    /// </summary>
    [Id(30)]
    public AgentStatus OldStatus { get; set; }
    
    /// <summary>
    /// The new status of the agent.
    /// </summary>
    [Id(31)]
    public AgentStatus NewStatus { get; set; }
    
    /// <summary>
    /// The timestamp when the status change occurred.
    /// </summary>
    [Id(32)]
    public DateTime StatusChangeTime { get; set; } = DateTime.UtcNow;
}