using System;
using System.Collections.Generic;

namespace Aevatar.Silo.AgentWarmup;

/// <summary>
/// Service for automatically discovering warmup-eligible agent types from assemblies
/// </summary>
public interface IAgentDiscoveryService
{
    /// <summary>
    /// Discovers all warmup-eligible agent types from loaded assemblies
    /// </summary>
    /// <param name="excludedTypes">Optional types to exclude from discovery</param>
    /// <returns>Collection of discovered agent types</returns>
    IEnumerable<Type> DiscoverWarmupEligibleAgentTypes(IEnumerable<Type>? excludedTypes = null);
    
    /// <summary>
    /// Checks if a agent type is eligible for warmup
    /// </summary>
    /// <param name="agentType">The agent type to check</param>
    /// <returns>True if the agent type is eligible for warmup</returns>
    bool IsWarmupEligible(Type agentType);
    
    /// <summary>
    /// Gets the identifier type for a agent type
    /// </summary>
    /// <param name="agentType">The agent type</param>
    /// <returns>The identifier type (Guid, string, int, long)</returns>
    Type GetAgentIdentifierType(Type agentType);
    
    /// <summary>
    /// Gets a mapping of agent types to their identifier types
    /// </summary>
    /// <returns>Dictionary mapping agent types to identifier types</returns>
    Dictionary<Type, Type> GetAgentTypeMapping();
} 