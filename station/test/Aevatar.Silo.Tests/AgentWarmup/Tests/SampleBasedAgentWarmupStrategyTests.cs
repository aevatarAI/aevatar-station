using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Aevatar.Silo.AgentWarmup.Strategies;
using Aevatar.Silo.AgentWarmup;

namespace Aevatar.Silo.Tests.AgentWarmup.Tests;

/// <summary>
/// Unit tests for SampleBasedAgentWarmupStrategy
/// Tests cover constructor validation, property behavior, sampling algorithm, and error handling
/// </summary>
public class SampleBasedAgentWarmupStrategyTests
{
    private readonly Mock<IMongoDbAgentIdentifierService> _mockMongoDbService;
    private readonly Mock<ILogger<SampleBasedAgentWarmupStrategy<Guid>>> _mockLogger;
    private readonly Type _testAgentType;

    public SampleBasedAgentWarmupStrategyTests()
    {
        _mockMongoDbService = new Mock<IMongoDbAgentIdentifierService>();
        _mockLogger = new Mock<ILogger<SampleBasedAgentWarmupStrategy<Guid>>>();
        _testAgentType = typeof(TestAgent);
    }

    #region Constructor Tests

    [Fact]
    public void ShouldCreateStrategyWithValidParameters()
    {
        // Arrange
        const string name = "TestStrategy";
        const double sampleRatio = 0.5;
        const int batchSize = 100;

        // Act
        var strategy = new SampleBasedAgentWarmupStrategy<Guid>(
            name, _testAgentType, sampleRatio, _mockMongoDbService.Object, _mockLogger.Object, null, batchSize);

        // Assert
        Assert.NotNull(strategy);
        Assert.Equal(name, strategy.Name);
        Assert.Contains(_testAgentType, strategy.ApplicableAgentTypes);
        Assert.Equal(75, strategy.Priority);
    }

