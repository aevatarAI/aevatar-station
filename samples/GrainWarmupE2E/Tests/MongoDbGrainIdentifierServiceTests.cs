using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using Moq;
using Aevatar.Silo.GrainWarmup;
using MongoDB.Bson;
using System.Reflection;

namespace GrainWarmupE2E.Tests;

public class MongoDbGrainIdentifierServiceTests
{
    [Fact]
    public void GetCollectionName_WithoutPrefix_ReturnsBaseName()
    {
        // Arrange
        var config = new GrainWarmupConfiguration
        {
            MongoDbIntegration = new MongoDbIntegrationConfiguration
            {
                CollectionNamingStrategy = "FullTypeName",
                CollectionPrefix = string.Empty
            }
        };
        
        var options = Options.Create(config);
        var logger = Mock.Of<ILogger<MongoDbGrainIdentifierService>>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Orleans:MongoDBClient", "mongodb://localhost:27017"),
                new KeyValuePair<string, string>("Orleans:DataBase", "test")
            })
            .Build();

        var service = new MongoDbGrainIdentifierService(options, logger, configuration);

        // Act
        var collectionName = service.GetCollectionName(typeof(TestGrain));

        // Assert
        Assert.Equal("GrainWarmupE2E.Tests.TestGrain", collectionName);
    }

    [Fact]
    public void GetCollectionName_WithPrefix_ReturnsPrefixedName()
    {
        // Arrange
        var config = new GrainWarmupConfiguration
        {
            MongoDbIntegration = new MongoDbIntegrationConfiguration
            {
                CollectionNamingStrategy = "FullTypeName",
                CollectionPrefix = "StreamStorage"
            }
        };
        
        var options = Options.Create(config);
        var logger = Mock.Of<ILogger<MongoDbGrainIdentifierService>>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Orleans:MongoDBClient", "mongodb://localhost:27017"),
                new KeyValuePair<string, string>("Orleans:DataBase", "test")
            })
            .Build();

        var service = new MongoDbGrainIdentifierService(options, logger, configuration);

        // Act
        var collectionName = service.GetCollectionName(typeof(TestGrain));

        // Assert
        Assert.Equal("StreamStorageGrainWarmupE2E.Tests.TestGrain", collectionName);
    }

    [Fact]
    public void GetCollectionName_WithTypeNameStrategy_ReturnsTypeNameWithPrefix()
    {
        // Arrange
        var config = new GrainWarmupConfiguration
        {
            MongoDbIntegration = new MongoDbIntegrationConfiguration
            {
                CollectionNamingStrategy = "TypeName",
                CollectionPrefix = "StreamDev"
            }
        };
        
        var options = Options.Create(config);
        var logger = Mock.Of<ILogger<MongoDbGrainIdentifierService>>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Orleans:MongoDBClient", "mongodb://localhost:27017"),
                new KeyValuePair<string, string>("Orleans:DataBase", "test")
            })
            .Build();

        var service = new MongoDbGrainIdentifierService(options, logger, configuration);

        // Act
        var collectionName = service.GetCollectionName(typeof(TestGrain));

        // Assert
        Assert.Equal("StreamDevTestGrain", collectionName);
    }

    [Fact]
    public void GetCollectionName_WithCustomStrategy_AppliesPrefix()
    {
        // Arrange
        var config = new GrainWarmupConfiguration
        {
            MongoDbIntegration = new MongoDbIntegrationConfiguration
            {
                CollectionNamingStrategy = "Custom",
                CollectionPrefix = "CustomPrefix"
            }
        };
        
        var options = Options.Create(config);
        var logger = Mock.Of<ILogger<MongoDbGrainIdentifierService>>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Orleans:MongoDBClient", "mongodb://localhost:27017"),
                new KeyValuePair<string, string>("Orleans:DataBase", "test")
            })
            .Build();

        var service = new MongoDbGrainIdentifierService(options, logger, configuration);

        // Act
        var collectionName = service.GetCollectionName(typeof(TestGrain));

        // Assert
        Assert.Equal("CustomPrefixgrains_testgrain", collectionName);
    }

    #region ExtractIdentifier Tests

    [Fact]
    public void ExtractIdentifier_GuidType_ValidFormat_ReturnsGuid()
    {
        // Arrange
        var service = CreateService();
        var expectedGuid = Guid.NewGuid();
        var document = new BsonDocument("_id", $"testdbgagent/{expectedGuid:N}"); // Format without hyphens

        // Act
        var result = InvokeExtractIdentifier<Guid>(service, document);

        // Assert
        Assert.Equal(expectedGuid, result);
    }

    [Fact]
    public void ExtractIdentifier_GuidType_ValidFormatWithHyphens_ReturnsGuid()
    {
        // Arrange
        var service = CreateService();
        var expectedGuid = Guid.NewGuid();
        var document = new BsonDocument("_id", $"usergagent/{expectedGuid}"); // Format with hyphens

        // Act
        var result = InvokeExtractIdentifier<Guid>(service, document);

        // Assert
        Assert.Equal(expectedGuid, result);
    }

    [Fact]
    public void ExtractIdentifier_StringType_ValidFormat_ReturnsString()
    {
        // Arrange
        var service = CreateService();
        var expectedString = "order-12345";
        var document = new BsonDocument("_id", $"ordergagent/{expectedString}");

        // Act
        var result = InvokeExtractIdentifier<string>(service, document);

        // Assert
        Assert.Equal(expectedString, result);
    }

    [Fact]
    public void ExtractIdentifier_LongType_ValidFormat_ReturnsLong()
    {
        // Arrange
        var service = CreateService();
        var expectedLong = 9876543210L;
        var document = new BsonDocument("_id", $"productgagent/{expectedLong}");

        // Act
        var result = InvokeExtractIdentifier<long>(service, document);

        // Assert
        Assert.Equal(expectedLong, result);
    }

    [Fact]
    public void ExtractIdentifier_IntType_ValidFormat_ReturnsInt()
    {
        // Arrange
        var service = CreateService();
        var expectedInt = 123456;
        var document = new BsonDocument("_id", $"categorygagent/{expectedInt}");

        // Act
        var result = InvokeExtractIdentifier<int>(service, document);

        // Assert
        Assert.Equal(expectedInt, result);
    }

    [Fact]
    public void ExtractIdentifier_InvalidFormat_NoSlash_ReturnsDefault()
    {
        // Arrange
        var service = CreateService();
        var document = new BsonDocument("_id", "testdbgagent99f2e278ae5e4a759075b15d64b4e749"); // Missing slash

        // Act
        var result = InvokeExtractIdentifier<Guid>(service, document);

        // Assert
        Assert.Equal(default(Guid), result);
    }

    [Fact]
    public void ExtractIdentifier_InvalidFormat_MultipleSlashes_ReturnsDefault()
    {
        // Arrange
        var service = CreateService();
        var document = new BsonDocument("_id", "test/db/gagent/99f2e278ae5e4a759075b15d64b4e749"); // Multiple slashes

        // Act
        var result = InvokeExtractIdentifier<Guid>(service, document);

        // Assert
        Assert.Equal(default(Guid), result);
    }

    [Fact]
    public void ExtractIdentifier_InvalidGuidFormat_ReturnsDefault()
    {
        // Arrange
        var service = CreateService();
        var document = new BsonDocument("_id", "testdbgagent/invalid-guid-format");

        // Act
        var result = InvokeExtractIdentifier<Guid>(service, document);

        // Assert
        Assert.Equal(default(Guid), result);
    }

    [Fact]
    public void ExtractIdentifier_InvalidLongFormat_ReturnsDefault()
    {
        // Arrange
        var service = CreateService();
        var document = new BsonDocument("_id", "productgagent/not-a-number");

        // Act
        var result = InvokeExtractIdentifier<long>(service, document);

        // Assert
        Assert.Equal(default(long), result);
    }

    [Fact]
    public void ExtractIdentifier_InvalidIntFormat_ReturnsDefault()
    {
        // Arrange
        var service = CreateService();
        var document = new BsonDocument("_id", "categorygagent/not-an-integer");

        // Act
        var result = InvokeExtractIdentifier<int>(service, document);

        // Assert
        Assert.Equal(default(int), result);
    }

    [Fact]
    public void ExtractIdentifier_NonStringId_ReturnsDefault()
    {
        // Arrange
        var service = CreateService();
        var document = new BsonDocument("_id", 12345); // Non-string _id

        // Act
        var result = InvokeExtractIdentifier<Guid>(service, document);

        // Assert
        Assert.Equal(default(Guid), result);
    }

    [Fact]
    public void ExtractIdentifier_MissingId_ReturnsDefault()
    {
        // Arrange
        var service = CreateService();
        var document = new BsonDocument(); // No _id field

        // Act
        var result = InvokeExtractIdentifier<Guid>(service, document);

        // Assert
        Assert.Equal(default(Guid), result);
    }

    [Fact]
    public void ExtractIdentifier_UnsupportedType_ReturnsDefault()
    {
        // Arrange
        var service = CreateService();
        var document = new BsonDocument("_id", "testgagent/some-value");

        // Act
        var result = InvokeExtractIdentifier<DateTime>(service, document);

        // Assert
        Assert.Equal(default(DateTime), result);
    }

    #endregion

    #region Helper Methods

    private MongoDbGrainIdentifierService CreateService()
    {
        var config = new GrainWarmupConfiguration
        {
            MongoDbIntegration = new MongoDbIntegrationConfiguration
            {
                CollectionNamingStrategy = "FullTypeName",
                CollectionPrefix = "StreamStorage"
            }
        };
        
        var options = Options.Create(config);
        var logger = Mock.Of<ILogger<MongoDbGrainIdentifierService>>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Orleans:MongoDBClient", "mongodb://localhost:27017"),
                new KeyValuePair<string, string>("Orleans:DataBase", "test")
            })
            .Build();

        return new MongoDbGrainIdentifierService(options, logger, configuration);
    }

    private TIdentifier? InvokeExtractIdentifier<TIdentifier>(MongoDbGrainIdentifierService service, BsonDocument document)
    {
        // Use reflection to call the private ExtractIdentifier method
        var method = typeof(MongoDbGrainIdentifierService)
            .GetMethod("ExtractIdentifier", BindingFlags.NonPublic | BindingFlags.Instance);
        
        var genericMethod = method!.MakeGenericMethod(typeof(TIdentifier));
        return (TIdentifier?)genericMethod.Invoke(service, new object[] { document });
    }

    #endregion
}

// Test grain class for testing
public class TestGrain
{
    // Empty test class
} 