using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using Moq;
using Orleans.Configuration;
using Orleans.Runtime;
using Orleans.Storage;
using Shouldly;
using Xunit;

using Aevatar.EventSourcing.Core.Storage;
using Aevatar.EventSourcing.MongoDB.Options;
using Aevatar.EventSourcing.Core.Exceptions;
using Aevatar.EventSourcing.MongoDB.Hosting;
using Aevatar.EventSourcing.MongoDB.Serializers;
using Aevatar.EventSourcing.MongoDB.Collections;

namespace Aevatar.EventSourcing.MongoDB.Tests;

[Collection(nameof(MongoDbTestCollection))]
public class MongoDbLogConsistentStorageMongoTests
{
    private readonly Mock<IMongoCollection<BsonDocument>> _mongoCollectionMock;
    private MongoDbLogConsistentStorage _storage;
    private readonly string _name;
    private MongoDbStorageOptions _mongoDbOptions;
    private readonly Mock<IOptions<ClusterOptions>> _clusterOptionsMock;
    private readonly Mock<ILogger<MongoDbLogConsistentStorage>> _loggerMock;
    private readonly Mock<IMongoClient> _mongoClientMock;
    private readonly Mock<IMongoDatabase> _mongoDatabaseMock;
    private readonly Mock<ICluster> _clusterMock;
    private readonly Mock<IEventSourcingCollectionFactory> _collectionFactoryMock;
    private readonly Mock<IEventSourcingCollection> _eventSourcingCollectionMock;

