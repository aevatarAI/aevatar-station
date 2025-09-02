# GAgent Unit Testing Guide - Part 2: Basic Testing Patterns

## Overview

This guide covers the fundamental testing patterns for GAgents, providing developers with the essential techniques needed to write effective unit tests. Understanding these patterns is crucial for building robust test suites that validate GAgent behavior correctly.

## Basic GAgent Test Pattern

### Arrange-Act-Assert Pattern

The fundamental pattern for GAgent testing follows the standard Arrange-Act-Assert structure:

```csharp
[Fact]
public async Task Should_UpdateState_When_EventReceived()
{
    // Arrange - Set up test conditions
    var agentId = Guid.NewGuid();
    var agent = await _agentFactory.GetGAgentAsync<IMyGAgent>(agentId);
    var testEvent = new MyEvent { Data = "test-data" };
    
    // Act - Execute the method being tested
    await agent.HandleEventAsync(testEvent);
    
    // Assert - Verify the expected outcome
    var state = await agent.GetStateAsync();
    state.Data.ShouldBe("test-data");
}
```

### Complete Example:

```csharp
[Fact]
public async Task Should_ProcessMessage_When_ValidInput()
{
    // Arrange
    var agentId = Guid.NewGuid();
    var agent = await _agentFactory.GetGAgentAsync<IMyGAgent>(agentId);
    var message = "Hello, World!";
    
    _outputHelper.WriteLine($"Testing message processing for: {message}");
    
    // Act
    var result = await agent.ProcessMessageAsync(message);
    
    // Assert
    result.ShouldNotBeNull();
    result.ShouldBe("Processed: Hello, World!");
    
    // Verify state changes
    var state = await agent.GetStateAsync();
    state.MessagesProcessed.ShouldBe(1);
    state.LastProcessedMessage.ShouldBe(message);
    
    _outputHelper.WriteLine($"Message processed successfully. Total processed: {state.MessagesProcessed}");
}
```

## State-Based Testing Patterns

### Testing State Initialization

```csharp
[Fact]
public async Task Should_InitializeState_When_AgentCreated()
{
    // Arrange
    var agentId = Guid.NewGuid();
    
    // Act
    var agent = await _agentFactory.GetGAgentAsync<IMyGAgent>(agentId);
    
    // Assert
    var state = await agent.GetStateAsync();
    state.ShouldNotBeNull();
    state.Id.ShouldBe(agentId);
    state.Status.ShouldBe("Initialized");
    state.CreatedAt.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1));
}
```

### Testing State Updates

```csharp
[Fact]
public async Task Should_UpdateState_When_MultipleEventsProcessed()
{
    // Arrange
    var agentId = Guid.NewGuid();
    var agent = await _agentFactory.GetGAgentAsync<IMyGAgent>(agentId);
    
    // Act - Process multiple events
    await agent.ProcessEventAsync(new Event1 { Value = 10 });
    await agent.ProcessEventAsync(new Event2 { Value = 20 });
    await agent.ProcessEventAsync(new Event3 { Value = 30 });
    
    // Assert - Verify cumulative state changes
    var state = await agent.GetStateAsync();
    state.TotalValue.ShouldBe(60);
    state.EventCount.ShouldBe(3);
    state.LastUpdated.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1));
}
```

### Testing State Validation

```csharp
[Fact]
public async Task Should_ValidateState_When_InvalidDataProvided()
{
    // Arrange
    var agentId = Guid.NewGuid();
    var agent = await _agentFactory.GetGAgentAsync<IMyGAgent>(agentId);
    
    // Act & Assert
    var exception = await Should.ThrowAsync<ValidationException>(
        () => agent.UpdateDataAsync(new InvalidData { Value = -1 })
    );
    
    exception.Message.ShouldContain("Value must be positive");
    
    // Verify state wasn't corrupted
    var state = await agent.GetStateAsync();
    state.Value.ShouldBe(0); // Default value
}
```

## Event Communication Testing Patterns

### Testing Event Publishing

