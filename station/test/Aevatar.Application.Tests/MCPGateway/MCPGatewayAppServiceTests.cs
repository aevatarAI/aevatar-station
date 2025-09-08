using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Application.Contracts.MCPGateway;
using Aevatar.Application.MCPGateway;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Aevatar.Application.Tests.MCPGateway;

/// <summary>
/// Integration tests for MCPGatewayAppService
/// </summary>
public class MCPGatewayAppServiceTests : AevatarApplicationTestBase
{
    private readonly IMCPGatewayAppService _appService;
    private readonly MockMCPGatewayManager _mockGatewayManager;
    private readonly ITestOutputHelper _testOutputHelper;

    public MCPGatewayAppServiceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        
        // Get the mock gateway manager from DI container
        _mockGatewayManager = GetRequiredService<MockMCPGatewayManager>();
        _appService = GetRequiredService<IMCPGatewayAppService>();
        
        // Ensure clean state for each test
        _mockGatewayManager.ClearTestData();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        // Remove any existing registrations to avoid conflicts
        var mockManagerDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(MockMCPGatewayManager));
        if (mockManagerDescriptor != null)
        {
            services.Remove(mockManagerDescriptor);
        }
        
        var gatewayManagerDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMCPGatewayManager));
        if (gatewayManagerDescriptor != null)
        {
            services.Remove(gatewayManagerDescriptor);
        }

        // Register as singleton to ensure same instance
        services.AddSingleton<MockMCPGatewayManager>();
        services.AddSingleton<IMCPGatewayManager>(provider => provider.GetRequiredService<MockMCPGatewayManager>());
    }

    [Fact]
    public async Task CreateAdapterAsync_WithValidInput_ShouldReturnCreatedAdapter()
    {
        // Arrange - state already cleared in constructor
        
        var input = new CreateMCPAdapterDto
        {
            Name = "test-adapter",
            ImageName = "test-image",
            ImageVersion = "1.0.0",
            Description = "Test adapter",
            Environment = new Dictionary<string, string> { ["ENV_VAR"] = "value" },
            Tags = ["test", "demo"]
        };

        // Act
        var result = await _appService.CreateAdapterAsync(input);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(input.Name);
        result.ImageName.ShouldBe(input.ImageName);
        result.ImageVersion.ShouldBe(input.ImageVersion);
        result.Status.ShouldBe(MCPAdapterStatus.Creating);

        // Verify adapter was created in mock
        var createdAdapter = await _mockGatewayManager.GetAdapterAsync(input.Name);
        createdAdapter.ShouldNotBeNull();
        createdAdapter.Name.ShouldBe(input.Name);

        _testOutputHelper.WriteLine($"Successfully created adapter: {result.Name}");
    }

    [Fact]
    public async Task CreateAdapterAsync_WithExistingName_ShouldThrowException()
    {
        // Arrange - state already cleared in constructor
        
        var input = new CreateMCPAdapterDto
        {
            Name = "existing-adapter",
            ImageName = "test-image",
            ImageVersion = "1.0.0"
        };

        // First create an adapter to make it "existing"
        await _mockGatewayManager.CreateAdapterAsync(input);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await _appService.CreateAdapterAsync(input);
        });

        _testOutputHelper.WriteLine($"Correctly threw exception for duplicate adapter: {input.Name}");
    }

    [Fact]
    public async Task GetAdaptersAsync_WithFilters_ShouldReturnFilteredResults()
    {
        // Arrange
        _mockGatewayManager.ClearTestData();
        
        // Add test adapters
        var adapters = new[]
        {
            new MCPAdapterDto 
            { 
                Name = "adapter1", 
                Status = MCPAdapterStatus.Running, 
                IsHealthy = true,
                Tags = new List<string> { "production" }
            },
            new MCPAdapterDto 
            { 
                Name = "adapter2", 
                Status = MCPAdapterStatus.Failed, 
                IsHealthy = false,
                Tags = new List<string> { "development" }
            },
            new MCPAdapterDto 
            { 
                Name = "test-adapter", 
                Status = MCPAdapterStatus.Running, 
                IsHealthy = true,
                Tags = new List<string> { "test" }
            }
        };

        foreach (var adapter in adapters)
        {
            _mockGatewayManager.AddTestAdapter(adapter);
        }

        var input = new GetAdaptersInput
        {
            Search = "test",
            Status = MCPAdapterStatus.Running,
            IsHealthy = true,
            MaxResultCount = 10,
            SkipCount = 0
        };

        // Act
        var result = await _appService.GetAdaptersAsync(input);

        // Assert
        result.ShouldNotBeNull();
        result.TotalCount.ShouldBe(2);
        result.Items.Count.ShouldBe(2);
        result.Items[1].Name.ShouldBe("test-adapter");

        _testOutputHelper.WriteLine($"Filtered {adapters.Length} adapters to {result.Items.Count} results");
    }

    [Fact]
    public async Task GetAdapterAsync_WithExistingName_ShouldReturnAdapter()
    {
        // Arrange
        _mockGatewayManager.ClearTestData();
        
        var adapterName = "test-adapter";
        var testAdapter = new MCPAdapterDto
        {
            Name = adapterName,
            Status = MCPAdapterStatus.Running,
            IsHealthy = true
        };

        _mockGatewayManager.AddTestAdapter(testAdapter);

        // Act
        var result = await _appService.GetAdapterAsync(adapterName);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(adapterName);
        result.Status.ShouldBe(MCPAdapterStatus.Running);

        _testOutputHelper.WriteLine($"Successfully retrieved adapter: {result.Name}");
    }

    [Fact]
    public async Task GetAdapterAsync_WithNonExistentName_ShouldThrowException()
    {
        // Arrange
        _mockGatewayManager.ClearTestData();
        var adapterName = "non-existent-adapter";

        // Act & Assert
        await Should.ThrowAsync<KeyNotFoundException>(async () =>
        {
            await _appService.GetAdapterAsync(adapterName);
        });

        _testOutputHelper.WriteLine($"Correctly threw exception for non-existent adapter: {adapterName}");
    }

    [Fact]
    public async Task UpdateAdapterAsync_WithValidInput_ShouldReturnUpdatedAdapter()
    {
        // Arrange
        _mockGatewayManager.ClearTestData();
        
        var adapterName = "test-adapter";
        var existingAdapter = new MCPAdapterDto 
        { 
            Name = adapterName,
            ImageVersion = "1.0.0",
            Description = "Original description"
        };
        
        _mockGatewayManager.AddTestAdapter(existingAdapter);

        var input = new UpdateMCPAdapterDto
        {
            ImageVersion = "2.0.0",
            Description = "Updated description"
        };

        // Act
        var result = await _appService.UpdateAdapterAsync(adapterName, input);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(adapterName);
        result.ImageVersion.ShouldBe(input.ImageVersion);
        result.Description.ShouldBe(input.Description);

        _testOutputHelper.WriteLine($"Successfully updated adapter: {result.Name}");
    }

    [Fact]
    public async Task DeleteAdapterAsync_WithExistingAdapter_ShouldComplete()
    {
        // Arrange
        _mockGatewayManager.ClearTestData();
        
        var adapterName = "test-adapter";
        var existingAdapter = new MCPAdapterDto { Name = adapterName };
        _mockGatewayManager.AddTestAdapter(existingAdapter);

        // Act
        await _appService.DeleteAdapterAsync(adapterName);

        // Assert - verify adapter was deleted
        var deletedAdapter = await _mockGatewayManager.GetAdapterAsync(adapterName);
        deletedAdapter.ShouldBeNull();

        _testOutputHelper.WriteLine($"Successfully deleted adapter: {adapterName}");
    }

    [Fact]
    public async Task GetAdapterStatusAsync_ShouldReturnStatus()
    {
        // Arrange
        var adapterName = "default-adapter"; // Use the default adapter from mock

        // Act
        var result = await _appService.GetAdapterStatusAsync(adapterName);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(adapterName);
        result.Status.ShouldBe(MCPAdapterStatus.Running);
        result.IsHealthy.ShouldBeTrue();

        _testOutputHelper.WriteLine($"Adapter {adapterName} status: {result.Status}, Healthy: {result.IsHealthy}");
    }

    [Fact]
    public async Task TestAdapterConnectionAsync_ShouldReturnTestResult()
    {
        // Arrange
        var adapterName = "default-adapter"; // Use the default adapter from mock

        // Act
        var result = await _appService.TestAdapterConnectionAsync(adapterName);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccessful.ShouldBeTrue();
        result.LatencyMs.ShouldBe(125.5);

        _testOutputHelper.WriteLine($"Connection test for {adapterName}: Success={result.IsSuccessful}, Latency={result.LatencyMs}ms");
    }

    [Fact]
    public async Task GetGatewayHealthAsync_ShouldReturnHealthStatus()
    {
        // Act
        var result = await _appService.GetGatewayHealthAsync();

        // Assert
        result.ShouldNotBeNull();
        result.IsHealthy.ShouldBeTrue();
        result.Version.ShouldBe("1.0.0-mock");

        _testOutputHelper.WriteLine($"Gateway health: {result.IsHealthy}, Version: {result.Version}");
    }

    [Fact]
    public async Task GetAdapterMetricsAsync_ShouldReturnMetrics()
    {
        // Arrange
        var adapterName = "default-adapter"; // Use the default adapter from mock
        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow;

        // Act
        var result = await _appService.GetAdapterMetricsAsync(adapterName, from, to);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(adapterName);
        result.RequestStats.ShouldNotBeNull();
        result.RequestStats.TotalRequests.ShouldBeGreaterThan(0);

        _testOutputHelper.WriteLine($"Metrics for {adapterName}: {result.RequestStats.TotalRequests} requests");
    }

    [Fact]
    public async Task GetAdapterLogsAsync_ShouldReturnLogs()
    {
        // Arrange
        var adapterName = "default-adapter"; // Use the default adapter from mock

        // Act
        var result = await _appService.GetAdapterLogsAsync(adapterName, 10);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBeGreaterThan(0);

        _testOutputHelper.WriteLine($"Retrieved {result.Count} log lines for adapter {adapterName}");
    }

    [Theory]
    [InlineData("", "test-image", "1.0.0", false)] // Empty name
    [InlineData("test-adapter", "", "1.0.0", false)] // Empty image name
    [InlineData("test-adapter", "test-image", "", false)] // Empty version
    [InlineData("test-adapter", "test-image", "1.0.0", true)] // Valid input
    public async Task CreateAdapterAsync_WithVariousInputs_ShouldValidateCorrectly(
        string name, string imageName, string imageVersion, bool shouldSucceed)
    {
        // Arrange
        _mockGatewayManager.ClearTestData();
        
        var input = new CreateMCPAdapterDto
        {
            Name = name,
            ImageName = imageName,
            ImageVersion = imageVersion,
            Description = "Test adapter"
        };

        // Act & Assert
        if (shouldSucceed)
        {
            var result = await _appService.CreateAdapterAsync(input);
            result.ShouldNotBeNull();
            result.Name.ShouldBe(name);
            _testOutputHelper.WriteLine($"Valid input test passed for: {name}");
        }
        else
        {
            await Should.ThrowAsync<Exception>(async () =>
            {
                await _appService.CreateAdapterAsync(input);
            });
            _testOutputHelper.WriteLine($"Invalid input test passed for: {name}");
        }
    }

    [Fact]
    public async Task GetAdaptersAsync_WithPagination_ShouldReturnPagedResults()
    {
        // Arrange
        _mockGatewayManager.ClearTestData();
        
        var allAdapters = new List<MCPAdapterDto>();
        for (int i = 1; i <= 25; i++)
        {
            allAdapters.Add(new MCPAdapterDto 
            { 
                Name = $"adapter-{i:D2}", 
                Status = MCPAdapterStatus.Running,
                IsHealthy = i % 5 != 0 // Every 5th adapter is unhealthy
            });
        }

        foreach (var adapter in allAdapters)
        {
            _mockGatewayManager.AddTestAdapter(adapter);
        }

        var input = new GetAdaptersInput
        {
            MaxResultCount = 10,
            SkipCount = 5,
            Sorting = "name asc"
        };

        // Act
        var result = await _appService.GetAdaptersAsync(input);

        // Assert
        result.ShouldNotBeNull();
        result.TotalCount.ShouldBe(26);
        result.Items.Count.ShouldBe(10);
        result.Items[0].Name.ShouldBe("adapter-06");
        result.Items[9].Name.ShouldBe("adapter-15");

        _testOutputHelper.WriteLine($"Pagination test: {result.Items.Count} items from total {result.TotalCount}");
    }

    [Fact]
    public async Task GetAdaptersAsync_WithHealthFilter_ShouldFilterCorrectly()
    {
        // Arrange
        _mockGatewayManager.ClearTestData();
        
        var allAdapters = new[]
        {
            new MCPAdapterDto { Name = "healthy-1", IsHealthy = true },
            new MCPAdapterDto { Name = "healthy-2", IsHealthy = true },
            new MCPAdapterDto { Name = "unhealthy-1", IsHealthy = false },
            new MCPAdapterDto { Name = "unhealthy-2", IsHealthy = false }
        };

        foreach (var adapter in allAdapters)
        {
            _mockGatewayManager.AddTestAdapter(adapter);
        }

        var input = new GetAdaptersInput
        {
            IsHealthy = true,
            MaxResultCount = 100
        };

        // Act
        var result = await _appService.GetAdaptersAsync(input);

        // Assert
        result.ShouldNotBeNull();
        result.TotalCount.ShouldBe(3);
        result.Items.All(a => a.IsHealthy).ShouldBeTrue();
        result.Items.Skip(1).All(a => a.Name.StartsWith("healthy")).ShouldBeTrue();

        _testOutputHelper.WriteLine($"Health filter test: {result.Items.Count} healthy adapters found");
    }
}