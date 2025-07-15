using Aevatar.Core.Abstractions;
using E2E.Grains;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Orleans;

namespace LayeredLatencyBenchmark;

/// <summary>
/// Orleans client for direct agent communication - LayeredLeaderAgent replaces CreatorGAgent
/// </summary>
public class OrleansAgentClient
{
    private readonly IClusterClient _clusterClient;
    private readonly IGAgentFactory _gAgentFactory;
    private readonly ILogger<OrleansAgentClient> _logger;

    public OrleansAgentClient(IClusterClient clusterClient, IGAgentFactory gAgentFactory, ILogger<OrleansAgentClient> logger)
    {
        _clusterClient = clusterClient;
        _gAgentFactory = gAgentFactory;
        _logger = logger;
    }

    /// <summary>
    /// Create any agent following HTTP API pattern - single method like /api/agent endpoint
    /// </summary>
    public async Task<T> CreateAgentAsync<T>(Guid agentId, string agentType, string name) where T : class, IGAgent
    {
        try
        {
            // Step 1: Create business agent via IGAgentFactory (following HTTP API InitializeBusinessAgent)
            var grainId = GrainId.Create($"E2E.Grains.{agentType}", agentId.ToString("N"));
            var businessAgent = await _gAgentFactory.GetGAgentAsync<T>(grainId);
            
            // Step 2: Get LayeredLeaderAgent as creator agent (replaces CreatorGAgent) 
            var creatorAgent = await _gAgentFactory.GetGAgentAsync<ILayeredLeaderAgent>(agentId);
            
            // Step 3: Create AgentData following HTTP API pattern
            var agentData = new E2E.Grains.AgentData
            {
                UserId = Guid.NewGuid(), // Benchmark user ID  
                AgentType = agentType, // Agent type parameter from caller
                Name = name, // Name parameter from caller
                Properties = "{}", // Empty properties for benchmark
                BusinessAgentGrainId = businessAgent.GetGrainId() // Points to business agent
            };
            
            // Step 4: Initialize agent following HTTP API pattern
            await creatorAgent.CreateAgentAsync(agentData);
            
            _logger.LogInformation("✅ Created agent {AgentId} with agentType={AgentType}, name={Name}", 
                agentId, agentData.AgentType, agentData.Name);
            return businessAgent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to create agent {AgentId} with type {AgentType}", agentId, agentType);
            throw;
        }
    }

    /// <summary>
    /// Create a leader agent - convenience wrapper
    /// </summary>
    public async Task<ILayeredLeaderAgent> CreateLeaderAgentAsync(Guid agentId)
    {
        return await CreateAgentAsync<ILayeredLeaderAgent>(agentId, "LayeredLeaderAgent", $"Leader-{agentId}");
    }

    /// <summary>
    /// Create a sub-agent - convenience wrapper  
    /// </summary>
    public async Task<ILayeredSubAgent> CreateSubAgentAsync(Guid agentId)
    {
        return await CreateAgentAsync<ILayeredSubAgent>(agentId, "LayeredSubAgent", $"SubAgent-{agentId}");
    }

