using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Application.Contracts.MCPGateway;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Aevatar.Application.Tests.MCPGateway;

/// <summary>
/// Simplified unit tests for MCP Gateway functionality
/// </summary>
public class SimpleMCPGatewayTests
{
    private readonly MockMCPGatewayManager _mockManager;
    private readonly ITestOutputHelper _testOutputHelper;

    public SimpleMCPGatewayTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        var mockLogger = new Mock<ILogger<MockMCPGatewayManager>>();
        _mockManager = new MockMCPGatewayManager(mockLogger.Object);
    }

    [Fact]
    public async Task MockMCPGatewayManager_CreateAdapter_ShouldWork()
    {
        // Arrange
        _mockManager.ClearTestData();
        
        var input = new CreateMCPAdapterDto
        {
            Name = "test-adapter",
            ImageName = "test-image",
            ImageVersion = "1.0.0",
            Description = "Test adapter"
        };

        // Act
        var result = await _mockManager.CreateAdapterAsync(input);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(input.Name);
        result.ImageName.ShouldBe(input.ImageName);
        result.Status.ShouldBe(MCPAdapterStatus.Creating);

        // Verify it can be retrieved
        var retrieved = await _mockManager.GetAdapterAsync(input.Name);
        retrieved.ShouldNotBeNull();
        retrieved.Name.ShouldBe(input.Name);

        _testOutputHelper.WriteLine($"Mock test passed: Created and retrieved adapter {result.Name}");
    }

    [Fact]
    public async Task MockMCPGatewayManager_GetAdapters_ShouldReturnPagedResults()
    {
        // Arrange
        _mockManager.ClearTestData();
        
        var adapters = new[]
        {
            new MCPAdapterDto { Name = "production-adapter", Status = MCPAdapterStatus.Running, IsHealthy = true },
            new MCPAdapterDto { Name = "development-adapter", Status = MCPAdapterStatus.Running, IsHealthy = false },
            new MCPAdapterDto { Name = "test-specific-adapter", Status = MCPAdapterStatus.Running, IsHealthy = true }
        };

        foreach (var adapter in adapters)
        {
            _mockManager.AddTestAdapter(adapter);
        }

        var input = new GetAdaptersInput
        {
            Search = "test-specific",
            MaxResultCount = 10,
            SkipCount = 0
        };

        // Act
        var result = await _mockManager.GetAdaptersAsync(input);

        // Assert
        result.ShouldNotBeNull();
        result.TotalCount.ShouldBe(1);
        result.Items.Count.ShouldBe(1);
        result.Items[0].Name.ShouldBe("test-specific-adapter");

        _testOutputHelper.WriteLine($"Pagination test passed: {result.Items.Count} items from {adapters.Length} total");
    }

    [Fact]
    public async Task MockMCPGatewayManager_GetAdapterStatus_ShouldWork()
    {
        // Act - use default adapter
        var result = await _mockManager.GetAdapterStatusAsync("default-adapter");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("default-adapter");
        result.Status.ShouldBe(MCPAdapterStatus.Running);
        result.IsHealthy.ShouldBeTrue();

        _testOutputHelper.WriteLine($"Status test passed: {result.Name} is {result.Status}");
    }

    [Fact]
    public async Task MockMCPGatewayManager_GetGatewayHealth_ShouldWork()
    {
        // Act
        var result = await _mockManager.GetGatewayHealthAsync();

        // Assert
        result.ShouldNotBeNull();
        result.IsHealthy.ShouldBeTrue();
        result.Version.ShouldBe("1.0.0-mock");

        _testOutputHelper.WriteLine($"Health test passed: Gateway is {result.IsHealthy}");
    }

    [Fact]
    public async Task MockMCPGatewayManager_TestConnection_ShouldWork()
    {
        // Act - use default adapter
        var result = await _mockManager.TestAdapterConnectionAsync("default-adapter");

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccessful.ShouldBeTrue();
        result.LatencyMs.ShouldBe(125.5);

        _testOutputHelper.WriteLine($"Connection test passed: Success={result.IsSuccessful}, Latency={result.LatencyMs}ms");
    }

    [Fact]
    public async Task MockMCPGatewayManager_GetLogs_ShouldWork()
    {
        // Act - use default adapter
        var result = await _mockManager.GetAdapterLogsAsync("default-adapter", 10);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBeGreaterThan(0);

        _testOutputHelper.WriteLine($"Logs test passed: Retrieved {result.Count} log lines");
    }
}
