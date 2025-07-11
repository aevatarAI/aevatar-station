// ABOUTME: This file defines the AgentCreatedEvent for agent creation tracking
// ABOUTME: Captures initial agent metadata when an agent is first created

using Aevatar.MetaData.Enums;

namespace Aevatar.MetaData.Events;

/// <summary>
/// Event raised when a new agent is created in the system.
/// </summary>
[GenerateSerializer]
public class AgentCreatedEvent : MetaDataStateLogEvent
{
    /// <summary>
    /// The name of the agent.
    /// </summary>
    [Id(20)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// The type of agent being created.
    /// </summary>
    [Id(21)]
    public string AgentType { get; set; } = string.Empty;
    
    /// <summary>
    /// Initial properties for the agent.
    /// </summary>
    [Id(22)]
    public Dictionary<string, string> Properties { get; set; } = new();
    
    /// <summary>
    /// The Orleans grain ID for the agent.
    /// </summary>
    [Id(23)]
    public GrainId AgentGrainId { get; set; }
    
    /// <summary>
    /// The initial status of the agent.
    /// </summary>
    [Id(24)]
    public AgentStatus InitialStatus { get; set; } = AgentStatus.Creating;
}