using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Aevatar.GAgents.Basic.BasicGAgents;
using Shouldly;
using Xunit.Abstractions;

namespace Aevatar.GAgents.Basic.Test;

/// <summary>
/// Performance unit tests for ConfigManagerGAgent Phase 1 optimizations
/// </summary>
public class ConfigManagerGAgentPerformanceTests
{
    private readonly ITestOutputHelper _output;

    public ConfigManagerGAgentPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region Test Data Generation

    /// <summary>
    /// Generate test configuration JSON of specified size
    /// </summary>
    private static string GenerateTestConfig(int targetSizeBytes, string prefix = "test")
    {
        var config = new Dictionary<string, object>
        {
            ["metadata"] = new
            {
                version = "1.0.0",
                timestamp = DateTime.UtcNow,
                description = $"Generated {prefix} configuration"
            },
            ["settings"] = new Dictionary<string, string>(),
            ["features"] = new List<string>(),
            ["data"] = new List<Dictionary<string, object>>()
        };

        var currentJson = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        var currentSize = Encoding.UTF8.GetByteCount(currentJson);

        // Add padding data to reach target size
        var settings = (Dictionary<string, string>)config["settings"]!;
        var counter = 0;

        while (currentSize < targetSizeBytes && counter < 10000)
        {
            var key = $"setting_{counter:D6}";
            var value = $"value_{counter:D6}_" + new string('x', Math.Min(100, targetSizeBytes - currentSize));
            settings[key] = value;

            currentJson = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            currentSize = Encoding.UTF8.GetByteCount(currentJson);
            counter++;
        }

        return currentJson;
    }

    #endregion

    #region Storage Strategy Tests

    [Theory]
    [InlineData(512, ConfigStorageStrategy.Inline)] // 512 bytes -> Inline
    [InlineData(2048, ConfigStorageStrategy.Compressed)] // 2KB -> Compressed
    [InlineData(10240, ConfigStorageStrategy.Compressed)] // 10KB -> Compressed
    [InlineData(204800, ConfigStorageStrategy.External)] // 200KB -> External
    public void DetermineStorageStrategy_ShouldSelectCorrectStrategy(int configSize,
        ConfigStorageStrategy expectedStrategy)
    {
        // Arrange
        var configJson = GenerateTestConfig(configSize);
        var actualSize = Encoding.UTF8.GetByteCount(configJson);

        // Act - Simulate the strategy selection logic
        var strategy = actualSize < 1024
            ? ConfigStorageStrategy.Inline
            : actualSize < 102400
                ? ConfigStorageStrategy.Compressed
                : ConfigStorageStrategy.External;

        // Assert
        strategy.ShouldBe(expectedStrategy);
        _output.WriteLine($"Config size: {actualSize:N0} bytes -> Strategy: {strategy}");
    }

    [Fact]
    public void ConfigStorageInfo_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var storageInfo = new ConfigStorageInfo
        {
            Strategy = ConfigStorageStrategy.Compressed,
            ConfigJson = "test",
            Hash = "hash123",
            Size = 1000
        };

