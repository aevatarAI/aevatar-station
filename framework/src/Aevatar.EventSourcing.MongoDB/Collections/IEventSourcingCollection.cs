using MongoDB.Bson;
using MongoDB.Driver;

namespace Aevatar.EventSourcing.MongoDB.Collections;

/// <summary>
/// Interface for EventSourcingCollection to enable mocking and testing
/// </summary>
public interface IEventSourcingCollection
{
    /// <summary>
    /// Gets the MongoDB collection with automatic index management
    /// </summary>
    IMongoCollection<BsonDocument> GetCollection();
} 