    public MongoDbLogConsistentStorageMongoTests()
    {
        _name = "TestStorage";
        _mongoClientMock = new Mock<IMongoClient>();
        _mongoDatabaseMock = new Mock<IMongoDatabase>();
        _mongoCollectionMock = new Mock<IMongoCollection<BsonDocument>>();
        _clusterMock = new Mock<ICluster>();
        _collectionFactoryMock = new Mock<IEventSourcingCollectionFactory>();
        _eventSourcingCollectionMock = new Mock<IEventSourcingCollection>();

        _mongoClientMock.Setup(x => x.GetDatabase(It.IsAny<string>(), null))
            .Returns(_mongoDatabaseMock.Object);
        _mongoDatabaseMock.Setup(x => x.GetCollection<BsonDocument>(It.IsAny<string>(), null))
            .Returns(_mongoCollectionMock.Object);
        _mongoClientMock.Setup(x => x.Cluster).Returns(_clusterMock.Object);

        // Setup collection factory mock
        _eventSourcingCollectionMock.Setup(x => x.GetCollection())
            .Returns(_mongoCollectionMock.Object);
        _collectionFactoryMock.Setup(x => x.CreateCollection(It.IsAny<IMongoClient>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(_eventSourcingCollectionMock.Object);

        _clusterOptionsMock = new Mock<IOptions<ClusterOptions>>();
        _clusterOptionsMock.Setup(x => x.Value).Returns(new ClusterOptions { ServiceId = "TestService" });
        _loggerMock = new Mock<ILogger<MongoDbLogConsistentStorage>>();

        var connectionString = AevatarMongoDbFixture.GetRandomConnectionString();
        var settings = MongoClientSettings.FromConnectionString(connectionString);
        settings.ClusterConfigurator = builder =>
        {
        };

        _mongoDbOptions = new MongoDbStorageOptions
        {
            ClientSettings = settings,
            Database = "TestDb",
            GrainStateSerializer = new BsonGrainSerializer()
        };

        _storage = new MongoDbLogConsistentStorage(_name, _mongoDbOptions, _clusterOptionsMock.Object, 
            _loggerMock.Object, _collectionFactoryMock.Object);
    }

    [Fact]
    public async Task ReadAsync_WhenMongoDbError_ThrowsMongoDbStorageException()
    {
        // Arrange
        var grainId = GrainId.Create("TestGrain", Guid.NewGuid().ToString());
        var grainTypeName = "TestGrainType";
        // Test data: Reading attempt with ID TestGrain/TestKey

        var mockCursor = new Mock<IAsyncCursor<BsonDocument>>();
        mockCursor.Setup(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MongoException("Test error"));

        _mongoCollectionMock.Setup(x => x.FindAsync(
            It.IsAny<FilterDefinition<BsonDocument>>(),
            It.IsAny<FindOptions<BsonDocument, BsonDocument>>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MongoException("Test error"));

        // Initialize the storage
        var observer = new TestSiloLifecycle();
        _storage.Participate(observer);
        await observer.OnStart(CancellationToken.None);

        // Act & Assert
        await Assert.ThrowsAsync<MongoDbStorageException>(() =>
            _storage.ReadAsync<TestLogEntry>(grainTypeName, grainId, 0, 1));
    }

    [Fact]
    public async Task GetLastVersionAsync_WhenMongoDbError_ThrowsMongoDbStorageException()
    {
        // Arrange
        var grainId = GrainId.Create("TestGrain", Guid.NewGuid().ToString());
        var grainTypeName = "TestGrainType";

        var mockCursor = new Mock<IAsyncCursor<BsonDocument>>();
        mockCursor.Setup(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MongoException("Test error"));

        _mongoCollectionMock.Setup(x => x.FindAsync(
            It.IsAny<FilterDefinition<BsonDocument>>(),
            It.IsAny<FindOptions<BsonDocument, BsonDocument>>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MongoException("Test error"));

        // Initialize the storage
        var observer = new TestSiloLifecycle();
        _storage.Participate(observer);
        await observer.OnStart(CancellationToken.None);

        // Act & Assert
        await Assert.ThrowsAsync<MongoDbStorageException>(() =>
            _storage.GetLastVersionAsync(grainTypeName, grainId));
    }

    [Fact]
    public async Task AppendAsync_WhenMongoDbError_ThrowsMongoDbStorageException()
    {
        // Arrange
        var grainId = GrainId.Create("TestGrain", Guid.NewGuid().ToString());
        var grainTypeName = "TestGrainType";
        var entries = new List<TestLogEntry> { new TestLogEntry { Data = "Test" } };

        var mockCursor = new Mock<IAsyncCursor<BsonDocument>>();
        mockCursor.Setup(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mongoCollectionMock.Setup(x => x.FindAsync(
            It.IsAny<FilterDefinition<BsonDocument>>(),
            It.IsAny<FindOptions<BsonDocument, BsonDocument>>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MongoException("Test error"));

        _mongoCollectionMock.Setup(x => x.InsertManyAsync(
            It.IsAny<IEnumerable<BsonDocument>>(),
            It.IsAny<InsertManyOptions>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MongoException("Test error"));

        // Initialize the storage
        var observer = new TestSiloLifecycle();
        _storage.Participate(observer);
        await observer.OnStart(CancellationToken.None);

        // Act & Assert
        await Assert.ThrowsAsync<MongoDbStorageException>(() =>
            _storage.AppendAsync(grainTypeName, grainId, entries, 1));
    }

    [Fact]
    public async Task Init_WhenMongoDbError_ThrowsMongoDbStorageException()
    {
        // Arrange
        // Test data: Initialize with specific MongoDB connection string
        var connectionString = AevatarMongoDbFixture.GetRandomConnectionString();
        var settings = MongoClientSettings.FromConnectionString(connectionString);
        settings.ClusterConfigurator = builder =>
        {
            builder.Subscribe<ClusterOpeningEvent>(e =>
            {
                throw new MongoException("Test error");
            });
        };

        var options = new MongoDbStorageOptions
        {
            ClientSettings = settings,
            Database = "TestDb",
            GrainStateSerializer = new BsonGrainSerializer()
        };

        var storage = new MongoDbLogConsistentStorage(_name, options, _clusterOptionsMock.Object, 
            _loggerMock.Object, _collectionFactoryMock.Object);

        var observer = new TestSiloLifecycle();
        storage.Participate(observer);

        // Act & Assert
        // The error will occur when trying to create the MongoDB client with invalid settings
        await Assert.ThrowsAsync<MongoDbStorageException>(() => observer.OnStart(CancellationToken.None));
    }

    [Fact]
    public async Task Close_WhenMongoDbError_ThrowsMongoDbStorageException()
    {
        // Arrange
        // Test data: Close connection for storage with name "TestStorage"
        var connectionString = AevatarMongoDbFixture.GetRandomConnectionString();
        var settings = MongoClientSettings.FromConnectionString(connectionString);
        settings.ClusterConfigurator = builder =>
        {
            builder.Subscribe<ClusterClosingEvent>(e =>
            {
                throw new MongoException("Test error");
            });
        };

        var options = new MongoDbStorageOptions
        {
            ClientSettings = settings,
            Database = "TestDb",
            GrainStateSerializer = new BsonGrainSerializer()
        };

        var storage = new MongoDbLogConsistentStorage(_name, options, _clusterOptionsMock.Object, 
            _loggerMock.Object, _collectionFactoryMock.Object);

        var observer = new TestSiloLifecycle();
        storage.Participate(observer);
        await observer.OnStart(CancellationToken.None);

        // Act & Assert
        await Assert.ThrowsAsync<MongoDbStorageException>(() =>
            observer.OnStop(CancellationToken.None));
    }

    [Fact]
    public async Task MongoDbStorageOptionsTests()
    {
        // Test data: Testing options with InitStage=0 and Database="TestDb"
        var connectionString = AevatarMongoDbFixture.GetRandomConnectionString();
        var settings = MongoClientSettings.FromConnectionString(connectionString);
        settings.ClusterConfigurator = builder =>
        {
            builder.Subscribe<ClusterClosingEvent>(e =>
            {
                throw new MongoException("Test error");
            });
        };

        var options = new MongoDbStorageOptions
        {
            ClientSettings = settings,
            Database = "TestDb",
            InitStage = 0,
            GrainStateSerializer = new BsonGrainSerializer()
        };
        options.InitStage.ShouldBe(0);
        options.Credentials.ShouldBeNull();

        var mongoDbOptionValidator = new MongoDbStorageOptionsValidator(options, "test");
        mongoDbOptionValidator.ValidateConfiguration();
        
        var emptyOptions = new MongoDbStorageOptions
        {
            GrainStateSerializer = new BsonGrainSerializer()
        };
        mongoDbOptionValidator = new MongoDbStorageOptionsValidator(emptyOptions, "test");
        Assert.Throws<OrleansConfigurationException>(() => mongoDbOptionValidator.ValidateConfiguration());
    }

    private class TestLogEntry
    {
        public required string Data { get; set; } = string.Empty;
    }
} 