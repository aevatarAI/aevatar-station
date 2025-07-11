// ABOUTME: This file defines the state class for integration testing with Orleans event sourcing
// ABOUTME: Implements IMetaDataState directly to provide Orleans-compatible state with metadata capabilities

using Aevatar.Core.Abstractions;
using Aevatar.MetaData;
using Aevatar.MetaData.Enums;

namespace Aevatar.MetaData.Tests;

/// <summary>
/// Test state class for Orleans integration testing with IMetaDataStateGAgent.
/// Implements IMetaDataState directly to provide full metadata state functionality.
/// </summary>
[GenerateSerializer]
public class TestMetaDataAgentState : StateBase, IMetaDataState
{
    /// <summary>
    /// The unique identifier for the agent.
    /// </summary>
    [Id(0)]
    public Guid Id { get; set; }
    
    /// <summary>
    /// The user ID that owns this agent.
    /// </summary>
    [Id(1)]
    public Guid UserId { get; set; }
    
    /// <summary>
    /// The type of agent (e.g., "CreatorAgent", "ChatAgent").
    /// </summary>
    [Id(2)]
    public string AgentType { get; set; } = string.Empty;
    
    /// <summary>
    /// The display name of the agent.
    /// </summary>
    [Id(3)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Key-value properties for the agent.
    /// </summary>
    [Id(4)]
    public Dictionary<string, string> Properties { get; set; } = new();
    
    /// <summary>
    /// The Orleans grain ID for the agent.
    /// </summary>
    [Id(5)]
    public GrainId AgentGrainId { get; set; }
    
    /// <summary>
    /// The timestamp when the agent was created.
    /// </summary>
    [Id(6)]
    public DateTime CreateTime { get; set; }
    
    /// <summary>
    /// The current status of the agent.
    /// </summary>
    [Id(7)]
    public AgentStatus Status { get; set; }
    
    /// <summary>
    /// The timestamp of the last activity.
    /// </summary>
    [Id(8)]
    public DateTime LastActivity { get; set; }
    
    /// <summary>
    /// Additional test-specific property for verifying Orleans event sourcing.
    /// </summary>
    [Id(100)]
    public int TestEventCount { get; set; }
    
    /// <summary>
    /// Test messages for verifying event processing.
    /// </summary>
    [Id(101)]
    public List<string> TestMessages { get; set; } = new();
}