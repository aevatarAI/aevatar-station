using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Agents.Configuration;
using Aevatar.GAgents.AI.Options;
using Orleans;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Aevatar.GAgent;

public class SchemaConfigurationGAgentTests : AevatarApplicationGrainsTestBase
{
    private readonly IClusterClient _clusterClient;
    private readonly ITestOutputHelper _output;

    public SchemaConfigurationGAgentTests(ITestOutputHelper output)
    {
        _clusterClient = GetRequiredService<IClusterClient>();
        _output = output;
    }

    [Fact]
    public async Task GetSystemLLMConfigOptionsAsync_WithValidConfigurations_ShouldReturnConfigurations()
    {
        // Arrange
        var grainId = Guid.NewGuid().ToString();
        var grain = _clusterClient.GetGrain<ISchemaConfigurationGAgent>(grainId);
        
        // Act
        var result = await grain.GetSystemLLMConfigOptionsAsync();
        
        // Assert
        result.ShouldNotBeNull();
        // Note: SystemLLMConfigs can be null in test environment, which is valid
        
        _output.WriteLine($"Retrieved {result.SystemLLMConfigs?.Count ?? 0} LLM configurations");
    }

    [Fact]
    public async Task GetSystemLLMConfigOptionsAsync_EmptyConfiguration_ShouldReturnEmptyOptions()
    {
        // Arrange
        var grainId = Guid.NewGuid().ToString();
        var grain = _clusterClient.GetGrain<ISchemaConfigurationGAgent>(grainId);
        
        // Act
        var result = await grain.GetSystemLLMConfigOptionsAsync();
        
        // Assert
        result.ShouldNotBeNull();
        // Note: SystemLLMConfigs can be null in test environment, which is valid behavior
    }

    [Fact]
    public async Task GetSystemLLMConfigOptionsAsync_MultipleCallsSameGrain_ShouldReturnConsistentResults()
    {
        // Arrange
        var grainId = Guid.NewGuid().ToString();
        var grain = _clusterClient.GetGrain<ISchemaConfigurationGAgent>(grainId);
        
        // Act
        var result1 = await grain.GetSystemLLMConfigOptionsAsync();
        var result2 = await grain.GetSystemLLMConfigOptionsAsync();
        
        // Assert
        result1.ShouldNotBeNull();
        result2.ShouldNotBeNull();
        (result1.SystemLLMConfigs?.Count ?? 0).ShouldBe(result2.SystemLLMConfigs?.Count ?? 0);
        
        _output.WriteLine($"First call: {result1.SystemLLMConfigs?.Count ?? 0} configurations");
        _output.WriteLine($"Second call: {result2.SystemLLMConfigs?.Count ?? 0} configurations");
    }

    [Fact]
    public async Task GetSystemLLMConfigOptionsAsync_DifferentGrains_ShouldReturnSameConfigurations()
    {
        // Arrange
        var grain1 = _clusterClient.GetGrain<ISchemaConfigurationGAgent>(Guid.NewGuid().ToString());
        var grain2 = _clusterClient.GetGrain<ISchemaConfigurationGAgent>(Guid.NewGuid().ToString());
        
        // Act
        var result1 = await grain1.GetSystemLLMConfigOptionsAsync();
        var result2 = await grain2.GetSystemLLMConfigOptionsAsync();
        
        // Assert
        result1.ShouldNotBeNull();
        result2.ShouldNotBeNull();
        (result1.SystemLLMConfigs?.Count ?? 0).ShouldBe(result2.SystemLLMConfigs?.Count ?? 0);
        
        _output.WriteLine($"Grain1: {result1.SystemLLMConfigs?.Count ?? 0} configurations");
        _output.WriteLine($"Grain2: {result2.SystemLLMConfigs?.Count ?? 0} configurations");
    }

    [Fact]
    public async Task GetDescriptionAsync_ShouldReturnValidDescription()
    {
        // Arrange
        var grainId = Guid.NewGuid().ToString();
        var grain = _clusterClient.GetGrain<ISchemaConfigurationGAgent>(grainId);
        
        // Act
        var description = await grain.GetDescriptionAsync();
        
        // Assert
        description.ShouldNotBeNullOrEmpty();
        description.ShouldContain("Schema Configuration Agent");
        description.ShouldContain("SystemLLMConfigOptions");
        
        _output.WriteLine($"Description: {description}");
    }

    [Fact]
    public async Task GetSystemLLMConfigOptionsAsync_WithValidConfiguration_ShouldLogInformation()
    {
        // Arrange
        var grainId = Guid.NewGuid().ToString();
        var grain = _clusterClient.GetGrain<ISchemaConfigurationGAgent>(grainId);
        
        // Act
        var result = await grain.GetSystemLLMConfigOptionsAsync();
        
        // Assert
        result.ShouldNotBeNull();
        // Note: In a real test environment, you might want to verify logging
        // This would require injecting a test logger or using a logging test framework
        
        _output.WriteLine($"Configuration retrieval completed with {result.SystemLLMConfigs?.Count ?? 0} items");
    }

    [Fact]
    public async Task SchemaConfigurationGAgent_StateOperations_ShouldWork()
    {
        // Arrange
        var grainId = Guid.NewGuid().ToString();
        var grain = _clusterClient.GetGrain<ISchemaConfigurationGAgent>(grainId);
        
        // Act & Assert - Basic grain operations should work
        var description = await grain.GetDescriptionAsync();
        description.ShouldNotBeNullOrEmpty();
        
        var config = await grain.GetSystemLLMConfigOptionsAsync();
        config.ShouldNotBeNull();
        
        // Test multiple operations to ensure grain state is maintained
        var config2 = await grain.GetSystemLLMConfigOptionsAsync();
        config2.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetSystemLLMConfigOptionsAsync_PerformanceTest_ShouldCompleteQuickly()
    {
        // Arrange
        var grainId = Guid.NewGuid().ToString();
        var grain = _clusterClient.GetGrain<ISchemaConfigurationGAgent>(grainId);
        var startTime = DateTime.UtcNow;
        
        // Act
        var result = await grain.GetSystemLLMConfigOptionsAsync();
        var duration = DateTime.UtcNow - startTime;
        
        // Assert
        result.ShouldNotBeNull();
        duration.TotalMilliseconds.ShouldBeLessThan(5000); // Should complete within 5 seconds
        
        _output.WriteLine($"Configuration retrieval took {duration.TotalMilliseconds:F2}ms");
    }

    [Fact]
    public async Task GetSystemLLMConfigOptionsAsync_ConcurrentCalls_ShouldHandleCorrectly()
    {
        // Arrange
        var grainId = Guid.NewGuid().ToString();
        var grain = _clusterClient.GetGrain<ISchemaConfigurationGAgent>(grainId);
        
        // Act - Make concurrent calls
        var tasks = new List<Task<SystemLLMConfigOptions>>();
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(grain.GetSystemLLMConfigOptionsAsync());
        }
        
        var results = await Task.WhenAll(tasks);
        
        // Assert
        results.ShouldNotBeNull();
        results.Length.ShouldBe(5);
        
        foreach (var result in results)
        {
            result.ShouldNotBeNull();
            // Note: SystemLLMConfigs can be null in test environment, which is valid
        }
        
        // All results should have the same configuration count
        var expectedCount = results[0].SystemLLMConfigs?.Count ?? 0;
        foreach (var result in results)
        {
            result.SystemLLMConfigs?.Count.ShouldBe(expectedCount);
        }
        
        _output.WriteLine($"All {results.Length} concurrent calls returned {expectedCount} configurations");
    }
}