```csharp
[Fact]
public async Task Should_PublishEvent_When_ConditionMet()
{
    // Arrange
    var agentId = Guid.NewGuid();
    var agent = await _agentFactory.GetGAgentAsync<IMyGAgent>(agentId);
    
    // Setup event subscription (if needed)
    var eventReceived = new TaskCompletionSource<MyEvent>();
    // Subscribe to events here
    
    // Act
    await agent.TriggerEventAsync();
    
    // Assert
    var eventResult = await eventReceived.Task.TimeoutAfter(TimeSpan.FromSeconds(5));
    eventResult.ShouldNotBeNull();
    eventResult.Source.ShouldBe(agentId);
}
```

### Testing Event Handling

```csharp
[Fact]
public async Task Should_HandleEvent_When_Received()
{
    // Arrange
    var agentId = Guid.NewGuid();
    var agent = await _agentFactory.GetGAgentAsync<IMyGAgent>(agentId);
    var testEvent = new MyEvent 
    { 
        Id = Guid.NewGuid(),
        Data = "test-data",
        Timestamp = DateTime.UtcNow
    };
    
    _outputHelper.WriteLine($"Sending event: {testEvent.Id}");
    
    // Act
    await agent.HandleMyEventAsync(testEvent);
    
    // Assert
    var state = await agent.GetStateAsync();
    state.LastEventId.ShouldBe(testEvent.Id);
    state.LastEventData.ShouldBe("test-data");
    state.EventsReceived.ShouldBe(1);
    
    _outputHelper.WriteLine($"Event handled successfully. Total events: {state.EventsReceived}");
}
```

### Testing Event Filtering

```csharp
[Theory]
[InlineData("important", true)]
[InlineData("normal", false)]
[InlineData("low", false)]
public async Task Should_FilterEvents_When_PrioritySet(string priority, bool shouldProcess)
{
    // Arrange
    var agentId = Guid.NewGuid();
    var agent = await _agentFactory.GetGAgentAsync<IMyGAgent>(agentId);
    
    // Configure agent to only process "important" events
    await agent.SetPriorityThresholdAsync("important");
    
    var testEvent = new MyEvent 
    { 
        Priority = priority,
        Data = $"test-{priority}"
    };
    
    // Act
    await agent.ProcessEventAsync(testEvent);
    
    // Assert
    var state = await agent.GetStateAsync();
    
    if (shouldProcess)
    {
        state.ProcessedEvents.ShouldBe(1);
        state.LastProcessedPriority.ShouldBe(priority);
    }
    else
    {
        state.ProcessedEvents.ShouldBe(0);
    }
}
```

## Test Data Management

### Factory Methods for Test Data

```csharp
public static class TestDataFactory
{
    public static MyGAgentConfig CreateValidConfig()
    {
        return new MyGAgentConfig
        {
            Name = "Test Agent",
            Enabled = true,
            MaxRetries = 3,
            Timeout = TimeSpan.FromSeconds(30)
        };
    }
    
    public static MyEvent CreateTestEvent(string data = "default")
    {
        return new MyEvent
        {
            Id = Guid.NewGuid(),
            Data = data,
            Timestamp = DateTime.UtcNow,
            Priority = "normal"
        };
    }
    
    public static List<MyEvent> CreateTestEvents(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => CreateTestEvent($"event-{i}"))
            .ToList();
    }
}
```

### Using Factory Methods in Tests

```csharp
[Fact]
public async Task Should_ProcessBatch_When_MultipleEventsReceived()
{
    // Arrange
    var agentId = Guid.NewGuid();
    var agent = await _agentFactory.GetGAgentAsync<IMyGAgent>(agentId);
    var events = TestDataFactory.CreateTestEvents(5);
    
    _outputHelper.WriteLine($"Processing {events.Count} events");
    
    // Act
    var results = await agent.ProcessBatchAsync(events);
    
    // Assert
    results.ShouldNotBeNull();
    results.Count.ShouldBe(5);
    results.All(r => r.Success).ShouldBeTrue();
    
    var state = await agent.GetStateAsync();
    state.TotalProcessed.ShouldBe(5);
    state.BatchCount.ShouldBe(1);
    
    _outputHelper.WriteLine($"Batch processed successfully. Success rate: {results.Count(r => r.Success)}/{results.Count}");
}
```

