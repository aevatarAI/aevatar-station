// ABOUTME: This file defines the AgentStatus enumeration for agent lifecycle management
// ABOUTME: Used to track the current operational state of agents in the system

namespace Aevatar.MetaData.Enums;

/// <summary>
/// Represents the current operational status of an agent in the system.
/// </summary>
[GenerateSerializer]
public enum AgentStatus
{
    /// <summary>
    /// Agent is being created and initialized.
    /// </summary>
    Creating = 0,
    
    /// <summary>
    /// Agent is active and available for work.
    /// </summary>
    Active = 1,
    
    /// <summary>
    /// Agent is temporarily paused but can be resumed.
    /// </summary>
    Paused = 2,
    
    /// <summary>
    /// Agent is being gracefully stopped.
    /// </summary>
    Stopping = 3,
    
    /// <summary>
    /// Agent has been stopped and is inactive.
    /// </summary>
    Stopped = 4,
    
    /// <summary>
    /// Agent encountered an error and is in an error state.
    /// </summary>
    Error = 5,
    
    /// <summary>
    /// Agent is being deleted and will be removed from the system.
    /// </summary>
    Deleting = 6
}