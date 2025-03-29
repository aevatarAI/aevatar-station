using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.EventSourcing.Core.Storage;
using Aevatar.EventSourcing.MongoDB.Options;
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
using Xunit;

namespace Aevatar.EventSourcing.MongoDB.Tests;

[Collection("MongoDb")]
public class MongoDbLogConsistentStorageTests
{
    private readonly Mock<IMongoCollection<BsonDocument>> _mongoCollectionMock;
    private readonly MongoDbLogConsistentStorage _storage;
    private readonly string _name;
    private readonly MongoDbStorageOptions _mongoDbOptions;
    private readonly Mock<IOptions<ClusterOptions>> _clusterOptionsMock;
    private readonly Mock<ILogger<MongoDbLogConsistentStorage>> _loggerMock;
    private readonly Mock<IMongoClient> _mongoClientMock;
    private readonly Mock<IMongoDatabase> _mongoDatabaseMock;
    private readonly Mock<ICluster> _clusterMock;

    public MongoDbLogConsistentStorageTests()
    {
        _name = "TestStorage";
        _mongoClientMock = new Mock<IMongoClient>();
        _mongoDatabaseMock = new Mock<IMongoDatabase>();
        _mongoCollectionMock = new Mock<IMongoCollection<BsonDocument>>();
        _clusterMock = new Mock<ICluster>();

        _mongoClientMock.Setup(x => x.GetDatabase(It.IsAny<string>(), null))
            .Returns(_mongoDatabaseMock.Object);
        _mongoDatabaseMock.Setup(x => x.GetCollection<BsonDocument>(It.IsAny<string>(), null))
            .Returns(_mongoCollectionMock.Object);
        _mongoClientMock.Setup(x => x.Cluster).Returns(_clusterMock.Object);

        _clusterOptionsMock = new Mock<IOptions<ClusterOptions>>();
        _clusterOptionsMock.Setup(x => x.Value).Returns(new ClusterOptions { ServiceId = "TestService" });
        _loggerMock = new Mock<ILogger<MongoDbLogConsistentStorage>>();

        var settings = MongoClientSettings.FromConnectionString("mongodb://localhost:27017");
        _mongoDbOptions = new MongoDbStorageOptions
        {
            ClientSettings = settings,
            Database = "TestDb"
        };

        _storage = new MongoDbLogConsistentStorage(_name, _mongoDbOptions, _clusterOptionsMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ReadAsync_WhenNotInitialized_ReturnsEmptyList()
    {
        // Arrange
        var grainId = GrainId.Create("TestGrain", "TestKey");
        var grainTypeName = "TestGrainType";

        // Act
        var result = await _storage.ReadAsync<TestLogEntry>(grainTypeName, grainId, 0, 10);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLastVersionAsync_WhenNotInitialized_ReturnsNegativeOne()
    {
        // Arrange
        var grainId = GrainId.Create("TestGrain", "TestKey");
        var grainTypeName = "TestGrainType";

        // Act
        var result = await _storage.GetLastVersionAsync(grainTypeName, grainId);

        // Assert
        Assert.Equal(-1, result);
    }

    [Fact]
    public async Task AppendAsync_WhenNotInitialized_ReturnsNegativeOne()
    {
        // Arrange
        var grainId = GrainId.Create("TestGrain", "TestKey");
        var grainTypeName = "TestGrainType";
        var entries = new List<TestLogEntry>();

        // Act
        var result = await _storage.AppendAsync(grainTypeName, grainId, entries, 0);

        // Assert
        Assert.Equal(-1, result);
    }

    [Fact]
    public async Task AppendAsync_WithEmptyEntries_ReturnsLastVersion()
    {
        // Arrange
        var grainId = GrainId.Create("TestGrain", "TestKey");
        var grainTypeName = "TestGrainType";
        var entries = new List<TestLogEntry>();

        // Act
        var result = await _storage.AppendAsync(grainTypeName, grainId, entries, 0);

        // Assert
        Assert.Equal(-1, result);
    }

    [Fact]
    public async Task AppendAsync_WithVersionConflict_ThrowsInconsistentStateException()
    {
        // Arrange
        var grainId = GrainId.Create("TestGrain", "TestKey");
        var grainTypeName = "TestGrainType";
        var entries = new List<TestLogEntry> { new TestLogEntry { Data = "Test" } };

        var mockCursor = new Mock<IAsyncCursor<BsonDocument>>();
        mockCursor.Setup(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mockCursor.SetupSequence(x => x.Current)
            .Returns(new[] { new BsonDocument { { "Version", 2 } } }); // Return a higher version than expected

        _mongoCollectionMock.Setup(x => x.FindAsync(
            It.IsAny<FilterDefinition<BsonDocument>>(),
            It.IsAny<FindOptions<BsonDocument, BsonDocument>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Initialize the storage
        var observer = new TestSiloLifecycle();
        _storage.Participate(observer);
        await observer.OnStart(CancellationToken.None);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InconsistentStateException>(() =>
            _storage.AppendAsync(grainTypeName, grainId, entries, 1));

        Assert.Contains("Version conflict", exception.Message);
    }

    private class TestLogEntry
    {
        public required string Data { get; set; }
    }
} 