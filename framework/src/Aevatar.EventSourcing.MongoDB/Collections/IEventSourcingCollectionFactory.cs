using MongoDB.Bson;
using MongoDB.Driver;

namespace Aevatar.EventSourcing.MongoDB.Collections;

/// <summary>
/// Factory interface for creating EventSourcingCollection instances.
/// This enables dependency injection and proper separation of concerns.
/// </summary>
public interface IEventSourcingCollectionFactory
{
    /// <summary>
    /// Creates an EventSourcingCollection for the specified collection and provider.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client to use for the collection.</param>
    /// <param name="collectionName">The name of the collection to create.</param>
    /// <param name="providerName">The name of the storage provider (used for options lookup).</param>
    /// <returns>A configured EventSourcingCollection instance.</returns>
    IEventSourcingCollection CreateCollection(IMongoClient mongoClient, string collectionName, string providerName);
} 