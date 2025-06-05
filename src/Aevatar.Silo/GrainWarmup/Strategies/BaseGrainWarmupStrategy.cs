using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aevatar.Silo.AgentWarmup.Strategies;

/// <summary>
/// Base class for agent warmup strategies providing common functionality
/// </summary>
/// <typeparam name="TIdentifier">The identifier type (Guid, string, int, long)</typeparam>
public abstract class BaseAgentWarmupStrategy<TIdentifier> : IAgentWarmupStrategy<TIdentifier>
{
    protected readonly ILogger Logger;

    protected BaseAgentWarmupStrategy(ILogger logger)
    {
        Logger = logger;
    }

    /// <summary>
    /// Strategy name for identification
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Agent types this strategy applies to (empty = applies to all)
    /// </summary>
    public abstract IEnumerable<Type> ApplicableAgentTypes { get; }

    /// <summary>
    /// Priority for execution order (higher = earlier)
    /// </summary>
    public abstract int Priority { get; }

    /// <summary>
    /// Gets the primary agent type (for interface compatibility)
    /// </summary>
    public virtual Type AgentType => ApplicableAgentTypes.FirstOrDefault() ?? typeof(object);

    /// <summary>
    /// Gets the estimated number of agents that will be warmed up
    /// </summary>
    public abstract int EstimatedAgentCount { get; }

    /// <summary>
    /// Generates agent identifiers for a specific agent type
    /// </summary>
    /// <param name="agentType">The agent type to generate identifiers for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of agent identifiers</returns>
    public abstract IAsyncEnumerable<TIdentifier> GenerateAgentIdentifiersAsync(
        Type agentType, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates the agent identifiers to warm up (strongly typed version)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enumerable of strongly typed agent identifiers</returns>
    public virtual IAsyncEnumerable<TIdentifier> GenerateAgentIdentifiersAsync(CancellationToken cancellationToken = default)
    {
        return GenerateAgentIdentifiersAsync(AgentType, cancellationToken);
    }

    /// <summary>
    /// Generates the agent identifiers to warm up (non-generic version)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enumerable of agent identifiers as objects</returns>
    async IAsyncEnumerable<object> IAgentWarmupStrategy.GenerateAgentIdentifiersAsync(CancellationToken cancellationToken)
    {
        await foreach (var identifier in GenerateAgentIdentifiersAsync(AgentType, cancellationToken))
        {
            yield return identifier!;
        }
    }

    /// <summary>
    /// Checks if strategy applies to a agent type
    /// </summary>
    /// <param name="agentType">The agent type to check</param>
    /// <returns>True if the strategy applies to this agent type</returns>
    public virtual bool AppliesTo(Type agentType)
    {
        var applicableTypes = ApplicableAgentTypes.ToList();
        
        // If no specific types are defined, applies to all
        if (!applicableTypes.Any())
            return true;

        // Check if the agent type is in the applicable types list
        return applicableTypes.Contains(agentType) || 
               applicableTypes.Any(t => t.IsAssignableFrom(agentType));
    }

    /// <summary>
    /// Creates agent reference for a specific agent type and identifier
    /// </summary>
    /// <typeparam name="T">The agent interface type that inherits from IGrainBase</typeparam>
    /// <param name="agentFactory">The agent factory</param>
    /// <param name="agentType">The agent type</param>
    /// <param name="identifier">The agent identifier</param>
    /// <returns>The agent reference of type T</returns>
    public virtual T CreateAgentReference<T>(IGrainFactory agentFactory, Type agentType, TIdentifier identifier) where T : IGrainBase
    {
        try
        {
            // Determine the agent interface type
            var agentInterface = GetAgentInterface(agentType);
            if (agentInterface == null)
            {
                throw new InvalidOperationException($"Could not determine agent interface for type {agentType.Name}");
            }

            // Create agent reference using non-generic GetGrain and cast to T
            var agent = typeof(TIdentifier) switch
            {
                var t when t == typeof(Guid) => agentFactory.GetGrain(agentInterface, (Guid)(object)identifier!),
                var t when t == typeof(string) => agentFactory.GetGrain(agentInterface, (string)(object)identifier!),
                var t when t == typeof(long) => agentFactory.GetGrain(agentInterface, (long)(object)identifier!),
                var t when t == typeof(int) => agentFactory.GetGrain(agentInterface, (long)(int)(object)identifier!),
                _ => throw new NotSupportedException($"Identifier type {typeof(TIdentifier).Name} is not supported")
            };

            return (T)agent;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating agent reference for type {AgentType} with identifier {Identifier}", 
                agentType.Name, identifier);
            throw;
        }
    }

