using System.Text;
using System.Text.Json;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Basic.BasicGAgents;
using Aevatar.GAgents.Basic.BasicGEvent;
using Aevatar.GAgents.TestBase;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Aevatar.GAgents.Basic.ConfigManagerGAgent.Test;

/// <summary>
/// Comprehensive unit tests for ConfigManagerGAgent
/// Tests configuration management functionality including updates, retrieval, validation, and performance
/// </summary>
[Collection(ClusterCollection.Name)]
public sealed class ConfigManagerGAgentTests : AevatarGAgentTestBase<AevatarGAgentTestBaseModule>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGAgentFactory _gAgentFactory;

    public ConfigManagerGAgentTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    #region Basic Functionality Tests

    [Fact]
    public async Task ConfigManagerGAgent_GetDescription_ShouldReturnCorrectDescription()
    {
        // Arrange
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(Guid.NewGuid());

        // Act - Get description before any configuration
        var initialDescription = await configManager.GetDescriptionAsync();

        // Assert
        initialDescription.ShouldNotBeNullOrEmpty();
        initialDescription.ShouldContain("Not configured");
        _testOutputHelper.WriteLine($"Initial description: {initialDescription}");

        // Update configuration
        await configManager.UpdateConfigAsync(new ConfigUpdateEvent
        {
            ConfigType = "TestConfig",
            ConfigJson = "{\"test\": \"value\"}"
        });

        // Act - Get description after configuration
        var updatedDescription = await configManager.GetDescriptionAsync();

        // Assert
        updatedDescription.ShouldContain("TestConfig");
        updatedDescription.ShouldContain("Total updates: 1");
        _testOutputHelper.WriteLine($"Updated description: {updatedDescription}");
    }

    #endregion

    #region Configuration Update Tests

    [Fact]
    public async Task ConfigManagerGAgent_UpdateConfig_ShouldSucceed()
    {
        // Arrange
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(Guid.NewGuid());

        var testConfig = new
        {
            DatabaseConnectionString = "Server=localhost;Database=TestDB;",
            ApiKey = "test-api-key-12345",
            MaxRetryAttempts = 3,
            TimeoutSeconds = 30
        };

        var updateEvent = new ConfigUpdateEvent
        {
            ConfigType = "DatabaseConfig",
            ConfigJson = JsonSerializer.Serialize(testConfig)
        };

        // Act
        var response = await configManager.UpdateConfigAsync(updateEvent);

        // Assert
        response.ShouldNotBeNull();
        response.Success.ShouldBeTrue();
        response.ConfigType.ShouldBe("DatabaseConfig");
        response.ConfigJson.ShouldBe(updateEvent.ConfigJson);
        response.ErrorMessage.ShouldBeNull();

        // Verify state changes
        var state = await configManager.GetStateAsync();
        state.ConfigType.ShouldBe("DatabaseConfig");
        state.TotalUpdates.ShouldBe(1);
        state.LastUpdated.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1));

        _testOutputHelper.WriteLine($"Config updated successfully, total updates: {state.TotalUpdates}");
    }

    [Fact]
    public async Task ConfigManagerGAgent_UpdateConfig_EmptyConfigType_ShouldFail()
    {
        // Arrange
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(Guid.NewGuid());

        var updateEvent = new ConfigUpdateEvent
        {
            ConfigType = "", // Empty config type
            ConfigJson = "{\"key\": \"value\"}"
        };

        // Act
        var response = await configManager.UpdateConfigAsync(updateEvent);

        // Assert
        response.ShouldNotBeNull();
        response.Success.ShouldBeFalse();
        response.ErrorMessage.ShouldContain("ConfigType cannot be empty");
        response.ConfigJson.ShouldBe(string.Empty);

        _testOutputHelper.WriteLine($"Empty config type correctly rejected: {response.ErrorMessage}");
    }

    [Fact]
    public async Task ConfigManagerGAgent_UpdateConfig_EmptyConfigJson_ShouldFail()
    {
        // Arrange
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(Guid.NewGuid());

        var updateEvent = new ConfigUpdateEvent
        {
            ConfigType = "TestConfig",
            ConfigJson = "" // Empty JSON
        };

        // Act
        var response = await configManager.UpdateConfigAsync(updateEvent);

        // Assert
        response.ShouldNotBeNull();
        response.Success.ShouldBeFalse();
        response.ErrorMessage.ShouldContain("ConfigJson cannot be empty");
        response.ConfigJson.ShouldBe(string.Empty);

        _testOutputHelper.WriteLine($"Empty config JSON correctly rejected: {response.ErrorMessage}");
    }

    [Fact]
    public async Task ConfigManagerGAgent_UpdateConfig_InvalidJson_ShouldFail()
    {
        // Arrange
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(Guid.NewGuid());

        var updateEvent = new ConfigUpdateEvent
        {
            ConfigType = "TestConfig",
            ConfigJson = "{invalid json format" // Invalid JSON
        };

        // Act
        var response = await configManager.UpdateConfigAsync(updateEvent);

        // Assert
        response.ShouldNotBeNull();
        response.Success.ShouldBeFalse();
        response.ErrorMessage.ShouldContain("Invalid JSON format");
        response.ConfigJson.ShouldBe(string.Empty);

        _testOutputHelper.WriteLine($"Invalid JSON correctly rejected: {response.ErrorMessage}");
    }

    #endregion

    #region Configuration Request Tests

    [Fact]
    public async Task ConfigManagerGAgent_RequestConfig_ShouldReturnStoredConfig()
    {
        // Arrange
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(Guid.NewGuid());

        var testConfig = new
        {
            ServiceUrl = "https://api.example.com",
            ApiVersion = "v2",
            EnableCaching = true
        };

        // First, update the configuration
        var updateEvent = new ConfigUpdateEvent
        {
            ConfigType = "ApiConfig",
            ConfigJson = JsonSerializer.Serialize(testConfig)
        };

        await configManager.UpdateConfigAsync(updateEvent);

        // Act - Request the configuration
        var requestEvent = new ConfigRequestEvent
        {
            ConfigType = "ApiConfig"
        };

        var response = await configManager.RequestConfigAsync(requestEvent);

        // Assert
        response.ShouldNotBeNull();
        response.Success.ShouldBeTrue();
        response.ConfigType.ShouldBe("ApiConfig");
        response.ConfigJson.ShouldBe(updateEvent.ConfigJson);
        response.ErrorMessage.ShouldBeNull();

        _testOutputHelper.WriteLine("Config request successful");
    }

    [Fact]
    public async Task ConfigManagerGAgent_RequestConfig_NoConfigStored_ShouldFail()
    {
        // Arrange
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(Guid.NewGuid());

        var requestEvent = new ConfigRequestEvent
        {
            ConfigType = "NonExistentConfig"
        };

        // Act
        var response = await configManager.RequestConfigAsync(requestEvent);

        // Assert
        response.ShouldNotBeNull();
        response.Success.ShouldBeFalse();
        response.ErrorMessage.ShouldBe("No configuration found");
        response.ConfigJson.ShouldBe(string.Empty);

        _testOutputHelper.WriteLine($"Non-existent config correctly handled: {response.ErrorMessage}");
    }

    [Fact]
    public async Task ConfigManagerGAgent_RequestConfig_TypeMismatch_ShouldFail()
    {
        // Arrange
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(Guid.NewGuid());

        // First, store a configuration
        var updateEvent = new ConfigUpdateEvent
        {
            ConfigType = "DatabaseConfig",
            ConfigJson = "{\"connectionString\": \"test\"}"
        };

        await configManager.UpdateConfigAsync(updateEvent);

        // Act - Request with different config type
        var requestEvent = new ConfigRequestEvent
        {
            ConfigType = "ApiConfig" // Different type
        };

        var response = await configManager.RequestConfigAsync(requestEvent);

        // Assert
        response.ShouldNotBeNull();
        response.Success.ShouldBeFalse();
        response.ErrorMessage.ShouldContain("Configuration type mismatch");
        response.ConfigJson.ShouldBe(string.Empty);

        _testOutputHelper.WriteLine($"Type mismatch correctly handled: {response.ErrorMessage}");
    }

    [Fact]
    public async Task ConfigManagerGAgent_RequestConfig_SpecificKey_ShouldReturnKeyValue()
    {
        // Arrange
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(Guid.NewGuid());

        var testConfig = new
        {
            DatabaseConnectionString = "Server=localhost;Database=TestDB;",
            ApiKey = "test-api-key-12345",
            MaxRetryAttempts = 3,
            TimeoutSeconds = 30
        };

        // Store configuration
        var updateEvent = new ConfigUpdateEvent
        {
            ConfigType = "AppConfig",
            ConfigJson = JsonSerializer.Serialize(testConfig)
        };

        await configManager.UpdateConfigAsync(updateEvent);

        // Act - Request specific key
        var requestEvent = new ConfigRequestEvent
        {
            ConfigType = "AppConfig",
            ConfigKey = "ApiKey"
        };

        var response = await configManager.RequestConfigAsync(requestEvent);

        // Assert
        response.ShouldNotBeNull();
        response.Success.ShouldBeTrue();
        response.ConfigType.ShouldBe("AppConfig");

        // The value should be JSON serialized
        var extractedValue = JsonSerializer.Deserialize<JsonElement>(response.ConfigJson);
        extractedValue.ToString().ShouldBe("test-api-key-12345");

        _testOutputHelper.WriteLine($"Specific key extraction successful: {extractedValue}");
    }

    [Fact]
    public async Task ConfigManagerGAgent_RequestConfig_NonExistentKey_ShouldFail()
    {
        // Arrange
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(Guid.NewGuid());

        var testConfig = new { ExistingKey = "value" };

        // Store configuration
        var updateEvent = new ConfigUpdateEvent
        {
            ConfigType = "TestConfig",
            ConfigJson = JsonSerializer.Serialize(testConfig)
        };

        await configManager.UpdateConfigAsync(updateEvent);

        // Act - Request non-existent key
        var requestEvent = new ConfigRequestEvent
        {
            ConfigType = "TestConfig",
            ConfigKey = "NonExistentKey"
        };

        var response = await configManager.RequestConfigAsync(requestEvent);

        // Assert
        response.ShouldNotBeNull();
        response.Success.ShouldBeFalse();
        response.ErrorMessage.ShouldContain("Configuration key 'NonExistentKey' not found");
        response.ConfigJson.ShouldBe(string.Empty);

        _testOutputHelper.WriteLine($"Non-existent key correctly handled: {response.ErrorMessage}");
    }

    #endregion

    #region State Management Tests

    [Fact]
    public async Task ConfigManagerGAgent_MultipleUpdates_ShouldUpdateCounters()
    {
        // Arrange
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(Guid.NewGuid());

        var config1 = new { Setting1 = "value1" };
        var config2 = new { Setting2 = "value2" };

        // Act - Perform multiple updates
        await configManager.UpdateConfigAsync(new ConfigUpdateEvent
        {
            ConfigType = "Config1",
            ConfigJson = JsonSerializer.Serialize(config1)
        });

        await configManager.UpdateConfigAsync(new ConfigUpdateEvent
        {
            ConfigType = "Config1", // Same type, update
            ConfigJson = JsonSerializer.Serialize(config2)
        });

        // Assert
        var state = await configManager.GetStateAsync();
        state.TotalUpdates.ShouldBe(2);
        state.ConfigUpdateTimes.ShouldContainKey("Config1");
        state.ConfigUpdateTimes["Config1"].ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1));

        _testOutputHelper.WriteLine($"Multiple updates tracked correctly: {state.TotalUpdates} total updates");
    }

    [Fact]
    public async Task ConfigManagerGAgent_GetState_ShouldShowCorrectInformation()
    {
        // Arrange
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(Guid.NewGuid());
        var testConfig = CreateSimpleTestConfig("StateTestConfig");

        // Act
        await configManager.UpdateConfigAsync(new ConfigUpdateEvent
        {
            ConfigType = "StateTestConfig",
            ConfigJson = testConfig
        });

        var state = await configManager.GetStateAsync();

        // Assert
        state.ShouldNotBeNull();
        state.ConfigType.ShouldBe("StateTestConfig");
        state.TotalUpdates.ShouldBeGreaterThan(0);
        state.LastUpdated.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1));

        _testOutputHelper.WriteLine(
            $"State information: Type={state.ConfigType}, Updates={state.TotalUpdates}, LastUpdated={state.LastUpdated}");
    }

    #endregion

    #region Storage Strategy Tests

    [Fact]
    public async Task ConfigManagerGAgent_SmallConfig_ShouldUseInlineStorage()
    {
        // Arrange
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(Guid.NewGuid());
        var smallConfig = CreateSimpleTestConfig("SmallConfig"); // Should be < 1KB

        // Act
        var response = await configManager.UpdateConfigAsync(new ConfigUpdateEvent
        {
            ConfigType = "SmallConfig",
            ConfigJson = smallConfig
        });

        // Assert
        response.Success.ShouldBeTrue();

        var state = await configManager.GetStateAsync();
        state.StorageStrategy.ShouldBe(ConfigStorageStrategy.Inline);
        state.ConfigJson.ShouldBe(smallConfig);
        state.CompressedConfig.ShouldBeEmpty();
        state.ExternalStorageKey.ShouldBeNullOrEmpty();

        _testOutputHelper.WriteLine($"Small config ({Encoding.UTF8.GetByteCount(smallConfig)} bytes) using inline storage");
    }

    [Fact]
    public async Task ConfigManagerGAgent_MediumConfig_ShouldUseCompressedStorage()
    {
        // Arrange
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(Guid.NewGuid());
        var mediumConfig = CreateLargeTestConfig(5 * 1024); // 5KB - should use compressed

        // Act
        var response = await configManager.UpdateConfigAsync(new ConfigUpdateEvent
        {
            ConfigType = "MediumConfig",
            ConfigJson = mediumConfig
        });

        // Assert
        response.Success.ShouldBeTrue();

        var state = await configManager.GetStateAsync();
        state.StorageStrategy.ShouldBe(ConfigStorageStrategy.Compressed);
        state.ConfigJson.ShouldBeNullOrEmpty();
        state.CompressedConfig.ShouldNotBeEmpty();
        state.ExternalStorageKey.ShouldBeNullOrEmpty();

        _testOutputHelper.WriteLine($"Medium config ({Encoding.UTF8.GetByteCount(mediumConfig)} bytes) using compressed storage");
    }

    [Fact]
    public async Task ConfigManagerGAgent_LargeConfig_ShouldUseExternalStorage()
    {
        // Arrange
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(Guid.NewGuid());
        var largeConfig = CreateLargeTestConfig(150 * 1024); // 150KB - should use external

        // Act
        var response = await configManager.UpdateConfigAsync(new ConfigUpdateEvent
        {
            ConfigType = "LargeConfig",
            ConfigJson = largeConfig
        });

        // Assert
        response.Success.ShouldBeTrue();

        var state = await configManager.GetStateAsync();
        state.StorageStrategy.ShouldBe(ConfigStorageStrategy.External);
        state.ConfigJson.ShouldBeNullOrEmpty();
        state.CompressedConfig.ShouldBeEmpty();
        state.ExternalStorageKey.ShouldNotBeNullOrEmpty();

        _testOutputHelper.WriteLine($"Large config ({Encoding.UTF8.GetByteCount(largeConfig)} bytes) using external storage");
    }

    #endregion

    #region Complex Configuration Tests

    [Fact]
    public async Task ConfigManagerGAgent_ComplexJsonConfig_ShouldHandleCorrectly()
    {
        // Arrange
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(Guid.NewGuid());

        var complexConfig = new
        {
            Database = new
            {
                ConnectionString = "Server=localhost;Database=TestDB;",
                PoolSize = 10,
                EnableSSL = true
            },
            Api = new
            {
                BaseUrl = "https://api.example.com",
                Endpoints = new[]
                {
                    "/users",
                    "/orders",
                    "/products"
                },
                RateLimits = new
                {
                    RequestsPerMinute = 100,
                    BurstSize = 20
                }
            },
            Features = new
            {
                EnableCaching = true,
                EnableLogging = false,
                CacheExpiryMinutes = 30
            }
        };

        // Act
        var updateEvent = new ConfigUpdateEvent
        {
            ConfigType = "ComplexAppConfig",
            ConfigJson = JsonSerializer.Serialize(complexConfig)
        };

        var updateResponse = await configManager.UpdateConfigAsync(updateEvent);

        // Assert update succeeded
        updateResponse.Success.ShouldBeTrue();

        // Test retrieving nested values
        var databaseRequest = new ConfigRequestEvent
        {
            ConfigType = "ComplexAppConfig",
            ConfigKey = "Database"
        };

        var databaseResponse = await configManager.RequestConfigAsync(databaseRequest);
        databaseResponse.Success.ShouldBeTrue();

        var databaseConfig = JsonSerializer.Deserialize<JsonElement>(databaseResponse.ConfigJson);
        databaseConfig.GetProperty("ConnectionString").GetString().ShouldBe("Server=localhost;Database=TestDB;");
        databaseConfig.GetProperty("PoolSize").GetInt32().ShouldBe(10);

        _testOutputHelper.WriteLine("Complex JSON configuration handled successfully");
    }

    [Fact]
    public async Task ConfigManagerGAgent_ConcurrentOperations_ShouldHandleCorrectly()
    {
        // Arrange
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(Guid.NewGuid());
        var tasks = new List<Task<ConfigResponseEvent>>();

        // Act - Execute concurrent update operations
        for (int i = 0; i < 5; i++)
        {
            var configIndex = i;
            var config = new { Index = configIndex, Value = $"config-{configIndex}" };

            var updateTask = configManager.UpdateConfigAsync(new ConfigUpdateEvent
            {
                ConfigType = "ConcurrentTestConfig",
                ConfigJson = JsonSerializer.Serialize(config)
            });

            tasks.Add(updateTask);
        }

        var results = await Task.WhenAll(tasks);

        // Assert - All operations should succeed (last one wins)
        results.ShouldAllBe(r => r.Success);

        var finalState = await configManager.GetStateAsync();
        finalState.TotalUpdates.ShouldBe(5);

        _testOutputHelper.WriteLine($"Concurrent operations completed successfully: {finalState.TotalUpdates} total updates");
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task ConfigManagerGAgent_LargeConfig_ShouldHandlePerformance()
    {
        // Arrange
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(Guid.NewGuid());
        var largeConfig = CreateLargeTestConfig(50 * 1024); // 50KB config

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var response = await configManager.UpdateConfigAsync(new ConfigUpdateEvent
        {
            ConfigType = "LargePerformanceConfig",
            ConfigJson = largeConfig
        });

        stopwatch.Stop();

        // Assert
        response.ShouldNotBeNull();
        response.Success.ShouldBeTrue();
        response.ErrorMessage.ShouldBeNullOrEmpty();

        var state = await configManager.GetStateAsync();
        state.ConfigSize.ShouldBeGreaterThan(0);

        _testOutputHelper.WriteLine(
            $"Large config ({Encoding.UTF8.GetByteCount(largeConfig)} bytes) handled in {stopwatch.ElapsedMilliseconds}ms");
        _testOutputHelper.WriteLine($"Storage strategy: {state.StorageStrategy}");

        // Performance assertion - should complete within reasonable time
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(5000); // 5 seconds max
    }

    #endregion

    #region Helper Methods

    private static string CreateSimpleTestConfig(string configName)
    {
        var config = new
        {
            name = configName,
            version = "1.0.0",
            timestamp = DateTime.UtcNow,
            settings = new
            {
                enabled = true,
                maxRetries = 3,
                timeout = 30
            }
        };

        return JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string CreateLargeTestConfig(int targetSizeBytes)
    {
        var config = new Dictionary<string, object>
        {
            ["type"] = "large_test_config",
            ["timestamp"] = DateTime.UtcNow,
            ["version"] = "1.0.0",
            ["data"] = new List<Dictionary<string, object>>()
        };

        var jsonString = JsonSerializer.Serialize(config);
        var currentSize = Encoding.UTF8.GetByteCount(jsonString);

        while (currentSize < targetSizeBytes)
        {
            var dataItem = new Dictionary<string, object>
            {
                ["id"] = Guid.NewGuid().ToString(),
                ["value"] = GenerateRandomString(100),
                ["metadata"] = new { category = "test", priority = Random.Shared.Next(1, 10) }
            };

            ((List<Dictionary<string, object>>)config["data"]).Add(dataItem);
            jsonString = JsonSerializer.Serialize(config);
            currentSize = Encoding.UTF8.GetByteCount(jsonString);
        }

        return jsonString;
    }

    private static string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var stringBuilder = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            stringBuilder.Append(chars[Random.Shared.Next(chars.Length)]);
        }

        return stringBuilder.ToString();
    }

    /// <summary>
    /// Creates a test configuration object for testing purposes
    /// </summary>
    private static object CreateTestConfiguration(string configName, string value)
    {
        return new
        {
            Name = configName,
            Value = value,
            CreatedAt = DateTime.UtcNow,
            IsEnabled = true,
            Settings = new
            {
                Timeout = 30,
                MaxRetries = 3
            }
        };
    }

    #endregion
}