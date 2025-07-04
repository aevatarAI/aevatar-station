# TODO-014: Create Integration Tests for New Services

## Task Overview
Create comprehensive integration tests for all new architecture services to ensure they work correctly together and with external dependencies (Orleans, Elasticsearch, etc.).

## Description
Develop a full suite of integration tests that validate the new architecture services working together in realistic scenarios. These tests should cover end-to-end workflows, service interactions, and integration with external systems like Orleans and Elasticsearch.

## Acceptance Criteria
- [ ] Create integration test framework and base classes
- [ ] Test complete agent lifecycle workflows
- [ ] Test agent discovery scenarios with real Elasticsearch
- [ ] Test event publishing and consumption flows
- [ ] Test Orleans integration and grain interactions
- [ ] Test error scenarios and resilience patterns
- [ ] Create performance baseline tests
- [ ] Test concurrent operations and race conditions
- [ ] Add database/Elasticsearch test containers
- [ ] Create test data generation utilities
- [ ] Document test setup and execution procedures

## File Locations
- `station/test/Aevatar.Application.IntegrationTests/`
- `station/test/Aevatar.Application.IntegrationTests/Services/AgentLifecycleServiceTests.cs`
- `station/test/Aevatar.Application.IntegrationTests/Services/AgentDiscoveryServiceTests.cs`
- `station/test/Aevatar.Application.IntegrationTests/Services/EventPublisherTests.cs`
- `station/test/Aevatar.Application.IntegrationTests/Services/TypeMetadataServiceTests.cs`
- `station/test/Aevatar.Application.IntegrationTests/Workflows/AgentWorkflowTests.cs`
- `station/test/Aevatar.Application.IntegrationTests/Infrastructure/TestBase.cs`

## Integration Test Framework

### Base Test Class
```csharp
public abstract class AgentArchitectureTestBase : IAsyncLifetime
{
    protected IServiceProvider ServiceProvider { get; private set; }
    protected ITestOutputHelper Output { get; }
    
    private readonly ElasticsearchContainer _elasticsearchContainer;
    private readonly OrleansTestCluster _orleansTestCluster;
    
    protected AgentArchitectureTestBase(ITestOutputHelper output)
    {
        Output = output;
        
        _elasticsearchContainer = new ElasticsearchBuilder()
            .WithImage("elasticsearch:8.8.0")
            .WithPortBinding(9200, true)
            .WithEnvironment("discovery.type", "single-node")
            .WithEnvironment("xpack.security.enabled", "false")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(9200))
            .Build();
    }
    
    public async Task InitializeAsync()
    {
        // Start test containers
        await _elasticsearchContainer.StartAsync();
        
        // Configure test services
        var services = new ServiceCollection();
        ConfigureTestServices(services);
        ServiceProvider = services.BuildServiceProvider();
        
        // Initialize test data
        await InitializeTestDataAsync();
    }
    
    private void ConfigureTestServices(IServiceCollection services)
    {
        // Configure test configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Elasticsearch:ConnectionString"] = _elasticsearchContainer.GetConnectionString(),
                ["Orleans:Environment"] = "Test",
                ["AgentArchitecture:EnableNewArchitecture"] = "true"
            })
            .Build();
        
        services.AddSingleton<IConfiguration>(configuration);
        
        // Add agent architecture services
        services.AddAgentArchitecture(configuration);
        
        // Add logging
        services.AddLogging(builder => builder.AddXUnit(Output));
        
        // Add test utilities
        services.AddScoped<TestDataGenerator>();
        services.AddScoped<AgentTestHelpers>();
    }
    
    protected abstract Task InitializeTestDataAsync();
    
    public async Task DisposeAsync()
    {
        await _elasticsearchContainer.DisposeAsync();
        _orleansTestCluster?.Dispose();
        ServiceProvider?.Dispose();
    }
}
```

