using System;
using System.Collections.Generic;

namespace Aevatar.Silo.GrainWarmup;

/// <summary>
/// Service for automatically discovering warmup-eligible grain types from assemblies
/// </summary>
public interface IGrainDiscoveryService
{
    /// <summary>
    /// Discovers all warmup-eligible grain types from loaded assemblies
    /// </summary>
    /// <param name="excludedTypes">Optional types to exclude from discovery</param>
    /// <returns>Collection of discovered grain types</returns>
    IEnumerable<Type> DiscoverWarmupEligibleGrainTypes(IEnumerable<Type>? excludedTypes = null);
    
    /// <summary>
    /// Checks if a grain type is eligible for warmup
    /// </summary>
    /// <param name="grainType">The grain type to check</param>
    /// <returns>True if the grain type is eligible for warmup</returns>
    bool IsWarmupEligible(Type grainType);
    
    /// <summary>
    /// Gets the identifier type for a grain type
    /// </summary>
    /// <param name="grainType">The grain type</param>
    /// <returns>The identifier type (Guid, string, int, long)</returns>
    Type GetGrainIdentifierType(Type grainType);
    
    /// <summary>
    /// Gets a mapping of grain types to their identifier types
    /// </summary>
    /// <returns>Dictionary mapping grain types to identifier types</returns>
    Dictionary<Type, Type> GetGrainTypeMapping();
} 