    [Fact]
    public void ShouldThrowArgumentNullException_WhenNameIsNull()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new SampleBasedAgentWarmupStrategy<Guid>(
                null!, _testAgentType, 0.5, _mockMongoDbService.Object, _mockLogger.Object));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void ShouldThrowArgumentNullException_WhenAgentTypeIsNull()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new SampleBasedAgentWarmupStrategy<Guid>(
                "TestStrategy", null!, 0.5, _mockMongoDbService.Object, _mockLogger.Object));

        Assert.Equal("agentType", exception.ParamName);
    }

    [Fact]
    public void ShouldThrowArgumentNullException_WhenMongoDbServiceIsNull()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new SampleBasedAgentWarmupStrategy<Guid>(
                "TestStrategy", _testAgentType, 0.5, null!, _mockLogger.Object));

        Assert.Equal("mongoDbService", exception.ParamName);
    }

    [Fact]
    public void ShouldThrowArgumentException_WhenSampleRatioIsZero()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new SampleBasedAgentWarmupStrategy<Guid>(
                "TestStrategy", _testAgentType, 0.0, _mockMongoDbService.Object, _mockLogger.Object));

        Assert.Equal("sampleRatio", exception.ParamName);
        Assert.Contains("Sample ratio must be between 0 and 1.0", exception.Message);
    }

    [Fact]
    public void ShouldThrowArgumentException_WhenSampleRatioIsNegative()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new SampleBasedAgentWarmupStrategy<Guid>(
                "TestStrategy", _testAgentType, -0.1, _mockMongoDbService.Object, _mockLogger.Object));

        Assert.Equal("sampleRatio", exception.ParamName);
        Assert.Contains("Sample ratio must be between 0 and 1.0", exception.Message);
    }

    [Fact]
    public void ShouldThrowArgumentException_WhenSampleRatioIsGreaterThanOne()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new SampleBasedAgentWarmupStrategy<Guid>(
                "TestStrategy", _testAgentType, 1.1, _mockMongoDbService.Object, _mockLogger.Object));

        Assert.Equal("sampleRatio", exception.ParamName);
        Assert.Contains("Sample ratio must be between 0 and 1.0", exception.Message);
    }

    [Fact]
    public void ShouldThrowArgumentException_WhenBatchSizeIsZeroOrNegative()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new SampleBasedAgentWarmupStrategy<Guid>(
                "TestStrategy", _testAgentType, 0.5, _mockMongoDbService.Object, _mockLogger.Object, null, 0));

        Assert.Equal("batchSize", exception.ParamName);
        Assert.Contains("Batch size must be positive", exception.Message);

        var exception2 = Assert.Throws<ArgumentException>(() =>
            new SampleBasedAgentWarmupStrategy<Guid>(
                "TestStrategy", _testAgentType, 0.5, _mockMongoDbService.Object, _mockLogger.Object, null, -1));

        Assert.Equal("batchSize", exception2.ParamName);
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void ShouldAcceptValidSampleRatio_BoundaryValues(double sampleRatio)
    {
        // Arrange & Act
        var strategy = new SampleBasedAgentWarmupStrategy<Guid>(
            "TestStrategy", _testAgentType, sampleRatio, _mockMongoDbService.Object, _mockLogger.Object);

        // Assert
        Assert.NotNull(strategy);
        Assert.Equal("TestStrategy", strategy.Name);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void ShouldReturnCorrectName()
    {
        // Arrange
        const string expectedName = "CustomSampleStrategy";
        var strategy = new SampleBasedAgentWarmupStrategy<Guid>(
            expectedName, _testAgentType, 0.5, _mockMongoDbService.Object, _mockLogger.Object);

        // Act
        var actualName = strategy.Name;

        // Assert
        Assert.Equal(expectedName, actualName);
    }

    [Fact]
    public void ShouldReturnCorrectApplicableAgentTypes()
    {
        // Arrange
        var strategy = new SampleBasedAgentWarmupStrategy<Guid>(
            "TestStrategy", _testAgentType, 0.5, _mockMongoDbService.Object, _mockLogger.Object);

        // Act
        var applicableTypes = strategy.ApplicableAgentTypes.ToList();

        // Assert
        Assert.Single(applicableTypes);
        Assert.Equal(_testAgentType, applicableTypes[0]);
    }

    [Fact]
    public void ShouldReturnCorrectPriority()
    {
        // Arrange
        var strategy = new SampleBasedAgentWarmupStrategy<Guid>(
            "TestStrategy", _testAgentType, 0.5, _mockMongoDbService.Object, _mockLogger.Object);

        // Act
        var priority = strategy.Priority;

        // Assert
        Assert.Equal(75, priority);
    }

    [Fact]
    public void ShouldReturnEstimatedAgentCount()
    {
        // Arrange
        const double sampleRatio = 0.3;
        var strategy = new SampleBasedAgentWarmupStrategy<Guid>(
            "TestStrategy", _testAgentType, sampleRatio, _mockMongoDbService.Object, _mockLogger.Object);

        // Act
        var estimatedCount = strategy.EstimatedAgentCount;

        // Assert
        Assert.Equal(300, estimatedCount); // 1000 * 0.3 = 300
    }

    #endregion

    #region Sampling Algorithm Tests

    [Fact]
    public async Task ShouldSampleCorrectPercentage_WithVariousRatios()
    {
        // Arrange
        var testIdentifiers = GenerateTestIdentifiers(1000);
        SetupMockMongoDbService(testIdentifiers);

        var testCases = new[] { 0.1, 0.25, 0.5, 0.75, 1.0 };

        foreach (var ratio in testCases)
        {
            var strategy = new SampleBasedAgentWarmupStrategy<Guid>(
                "TestStrategy", _testAgentType, ratio, _mockMongoDbService.Object, _mockLogger.Object, 42); // Fixed seed

            // Act
            var sampledIdentifiers = new List<Guid>();
            await foreach (var identifier in strategy.GenerateAgentIdentifiersAsync(_testAgentType))
            {
                sampledIdentifiers.Add(identifier);
            }

            // Assert
            var expectedCount = Math.Max(1, (int)(testIdentifiers.Count * ratio));
            Assert.Equal(expectedCount, sampledIdentifiers.Count);
            Assert.True(sampledIdentifiers.All(id => testIdentifiers.Contains(id)));
        }
    }

    [Fact]
    public async Task ShouldReturnAllIdentifiers_WhenSampleRatioIsOne()
    {
        // Arrange
        var testIdentifiers = GenerateTestIdentifiers(100);
        SetupMockMongoDbService(testIdentifiers);

        var strategy = new SampleBasedAgentWarmupStrategy<Guid>(
            "TestStrategy", _testAgentType, 1.0, _mockMongoDbService.Object, _mockLogger.Object);

        // Act
        var sampledIdentifiers = new List<Guid>();
        await foreach (var identifier in strategy.GenerateAgentIdentifiersAsync(_testAgentType))
        {
            sampledIdentifiers.Add(identifier);
        }

        // Assert
        Assert.Equal(testIdentifiers.Count, sampledIdentifiers.Count);
        Assert.True(testIdentifiers.All(id => sampledIdentifiers.Contains(id)));
    }

    [Fact]
    public async Task ShouldReturnAtLeastOneIdentifier_WhenSampleRatioIsVerySmall()
    {
        // Arrange
        var testIdentifiers = GenerateTestIdentifiers(1000);
        SetupMockMongoDbService(testIdentifiers);

        var strategy = new SampleBasedAgentWarmupStrategy<Guid>(
            "TestStrategy", _testAgentType, 0.0001, _mockMongoDbService.Object, _mockLogger.Object);

        // Act
        var sampledIdentifiers = new List<Guid>();
        await foreach (var identifier in strategy.GenerateAgentIdentifiersAsync(_testAgentType))
        {
            sampledIdentifiers.Add(identifier);
        }

        // Assert
        Assert.True(sampledIdentifiers.Count >= 1);
    }

    [Fact]
    public async Task ShouldProduceDeterministicResults_WithSameSeed()
    {
        // Arrange
        var testIdentifiers = GenerateTestIdentifiers(100);
        SetupMockMongoDbService(testIdentifiers);

        const int seed = 12345;
        var strategy1 = new SampleBasedAgentWarmupStrategy<Guid>(
            "TestStrategy1", _testAgentType, 0.5, _mockMongoDbService.Object, _mockLogger.Object, seed);
        var strategy2 = new SampleBasedAgentWarmupStrategy<Guid>(
            "TestStrategy2", _testAgentType, 0.5, _mockMongoDbService.Object, _mockLogger.Object, seed);

        // Act
        var sampledIdentifiers1 = new List<Guid>();
        await foreach (var identifier in strategy1.GenerateAgentIdentifiersAsync(_testAgentType))
        {
            sampledIdentifiers1.Add(identifier);
        }

        var sampledIdentifiers2 = new List<Guid>();
        await foreach (var identifier in strategy2.GenerateAgentIdentifiersAsync(_testAgentType))
        {
            sampledIdentifiers2.Add(identifier);
        }

        // Assert
        Assert.Equal(sampledIdentifiers1.Count, sampledIdentifiers2.Count);
        Assert.True(sampledIdentifiers1.SequenceEqual(sampledIdentifiers2));
    }

    [Fact]
    public async Task ShouldProduceDifferentResults_WithDifferentSeeds()
    {
        // Arrange
        var testIdentifiers = GenerateTestIdentifiers(100);
        SetupMockMongoDbService(testIdentifiers);

        var strategy1 = new SampleBasedAgentWarmupStrategy<Guid>(
            "TestStrategy1", _testAgentType, 0.5, _mockMongoDbService.Object, _mockLogger.Object, 12345);
        var strategy2 = new SampleBasedAgentWarmupStrategy<Guid>(
            "TestStrategy2", _testAgentType, 0.5, _mockMongoDbService.Object, _mockLogger.Object, 54321);

        // Act
        var sampledIdentifiers1 = new List<Guid>();
        await foreach (var identifier in strategy1.GenerateAgentIdentifiersAsync(_testAgentType))
        {
            sampledIdentifiers1.Add(identifier);
        }

        var sampledIdentifiers2 = new List<Guid>();
        await foreach (var identifier in strategy2.GenerateAgentIdentifiersAsync(_testAgentType))
        {
            sampledIdentifiers2.Add(identifier);
        }

        // Assert
        Assert.Equal(sampledIdentifiers1.Count, sampledIdentifiers2.Count);
        Assert.False(sampledIdentifiers1.SequenceEqual(sampledIdentifiers2));
    }

    [Fact]
    public async Task ShouldHandleEmptyIdentifierList()
    {
        // Arrange
        SetupMockMongoDbService(new List<Guid>());

        var strategy = new SampleBasedAgentWarmupStrategy<Guid>(
            "TestStrategy", _testAgentType, 0.5, _mockMongoDbService.Object, _mockLogger.Object);

        // Act
        var sampledIdentifiers = new List<Guid>();
        await foreach (var identifier in strategy.GenerateAgentIdentifiersAsync(_testAgentType))
        {
            sampledIdentifiers.Add(identifier);
        }

        // Assert
        Assert.Empty(sampledIdentifiers);
    }

    [Fact]
    public async Task ShouldHandleSingleIdentifier()
    {
        // Arrange
        var singleIdentifier = Guid.NewGuid();
        SetupMockMongoDbService(new List<Guid> { singleIdentifier });

        var strategy = new SampleBasedAgentWarmupStrategy<Guid>(
            "TestStrategy", _testAgentType, 0.5, _mockMongoDbService.Object, _mockLogger.Object);

        // Act
        var sampledIdentifiers = new List<Guid>();
        await foreach (var identifier in strategy.GenerateAgentIdentifiersAsync(_testAgentType))
        {
            sampledIdentifiers.Add(identifier);
        }

        // Assert
        Assert.Single(sampledIdentifiers);
        Assert.Equal(singleIdentifier, sampledIdentifiers[0]);
    }

    [Fact]
    public async Task ShouldHandleLargeIdentifierList()
    {
        // Arrange
        var testIdentifiers = GenerateTestIdentifiers(10000);
        SetupMockMongoDbService(testIdentifiers);

        var strategy = new SampleBasedAgentWarmupStrategy<Guid>(
            "TestStrategy", _testAgentType, 0.1, _mockMongoDbService.Object, _mockLogger.Object);

        // Act
        var sampledIdentifiers = new List<Guid>();
        await foreach (var identifier in strategy.GenerateAgentIdentifiersAsync(_testAgentType))
        {
            sampledIdentifiers.Add(identifier);
        }

        // Assert
        Assert.Equal(1000, sampledIdentifiers.Count); // 10000 * 0.1 = 1000
        Assert.True(sampledIdentifiers.All(id => testIdentifiers.Contains(id)));
    }

    #endregion

    #region MongoDB Integration Tests

    [Fact]
    public async Task ShouldRetrieveIdentifiersFromMongoDB()
    {
        // Arrange
        var testIdentifiers = GenerateTestIdentifiers(50);
        SetupMockMongoDbService(testIdentifiers);

        var strategy = new SampleBasedAgentWarmupStrategy<Guid>(
            "TestStrategy", _testAgentType, 0.5, _mockMongoDbService.Object, _mockLogger.Object);

        // Act
        var sampledIdentifiers = new List<Guid>();
        await foreach (var identifier in strategy.GenerateAgentIdentifiersAsync(_testAgentType))
        {
            sampledIdentifiers.Add(identifier);
        }

        // Assert
        _mockMongoDbService.Verify(
            x => x.GetAgentIdentifiersAsync<Guid>(_testAgentType, null, It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.NotEmpty(sampledIdentifiers);
    }

    [Fact]
    public async Task ShouldHandleMongoDBConnectionFailure()
    {
        // Arrange
        _mockMongoDbService
            .Setup(x => x.GetAgentIdentifiersAsync<Guid>(_testAgentType, null, It.IsAny<CancellationToken>()))
            .Throws(new InvalidOperationException("MongoDB connection failed"));

        var strategy = new SampleBasedAgentWarmupStrategy<Guid>(
            "TestStrategy", _testAgentType, 0.5, _mockMongoDbService.Object, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var identifier in strategy.GenerateAgentIdentifiersAsync(_testAgentType))
            {
                // Should not reach here
            }
        });
    }

    [Fact]
    public async Task ShouldHandleEmptyMongoDBCollection()
    {
        // Arrange
        SetupMockMongoDbService(new List<Guid>());

        var strategy = new SampleBasedAgentWarmupStrategy<Guid>(
            "TestStrategy", _testAgentType, 0.5, _mockMongoDbService.Object, _mockLogger.Object);

        // Act
        var sampledIdentifiers = new List<Guid>();
        await foreach (var identifier in strategy.GenerateAgentIdentifiersAsync(_testAgentType))
        {
            sampledIdentifiers.Add(identifier);
        }

        // Assert
        Assert.Empty(sampledIdentifiers);
    }

    [Fact]
    public async Task ShouldRespectCancellationToken()
    {
        // Arrange
        var testIdentifiers = GenerateTestIdentifiers(1000);
        SetupMockMongoDbService(testIdentifiers);

        var strategy = new SampleBasedAgentWarmupStrategy<Guid>(
            "TestStrategy", _testAgentType, 1.0, _mockMongoDbService.Object, _mockLogger.Object);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (var identifier in strategy.GenerateAgentIdentifiersAsync(_testAgentType, cts.Token))
            {
                // Should not reach here due to cancellation
            }
        });
    }

    #endregion

    #region Batch Processing Tests

    [Fact]
    public async Task ShouldAddDelayAfterBatchSize()
    {
        // Arrange
        var testIdentifiers = GenerateTestIdentifiers(250);
        SetupMockMongoDbService(testIdentifiers);

        const int batchSize = 100;
        var strategy = new SampleBasedAgentWarmupStrategy<Guid>(
            "TestStrategy", _testAgentType, 1.0, _mockMongoDbService.Object, _mockLogger.Object, null, batchSize);

        // Act
        var startTime = DateTime.UtcNow;
        var sampledIdentifiers = new List<Guid>();
        await foreach (var identifier in strategy.GenerateAgentIdentifiersAsync(_testAgentType))
        {
            sampledIdentifiers.Add(identifier);
        }
        var endTime = DateTime.UtcNow;

        // Assert
        Assert.Equal(250, sampledIdentifiers.Count);
        // Should have at least 2 batch delays (after 100 and 200 items)
        Assert.True((endTime - startTime).TotalMilliseconds >= 20);
    }

    [Fact]
    public async Task ShouldRespectCustomBatchSize()
    {
        // Arrange
        var testIdentifiers = GenerateTestIdentifiers(50);
        SetupMockMongoDbService(testIdentifiers);

        const int customBatchSize = 25;
        var strategy = new SampleBasedAgentWarmupStrategy<Guid>(
            "TestStrategy", _testAgentType, 1.0, _mockMongoDbService.Object, _mockLogger.Object, null, customBatchSize);

        // Act
        var sampledIdentifiers = new List<Guid>();
        await foreach (var identifier in strategy.GenerateAgentIdentifiersAsync(_testAgentType))
        {
            sampledIdentifiers.Add(identifier);
        }

        // Assert
        Assert.Equal(50, sampledIdentifiers.Count);
    }

    [Fact]
    public async Task ShouldHandleCancellationDuringBatchProcessing()
    {
        // Arrange
        var testIdentifiers = GenerateTestIdentifiers(1000);
        SetupMockMongoDbService(testIdentifiers);

        var strategy = new SampleBasedAgentWarmupStrategy<Guid>(
            "TestStrategy", _testAgentType, 1.0, _mockMongoDbService.Object, _mockLogger.Object, null, 50);

        using var cts = new CancellationTokenSource();

        // Act
        var sampledIdentifiers = new List<Guid>();
        var enumerator = strategy.GenerateAgentIdentifiersAsync(_testAgentType, cts.Token).GetAsyncEnumerator();

        try
        {
            // Get a few identifiers then cancel
            for (int i = 0; i < 75; i++)
            {
                if (await enumerator.MoveNextAsync())
                {
                    sampledIdentifiers.Add(enumerator.Current);
                }
            }

            cts.Cancel();

            // Try to get more - should throw
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                while (await enumerator.MoveNextAsync())
                {
                    sampledIdentifiers.Add(enumerator.Current);
                }
            });
        }
        finally
        {
            await enumerator.DisposeAsync();
        }

        // Assert
        Assert.True(sampledIdentifiers.Count >= 50); // Should have gotten at least one batch
        Assert.True(sampledIdentifiers.Count < 1000); // Should not have gotten all
    }

    #endregion

    #region Helper Methods

    private List<Guid> GenerateTestIdentifiers(int count)
    {
        return Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToList();
    }

    private void SetupMockMongoDbService(List<Guid> identifiers)
    {
        _mockMongoDbService
            .Setup(x => x.GetAgentIdentifiersAsync<Guid>(_testAgentType, null, It.IsAny<CancellationToken>()))
            .Returns(identifiers.ToAsyncEnumerable());
    }

    #endregion

    #region Test Agent Type

    private class TestAgent
    {
        // Test agent type for testing purposes
    }

    #endregion
} 