### Test Data Generation
```csharp
public class TestDataGenerator
{
    private readonly ITypeMetadataService _typeMetadataService;
    private readonly IElasticsearchClient _elasticsearchClient;
    
    public async Task<List<AgentInfo>> GenerateTestAgentsAsync(int count = 10)
    {
        var agents = new List<AgentInfo>();
        var agentTypes = new[] { "BusinessAgent", "TaskAgent", "ChatAgent" };
        var userIds = Enumerable.Range(1, 5).Select(_ => Guid.NewGuid()).ToList();
        
        for (int i = 0; i < count; i++)
        {
            var agent = new AgentInfo
            {
                Id = Guid.NewGuid(),
                UserId = userIds[i % userIds.Count],
                AgentType = agentTypes[i % agentTypes.Length],
                Name = $"Test Agent {i + 1}",
                Properties = new Dictionary<string, object>
                {
                    ["TestProperty"] = $"Value_{i}",
                    ["NumericProperty"] = i
                },
                Status = (AgentStatus)(i % 4),
                CreatedAt = DateTime.UtcNow.AddDays(-i),
                LastActivity = DateTime.UtcNow.AddHours(-i)
            };
            
            agents.Add(agent);
        }
        
        // Index in Elasticsearch
        await IndexAgentsAsync(agents);
        
        return agents;
    }
    
    private async Task IndexAgentsAsync(List<AgentInfo> agents)
    {
        var indexOperations = agents.Select(agent => new IndexOperation<AgentInstanceState>(
            MapToAgentInstanceState(agent))
        {
            Index = "agent-instances"
        });
        
        var bulkRequest = new BulkRequest
        {
            Operations = indexOperations.Cast<IBulkOperation>().ToList()
        };
        
        await _elasticsearchClient.BulkAsync(bulkRequest);
        
        // Refresh index for immediate search availability
        await _elasticsearchClient.Indices.RefreshAsync("agent-instances");
    }
}
```

## Service Integration Tests

### AgentLifecycleService Integration Tests
```csharp
public class AgentLifecycleServiceIntegrationTests : AgentArchitectureTestBase
{
    private readonly IAgentLifecycleService _lifecycleService;
    private readonly TestDataGenerator _testDataGenerator;
    
    public AgentLifecycleServiceIntegrationTests(ITestOutputHelper output) : base(output)
    {
    }
    
    protected override async Task InitializeTestDataAsync()
    {
        _lifecycleService = ServiceProvider.GetRequiredService<IAgentLifecycleService>();
        _testDataGenerator = ServiceProvider.GetRequiredService<TestDataGenerator>();
        
        // Generate test agent types
        await SeedAgentTypesAsync();
    }
    
    [Fact]
    public async Task CreateAgentAsync_ValidRequest_CreatesAgentSuccessfully()
    {
        // Arrange
        var request = new CreateAgentRequest
        {
            UserId = Guid.NewGuid(),
            AgentType = "BusinessAgent",
            Name = "Test Business Agent",
            Properties = new Dictionary<string, object>
            {
                ["Department"] = "Sales",
                ["Priority"] = "High"
            }
        };
        
        // Act
        var result = await _lifecycleService.CreateAgentAsync(request);
        
        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(request.UserId, result.UserId);
        Assert.Equal(request.AgentType, result.AgentType);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(AgentStatus.Active, result.Status);
        
        // Verify agent is searchable in Elasticsearch
        await Task.Delay(1000); // Allow for indexing
        var discoveryService = ServiceProvider.GetRequiredService<IAgentDiscoveryService>();
        var foundAgent = await discoveryService.FindAgentByIdAsync(result.Id);
        Assert.NotNull(foundAgent);
    }
    
    [Fact]
    public async Task UpdateAgentAsync_ExistingAgent_UpdatesSuccessfully()
    {
        // Arrange
        var agent = await CreateTestAgentAsync();
        var updateRequest = new UpdateAgentRequest
        {
            Name = "Updated Agent Name",
            Properties = new Dictionary<string, object>
            {
                ["NewProperty"] = "NewValue"
            }
        };
        
        // Act
        var result = await _lifecycleService.UpdateAgentAsync(agent.Id, updateRequest);
        
        // Assert
        Assert.Equal(updateRequest.Name, result.Name);
        Assert.Contains("NewProperty", result.Properties.Keys);
        Assert.True(result.LastActivity > agent.LastActivity);
    }
    
    [Fact]
    public async Task DeleteAgentAsync_ExistingAgent_DeletesSuccessfully()
    {
        // Arrange
        var agent = await CreateTestAgentAsync();
        
        // Act
        await _lifecycleService.DeleteAgentAsync(agent.Id);
        
        // Assert
        var discoveryService = ServiceProvider.GetRequiredService<IAgentDiscoveryService>();
        var foundAgent = await discoveryService.FindAgentByIdAsync(agent.Id);
        Assert.Null(foundAgent);
    }
    
    private async Task<AgentInfo> CreateTestAgentAsync()
    {
        var request = new CreateAgentRequest
        {
            UserId = Guid.NewGuid(),
            AgentType = "BusinessAgent",
            Name = "Test Agent",
            Properties = new Dictionary<string, object>()
        };
        
        return await _lifecycleService.CreateAgentAsync(request);
    }
}
```

