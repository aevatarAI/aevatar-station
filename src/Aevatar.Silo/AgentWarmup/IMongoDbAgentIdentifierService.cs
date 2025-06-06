using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aevatar.Silo.AgentWarmup;

/// <summary>
/// Service for retrieving agent identifiers directly from MongoDB collections
/// </summary>
public interface IMongoDbAgentIdentifierService
{
    /// <summary>
    /// Gets agent identifiers from MongoDB for a specific agent type
    /// </summary>
    /// <typeparam name="TIdentifier">The identifier type (Guid, string, int, long)</typeparam>
    /// <param name="agentType">The agent type</param>
    /// <param name="maxCount">Maximum number of identifiers to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of agent identifiers</returns>
    IAsyncEnumerable<TIdentifier> GetAgentIdentifiersAsync<TIdentifier>(
        Type agentType, 
        int? maxCount = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the MongoDB collection name for a agent type
    /// </summary>
    /// <param name="agentType">The agent type</param>
    /// <returns>The collection name</returns>
    string GetCollectionName(Type agentType);
    
    /// <summary>
    /// Checks if a collection exists for the agent type
    /// </summary>
    /// <param name="agentType">The agent type</param>
    /// <returns>True if the collection exists</returns>
    Task<bool> CollectionExistsAsync(Type agentType);
    
    /// <summary>
    /// Gets the count of documents in a agent collection
    /// </summary>
    /// <param name="agentType">The agent type</param>
    /// <returns>The number of documents in the collection</returns>
    Task<long> GetAgentCountAsync(Type agentType);
} 