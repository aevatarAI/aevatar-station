using Orleans;

namespace Aevatar.Silo.GrainWarmup;

/// <summary>
/// Base interface for grain warmup strategies (non-generic for service compatibility)
/// </summary>
public interface IGrainWarmupStrategy
{
    /// <summary>
    /// Gets the name of this warmup strategy
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Gets the grain type that this strategy applies to
    /// </summary>
    Type GrainType { get; }
    
    /// <summary>
    /// Gets the priority for execution order (higher = earlier)
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// Gets the estimated number of grains that will be warmed up
    /// </summary>
    int EstimatedGrainCount { get; }
    
    /// <summary>
    /// Generates the grain identifiers to warm up (non-generic version)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enumerable of grain identifiers as objects</returns>
    IAsyncEnumerable<object> GenerateGrainIdentifiersAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a grain reference for the given identifier (generic version)
    /// </summary>
    /// <typeparam name="T">The grain interface type that inherits from IGrainBase</typeparam>
    /// <param name="grainFactory">The grain factory</param>
    /// <param name="identifier">The grain identifier</param>
    /// <returns>The grain reference of type T</returns>
    T CreateGrainReference<T>(IGrainFactory grainFactory, object identifier) where T : IGrainBase;
    
    /// <summary>
    /// Validates that the strategy configuration is correct
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    bool IsValid();
    
    /// <summary>
    /// Checks if strategy applies to a grain type
    /// </summary>
    /// <param name="grainType">The grain type to check</param>
    /// <returns>True if the strategy applies to this grain type</returns>
    bool AppliesTo(Type grainType);
}

/// <summary>
/// Generic interface for grain warmup strategies with strong typing for identifiers
/// </summary>
/// <typeparam name="TIdentifier">The type of grain identifier (Guid, string, int, long)</typeparam>
public interface IGrainWarmupStrategy<TIdentifier> : IGrainWarmupStrategy
{
    /// <summary>
    /// Generates the grain identifiers to warm up (strongly typed version)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enumerable of strongly typed grain identifiers</returns>
    new IAsyncEnumerable<TIdentifier> GenerateGrainIdentifiersAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates the grain identifiers to warm up for a specific grain type
    /// </summary>
    /// <param name="grainType">The grain type to generate identifiers for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enumerable of strongly typed grain identifiers</returns>
    IAsyncEnumerable<TIdentifier> GenerateGrainIdentifiersAsync(Type grainType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a grain reference for the given identifier (strongly typed version)
    /// </summary>
    /// <typeparam name="T">The grain interface type that inherits from IGrainBase</typeparam>
    /// <param name="grainFactory">The grain factory</param>
    /// <param name="identifier">The strongly typed grain identifier</param>
    /// <returns>The grain reference of type T</returns>
    T CreateGrainReference<T>(IGrainFactory grainFactory, TIdentifier identifier) where T : IGrainBase;
    
    /// <summary>
    /// Creates a grain reference for a specific grain type and identifier
    /// </summary>
    /// <typeparam name="T">The grain interface type that inherits from IGrainBase</typeparam>
    /// <param name="grainFactory">The grain factory</param>
    /// <param name="grainType">The grain type</param>
    /// <param name="identifier">The grain identifier</param>
    /// <returns>The grain reference of type T</returns>
    T CreateGrainReference<T>(IGrainFactory grainFactory, Type grainType, TIdentifier identifier) where T : IGrainBase;
} 