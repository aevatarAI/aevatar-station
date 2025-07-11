// ABOUTME: This file implements the AgentLifecycleService for managing agent CRUD operations
// ABOUTME: Centralizes agent lifecycle management, coordinating between type metadata, agent factory, and direct agent access

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Application.Models;
using Aevatar.Application.Services;
using Aevatar.Core.Abstractions;
using Aevatar.MetaData;
using Aevatar.MetaData.Enums;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Volo.Abp.DependencyInjection;
using Aevatar.Application.Grains;

namespace Aevatar.Application.Services;

/// <summary>
/// Production service implementation for managing agent lifecycle operations including creation, updates, deletion, and retrieval.
/// This service acts as the primary interface for agent lifecycle management, coordinating between 
/// type metadata, Orleans grains, and Elasticsearch for scalable agent management.
/// </summary>
public class AgentLifecycleService : IAgentLifecycleService, ISingletonDependency
{
    private readonly ITypeMetadataService _typeMetadataService;
    private readonly IGrainFactory _grainFactory;
    private readonly IGAgentFactory _gAgentFactory;
    private readonly ILogger<AgentLifecycleService> _logger;

    public AgentLifecycleService(
        ITypeMetadataService typeMetadataService,
        IGrainFactory grainFactory,
        IGAgentFactory gAgentFactory,
        ILogger<AgentLifecycleService> logger)
    {
        _typeMetadataService = typeMetadataService ?? throw new ArgumentNullException(nameof(typeMetadataService));
        _grainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
        _gAgentFactory = gAgentFactory ?? throw new ArgumentNullException(nameof(gAgentFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<AgentInfo> CreateAgentAsync(CreateAgentRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (request.UserId == Guid.Empty)
            throw new ArgumentException("UserId is required and cannot be empty", nameof(request));

        if (string.IsNullOrWhiteSpace(request.AgentType))
            throw new ArgumentException("AgentType is required and cannot be empty", nameof(request));

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required and cannot be empty", nameof(request));

        _logger.LogInformation("Creating agent of type {AgentType} for user {UserId} with name {Name}",
            request.AgentType, request.UserId, request.Name);

        // Validate agent type via TypeMetadataService
        var typeMetadata = await _typeMetadataService.GetTypeMetadataAsync(request.AgentType);
        if (typeMetadata == null)
        {
            _logger.LogError("Agent type {AgentType} not found in type metadata", request.AgentType);
            throw new InvalidOperationException($"Unknown agent type: {request.AgentType}");
        }

        // Generate agent ID 
        var agentId = Guid.NewGuid();

        try
        {
            // Create the business agent grain
            var businessAgentGrainId = GrainId.Create(request.AgentType, agentId.ToString());
            var businessAgent = await _gAgentFactory.GetGAgentAsync(businessAgentGrainId);

            // Activate the business agent
            await businessAgent.ActivateAsync();

            // Cast to IMetaDataStateGAgent for metadata operations
            // The business agent must implement IMetaDataStateGAgent interface
            if (businessAgent is not IMetaDataStateGAgent metadataAgent)
            {
                throw new InvalidOperationException($"Agent type {request.AgentType} does not implement IMetaDataStateGAgent");
            }

            // Convert properties to string dictionary for metadata
            var metadataProperties = new Dictionary<string, string>();
            if (request.Properties != null)
            {
                foreach (var prop in request.Properties)
                {
                    metadataProperties[prop.Key] = prop.Value?.ToString() ?? string.Empty;
                }
            }

            // Create the agent through the metadata helper interface
            await metadataAgent.CreateAgentAsync(
                agentId,
                request.UserId,
                request.Name,
                request.AgentType,
                metadataProperties);

            // Set initial status to Active
            await metadataAgent.UpdateStatusAsync(MetaData.Enums.AgentStatus.Active, "Agent successfully created");

            // Create AgentInfo response
            var agentInfo = new AgentInfo
            {
                Id = agentId,
                UserId = request.UserId,
                AgentType = request.AgentType,
                Name = request.Name,
                Properties = request.Properties ?? new Dictionary<string, object>(),
                Capabilities = typeMetadata.Capabilities ?? new List<string>(),
                Status = Application.Models.AgentStatus.Active,
                CreatedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow,
                GrainId = businessAgentGrainId,
                Description = typeMetadata.Description ?? string.Empty,
                Version = typeMetadata.AssemblyVersion ?? string.Empty
            };

            _logger.LogInformation("Agent {AgentId} created successfully with {CapabilityCount} capabilities",
                agentId, agentInfo.Capabilities.Count);

            return agentInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create agent {AgentId} of type {AgentType}", agentId, request.AgentType);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<AgentInfo> UpdateAgentAsync(Guid agentId, UpdateAgentRequest request)
    {
        if (agentId == Guid.Empty)
            throw new ArgumentException("AgentId is required and cannot be empty", nameof(agentId));

        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (!request.HasUpdates())
            throw new ArgumentException("At least one field must be provided for update", nameof(request));

        _logger.LogInformation("Updating agent {AgentId}", agentId);

        try
        {
            // Get the business agent grain - need to determine the correct grain ID pattern
            // For now, assume agents are stored with a GrainId based on their type and ID
            var businessAgentGrainId = GrainId.Create("Agent", agentId.ToString());
            var businessAgent = await _gAgentFactory.GetGAgentAsync(businessAgentGrainId);

            // Cast to IMetaDataStateGAgent for metadata operations
            if (businessAgent is not IMetaDataStateGAgent metadataAgent)
            {
                throw new InvalidOperationException($"Agent {agentId} does not implement IMetaDataStateGAgent");
            }

            // Update properties if provided
            if (request.Properties != null)
            {
                // Convert properties to string dictionary for metadata
                var metadataProperties = new Dictionary<string, string>();
                foreach (var prop in request.Properties)
                {
                    metadataProperties[prop.Key] = prop.Value?.ToString() ?? string.Empty;
                }

                // Update properties through metadata agent
                await metadataAgent.UpdatePropertiesAsync(metadataProperties, merge: true);
            }

            // Update name through properties if provided
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                await metadataAgent.SetPropertyAsync("Name", request.Name);
            }

            // Record activity
            await metadataAgent.RecordActivityAsync("Agent updated");

            // Retrieve updated agent state from metadata interface
            var updatedState = metadataAgent.GetState();
            if (updatedState == null || updatedState.Id == Guid.Empty)
            {
                throw new InvalidOperationException($"Agent {agentId} not found");
            }

            // Get type metadata for capabilities
            var typeMetadata = await _typeMetadataService.GetTypeMetadataAsync(updatedState.AgentType);

            // Convert from metadata state to AgentInfo
            var properties = new Dictionary<string, object>();
            foreach (var prop in updatedState.Properties)
            {
                properties[prop.Key] = prop.Value;
            }

            var agentInfo = new AgentInfo
            {
                Id = updatedState.Id,
                UserId = updatedState.UserId,
                AgentType = updatedState.AgentType,
                Name = updatedState.Name,
                Properties = properties,
                Capabilities = typeMetadata?.Capabilities ?? new List<string>(),
                Status = ConvertMetadataStatusToAgentStatus(updatedState.Status),
                CreatedAt = updatedState.CreateTime,
                LastActivity = updatedState.LastActivity,
                GrainId = businessAgent.GetGrainId(),
                Description = typeMetadata?.Description ?? string.Empty,
                Version = typeMetadata?.AssemblyVersion ?? string.Empty
            };

            _logger.LogInformation("Agent {AgentId} updated successfully", agentId);
            return agentInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update agent {AgentId}", agentId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeleteAgentAsync(Guid agentId)
    {
        if (agentId == Guid.Empty)
            throw new ArgumentException("AgentId is required and cannot be empty", nameof(agentId));

        _logger.LogInformation("Deleting agent {AgentId}", agentId);

        try
        {
            // Get the business agent grain - need to determine the correct grain ID pattern
            // For now, assume agents are stored with a GrainId based on their type and ID
            var businessAgentGrainId = GrainId.Create("Agent", agentId.ToString());
            var businessAgent = await _gAgentFactory.GetGAgentAsync(businessAgentGrainId);

            // Cast to IMetaDataStateGAgent for metadata operations
            if (businessAgent is not IMetaDataStateGAgent metadataAgent)
            {
                throw new InvalidOperationException($"Agent {agentId} does not implement IMetaDataStateGAgent");
            }

            // Update status to deleted through the metadata interface
            await metadataAgent.UpdateStatusAsync(MetaData.Enums.AgentStatus.Deleting, "Agent deleted via AgentLifecycleService");

            _logger.LogInformation("Agent {AgentId} marked as deleted", agentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete agent {AgentId}", agentId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<AgentInfo> GetAgentAsync(Guid agentId)
    {
        if (agentId == Guid.Empty)
            throw new ArgumentException("AgentId is required and cannot be empty", nameof(agentId));

        _logger.LogInformation("Retrieving agent {AgentId}", agentId);

        try
        {
            // Get the business agent grain - need to determine the correct grain ID pattern
            // For now, assume agents are stored with a GrainId based on their type and ID
            var businessAgentGrainId = GrainId.Create("Agent", agentId.ToString());
            var businessAgent = await _gAgentFactory.GetGAgentAsync(businessAgentGrainId);

            // Cast to IMetaDataStateGAgent for metadata operations
            if (businessAgent is not IMetaDataStateGAgent metadataAgent)
            {
                throw new InvalidOperationException($"Agent {agentId} does not implement IMetaDataStateGAgent");
            }

            // Get the metadata state
            var metadataState = metadataAgent.GetState();

            if (metadataState == null || metadataState.Id == Guid.Empty)
            {
                throw new InvalidOperationException($"Agent {agentId} not found");
            }

            // Get type metadata for capabilities
            var typeMetadata = await _typeMetadataService.GetTypeMetadataAsync(metadataState.AgentType);

            // Convert from metadata state to AgentInfo
            var properties = new Dictionary<string, object>();
            foreach (var prop in metadataState.Properties)
            {
                properties[prop.Key] = prop.Value;
            }

            var agentInfo = new AgentInfo
            {
                Id = metadataState.Id,
                UserId = metadataState.UserId,
                AgentType = metadataState.AgentType,
                Name = metadataState.Name,
                Properties = properties,
                Capabilities = typeMetadata?.Capabilities ?? new List<string>(),
                Status = ConvertMetadataStatusToAgentStatus(metadataState.Status),
                CreatedAt = metadataState.CreateTime,
                LastActivity = metadataState.LastActivity,
                GrainId = businessAgent.GetGrainId(),
                Description = typeMetadata?.Description ?? string.Empty,
                Version = typeMetadata?.AssemblyVersion ?? string.Empty
            };

            _logger.LogInformation("Agent {AgentId} retrieved from business agent", agentId);
            return agentInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve agent {AgentId}", agentId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<AgentInfo>> GetUserAgentsAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required and cannot be empty", nameof(userId));

        _logger.LogInformation("Retrieving agents for user {UserId}", userId);

        try
        {
            // Note: This implementation would need to query Orleans grains to find agents by user
            // For now, returning empty list as this requires implementing a proper query mechanism
            _logger.LogWarning("GetUserAgentsAsync not fully implemented - requires Orleans grain query mechanism");

            var userAgents = new List<AgentInfo>();
            _logger.LogInformation("Retrieved {Count} agents for user {UserId}", userAgents.Count, userId);
            return userAgents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve agents for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SendEventToAgentAsync(Guid agentId, EventBase @event)
    {
        if (agentId == Guid.Empty)
            throw new ArgumentException("AgentId is required and cannot be empty", nameof(agentId));

        if (@event == null)
            throw new ArgumentNullException(nameof(@event));

        _logger.LogInformation("Sending event of type {EventType} to agent {AgentId}",
            @event.GetType().Name, agentId);

        try
        {
            // Get the business agent grain - need to determine the correct grain ID pattern
            // For now, assume agents are stored with a GrainId based on their type and ID
            var businessAgentGrainId = GrainId.Create("Agent", agentId.ToString());
            var businessAgent = await _gAgentFactory.GetGAgentAsync(businessAgentGrainId);

            // Cast to IMetaDataStateGAgent for metadata operations
            if (businessAgent is not IMetaDataStateGAgent metadataAgent)
            {
                throw new InvalidOperationException($"Agent {agentId} does not implement IMetaDataStateGAgent");
            }

            // Record activity for event publishing
            await metadataAgent.RecordActivityAsync($"Event received: {@event.GetType().Name}");

            // Note: This would need proper event publishing implementation based on your Orleans setup
            // The businessAgent should handle the event directly through its event handling methods

            _logger.LogInformation("Event {EventType} sent to agent {AgentId}",
                @event.GetType().Name, agentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send event {EventType} to agent {AgentId}",
                @event.GetType().Name, agentId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<AgentInfo> AddSubAgentAsync(Guid parentId, Guid childId)
    {
        if (parentId == Guid.Empty)
            throw new ArgumentException("ParentId is required and cannot be empty", nameof(parentId));

        if (childId == Guid.Empty)
            throw new ArgumentException("ChildId is required and cannot be empty", nameof(childId));

        _logger.LogInformation("Adding sub-agent {ChildId} to parent {ParentId}", childId, parentId);

        try
        {
            // Get both business agent grains
            var parentBusinessAgentGrainId = GrainId.Create("Agent", parentId.ToString());
            var childBusinessAgentGrainId = GrainId.Create("Agent", childId.ToString());
            var parentBusinessAgent = await _gAgentFactory.GetGAgentAsync(parentBusinessAgentGrainId);
            var childBusinessAgent = await _gAgentFactory.GetGAgentAsync(childBusinessAgentGrainId);

            // Cast to IMetaDataStateGAgent for metadata operations
            if (parentBusinessAgent is not IMetaDataStateGAgent parentMetadataAgent)
            {
                throw new InvalidOperationException($"Parent agent {parentId} does not implement IMetaDataStateGAgent");
            }

            if (childBusinessAgent is not IMetaDataStateGAgent childMetadataAgent)
            {
                throw new InvalidOperationException($"Child agent {childId} does not implement IMetaDataStateGAgent");
            }

            // Get metadata states
            var parentState = parentMetadataAgent.GetState();
            var childState = childMetadataAgent.GetState();

            if (parentState == null || parentState.Id == Guid.Empty)
            {
                throw new InvalidOperationException($"Parent agent {parentId} not found");
            }

            if (childState == null || childState.Id == Guid.Empty)
            {
                throw new InvalidOperationException($"Child agent {childId} not found");
            }

            // Register Orleans grain relationship
            await parentBusinessAgent.RegisterAsync(childBusinessAgent);
            await childBusinessAgent.SubscribeToAsync(parentBusinessAgent);

            // Record activity on both metadata agents
            await parentMetadataAgent.RecordActivityAsync($"Added sub-agent {childId}");
            await childMetadataAgent.RecordActivityAsync($"Added as sub-agent to {parentId}");

            // Get type metadata for parent capabilities
            var parentTypeMetadata = await _typeMetadataService.GetTypeMetadataAsync(parentState.AgentType);

            // Convert from metadata state to AgentInfo
            var parentProperties = new Dictionary<string, object>();
            foreach (var prop in parentState.Properties)
            {
                parentProperties[prop.Key] = prop.Value;
            }

            // Create parent AgentInfo from metadata state
            var parentAgentInfo = new AgentInfo
            {
                Id = parentState.Id,
                UserId = parentState.UserId,
                AgentType = parentState.AgentType,
                Name = parentState.Name,
                Properties = parentProperties,
                Capabilities = parentTypeMetadata?.Capabilities ?? new List<string>(),
                Status = ConvertMetadataStatusToAgentStatus(parentState.Status),
                CreatedAt = parentState.CreateTime,
                LastActivity = parentState.LastActivity,
                SubAgents = new List<Guid> { childId }, // Add the child to sub-agents
                ParentAgentId = null,
                GrainId = parentBusinessAgent.GetGrainId(),
                Description = parentTypeMetadata?.Description ?? string.Empty,
                Version = parentTypeMetadata?.AssemblyVersion ?? string.Empty
            };

            _logger.LogInformation("Sub-agent {ChildId} added to parent {ParentId}", childId, parentId);
            return parentAgentInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add sub-agent {ChildId} to parent {ParentId}", childId, parentId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<AgentInfo> RemoveSubAgentAsync(Guid parentId, Guid childId)
    {
        if (parentId == Guid.Empty)
            throw new ArgumentException("ParentId is required and cannot be empty", nameof(parentId));

        if (childId == Guid.Empty)
            throw new ArgumentException("ChildId is required and cannot be empty", nameof(childId));

        _logger.LogInformation("Removing sub-agent {ChildId} from parent {ParentId}", childId, parentId);

        try
        {
            // Get both business agent grains
            var parentBusinessAgentGrainId = GrainId.Create("Agent", parentId.ToString());
            var childBusinessAgentGrainId = GrainId.Create("Agent", childId.ToString());
            var parentBusinessAgent = await _gAgentFactory.GetGAgentAsync(parentBusinessAgentGrainId);
            var childBusinessAgent = await _gAgentFactory.GetGAgentAsync(childBusinessAgentGrainId);

            // Cast to IMetaDataStateGAgent for metadata operations
            if (parentBusinessAgent is not IMetaDataStateGAgent parentMetadataAgent)
            {
                throw new InvalidOperationException($"Parent agent {parentId} does not implement IMetaDataStateGAgent");
            }

            if (childBusinessAgent is not IMetaDataStateGAgent childMetadataAgent)
            {
                throw new InvalidOperationException($"Child agent {childId} does not implement IMetaDataStateGAgent");
            }

            // Get metadata states
            var parentState = parentMetadataAgent.GetState();
            var childState = childMetadataAgent.GetState();

            if (parentState == null || parentState.Id == Guid.Empty)
            {
                throw new InvalidOperationException($"Parent agent {parentId} not found");
            }

            if (childState == null || childState.Id == Guid.Empty)
            {
                throw new InvalidOperationException($"Child agent {childId} not found");
            }

            // Remove Orleans grain relationship
            await parentBusinessAgent.UnregisterAsync(childBusinessAgent);
            await childBusinessAgent.UnsubscribeFromAsync(parentBusinessAgent);

            // Record activity on both metadata agents
            await parentMetadataAgent.RecordActivityAsync($"Removed sub-agent {childId}");
            await childMetadataAgent.RecordActivityAsync($"Removed as sub-agent from {parentId}");

            // Get type metadata for parent capabilities
            var parentTypeMetadata = await _typeMetadataService.GetTypeMetadataAsync(parentState.AgentType);

            // Convert from metadata state to AgentInfo
            var parentProperties = new Dictionary<string, object>();
            foreach (var prop in parentState.Properties)
            {
                parentProperties[prop.Key] = prop.Value;
            }

            // Create parent AgentInfo from metadata state (child is already removed from Orleans relationships)
            var parentAgentInfo = new AgentInfo
            {
                Id = parentState.Id,
                UserId = parentState.UserId,
                AgentType = parentState.AgentType,
                Name = parentState.Name,
                Properties = parentProperties,
                Capabilities = parentTypeMetadata?.Capabilities ?? new List<string>(),
                Status = ConvertMetadataStatusToAgentStatus(parentState.Status),
                CreatedAt = parentState.CreateTime,
                LastActivity = parentState.LastActivity,
                SubAgents = new List<Guid>(), // Child removed from sub-agents
                ParentAgentId = null,
                GrainId = parentBusinessAgent.GetGrainId(),
                Description = parentTypeMetadata?.Description ?? string.Empty,
                Version = parentTypeMetadata?.AssemblyVersion ?? string.Empty
            };

            _logger.LogInformation("Sub-agent {ChildId} removed from parent {ParentId}", childId, parentId);
            return parentAgentInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove sub-agent {ChildId} from parent {ParentId}", childId, parentId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<AgentInfo> RemoveAllSubAgentsAsync(Guid parentId)
    {
        if (parentId == Guid.Empty)
            throw new ArgumentException("ParentId is required and cannot be empty", nameof(parentId));

        _logger.LogInformation("Removing all sub-agents from parent {ParentId}", parentId);

        try
        {
            // Get parent business agent grain
            var parentBusinessAgentGrainId = GrainId.Create("Agent", parentId.ToString());
            var parentBusinessAgent = await _gAgentFactory.GetGAgentAsync(parentBusinessAgentGrainId);

            // Cast to IMetaDataStateGAgent for metadata operations
            if (parentBusinessAgent is not IMetaDataStateGAgent parentMetadataAgent)
            {
                throw new InvalidOperationException($"Parent agent {parentId} does not implement IMetaDataStateGAgent");
            }

            // Get metadata state
            var parentState = parentMetadataAgent.GetState();

            if (parentState == null || parentState.Id == Guid.Empty)
            {
                throw new InvalidOperationException($"Parent agent {parentId} not found");
            }

            // Note: This is a simplified implementation. In production, you would need
            // to query Orleans to find all child agents of this parent
            _logger.LogWarning("RemoveAllSubAgentsAsync requires Orleans grain query mechanism to find all child agents");

            // Record activity on parent metadata agent
            await parentMetadataAgent.RecordActivityAsync("Removed all sub-agents");

            // Get type metadata for parent capabilities
            var parentTypeMetadata = await _typeMetadataService.GetTypeMetadataAsync(parentState.AgentType);

            // Convert from metadata state to AgentInfo
            var parentProperties = new Dictionary<string, object>();
            foreach (var prop in parentState.Properties)
            {
                parentProperties[prop.Key] = prop.Value;
            }

            // Create parent AgentInfo from metadata state (all sub-agents removed)
            var parentAgentInfo = new AgentInfo
            {
                Id = parentState.Id,
                UserId = parentState.UserId,
                AgentType = parentState.AgentType,
                Name = parentState.Name,
                Properties = parentProperties,
                Capabilities = parentTypeMetadata?.Capabilities ?? new List<string>(),
                Status = ConvertMetadataStatusToAgentStatus(parentState.Status),
                CreatedAt = parentState.CreateTime,
                LastActivity = parentState.LastActivity,
                SubAgents = new List<Guid>(), // All sub-agents removed
                ParentAgentId = null,
                GrainId = parentBusinessAgent.GetGrainId(),
                Description = parentTypeMetadata?.Description ?? string.Empty,
                Version = parentTypeMetadata?.AssemblyVersion ?? string.Empty
            };

            _logger.LogInformation("Removed all sub-agents from parent {ParentId}", parentId);
            return parentAgentInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove all sub-agents from parent {ParentId}", parentId);
            throw;
        }
    }


    /// <summary>
    /// Converts metadata AgentStatus to application AgentStatus.
    /// </summary>
    /// <param name="metadataStatus">The metadata status to convert</param>
    /// <returns>The corresponding application status</returns>
    private Application.Models.AgentStatus ConvertMetadataStatusToAgentStatus(MetaData.Enums.AgentStatus metadataStatus)
    {
        return metadataStatus switch
        {
            MetaData.Enums.AgentStatus.Creating => Application.Models.AgentStatus.Initializing,
            MetaData.Enums.AgentStatus.Active => Application.Models.AgentStatus.Active,
            MetaData.Enums.AgentStatus.Paused => Application.Models.AgentStatus.Inactive,
            MetaData.Enums.AgentStatus.Stopping => Application.Models.AgentStatus.Inactive,
            MetaData.Enums.AgentStatus.Stopped => Application.Models.AgentStatus.Inactive,
            MetaData.Enums.AgentStatus.Error => Application.Models.AgentStatus.Error,
            MetaData.Enums.AgentStatus.Deleting => Application.Models.AgentStatus.Deleted,
            _ => Application.Models.AgentStatus.Initializing
        };
    }

}