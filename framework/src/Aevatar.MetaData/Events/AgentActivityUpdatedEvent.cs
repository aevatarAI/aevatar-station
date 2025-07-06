// ABOUTME: This file defines the AgentActivityUpdatedEvent for agent activity tracking
// ABOUTME: Captures activity updates and heartbeat-style events

namespace Aevatar.MetaData.Events;

/// <summary>
/// Event raised when an agent's activity is updated.
/// </summary>
[GenerateSerializer]
public class AgentActivityUpdatedEvent : MetaDataStateLogEvent
{
    /// <summary>
    /// The type of activity that occurred.
    /// </summary>
    [Id(50)]
    public string ActivityType { get; set; } = string.Empty;
    
    /// <summary>
    /// The timestamp when the activity occurred.
    /// </summary>
    [Id(51)]
    public DateTime ActivityTime { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Optional details about the activity.
    /// </summary>
    [Id(52)]
    public string? ActivityDetails { get; set; }
}