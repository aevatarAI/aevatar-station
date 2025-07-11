// ABOUTME: This file provides a test implementation of IMetaDataState for unit testing
// ABOUTME: Used to verify the default Apply method behavior without Orleans dependencies

using Aevatar.Core.Abstractions;
using Aevatar.MetaData;
using Aevatar.MetaData.Enums;

namespace Aevatar.MetaData.Tests;

/// <summary>
/// Test implementation of IMetaDataState for unit testing.
/// </summary>
public class TestMetaDataState : StateBase, IMetaDataState
{
    /// <summary>
    /// The unique identifier for the agent.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// The user ID that owns this agent.
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// The type of agent (e.g., "CreatorAgent", "ChatAgent").
    /// </summary>
    public string AgentType { get; set; } = string.Empty;
    
    /// <summary>
    /// The display name of the agent.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Key-value properties for the agent.
    /// </summary>
    public Dictionary<string, string> Properties { get; set; } = new();
    
    /// <summary>
    /// The Orleans grain ID for the agent.
    /// </summary>
    public GrainId AgentGrainId { get; set; }
    
    /// <summary>
    /// The timestamp when the agent was created.
    /// </summary>
    public DateTime CreateTime { get; set; }
    
    /// <summary>
    /// The current status of the agent.
    /// </summary>
    public AgentStatus Status { get; set; }
    
    /// <summary>
    /// The timestamp of the last activity.
    /// </summary>
    public DateTime LastActivity { get; set; }
}