### AgentDiscoveryService Integration Tests
```csharp
public class AgentDiscoveryServiceIntegrationTests : AgentArchitectureTestBase
{
    private readonly IAgentDiscoveryService _discoveryService;
    private readonly TestDataGenerator _testDataGenerator;
    private List<AgentInfo> _testAgents;
    
    public AgentDiscoveryServiceIntegrationTests(ITestOutputHelper output) : base(output)
    {
    }
    
    protected override async Task InitializeTestDataAsync()
    {
        _discoveryService = ServiceProvider.GetRequiredService<IAgentDiscoveryService>();
        _testDataGenerator = ServiceProvider.GetRequiredService<TestDataGenerator>();
        
        // Generate test data
        _testAgents = await _testDataGenerator.GenerateTestAgentsAsync(20);
    }
    
    [Fact]
    public async Task FindAgentsAsync_ByUserId_ReturnsCorrectAgents()
    {
        // Arrange
        var targetUserId = _testAgents.First().UserId;
        var expectedCount = _testAgents.Count(a => a.UserId == targetUserId);
        
        var query = new AgentDiscoveryQuery
        {
            UserId = targetUserId
        };
        
        // Act
        var result = await _discoveryService.FindAgentsAsync(query);
        
        // Assert
        Assert.Equal(expectedCount, result.Count);
        Assert.All(result, agent => Assert.Equal(targetUserId, agent.UserId));
    }
    
    [Fact]
    public async Task FindAgentsAsync_ByCapability_ReturnsAgentsWithCapability()
    {
        // Arrange
        var query = new AgentDiscoveryQuery
        {
            RequiredCapabilities = new List<string> { "TaskCompleted" }
        };
        
        // Act
        var result = await _discoveryService.FindAgentsAsync(query);
        
        // Assert
        Assert.All(result, agent => 
            Assert.Contains("TaskCompleted", agent.Capabilities));
    }
    
    [Fact]
    public async Task FindAgentsAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var query = new AgentDiscoveryQuery
        {
            Skip = 5,
            Take = 10,
            SortBy = "CreatedAt",
            SortOrder = SortOrder.Descending
        };
        
        // Act
        var result = await _discoveryService.FindAgentsWithPaginationAsync(query);
        
        // Assert
        Assert.True(result.Agents.Count <= 10);
        Assert.Equal(5, result.Skip);
        Assert.Equal(10, result.Take);
        Assert.True(result.TotalCount >= result.Agents.Count);
    }
    
    [Fact]
    public async Task FindAgentsAsync_ComplexQuery_ReturnsFilteredResults()
    {
        // Arrange
        var targetUserId = _testAgents.First().UserId;
        var query = new AgentDiscoveryQuery
        {
            UserId = targetUserId,
            AgentTypes = new List<string> { "BusinessAgent", "TaskAgent" },
            Status = AgentStatus.Active,
            CreatedAfter = DateTime.UtcNow.AddDays(-10)
        };
        
        // Act
        var result = await _discoveryService.FindAgentsAsync(query);
        
        // Assert
        Assert.All(result, agent =>
        {
            Assert.Equal(targetUserId, agent.UserId);
            Assert.Contains(agent.AgentType, new[] { "BusinessAgent", "TaskAgent" });
            Assert.Equal(AgentStatus.Active, agent.Status);
            Assert.True(agent.CreatedAt > DateTime.UtcNow.AddDays(-10));
        });
    }
}
```