    /// <summary>
    /// Register sub-agents with leader using GAgent framework hierarchy
    /// </summary>
    public async Task RegisterSubAgentsAsync(Guid leaderAgentId, List<Guid> subAgentIds)
    {
        try
        {
            if (subAgentIds.Count == 0)
            {
                _logger.LogWarning("No sub-agents to register");
                return;
            }

            // Get the leader agent
            var leaderAgent = await _gAgentFactory.GetGAgentAsync<ILayeredLeaderAgent>(leaderAgentId);
            
            // Get all sub-agents  
            var subAgents = new List<IGAgent>();
            foreach (var subAgentId in subAgentIds)
            {
                var subAgent = await _gAgentFactory.GetGAgentAsync<ILayeredSubAgent>(subAgentId);
                subAgents.Add(subAgent);
            }
            
            // Register sub-agents with leader using GAgent framework
            await leaderAgent.RegisterManyAsync(subAgents);
            
            _logger.LogInformation("✅ Registered {SubAgentCount} sub-agents with leader {LeaderAgentId}", subAgentIds.Count, leaderAgentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to register sub-agents with leader {LeaderAgentId}", leaderAgentId);
            throw;
        }
    }

    /// <summary>
    /// Publish event to leader via LayeredLeaderAgent.PublishEventAsync (HTTP API compatible)
    /// </summary>
    public async Task PublishEventToLeaderAsync(Guid leaderAgentId, LayeredTestEvent testEvent)
    {
        try
        {
            // Find the target leader by GUID
            var leaderAgent = await _gAgentFactory.GetGAgentAsync<ILayeredLeaderAgent>(leaderAgentId);
            
            // Call LayeredLeaderAgent.PublishEventAsync directly (matches HTTP API pattern)
            await leaderAgent.PublishEventAsync(testEvent);
            
            _logger.LogDebug("✅ Published event {CorrelationId} to leader {LeaderId} using LayeredLeaderAgent.PublishEventAsync", testEvent.CorrelationId, leaderAgentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to publish event {CorrelationId} to leader {LeaderId}", testEvent.CorrelationId, leaderAgentId);
            throw;
        }
    }

    /// <summary>
    /// Activate leader agent by calling GetDescriptionAsync (triggers OnActivateAsync)
    /// </summary>
    public async Task<string> GetDescriptionAsync(ILayeredLeaderAgent leaderAgent)
    {
        try
        {
            var description = await leaderAgent.GetDescriptionAsync();
            _logger.LogDebug("✅ Activated leader agent: {Description}", description);
            return description;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to activate leader agent");
            throw;
        }
    }

    /// <summary>
    /// Activate sub-agent by calling GetDescriptionAsync (triggers OnActivateAsync)
    /// </summary>
    public async Task<string> GetDescriptionAsync(ILayeredSubAgent subAgent)
    {
        try
        {
            var description = await subAgent.GetDescriptionAsync();
            _logger.LogDebug("✅ Activated sub-agent: {Description}", description);
            return description;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to activate sub-agent");
            throw;
        }
    }

    /// <summary>
    /// Get leader agent metrics
    /// </summary>
    public async Task<LayeredMetrics> GetLeaderMetricsAsync(ILayeredLeaderAgent leaderAgent)
    {
        try
        {
            return await leaderAgent.GetLayeredMetricsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to get leader metrics");
            throw;
        }
    }

    /// <summary>
    /// Get sub-agent metrics
    /// </summary>
    public async Task<LayeredMetrics> GetSubAgentMetricsAsync(ILayeredSubAgent subAgent)
    {
        try
        {
            return await subAgent.GetLayeredMetricsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to get sub-agent metrics");
            throw;
        }
    }

    /// <summary>
    /// Reset leader agent metrics
    /// </summary>
    public async Task ResetLeaderMetricsAsync(ILayeredLeaderAgent leaderAgent)
    {
        try
        {
            await leaderAgent.ResetMetricsAsync();
            _logger.LogDebug("✅ Reset leader metrics");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to reset leader metrics");
            throw;
        }
    }

    /// <summary>
    /// Reset sub-agent metrics
    /// </summary>
    public async Task ResetSubAgentMetricsAsync(ILayeredSubAgent subAgent)
    {
        try
        {
            await subAgent.ResetMetricsAsync();
            _logger.LogDebug("✅ Reset sub-agent metrics");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to reset sub-agent metrics");
            throw;
        }
    }

    /// <summary>
    /// Generate a consistent GUID from a string (for backward compatibility if needed)
    /// </summary>
    private static Guid GenerateConsistentGuid(string input)
    {
        // Use MD5 hash to generate consistent GUID from string input
        using (var md5 = System.Security.Cryptography.MD5.Create())
        {
            var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return new Guid(hash);
        }
    }
} 