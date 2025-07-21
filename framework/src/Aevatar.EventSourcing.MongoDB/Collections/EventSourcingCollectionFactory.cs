using Aevatar.EventSourcing.MongoDB.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Aevatar.EventSourcing.MongoDB.Collections;

/// <summary>
/// Factory implementation for creating EventSourcingCollection instances.
/// This class is registered with IoC and creates MongoDB clients as needed.
/// </summary>
public class EventSourcingCollectionFactory : IEventSourcingCollectionFactory
{
    private readonly IOptionsMonitor<MongoDbStorageOptions> _optionsMonitor;
    private readonly ILoggerFactory _loggerFactory;

    public EventSourcingCollectionFactory(
        IOptionsMonitor<MongoDbStorageOptions> optionsMonitor,
        ILoggerFactory loggerFactory)
    {
        _optionsMonitor = optionsMonitor;
        _loggerFactory = loggerFactory;
    }

    public IEventSourcingCollection CreateCollection(IMongoClient mongoClient, string collectionName, string providerName)
    {
        var options = _optionsMonitor.Get(providerName);
        
        if (string.IsNullOrEmpty(options.Database))
        {
            throw new InvalidOperationException($"Database name is not configured in MongoDbStorageOptions for provider '{providerName}'.");
        }

        var logger = _loggerFactory.CreateLogger<EventSourcingCollection>();
        
        return new EventSourcingCollection(
            mongoClient, // Use provided client instead of creating new one
            options.Database,
            collectionName,
            options,
            options.CollectionConfigurator, // Pass user's collection configurator
            options.CreateShardKey, // Use user's shard key setting
            logger);
    }
} 