### EventPublisher Integration Tests
```csharp
public class EventPublisherIntegrationTests : AgentArchitectureTestBase
{
    private readonly IEventPublisher _eventPublisher;
    private readonly TestAgentGrain _testAgent;
    
    public EventPublisherIntegrationTests(ITestOutputHelper output) : base(output)
    {
    }
    
    protected override async Task InitializeTestDataAsync()
    {
        _eventPublisher = ServiceProvider.GetRequiredService<IEventPublisher>();
        
        // Set up test agent grain for event reception
        var orleansClient = ServiceProvider.GetRequiredService<IClusterClient>();
        _testAgent = orleansClient.GetGrain<TestAgentGrain>(Guid.NewGuid());
        await _testAgent.InitializeAsync();
    }
    
    [Fact]
    public async Task PublishEventAsync_ValidEvent_DeliversToAgent()
    {
        // Arrange
        var testEvent = new TaskCompletedEvent
        {
            TaskId = Guid.NewGuid(),
            CompletedAt = DateTime.UtcNow,
            Result = "Success"
        };
        
        // Act
        await _eventPublisher.PublishEventAsync(testEvent, _testAgent.GetPrimaryKey().ToString());
        
        // Wait for event processing
        await Task.Delay(2000);
        
        // Assert
        var receivedEvents = await _testAgent.GetReceivedEventsAsync();
        Assert.Contains(receivedEvents, e => 
            e is TaskCompletedEvent tce && tce.TaskId == testEvent.TaskId);
    }
    
    [Fact]
    public async Task PublishBroadcastEventAsync_ValidEvent_DeliversToMultipleAgents()
    {
        // Arrange
        var orleansClient = ServiceProvider.GetRequiredService<IClusterClient>();
        var agents = new[]
        {
            orleansClient.GetGrain<TestAgentGrain>(Guid.NewGuid()),
            orleansClient.GetGrain<TestAgentGrain>(Guid.NewGuid()),
            orleansClient.GetGrain<TestAgentGrain>(Guid.NewGuid())
        };
        
        foreach (var agent in agents)
        {
            await agent.InitializeAsync();
            await agent.SubscribeToBroadcastAsync("test-namespace");
        }
        
        var broadcastEvent = new SystemAnnouncementEvent
        {
            Message = "System maintenance scheduled",
            Timestamp = DateTime.UtcNow
        };
        
        // Act
        await _eventPublisher.PublishBroadcastEventAsync(broadcastEvent, "test-namespace");
        
        // Wait for event processing
        await Task.Delay(3000);
        
        // Assert
        foreach (var agent in agents)
        {
            var receivedEvents = await agent.GetReceivedEventsAsync();
            Assert.Contains(receivedEvents, e => 
                e is SystemAnnouncementEvent sae && sae.Message == broadcastEvent.Message);
        }
    }
}
```

## Workflow Integration Tests

### Complete Agent Lifecycle Workflow
```csharp
public class AgentWorkflowIntegrationTests : AgentArchitectureTestBase
{
    [Fact]
    public async Task CompleteAgentLifecycle_CreateUpdateDiscoverDelete_WorksEndToEnd()
    {
        // Arrange
        var lifecycleService = ServiceProvider.GetRequiredService<IAgentLifecycleService>();
        var discoveryService = ServiceProvider.GetRequiredService<IAgentDiscoveryService>();
        var eventPublisher = ServiceProvider.GetRequiredService<IEventPublisher>();
        
        var userId = Guid.NewGuid();
        
        // Act & Assert - Create
        var createRequest = new CreateAgentRequest
        {
            UserId = userId,
            AgentType = "BusinessAgent",
            Name = "Workflow Test Agent",
            Properties = new Dictionary<string, object> { ["Department"] = "Engineering" }
        };
        
        var createdAgent = await lifecycleService.CreateAgentAsync(createRequest);
        Assert.NotNull(createdAgent);
        Assert.Equal(AgentStatus.Active, createdAgent.Status);
        
        // Act & Assert - Discover
        await Task.Delay(1000); // Allow for Elasticsearch indexing
        
        var discoveryQuery = new AgentDiscoveryQuery { UserId = userId };
        var discoveredAgents = await discoveryService.FindAgentsAsync(discoveryQuery);
        Assert.Single(discoveredAgents);
        Assert.Equal(createdAgent.Id, discoveredAgents.First().Id);
        
        // Act & Assert - Update
        var updateRequest = new UpdateAgentRequest
        {
            Name = "Updated Workflow Agent",
            Properties = new Dictionary<string, object> { ["Department"] = "Sales" }
        };
        
        var updatedAgent = await lifecycleService.UpdateAgentAsync(createdAgent.Id, updateRequest);
        Assert.Equal("Updated Workflow Agent", updatedAgent.Name);
        Assert.Equal("Sales", updatedAgent.Properties["Department"]);
        
        // Act & Assert - Send Event
        var testEvent = new TaskAssignedEvent
        {
            TaskId = Guid.NewGuid(),
            AssignedTo = createdAgent.Id,
            AssignedAt = DateTime.UtcNow
        };
        
        await eventPublisher.PublishEventAsync(testEvent, createdAgent.Id.ToString());
        
        // Act & Assert - Delete
        await lifecycleService.DeleteAgentAsync(createdAgent.Id);
        
        // Verify deletion
        await Task.Delay(1000);
        var deletedAgent = await discoveryService.FindAgentByIdAsync(createdAgent.Id);
        Assert.Null(deletedAgent);
    }
}
```