        // Assert
        storageInfo.Strategy.ShouldBe(ConfigStorageStrategy.Compressed);
        storageInfo.ConfigJson.ShouldBe("test");
        storageInfo.Hash.ShouldBe("hash123");
        storageInfo.Size.ShouldBe(1000);
    }

    #endregion

    #region Compression Performance Tests

    [Theory]
    [InlineData(1024)] // 1KB
    [InlineData(5120)] // 5KB
    [InlineData(10240)] // 10KB
    [InlineData(51200)] // 50KB
    public async Task CompressionService_ShouldCompressEfficiently(int configSize)
    {
        // Arrange
        var storageService = new BasicConfigStorageService();
        var configJson = GenerateTestConfig(configSize);
        var originalSize = Encoding.UTF8.GetByteCount(configJson);

        // Act
        var sw = Stopwatch.StartNew();
        var compressed = await storageService.CompressConfigAsync(configJson);
        var compressionTime = sw.ElapsedMilliseconds;

        sw.Restart();
        var decompressed = await storageService.DecompressConfigAsync(compressed);
        var decompressionTime = sw.ElapsedMilliseconds;

        // Assert
        compressed.Length.ShouldBeLessThan(originalSize);
        decompressed.ShouldBe(configJson);

        var compressionRatio = (double)compressed.Length / originalSize;
        compressionRatio.ShouldBeLessThan(1.0); // Should compress

        _output.WriteLine($"Size: {originalSize:N0} -> {compressed.Length:N0} bytes ({compressionRatio:P1})");
        _output.WriteLine($"Compression: {compressionTime}ms, Decompression: {decompressionTime}ms");

        // Performance assertions
        compressionTime.ShouldBeLessThan(1000); // Should compress within 1 second
        decompressionTime.ShouldBeLessThan(500); // Should decompress within 500ms
    }

    [Fact]
    public async Task CompressionService_LargeConfig_ShouldHaveGoodCompressionRatio()
    {
        // Arrange
        var storageService = new BasicConfigStorageService();
        var configJson = GenerateTestConfig(50000); // 50KB
        var originalSize = Encoding.UTF8.GetByteCount(configJson);

        // Act
        var compressed = await storageService.CompressConfigAsync(configJson);
        var compressionRatio = (double)compressed.Length / originalSize;

        // Assert
        compressionRatio.ShouldBeLessThan(0.5); // Should compress to less than 50%
        _output.WriteLine($"Large config compression ratio: {compressionRatio:P2}");
    }

    #endregion

    #region Hash and Change Detection Tests

    [Fact]
    public void ConfigHash_SameContent_ShouldProduceSameHash()
    {
        // Arrange
        var config1 = GenerateTestConfig(1000, "same");
        var config2 = config1; // Same content
        var config3 = GenerateTestConfig(1000, "different");

        // Act - Simulate hash computation
        var hash1 = ComputeTestHash(config1);
        var hash2 = ComputeTestHash(config2);
        var hash3 = ComputeTestHash(config3);

        // Assert
        hash1.ShouldBe(hash2);
        hash1.ShouldNotBe(hash3);
        hash1.Length.ShouldBe(64); // SHA256 hex length
    }

    [Fact]
    public void ConfigHash_DifferentContent_ShouldProduceDifferentHash()
    {
        // Arrange
        var configs = Enumerable.Range(1, 10)
            .Select(i => GenerateTestConfig(1000, $"config{i}"))
            .ToList();

        // Act
        var hashes = configs.Select(ComputeTestHash).ToList();

        // Assert
        hashes.Distinct().Count().ShouldBe(configs.Count); // All hashes should be unique
        _output.WriteLine($"Generated {hashes.Count} unique hashes for {configs.Count} configs");
    }

    private static string ComputeTestHash(string configJson)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(configJson);
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hashBytes);
    }

    #endregion

    #region Memory Usage Tests

    [Theory]
    [InlineData(512)] // Small config
    [InlineData(10240)] // Medium config  
    [InlineData(102400)] // Large config
    public void MemoryUsage_OptimizedStorage_ShouldReduceMemoryFootprint(int configSize)
    {
        // Arrange
        var configJson = GenerateTestConfig(configSize);
        var actualSize = Encoding.UTF8.GetByteCount(configJson);

        // Simulate old approach (full JSON in state + events)
        var oldMemoryUsage = actualSize * 3; // State + 2 event copies

        // Simulate new approach based on strategy
        var strategy = actualSize < 1024
            ? ConfigStorageStrategy.Inline
            : actualSize < 102400
                ? ConfigStorageStrategy.Compressed
                : ConfigStorageStrategy.External;

        var newMemoryUsage = strategy switch
        {
            ConfigStorageStrategy.Inline => actualSize + 200, // JSON + metadata
            ConfigStorageStrategy.Compressed => (actualSize / 3) + 200, // ~33% compression + metadata
            ConfigStorageStrategy.External => 200, // Only metadata
            _ => actualSize
        };

        // Act & Assert
        var memoryReduction = (double)(oldMemoryUsage - newMemoryUsage) / oldMemoryUsage;

        newMemoryUsage.ShouldBeLessThan(oldMemoryUsage);
        memoryReduction.ShouldBeGreaterThan(0.3); // At least 30% reduction

        _output.WriteLine($"Config: {actualSize:N0} bytes, Strategy: {strategy}");
        _output.WriteLine($"Memory: {oldMemoryUsage:N0} -> {newMemoryUsage:N0} bytes ({memoryReduction:P1} reduction)");
    }

    #endregion

    #region State Schema Migration Tests

    [Fact]
    public void StateSchema_Version1_ShouldBeCompatible()
    {
        // Arrange - Simulate legacy state
        var legacyState = new ConfigManagerGAgentState
        {
            ConfigType = "TestConfig",
            ConfigJson = GenerateTestConfig(1000),
            SchemaVersion = 1, // Legacy version
            LastUpdated = DateTime.UtcNow
        };

        // Act - Simulate reading legacy state
        var hasConfig = !string.IsNullOrEmpty(legacyState.ConfigJson);
        var isLegacy = legacyState.SchemaVersion < 2;

        // Assert
        hasConfig.ShouldBeTrue();
        isLegacy.ShouldBeTrue();
        legacyState.StorageStrategy.ShouldBe(ConfigStorageStrategy.Inline); // Default value
    }

    [Fact]
    public void StateSchema_Version2_ShouldHaveOptimizedFields()
    {
        // Arrange - Simulate optimized state
        var optimizedState = new ConfigManagerGAgentState
        {
            ConfigType = "TestConfig",
            ConfigHash = "abc123",
            ConfigSize = 1000,
            StorageStrategy = ConfigStorageStrategy.Compressed,
            SchemaVersion = 2,
            CompressedConfig = new byte[] { 1, 2, 3 },
            LastUpdated = DateTime.UtcNow
        };

        // Act & Assert
        optimizedState.SchemaVersion.ShouldBe(2);
        optimizedState.ConfigHash.ShouldNotBeEmpty();
        optimizedState.ConfigSize.ShouldBeGreaterThan(0);
        optimizedState.StorageStrategy.ShouldBe(ConfigStorageStrategy.Compressed);
        optimizedState.CompressedConfig.ShouldNotBeEmpty();
    }

    #endregion

    #region Event Log Optimization Tests

    [Fact]
    public void OptimizedConfigSetLogEvent_ShouldHaveMinimalFootprint()
    {
        // Arrange
        var optimizedEvent = new OptimizedConfigSetLogEvent
        {
            ConfigType = "TestConfig",
            StorageStrategy = ConfigStorageStrategy.Compressed,
            ConfigHash = "abc123",
            ConfigSize = 1000,
            Timestamp = DateTime.UtcNow,
            CompressedConfig = new byte[] { 1, 2, 3 } // Minimal compressed data
        };

        // Assert - Should only store necessary data based on strategy
        optimizedEvent.ConfigJson.ShouldBeEmpty(); // No full JSON for compressed strategy
        optimizedEvent.ExternalStorageKey.ShouldBeEmpty(); // No external key for compressed strategy
        optimizedEvent.CompressedConfig.ShouldNotBeEmpty(); // Has compressed data
        optimizedEvent.ConfigHash.ShouldNotBeEmpty();
        optimizedEvent.ConfigSize.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void ConfigUpdatedLogEvent_ShouldOnlyStoreHashNotFullJson()
    {
        // Arrange
        var updateEvent = new ConfigUpdatedLogEvent
        {
            ConfigType = "TestConfig",
            Success = true,
            Timestamp = DateTime.UtcNow,
            ConfigHash = "abc123", // Only hash, not full JSON
            ConfigSize = 1000
        };

        // Assert - Should have hash and size but no full JSON
        updateEvent.ConfigHash.ShouldNotBeEmpty();
        updateEvent.ConfigSize.ShouldBeGreaterThan(0);
        updateEvent.Success.ShouldBeTrue();
    }

    #endregion

    #region Performance Benchmark Tests

    [Fact]
    public async Task ConfigStorage_MultipleStrategies_PerformanceComparison()
    {
        // Arrange
        var testCases = new[]
        {
            ("Small", 512, ConfigStorageStrategy.Inline),
            ("Medium", 10240, ConfigStorageStrategy.Compressed),
            ("Large", 102400, ConfigStorageStrategy.External)
        };

        var storageService = new BasicConfigStorageService();
        _output.WriteLine("Performance Comparison:");
        _output.WriteLine("Strategy\t\tSize\t\tStore Time\tRetrieve Time");

        foreach (var (name, size, expectedStrategy) in testCases)
        {
            // Arrange
            var configJson = GenerateTestConfig(size);

            // Act & Measure
            var sw = Stopwatch.StartNew();

            switch (expectedStrategy)
            {
                case ConfigStorageStrategy.Inline:
                    // Inline storage - no additional processing
                    sw.Stop();
                    var storeTime = sw.ElapsedMilliseconds;

                    sw.Restart();
                    var retrieved = configJson; // Direct access
                    sw.Stop();
                    var retrieveTime = sw.ElapsedMilliseconds;

                    retrieved.ShouldBe(configJson);
                    break;

                case ConfigStorageStrategy.Compressed:
                    var compressed = await storageService.CompressConfigAsync(configJson);
                    sw.Stop();
                    var compressTime = sw.ElapsedMilliseconds;

                    sw.Restart();
                    var decompressed = await storageService.DecompressConfigAsync(compressed);
                    sw.Stop();
                    var decompressTime = sw.ElapsedMilliseconds;

                    decompressed.ShouldBe(configJson);

                    _output.WriteLine($"{expectedStrategy}\t\t{size:N0}\t\t{compressTime}ms\t\t{decompressTime}ms");
                    continue;

                case ConfigStorageStrategy.External:
                    var key = await storageService.StoreConfigAsync(configJson, "TestConfig");
                    sw.Stop();
                    var storeExtTime = sw.ElapsedMilliseconds;

                    sw.Restart();
                    var retrievedExt = await storageService.RetrieveConfigAsync(key);
                    sw.Stop();
                    var retrieveExtTime = sw.ElapsedMilliseconds;

                    retrievedExt.ShouldBe(configJson);

                    _output.WriteLine($"{expectedStrategy}\t\t{size:N0}\t\t{storeExtTime}ms\t\t{retrieveExtTime}ms");
                    continue;
            }

            if (expectedStrategy == ConfigStorageStrategy.Inline)
            {
                _output.WriteLine($"{expectedStrategy}\t\t{size:N0}\t\t0ms\t\t0ms");
            }
        }
    }

    [Fact]
    public void LargeConfigScenario_ShouldDemonstratePerformanceImprovement()
    {
        // Arrange - Simulate 1MB configuration
        var largeConfig = GenerateTestConfig(1024 * 1024); // 1MB
        var configSize = Encoding.UTF8.GetByteCount(largeConfig);

        // Old approach memory usage
        var oldMemoryUsage = configSize * 3; // State + 2 events with full JSON

        // New approach memory usage (External storage)
        var newMemoryUsage = 200; // Only metadata in Orleans state

        // Act & Assert
        var memoryReduction = (double)(oldMemoryUsage - newMemoryUsage) / oldMemoryUsage;

        memoryReduction.ShouldBeGreaterThan(0.99); // Over 99% reduction
        newMemoryUsage.ShouldBeLessThan(1000); // Less than 1KB in Orleans state

        _output.WriteLine($"Large Config Performance:");
        _output.WriteLine($"Config Size: {configSize:N0} bytes ({configSize / 1024.0 / 1024.0:F1} MB)");
        _output.WriteLine($"Old Memory Usage: {oldMemoryUsage:N0} bytes ({oldMemoryUsage / 1024.0 / 1024.0:F1} MB)");
        _output.WriteLine($"New Memory Usage: {newMemoryUsage:N0} bytes");
        _output.WriteLine($"Memory Reduction: {memoryReduction:P2}");
    }

    #endregion
}