### Test Data Builders

```csharp
public class MyEventBuilder
{
    private MyEvent _event = new MyEvent();
    
    public MyEventBuilder WithId(Guid id)
    {
        _event.Id = id;
        return this;
    }
    
    public MyEventBuilder WithData(string data)
    {
        _event.Data = data;
        return this;
    }
    
    public MyEventBuilder WithPriority(string priority)
    {
        _event.Priority = priority;
        return this;
    }
    
    public MyEventBuilder WithTimestamp(DateTime timestamp)
    {
        _event.Timestamp = timestamp;
        return this;
    }
    
    public MyEvent Build()
    {
        return new MyEvent
        {
            Id = _event.Id != Guid.Empty ? _event.Id : Guid.NewGuid(),
            Data = _event.Data ?? "default-data",
            Priority = _event.Priority ?? "normal",
            Timestamp = _event.Timestamp != default ? _event.Timestamp : DateTime.UtcNow
        };
    }
}
```

### Using Builders in Tests

```csharp
[Theory]
[InlineData("high", 100)]
[InlineData("medium", 50)]
[InlineData("low", 10)]
public async Task Should_ApplyPriorityRules_When_EventProcessed(string priority, int expectedScore)
{
    // Arrange
    var agentId = Guid.NewGuid();
    var agent = await _agentFactory.GetGAgentAsync<IMyGAgent>(agentId);
    
    var testEvent = new MyEventBuilder()
        .WithData("test-data")
        .WithPriority(priority)
        .Build();
    
    _outputHelper.WriteLine($"Testing priority: {priority}, expected score: {expectedScore}");
    
    // Act
    var result = await agent.ProcessWithPriorityAsync(testEvent);
    
    // Assert
    result.ShouldNotBeNull();
    result.PriorityScore.ShouldBe(expectedScore);
    result.Priority.ShouldBe(priority);
    
    var state = await agent.GetStateAsync();
    state.PriorityScores[priority].ShouldBe(expectedScore);
    
    _outputHelper.WriteLine($"Priority rule applied correctly. Score: {result.PriorityScore}");
}
```

## Complex Test Data Setup

### Setting Up Multi-Agent Scenarios

```csharp
[Fact]
public async Task Should_CoordinateAgents_When_WorkflowTriggered()
{
    // Arrange - Create multiple agents
    var orchestratorId = Guid.NewGuid();
    var worker1Id = Guid.NewGuid();
    var worker2Id = Guid.NewGuid();
    
    var orchestrator = await _agentFactory.GetGAgentAsync<IOrchestratorGAgent>(orchestratorId);
    var worker1 = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(worker1Id);
    var worker2 = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(worker2Id);
    
    // Configure workers
    await worker1.SetCapabilityAsync("data-processing");
    await worker2.SetCapabilityAsync("analysis");
    
    // Configure orchestrator with worker references
    await orchestrator.RegisterWorkerAsync(worker1Id, "data-processing");
    await orchestrator.RegisterWorkerAsync(worker2Id, "analysis");
    
    var workflowData = new WorkflowData
    {
        Id = Guid.NewGuid(),
        Type = "data-analysis",
        Payload = "sample-data"
    };
    
    _outputHelper.WriteLine($"Starting workflow: {workflowData.Id}");
    
    // Act
    var result = await orchestrator.ExecuteWorkflowAsync(workflowData);
    
    // Assert
    result.ShouldNotBeNull();
    result.Status.ShouldBe("Completed");
    result.Steps.Count.ShouldBe(2);
    
    // Verify worker states
    var worker1State = await worker1.GetStateAsync();
    var worker2State = await worker2.GetStateAsync();
    
    worker1State.TasksCompleted.ShouldBe(1);
    worker2State.TasksCompleted.ShouldBe(1);
    
    _outputHelper.WriteLine($"Workflow completed successfully. Steps: {result.Steps.Count}");
}
```

### Test Data with Dependencies

