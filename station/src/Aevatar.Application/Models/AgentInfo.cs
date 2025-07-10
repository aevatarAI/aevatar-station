// ABOUTME: This file defines the response model for agent information
// ABOUTME: Combines agent instance data with type metadata for comprehensive agent information

using System;
using System.Collections.Generic;
using System.Linq;
using Orleans;
using Orleans.Runtime;
using Orleans.Serialization;

namespace Aevatar.Application.Models;

/// <summary>
/// Comprehensive information about an agent instance.
/// Combines agent instance state with type metadata to provide complete agent information.
/// </summary>
[GenerateSerializer]
public class AgentInfo
{
    /// <summary>
    /// The unique identifier of the agent instance.
    /// </summary>
    [Id(0)]
    public Guid Id { get; set; }

    /// <summary>
    /// The unique identifier of the user who owns this agent.
    /// </summary>
    [Id(1)]
    public Guid UserId { get; set; }

    /// <summary>
    /// The type of agent, corresponds to the registered agent type.
    /// </summary>
    [Id(2)]
    public string AgentType { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the agent instance.
    /// </summary>
    [Id(3)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Custom properties for agent configuration.
    /// </summary>
    [Id(4)]
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>
    /// List of capabilities this agent type supports.
    /// Derived from the agent type metadata.
    /// </summary>
    [Id(5)]
    public List<string> Capabilities { get; set; } = new();

    /// <summary>
    /// The current status of the agent instance.
    /// </summary>
    [Id(6)]
    public AgentStatus Status { get; set; }

    /// <summary>
    /// The timestamp when the agent was created.
    /// </summary>
    [Id(7)]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// The timestamp of the last activity from this agent.
    /// </summary>
    [Id(8)]
    public DateTime LastActivity { get; set; }

    /// <summary>
    /// List of sub-agent IDs if this agent has children.
    /// </summary>
    [Id(9)]
    public List<Guid> SubAgents { get; set; } = new();

    /// <summary>
    /// The parent agent ID if this agent is a sub-agent.
    /// </summary>
    [Id(10)]
    public Guid? ParentAgentId { get; set; }

    /// <summary>
    /// The Orleans grain ID for direct grain access.
    /// </summary>
    [Id(11)]
    public GrainId? GrainId { get; set; }

    /// <summary>
    /// Additional metadata about the agent type.
    /// </summary>
    [Id(12)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The version of the agent type.
    /// </summary>
    [Id(13)]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Determines if the agent is currently active.
    /// </summary>
    public bool IsActive => Status == AgentStatus.Active;

    /// <summary>
    /// Determines if the agent is deleted.
    /// </summary>
    public bool IsDeleted => Status == AgentStatus.Deleted;

    /// <summary>
    /// Determines if the agent has sub-agents.
    /// </summary>
    public bool HasSubAgents => SubAgents.Any();

    /// <summary>
    /// Determines if the agent is a sub-agent.
    /// </summary>
    public bool IsSubAgent => ParentAgentId.HasValue;

    /// <summary>
    /// Gets the time since the last activity.
    /// </summary>
    public TimeSpan TimeSinceLastActivity => DateTime.UtcNow - LastActivity;
}

/// <summary>
/// Enumeration of possible agent statuses.
/// </summary>
[GenerateSerializer]
public enum AgentStatus
{
    /// <summary>
    /// Agent is in the process of being initialized.
    /// </summary>
    [Id(0)]
    Initializing,

    /// <summary>
    /// Agent is active and ready to process requests.
    /// </summary>
    [Id(1)]
    Active,

    /// <summary>
    /// Agent is inactive but not deleted.
    /// </summary>
    [Id(2)]
    Inactive,

    /// <summary>
    /// Agent encountered an error and is not functioning.
    /// </summary>
    [Id(3)]
    Error,

    /// <summary>
    /// Agent has been deleted and is no longer available.
    /// </summary>
    [Id(4)]
    Deleted
}