    /// <summary>
    /// Gets the agent interface type for a agent implementation type
    /// </summary>
    /// <param name="agentType">The agent implementation type</param>
    /// <returns>The agent interface type</returns>
    protected virtual Type? GetAgentInterface(Type agentType)
    {
        // Look for Orleans agent interfaces
        var interfaces = agentType.GetInterfaces();
        
        // Find the most specific agent interface
        var agentInterfaces = interfaces.Where(i => 
            typeof(IGrainWithGuidKey).IsAssignableFrom(i) ||
            typeof(IGrainWithStringKey).IsAssignableFrom(i) ||
            typeof(IGrainWithIntegerKey).IsAssignableFrom(i) ||
            typeof(IGrainWithGuidCompoundKey).IsAssignableFrom(i) ||
            typeof(IGrainWithIntegerCompoundKey).IsAssignableFrom(i))
            .ToList();

        // Return the most specific interface (not the base Orleans interfaces)
        return agentInterfaces
            .Where(i => i != typeof(IGrainWithGuidKey) && 
                       i != typeof(IGrainWithStringKey) && 
                       i != typeof(IGrainWithIntegerKey) &&
                       i != typeof(IGrainWithGuidCompoundKey) &&
                       i != typeof(IGrainWithIntegerCompoundKey))
            .FirstOrDefault() ?? agentInterfaces.FirstOrDefault();
    }

    /// <summary>
    /// Validates that the identifier type matches the agent's expected identifier type
    /// </summary>
    /// <param name="agentType">The agent type</param>
    /// <returns>True if the identifier type is compatible</returns>
    protected virtual bool ValidateIdentifierType(Type agentType)
    {
        var interfaces = agentType.GetInterfaces();

        if (typeof(TIdentifier) == typeof(Guid))
        {
            return interfaces.Any(i => 
                typeof(IGrainWithGuidKey).IsAssignableFrom(i) ||
                typeof(IGrainWithGuidCompoundKey).IsAssignableFrom(i));
        }
        else if (typeof(TIdentifier) == typeof(string))
        {
            return interfaces.Any(i => typeof(IGrainWithStringKey).IsAssignableFrom(i));
        }
        else if (typeof(TIdentifier) == typeof(long) || typeof(TIdentifier) == typeof(int))
        {
            return interfaces.Any(i => 
                typeof(IGrainWithIntegerKey).IsAssignableFrom(i) ||
                typeof(IGrainWithIntegerCompoundKey).IsAssignableFrom(i));
        }

        return false;
    }

    /// <summary>
    /// Logs strategy execution information
    /// </summary>
    /// <param name="agentType">The agent type being processed</param>
    /// <param name="identifierCount">Number of identifiers being processed</param>
    protected virtual void LogStrategyExecution(Type agentType, int identifierCount)
    {
        Logger.LogInformation("Strategy {StrategyName} processing {Count} identifiers for agent type {AgentType}", 
            Name, identifierCount, agentType.Name);
    }

    /// <summary>
    /// Creates a agent reference for the given identifier (strongly typed version)
    /// </summary>
    /// <typeparam name="T">The agent interface type that inherits from IGrainBase</typeparam>
    /// <param name="agentFactory">The agent factory</param>
    /// <param name="identifier">The strongly typed agent identifier</param>
    /// <returns>The agent reference of type T</returns>
    public virtual T CreateAgentReference<T>(IGrainFactory agentFactory, TIdentifier identifier) where T : IGrainBase
    {
        return CreateAgentReference<T>(agentFactory, AgentType, identifier);
    }

    /// <summary>
    /// Creates a agent reference for the given identifier (non-generic version)
    /// </summary>
    /// <typeparam name="T">The agent interface type that inherits from IGrainBase</typeparam>
    /// <param name="agentFactory">The agent factory</param>
    /// <param name="identifier">The agent identifier</param>
    /// <returns>The agent reference of type T</returns>
    public virtual T CreateAgentReference<T>(IGrainFactory agentFactory, object identifier) where T : IGrainBase
    {
        return CreateAgentReference<T>(agentFactory, (TIdentifier)identifier);
    }

    /// <summary>
    /// Validates that the strategy configuration is correct
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public virtual bool IsValid()
    {
        return ApplicableAgentTypes.Any() && EstimatedAgentCount > 0;
    }
} 