```csharp
public class ComplexTestDataBuilder
{
    private readonly IGAgentFactory _agentFactory;
    private Dictionary<Guid, object> _agents = new();
    
    public ComplexTestDataBuilder(IGAgentFactory agentFactory)
    {
        _agentFactory = agentFactory;
    }
    
    public ComplexTestDataBuilder AddAgent<T>(Guid id, Action<T> configure = null)
        where T : class, IGrainWithGuidKey
    {
        var agent = _agentFactory.GetGAgentAsync<T>(id).GetAwaiter().GetResult();
        configure?.Invoke(agent);
        _agents[id] = agent;
        return this;
    }
    
    public ComplexTestDataBuilder WithAgentRelationship(Guid fromId, Guid toId, string relationshipType)
    {
        // Configure relationships between agents
        var fromAgent = _agents[fromId] as IRelationshipGAgent;
        fromAgent?.AddRelationshipAsync(toId, relationshipType).GetAwaiter().GetResult();
        return this;
    }
    
    public ComplexTestDataBuilder WithInitialData(Guid agentId, object data)
    {
        var agent = _agents[agentId] as IDataInitializable;
        agent?.InitializeAsync(data).GetAwaiter().GetResult();
        return this;
    }
    
    public async Task<Dictionary<Guid, object>> BuildAsync()
    {
        // Allow agents to settle
        await Task.Delay(100);
        return _agents;
    }
}
```

### Using Complex Test Data Builder

```csharp
[Fact]
public async Task Should_HandleComplexWorkflow_When_MultipleAgentsCooperate()
{
    // Arrange
    var orchestratorId = Guid.NewGuid();
    var processorId = Guid.NewGuid();
    var analyzerId = Guid.NewGuid();
    var storageId = Guid.NewGuid();
    
    var testDataBuilder = new ComplexTestDataBuilder(_agentFactory)
        .AddAgent<IOrchestratorGAgent>(orchestratorId, o => o.SetTimeout(TimeSpan.FromSeconds(30)))
        .AddAgent<IProcessorGAgent>(processorId, p => p.SetMaxRetries(3))
        .AddAgent<IAnalyzerGAgent>(analyzerId, a => a.SetAnalysisDepth("deep"))
        .AddAgent<IStorageGAgent>(storageId, s => s.SetStorageType("persistent"))
        .WithAgentRelationship(orchestratorId, processorId, "processes")
        .WithAgentRelationship(orchestratorId, analyzerId, "analyzes")
        .WithAgentRelationship(orchestratorId, storageId, "stores")
        .WithInitialData(processorId, new ProcessorConfig { BatchSize = 10 })
        .WithInitialData(analyzerId, new AnalyzerConfig { Model = "advanced" });
    
    var agents = await testDataBuilder.BuildAsync();
    var orchestrator = agents[orchestratorId] as IOrchestratorGAgent;
    
    var complexTask = new ComplexTask
    {
        Id = Guid.NewGuid(),
        Data = "complex-sample-data",
        Requirements = new[] { "process", "analyze", "store" }
    };
    
    _outputHelper.WriteLine($"Executing complex task: {complexTask.Id}");
    
    // Act
    var result = await orchestrator.ExecuteComplexTaskAsync(complexTask);
    
    // Assert
    result.ShouldNotBeNull();
    result.Status.ShouldBe("Completed");
    result.Phases.Count.ShouldBe(3);
    
    // Verify all agents participated
    foreach (var agentEntry in agents)
    {
        if (agentEntry.Value is ITaskParticipant participant)
        {
            var participation = await participant.GetParticipationStatusAsync(complexTask.Id);
            participation.HasParticipated.ShouldBeTrue();
            participation.TasksCompleted.ShouldBeGreaterThan(0);
        }
    }
    
    _outputHelper.WriteLine($"Complex task completed. Phases: {result.Phases.Count}");
}
```

## Error Handling Testing Patterns

### Testing Expected Exceptions

```csharp
[Fact]
public async Task Should_ThrowException_When_InvalidInputProvided()
{
    // Arrange
    var agentId = Guid.NewGuid();
    var agent = await _agentFactory.GetGAgentAsync<IMyGAgent>(agentId);
    var invalidRequest = new MyRequest { Data = null };
    
    // Act & Assert
    var exception = await Should.ThrowAsync<ArgumentException>(
        () => agent.ProcessRequestAsync(invalidRequest)
    );
    
    exception.Message.ShouldContain("Data cannot be null");
    exception.ParamName.ShouldBe("request");
}
```

