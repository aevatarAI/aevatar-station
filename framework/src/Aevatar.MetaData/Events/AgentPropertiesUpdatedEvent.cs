// ABOUTME: This file defines the AgentPropertiesUpdatedEvent for agent metadata tracking
// ABOUTME: Captures changes to agent properties and configuration

namespace Aevatar.MetaData.Events;

/// <summary>
/// Event raised when an agent's properties are updated.
/// </summary>
[GenerateSerializer]
public class AgentPropertiesUpdatedEvent : MetaDataStateLogEvent
{
    /// <summary>
    /// The properties that were updated.
    /// </summary>
    [Id(40)]
    public Dictionary<string, string> UpdatedProperties { get; set; } = new();
    
    /// <summary>
    /// The properties that were removed (if any).
    /// </summary>
    [Id(41)]
    public List<string> RemovedProperties { get; set; } = new();
    
    /// <summary>
    /// Whether the properties were merged (true) or replaced (false).
    /// </summary>
    [Id(42)]
    public bool WasMerged { get; set; }
    
    /// <summary>
    /// The timestamp when the properties were updated.
    /// </summary>
    [Id(43)]
    public DateTime UpdateTime { get; set; } = DateTime.UtcNow;
}