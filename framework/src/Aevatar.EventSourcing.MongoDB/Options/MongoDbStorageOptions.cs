using MongoDB.Driver;
using Orleans.Storage;
using Orleans.Providers.MongoDB.StorageProviders.Serializers;
using System;

namespace Aevatar.EventSourcing.MongoDB.Options;

public class MongoDbStorageOptions : IStorageProviderSerializerOptions
{
    public int InitStage { get; set; } = ServiceLifecycleStage.ApplicationServices;

    /// <summary>
    /// This is not in use, it is only for compatibility
    /// </summary>
    public IGrainStorageSerializer GrainStorageSerializer { get; set; } = null!;

    /// <summary>
    /// The serializer to use for the grain state.
    /// </summary>
    public IGrainStateSerializer GrainStateSerializer { get; set; } = null!;

    [Redact] public MongoClientSettings ClientSettings { get; set; } = null!;

    public string? Database { get; set; }

    [Redact] public MongoCredential? Credentials { get; set; }

    /// <summary>
    /// The collection configurator for customizing MongoDB collection settings.
    /// This allows users to set read/write concerns, serialization settings, etc.
    /// </summary>
    public Action<MongoCollectionSettings>? CollectionConfigurator { get; set; }

    /// <summary>
    /// Whether to create a shard key for MongoDB collections.
    /// </summary>
    public bool CreateShardKey { get; set; }

    /// <summary>
    /// List of index definitions to create on event sourcing collections.
    /// </summary>
    public List<IndexDefinition> Indexes { get; set; } = new List<IndexDefinition>();

    /// <summary>
    /// Whether to automatically create indexes during collection initialization.
    /// Default is true.
    /// </summary>
    public bool CreateIndexesOnInitialization { get; set; } = true;

    /// <summary>
    /// Whether to ignore index creation conflicts and continue operation.
    /// Default is true.
    /// </summary>
    public bool IgnoreIndexConflicts { get; set; } = true;

    /// <summary>
    /// Whether to create default indexes for event sourcing operations.
    /// Default is true.
    /// </summary>
    public bool CreateDefaultIndexes { get; set; } = true;
}