using System.Diagnostics;
using Aevatar.EventSourcing.Core.Storage;
using Aevatar.EventSourcing.MongoDB.Collections;
using Aevatar.EventSourcing.MongoDB.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Aevatar.EventSourcing.MongoDB.Serializers;
using MongoDB.Driver;
using Orleans.Configuration;
using Orleans.Storage;
using Orleans.Providers.MongoDB.StorageProviders.Serializers;

namespace Aevatar.EventSourcing.MongoDB;

public class MongoDbLogConsistentStorage : ILogConsistentStorage, ILifecycleParticipant<ISiloLifecycle>
{
    private readonly ILogger<MongoDbLogConsistentStorage> _logger;
    private readonly string _name;
    private readonly MongoDbStorageOptions _mongoDbOptions;
    private readonly IEventSourcingCollectionFactory _collectionFactory;

    private IMongoClient? _client;

    private bool _initialized;
    private readonly string _serviceId;

    private readonly string _fieldData = "snapshot";
    private readonly IGrainStateSerializer _grainStateSerializer;
    
    public MongoDbLogConsistentStorage(string name, MongoDbStorageOptions options,
        IOptions<ClusterOptions> clusterOptions, ILogger<MongoDbLogConsistentStorage> logger,
        IEventSourcingCollectionFactory collectionFactory)
    {
        _name = name;
        _mongoDbOptions = options;
        _serviceId = clusterOptions.Value.ServiceId;
        _logger = logger;
        _collectionFactory = collectionFactory;
        
        if (options.GrainStateSerializer is null)
        {
            throw new ArgumentNullException(nameof(options.GrainStateSerializer), "GrainStateSerializer is required");
        }
        else
        {
            _grainStateSerializer = options.GrainStateSerializer;
        }
    }