## Performance and Load Tests

### Concurrent Operations Test
```csharp
public class PerformanceIntegrationTests : AgentArchitectureTestBase
{
    [Fact]
    public async Task ConcurrentAgentCreation_MultipleThreads_HandlesCorrectly()
    {
        // Arrange
        var lifecycleService = ServiceProvider.GetRequiredService<IAgentLifecycleService>();
        var concurrentOperations = 50;
        var tasks = new List<Task<AgentInfo>>();
        
        // Act
        for (int i = 0; i < concurrentOperations; i++)
        {
            var request = new CreateAgentRequest
            {
                UserId = Guid.NewGuid(),
                AgentType = "BusinessAgent",
                Name = $"Concurrent Agent {i}",
                Properties = new Dictionary<string, object>()
            };
            
            tasks.Add(lifecycleService.CreateAgentAsync(request));
        }
        
        var results = await Task.WhenAll(tasks);
        
        // Assert
        Assert.Equal(concurrentOperations, results.Length);
        Assert.All(results, agent => Assert.NotEqual(Guid.Empty, agent.Id));
        
        // Verify all agents are unique
        var uniqueIds = results.Select(a => a.Id).Distinct().Count();
        Assert.Equal(concurrentOperations, uniqueIds);
    }
    
    [Fact]
    public async Task HighVolumeDiscovery_LargeDataset_MaintainsPerformance()
    {
        // Arrange
        var testDataGenerator = ServiceProvider.GetRequiredService<TestDataGenerator>();
        var discoveryService = ServiceProvider.GetRequiredService<IAgentDiscoveryService>();
        
        // Generate large dataset
        await testDataGenerator.GenerateTestAgentsAsync(1000);
        
        var stopwatch = Stopwatch.StartNew();
        
        // Act
        var query = new AgentDiscoveryQuery
        {
            RequiredCapabilities = new List<string> { "TaskCompleted" },
            Take = 100
        };
        
        var results = await discoveryService.FindAgentsAsync(query);
        stopwatch.Stop();
        
        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
            $"Query took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");
        Assert.True(results.Count <= 100);
    }
}
```

## Test Utilities and Helpers

### Agent Test Helpers
```csharp
public class AgentTestHelpers
{
    private readonly IServiceProvider _serviceProvider;
    
    public AgentTestHelpers(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public async Task<AgentInfo> CreateTestAgentAsync(
        string agentType = "BusinessAgent",
        Guid? userId = null,
        Dictionary<string, object> properties = null)
    {
        var lifecycleService = _serviceProvider.GetRequiredService<IAgentLifecycleService>();
        
        var request = new CreateAgentRequest
        {
            UserId = userId ?? Guid.NewGuid(),
            AgentType = agentType,
            Name = $"Test {agentType} {Guid.NewGuid():N}",
            Properties = properties ?? new Dictionary<string, object>()
        };
        
        return await lifecycleService.CreateAgentAsync(request);
    }
    
    public async Task WaitForElasticsearchRefreshAsync(int delayMs = 1000)
    {
        await Task.Delay(delayMs);
        
        var client = _serviceProvider.GetRequiredService<IElasticsearchClient>();
        await client.Indices.RefreshAsync("agent-instances");
    }
    
    public async Task CleanupTestDataAsync()
    {
        var client = _serviceProvider.GetRequiredService<IElasticsearchClient>();
        
        // Delete test indices
        await client.Indices.DeleteAsync("agent-instances");
    }
}
```

## Dependencies
- All new architecture services (TODO-002 through TODO-013)
- Testcontainers for Elasticsearch and Orleans
- xUnit testing framework
- Orleans TestKit
- Test data generation utilities

## Success Metrics
- All integration tests pass consistently
- Tests cover 90%+ of service integration scenarios
- Performance tests validate acceptable response times
- Concurrent operation tests pass without race conditions
- End-to-end workflows complete successfully

## CI/CD Integration
- Tests run in CI pipeline with real containers
- Performance baseline tracking
- Test result reporting and analysis
- Automatic rollback triggers on test failures

## Priority: Low
These tests should be implemented after all core services are complete to validate the entire architecture works together correctly.