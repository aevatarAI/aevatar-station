// ABOUTME: This file defines the interface for agent lifecycle management operations
// ABOUTME: Centralizes agent CRUD operations, replacing CreatorGAgent's factory responsibilities with a dedicated service

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Application.Models;
using Aevatar.Core.Abstractions;

namespace Aevatar.Application.Services;

/// <summary>
/// Service interface for managing agent lifecycle operations including creation, updates, deletion, and retrieval.
/// This service acts as the primary interface for agent lifecycle management, coordinating between 
/// type metadata, agent factory, and direct agent access.
/// </summary>
public interface IAgentLifecycleService
{
    /// <summary>
    /// Creates a new agent instance with the specified configuration.
    /// </summary>
    /// <param name="request">The agent creation request containing user ID, agent type, name, and properties</param>
    /// <returns>AgentInfo containing the created agent's metadata and current state</returns>
    /// <exception cref="ArgumentException">Thrown when request parameters are invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when agent type is not found or creation fails</exception>
    Task<AgentInfo> CreateAgentAsync(CreateAgentRequest request);

    /// <summary>
    /// Updates an existing agent's configuration and properties.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent to update</param>
    /// <param name="request">The update request containing new name and properties</param>
    /// <returns>AgentInfo containing the updated agent's metadata and current state</returns>
    /// <exception cref="ArgumentException">Thrown when agentId is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when agent is not found</exception>
    Task<AgentInfo> UpdateAgentAsync(Guid agentId, UpdateAgentRequest request);

    /// <summary>
    /// Deletes an agent by marking it as deleted and performing cleanup operations.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent to delete</param>
    /// <exception cref="ArgumentException">Thrown when agentId is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when agent is not found</exception>
    Task DeleteAgentAsync(Guid agentId);

    /// <summary>
    /// Retrieves a specific agent by its unique identifier.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent to retrieve</param>
    /// <returns>AgentInfo containing the agent's metadata and current state</returns>
    /// <exception cref="ArgumentException">Thrown when agentId is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when agent is not found</exception>
    Task<AgentInfo> GetAgentAsync(Guid agentId);

    /// <summary>
    /// Retrieves all agents belonging to a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose agents to retrieve</param>
    /// <returns>List of AgentInfo objects containing metadata and current state for each agent</returns>
    /// <exception cref="ArgumentException">Thrown when userId is invalid</exception>
    Task<List<AgentInfo>> GetUserAgentsAsync(Guid userId);

    /// <summary>
    /// Sends an event to a specific agent for processing.
    /// </summary>
    /// <param name="agentId">The unique identifier of the target agent</param>
    /// <param name="event">The event to send to the agent</param>
    /// <exception cref="ArgumentException">Thrown when agentId is invalid or event is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when agent is not found or event cannot be delivered</exception>
    Task SendEventToAgentAsync(Guid agentId, EventBase @event);

    /// <summary>
    /// Adds a sub-agent relationship between a parent and child agent.
    /// </summary>
    /// <param name="parentId">The unique identifier of the parent agent</param>
    /// <param name="childId">The unique identifier of the child agent</param>
    /// <returns>AgentInfo containing the updated parent agent's metadata and current state</returns>
    /// <exception cref="ArgumentException">Thrown when parentId or childId is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when parent or child agent is not found</exception>
    Task<AgentInfo> AddSubAgentAsync(Guid parentId, Guid childId);

    /// <summary>
    /// Removes a sub-agent relationship between a parent and child agent.
    /// </summary>
    /// <param name="parentId">The unique identifier of the parent agent</param>
    /// <param name="childId">The unique identifier of the child agent</param>
    /// <returns>AgentInfo containing the updated parent agent's metadata and current state</returns>
    /// <exception cref="ArgumentException">Thrown when parentId or childId is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when parent agent is not found</exception>
    Task<AgentInfo> RemoveSubAgentAsync(Guid parentId, Guid childId);

    /// <summary>
    /// Removes all sub-agent relationships for a parent agent.
    /// </summary>
    /// <param name="parentId">The unique identifier of the parent agent</param>
    /// <returns>AgentInfo containing the updated parent agent's metadata and current state</returns>
    /// <exception cref="ArgumentException">Thrown when parentId is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when parent agent is not found</exception>
    Task<AgentInfo> RemoveAllSubAgentsAsync(Guid parentId);
}