    public async Task<IReadOnlyList<TLogEntry>> ReadAsync<TLogEntry>(string grainTypeName, GrainId grainId,
        int fromVersion, int maxCount)
    {
        if (!_initialized || _client == null || maxCount <= 0)
        {
            return new List<TLogEntry>();
        }

        var collectionName = GetStreamName(grainId);
        try
        {
            var collection = GetCollection(collectionName).GetCollection();

            var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("GrainId", grainId.ToString()),
                Builders<BsonDocument>.Filter.Gte("Version", fromVersion)
            );
            var sort = Builders<BsonDocument>.Sort.Ascending("Version");
            var options = new FindOptions<BsonDocument>
            {
                Limit = maxCount,
                Sort = sort
            };

            var documents = await collection.FindAsync(filter, options).ConfigureAwait(false);
            var results = new List<TLogEntry>();

            await documents.ForEachAsync(document =>
            {
                try
                {
                    // Deserialize using framework format (Orleans data should already be converted)
                    var logEntry = _grainStateSerializer.Deserialize<TLogEntry>(document[_fieldData]);
                    results.Add(logEntry);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "🚨 Failed to deserialize log entry for {GrainType} grain {GrainId} - Document: {Document}", 
                        grainTypeName, grainId, document.ToJson());
                    throw;
                }
            }).ConfigureAwait(false);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to read log entries for {GrainType} grain with ID {GrainId} and collection {CollectionName}",
                grainTypeName, grainId, collectionName);
            throw new MongoDbStorageException(FormattableString.Invariant(
                $"Failed to read log entries for {grainTypeName} with ID {grainId} and collection {collectionName}. {ex.GetType()}: {ex.Message}"));
        }
    }

    private IMongoDatabase GetDatabase()
    {
        return _client!.GetDatabase(_mongoDbOptions.Database);
    }

    private IEventSourcingCollection GetCollection(string collectionName)
    {
        if (!_initialized || _client == null)
        {
            throw new InvalidOperationException("MongoDB client is not initialized. Call Init() first.");
        }

        return _collectionFactory.CreateCollection(_client, collectionName, _name);
    }

    public async Task<int> GetLastVersionAsync(string grainTypeName, GrainId grainId)
    {
        if (!_initialized || _client == null)
        {
            return -1;
        }

        var collectionName = GetStreamName(grainId);
        try
        {
            var collection = GetCollection(collectionName).GetCollection();

            var grainIdString = grainId.ToString();
            var filter = Builders<BsonDocument>.Filter.Eq("GrainId", grainIdString);
            var sort = Builders<BsonDocument>.Sort.Descending("Version");
            var options = new FindOptions<BsonDocument>
            {
                Limit = 1,
                Sort = sort
            };

            var document = await collection.Find(filter).Sort(sort).Limit(1).FirstOrDefaultAsync()
                .ConfigureAwait(false);
            if (document == null)
            {
                return -1;
            }

            return document["Version"].AsInt32;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to read last log entry for {GrainType} grain with ID {GrainId} and collection {CollectionName}",
                grainTypeName, grainId, collectionName);
            throw new MongoDbStorageException(FormattableString.Invariant(
                $"Failed to read last log entry for {grainTypeName} with ID {grainId} and collection {collectionName}. {ex.GetType()}: {ex.Message}"));
        }
    }

    public async Task<int> AppendAsync<TLogEntry>(string grainTypeName, GrainId grainId, IList<TLogEntry> entries,
        int expectedVersion)
    {
        if (!_initialized || _client == null)
        {
            return -1;
        }

        var collectionName = GetStreamName(grainId);
        if (entries.Count == 0)
        {
            return await GetLastVersionAsync(grainTypeName, grainId);
        }

        try
        {
            var collection = GetCollection(collectionName).GetCollection();

            var currentVersion = await GetLastVersionAsync(grainTypeName, grainId).ConfigureAwait(false);
            if (currentVersion != expectedVersion)
            {
                throw new InconsistentStateException(
                    $"Version conflict ({nameof(AppendAsync)}): ServiceId={_serviceId} ProviderName={_name} GrainType={grainTypeName} GrainId={grainId} Version={expectedVersion}.");
            }

            var grainIdString = grainId.ToString();
            var documents = new List<BsonDocument>();

            foreach (var entry in entries)
            {
                currentVersion++;
                
                // Serialize the entry using our grain state serializer
                var data = _grainStateSerializer.Serialize(entry);
                
                var document = new BsonDocument
                {
                    ["GrainId"] = grainIdString,
                    ["Version"] = currentVersion,
                    [_fieldData] = data
                };
                
                documents.Add(document);
            }

            await collection.InsertManyAsync(documents).ConfigureAwait(false);

            return currentVersion;
        }
        catch (Exception ex) when (ex is not InconsistentStateException)
        {
            _logger.LogError(ex,
                "Failed to write log entries for {GrainType} grain with ID {GrainId} and collection {CollectionName}",
                grainTypeName, grainId, collectionName);
            throw new MongoDbStorageException(FormattableString.Invariant(
                $"Failed to write log entries for {grainTypeName} with ID {grainId} and collection {collectionName}. {ex.GetType()}: {ex.Message}"));
        }
    }

    public async Task SetInitialVersionAsync(string grainTypeName, GrainId grainId, int initialVersion)
    {
        var grainIdString = grainId.ToString();
        var collectionName = GetStreamName(grainId);
        
        try
        {
            var database = GetDatabase();
            var collection = database.GetCollection<BsonDocument>(collectionName);
            
            // Check if any data already exists
            var existingVersion = await GetLastVersionAsync(grainTypeName, grainId);
            if (existingVersion >= 0)
            {
                _logger.LogInformation("Grain {GrainId} already has version {ExistingVersion}, skipping initial version setup", 
                    grainId, existingVersion);
                return;
            }
            
            // Create a placeholder document with the initial version
            var placeholderDocument = new BsonDocument
            {
                ["GrainId"] = grainIdString,
                ["Version"] = initialVersion,
                [_fieldData] = BsonDocument.Parse("{\"_t\":\"MigrationPlaceholder\",\"Message\":\"Version placeholder for Orleans migration\"}")
            };
            
            await collection.InsertOneAsync(placeholderDocument).ConfigureAwait(false);
            
            _logger.LogDebug("Set initial version {InitialVersion} for grain {GrainId}", 
                initialVersion, grainId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to set initial version {InitialVersion} for {GrainType} grain with ID {GrainId} and collection {CollectionName}",
                initialVersion, grainTypeName, grainId, collectionName);
            throw new MongoDbStorageException(FormattableString.Invariant(
                $"Failed to set initial version {initialVersion} for {grainTypeName} with ID {grainId} and collection {collectionName}. {ex.GetType()}: {ex.Message}"));
        }
    }

    public void Participate(ISiloLifecycle observer)
    {
        var name = OptionFormattingUtilities.Name<MongoDbLogConsistentStorage>(_name);
        observer.Subscribe(name, _mongoDbOptions.InitStage, Init, Close);
    }

    private async Task Init(CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        try
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("EventStoreLogConsistentStorage {Name} is initializing: ServiceId={ServiceId}", _name,
                    _serviceId);
            }

            _client = new MongoClient(_mongoDbOptions.ClientSettings);
            
            _initialized = true;
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                timer.Stop();
                _logger.LogDebug("Init: Name={Name} ServiceId={ServiceId}, initialized in {ElapsedMilliseconds} ms",
                    _name, _serviceId, timer.Elapsed.TotalMilliseconds.ToString("0.00"));
            }
        }
        catch (Exception ex)
        {
            timer.Stop();
            _logger.LogError(ex, "Init: Name={Name} ServiceId={ServiceId}, errored in {ElapsedMilliseconds} ms", _name,
                _serviceId, timer.Elapsed.TotalMilliseconds.ToString("0.00"));
            throw new MongoDbStorageException(FormattableString.Invariant($"{ex.GetType()}: {ex.Message}"));
        }

        return;
    }
    
    private async Task Close(CancellationToken cancellationToken)
    {
        if (!_initialized || _client == null)
        {
            return;
        }

        try
        {
            _client.Cluster.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Close: Name={Name} ServiceId={ServiceId}", _name, _serviceId);
            throw new MongoDbStorageException(FormattableString.Invariant($"{ex.GetType()}: {ex.Message}"));
        }
    }

    private string GetStreamName(GrainId grainId)
    {
        return $"{_serviceId}/{_name}/log/{grainId.Type}";
    }

}