### Testing Timeout Scenarios

```csharp
[Fact]
public async Task Should_Timeout_When_OperationTakesTooLong()
{
    // Arrange
    var agentId = Guid.NewGuid();
    var agent = await _agentFactory.GetGAgentAsync<IMyGAgent>(agentId);
    
    // Configure agent to simulate long operation
    await agent.SetDelayAsync(TimeSpan.FromSeconds(5));
    
    // Act & Assert
    var exception = await Should.ThrowAsync<TaskCanceledException>(
        () => agent.ProcessWithTimeoutAsync(TimeSpan.FromSeconds(2))
    );
    
    // Verify state is still consistent
    var state = await agent.GetStateAsync();
    state.TimedOutOperations.ShouldBe(1);
}
```

### Testing Recovery from Errors

```csharp
[Fact]
public async Task Should_RecoverFromError_When_RetryPolicyApplied()
{
    // Arrange
    var agentId = Guid.NewGuid();
    var agent = await _agentFactory.GetGAgentAsync<IMyGAgent>(agentId);
    
    // Configure agent to fail first time, succeed second time
    await agent.SetFailCountAsync(1);
    
    var testData = "test-data";
    
    _outputHelper.WriteLine("Testing retry policy with 1 expected failure");
    
    // Act
    var result = await agent.ProcessWithRetryAsync(testData, maxRetries: 3);
    
    // Assert
    result.ShouldNotBeNull();
    result.Success.ShouldBeTrue();
    result.Attempts.ShouldBe(2);
    
    var state = await agent.GetStateAsync();
    state.SuccessfulRetries.ShouldBe(1);
    
    _outputHelper.WriteLine($"Operation succeeded after {result.Attempts} attempts");
}
```

## Performance Testing Patterns

### Testing Response Times

```csharp
[Fact]
public async Task Should_RespondWithinExpectedTime_When_LoadIsNormal()
{
    // Arrange
    var agentId = Guid.NewGuid();
    var agent = await _agentFactory.GetGAgentAsync<IMyGAgent>(agentId);
    var expectedMaxTime = TimeSpan.FromMilliseconds(100);
    
    var testData = Enumerable.Range(1, 10)
        .Select(i => $"item-{i}")
        .ToList();
    
    _outputHelper.WriteLine($"Testing response time with {testData.Count} items");
    
    // Act
    var stopwatch = Stopwatch.StartNew();
    var results = await agent.ProcessBatchAsync(testData);
    stopwatch.Stop();
    
    // Assert
    results.ShouldNotBeNull();
    results.Count.ShouldBe(testData.Count);
    
    var responseTime = stopwatch.Elapsed;
    responseTime.ShouldBeLessThan(expectedMaxTime);
    
    var state = await agent.GetStateAsync();
    state.AverageResponseTime.ShouldBeLessThan(expectedMaxTime.TotalMilliseconds);
    
    _outputHelper.WriteLine($"Response time: {responseTime.TotalMilliseconds}ms (max: {expectedMaxTime.TotalMilliseconds}ms)");
}
```

### Testing Memory Usage

```csharp
[Fact]
public async Task Should_ManageMemoryEfficiently_When_ProcessingLargeData()
{
    // Arrange
    var agentId = Guid.NewGuid();
    var agent = await _agentFactory.GetGAgentAsync<IMyGAgent>(agentId);
    
    var largeData = Enumerable.Range(1, 1000)
        .Select(i => new LargeDataItem 
        { 
            Id = i, 
            Data = new string('x', 1000) 
        })
        .ToList();
    
    var initialMemory = GC.GetTotalMemory(true);
    
    _outputHelper.WriteLine($"Processing {largeData.Count} large items");
    
    // Act
    var results = await agent.ProcessLargeDataAsync(largeData);
    
    // Force garbage collection
    GC.Collect();
    GC.WaitForPendingFinalizers();
    var finalMemory = GC.GetTotalMemory(true);
    
    // Assert
    results.ShouldNotBeNull();
    results.Count.ShouldBe(largeData.Count);
    
    var memoryIncrease = finalMemory - initialMemory;
    memoryIncrease.ShouldBeLessThan(10 * 1024 * 1024); // Less than 10MB
    
    _outputHelper.WriteLine($"Memory usage: {memoryIncrease / 1024 / 1024:F2}MB");
}
```

