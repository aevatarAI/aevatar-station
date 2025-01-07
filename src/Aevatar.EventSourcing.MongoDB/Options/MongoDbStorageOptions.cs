using MongoDB.Driver;
using Orleans.Storage;

namespace Aevatar.EventSourcing.MongoDB.Options;

public class MongoDbStorageOptions : IStorageProviderSerializerOptions
{
    public int InitStage { get; set; } = ServiceLifecycleStage.ApplicationServices;

    public IGrainStorageSerializer GrainStorageSerializer { get; set; } = null!;

    [Redact] public MongoClientSettings ClientSettings { get; set; } = null!;

    public string? Database { get; set; }

    [Redact] public MongoCredential? Credentials { get; set; }
}