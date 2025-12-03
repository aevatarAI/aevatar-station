using Aevatar.EventSourcing.MongoDB.Collections;
using Aevatar.EventSourcing.MongoDB.Options;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Aevatar.EventSourcing.MongoDB.Tests;

[Collection(nameof(MongoDbTestCollection))]
public class IndexCreationTests : IDisposable
{
    private readonly IMongoClient _mongoClient;
    private readonly IMongoDatabase _database;
    private readonly string _databaseName;
    private readonly ILogger<EventSourcingCollection> _logger;

    public IndexCreationTests()
    {
        var connectionString = AevatarMongoDbFixture.GetRandomConnectionString();
        _mongoClient = new MongoClient(connectionString);
        _databaseName = $"EventSourcingTest_{Guid.NewGuid():N}";
        _database = _mongoClient.GetDatabase(_databaseName);
        
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<EventSourcingCollection>();
    }

    [Fact]
    public void Should_Create_Default_Indexes_When_Enabled()
    {
        // Arrange
        var options = new MongoDbStorageOptions
        {
            CreateIndexesOnInitialization = true,
            IgnoreIndexConflicts = true,
            Indexes = new List<IndexDefinition>() // Empty list triggers default indexes
        };
        
        var testCollectionName = "TestCollection_Default";

        // Act
        var eventSourcingCollection = new EventSourcingCollection(
            _mongoClient, 
            _databaseName, 
            testCollectionName, 
            options, 
            null!, 
            false, 
            _logger);
            
        var collection = eventSourcingCollection.GetCollection();

        // Assert
        var indexes = collection.Indexes.List();
        var indexList = indexes.ToList();
        
        // Should have the default _id index plus our 2 default indexes (total 3)
        Assert.True(indexList.Count >= 3, $"Expected at least 3 indexes, but found {indexList.Count}");
        
        // Check for our specific indexes by name
        var indexNames = indexList.Select(idx => idx["name"].AsString).ToList();
        
        Assert.Contains("GrainId_1", indexNames);
        Assert.Contains("GrainId_1_Version_-1", indexNames);
        // Note: Using MongoDB default naming convention: fieldname_1_field2_-1
    }

    [Fact]
    public void Should_Create_Custom_Indexes()
    {
        // Arrange
        var customIndexes = new List<IndexDefinition>
        {
            new IndexDefinition
            {
                Name = "CustomIndex1",
                Keys = new List<IndexKey> { new IndexKey("CustomField1", Options.SortDirection.Ascending) }
            },
            new IndexDefinition
            {
                Name = "CustomIndex2", 
                Keys = new List<IndexKey> 
                { 
                    new IndexKey("CustomField2", Options.SortDirection.Ascending),
                    new IndexKey("CustomField3", Options.SortDirection.Ascending)
                }
            },
            // Test MongoDB default naming when no explicit name is provided
            new IndexDefinition
            {
                // No Name property set - should use MongoDB default naming
                Keys = new List<IndexKey> 
                { 
                    new IndexKey("AutoField1", Options.SortDirection.Ascending),
                    new IndexKey("AutoField2", Options.SortDirection.Descending)
                }
            }
        };

        var options = new MongoDbStorageOptions
        {
            CreateIndexesOnInitialization = true,
            IgnoreIndexConflicts = true,
            Indexes = customIndexes
        };
        
        var testCollectionName = "TestCollection_Custom";

        // Act
        var eventSourcingCollection = new EventSourcingCollection(
            _mongoClient, 
            _databaseName, 
            testCollectionName, 
            options, 
            null!, 
            false, 
            _logger);
            
        var collection = eventSourcingCollection.GetCollection();

        // Assert
        var indexes = collection.Indexes.List();
        var indexList = indexes.ToList();
        var indexNames = indexList.Select(idx => idx["name"].AsString).ToList();
        
        Assert.Contains("CustomIndex1", indexNames);
        Assert.Contains("CustomIndex2", indexNames);
        // Verify MongoDB default naming convention is used when no explicit name is provided
        Assert.Contains("AutoField1_1_AutoField2_-1", indexNames);
    }

    [Fact]
    public void Should_Handle_Index_Creation_Gracefully_When_Disabled()
    {
        // Arrange
        var options = new MongoDbStorageOptions
        {
            CreateIndexesOnInitialization = false // Disabled
        };
        
        var testCollectionName = "TestCollection_Disabled";

        // Act
        var eventSourcingCollection = new EventSourcingCollection(
            _mongoClient, 
            _databaseName, 
            testCollectionName, 
            options, 
            null!, 
            false, 
            _logger);
            
        var collection = eventSourcingCollection.GetCollection();

        // Force collection creation by inserting and removing a dummy document
        var dummyDoc = new BsonDocument { ["_id"] = "dummy" };
        collection.InsertOne(dummyDoc);
        collection.DeleteOne(Builders<BsonDocument>.Filter.Eq("_id", "dummy"));

        // Assert
        var indexes = collection.Indexes.List();
        var indexList = indexes.ToList();
        
        // Should only have the default _id index (no custom indexes created)
        Assert.Single(indexList);
        Assert.Equal("_id_", indexList[0]["name"].AsString);
    }



    public void Dispose()
    {
        try
        {
            _mongoClient.DropDatabase(_databaseName);
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }
} 