## Best Practices

### 1. Test Organization
- **Single Responsibility**: Each test should verify one specific behavior
- **Descriptive Names**: Use clear, descriptive test method names
- **Consistent Structure**: Follow Arrange-Act-Assert pattern consistently

### 2. Test Data Management
- **Factory Methods**: Use factory methods for creating test data
- **Builder Pattern**: Use builders for complex object creation
- **Realistic Data**: Use realistic but simple test data

### 3. Error Handling
- **Test Exceptions**: Test both expected and unexpected exceptions
- **Timeout Handling**: Test timeout scenarios appropriately
- **Recovery Testing**: Test error recovery mechanisms

### 4. Performance Considerations
- **Response Time**: Test response times under various loads
- **Memory Usage**: Monitor memory usage for large operations
- **Resource Cleanup**: Ensure proper resource cleanup

### 5. Logging and Debugging
- **Detailed Logging**: Log important test steps and results
- **State Verification**: Log state changes for debugging
- **Performance Metrics**: Log performance metrics for analysis

## Common Anti-Patterns

### 1. Multiple Assertions in Single Test
```csharp
// Bad
[Fact]
public async Task BadTest()
{
    var agent = await _agentFactory.GetGAgentAsync<IMyGAgent>(Guid.NewGuid());
    var result1 = await agent.Method1Async();
    var result2 = await agent.Method2Async();
    result1.ShouldNotBeNull();
    result2.ShouldNotBeNull();
}

// Good
[Fact]
public async Task Should_Method1ReturnResult()
{
    var agent = await _agentFactory.GetGAgentAsync<IMyGAgent>(Guid.NewGuid());
    var result = await agent.Method1Async();
    result.ShouldNotBeNull();
}

[Fact]
public async Task Should_Method2ReturnResult()
{
    var agent = await _agentFactory.GetGAgentAsync<IMyGAgent>(Guid.NewGuid());
    var result = await agent.Method2Async();
    result.ShouldNotBeNull();
}
```

### 2. Hardcoded Test Data
```csharp
// Bad
[Fact]
public async Task BadTest()
{
    var agent = await _agentFactory.GetGAgentAsync<IMyGAgent>(Guid.NewGuid());
    await agent.ProcessAsync("hardcoded-data");
}

// Good
[Fact]
public async Task Should_ProcessData()
{
    var agent = await _agentFactory.GetGAgentAsync<IMyGAgent>(Guid.NewGuid());
    var testData = TestDataFactory.CreateValidData();
    await agent.ProcessAsync(testData);
}
```

### 3. Missing Cleanup
```csharp
// Bad
[Fact]
public async Task BadTest()
{
    var agent = await _agentFactory.GetGAgentAsync<IMyGAgent>(Guid.NewGuid());
    // No cleanup, resources may leak
}

// Good - Use base class cleanup
[Fact]
public async Task Should_CleanUpResources()
{
    var agent = await _agentFactory.GetGAgentAsync<IMyGAgent>(Guid.NewGuid());
    // Base class handles cleanup automatically
}
```

## Next Steps

After mastering basic testing patterns, proceed to:

- **Part 3: Common Test Scenarios** - Learn specific testing scenarios for GAgents
- **Part 4: Integration Testing Patterns** - Master advanced integration testing techniques
- **Part 5: AI GAgent Testing Patterns** - Explore AI-specific testing patterns

## References

- [xUnit Theory Documentation](https://xunit.net/docs/shared-context)
- [Shouldly Assertion Patterns](https://shouldly.readthedocs.io/en/latest/assertions/)
- [Test-Driven Development](https://martinfowler.com/bliki/TestDrivenDevelopment.html)
- [Arrange-Act-Assert Pattern](https://wiki.c2.com/?ArrangeActAssert)