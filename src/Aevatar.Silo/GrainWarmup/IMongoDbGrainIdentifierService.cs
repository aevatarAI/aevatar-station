using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aevatar.Silo.GrainWarmup;

/// <summary>
/// Service for retrieving grain identifiers directly from MongoDB collections
/// </summary>
public interface IMongoDbGrainIdentifierService
{
    /// <summary>
    /// Gets grain identifiers from MongoDB for a specific grain type
    /// </summary>
    /// <typeparam name="TIdentifier">The identifier type (Guid, string, int, long)</typeparam>
    /// <param name="grainType">The grain type</param>
    /// <param name="maxCount">Maximum number of identifiers to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of grain identifiers</returns>
    IAsyncEnumerable<TIdentifier> GetGrainIdentifiersAsync<TIdentifier>(
        Type grainType, 
        int? maxCount = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the MongoDB collection name for a grain type
    /// </summary>
    /// <param name="grainType">The grain type</param>
    /// <returns>The collection name</returns>
    string GetCollectionName(Type grainType);
    
    /// <summary>
    /// Checks if a collection exists for the grain type
    /// </summary>
    /// <param name="grainType">The grain type</param>
    /// <returns>True if the collection exists</returns>
    Task<bool> CollectionExistsAsync(Type grainType);
    
    /// <summary>
    /// Gets the count of documents in a grain collection
    /// </summary>
    /// <param name="grainType">The grain type</param>
    /// <returns>The number of documents in the collection</returns>
    Task<long> GetGrainCountAsync(Type grainType);
} 