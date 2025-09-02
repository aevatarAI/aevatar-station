# GAgent Unit Testing Guide - Part 4: Integration Testing Patterns

## Overview

This guide covers integration testing patterns for GAgents, focusing on testing how multiple agents work together, end-to-end workflows, system health monitoring, and performance under realistic conditions. Integration testing ensures that components interact correctly and the system behaves as expected in production-like environments.

## End-to-End Workflow Testing

### Multi-Agent Workflow Testing

```csharp
[Fact]
public async Task Should_ExecuteCompleteWorkflow_When_MultipleAgentsCooperate()
{
    // Arrange - Create all agents involved in the workflow
    var orchestratorId = Guid.NewGuid();
    var processorId = Guid.NewGuid();
    var validatorId = Guid.NewGuid();
    var storageId = Guid.NewGuid();
    
    var orchestrator = await _agentFactory.GetGAgentAsync<IWorkflowOrchestratorGAgent>(orchestratorId);
    var processor = await _agentFactory.GetGAgentAsync<IDataProcessorGAgent>(processorId);
    var validator = await _agentFactory.GetGAgentAsync<IValidatorGAgent>(validatorId);
    var storage = await _agentFactory.GetGAgentAsync<IStorageGAgent>(storageId);
    
    // Configure agent relationships and capabilities
    await orchestrator.RegisterAgentAsync(processorId, "data-processing");
    await orchestrator.RegisterAgentAsync(validatorId, "validation");
    await orchestrator.RegisterAgentAsync(storageId, "storage");
    
    await processor.SetConfigurationAsync(new ProcessorConfig
    {
        BatchSize = 10,
        Timeout = TimeSpan.FromSeconds(30),
        RetryCount = 3
    });
    
    await validator.SetRulesAsync(new ValidationRules
    {
        RequiredFields = new[] { "Id", "Name", "Type" },
        ValidationLevel = "Strict"
    });
    
    var workflowRequest = new WorkflowRequest
    {
        WorkflowId = Guid.NewGuid(),
        WorkflowType = "DataProcessing",
        Data = new WorkflowData
        {
            Source = "test-data-source",
            Items = Enumerable.Range(1, 50)
                .Select(i => new DataItem 
                { 
                    Id = i, 
                    Name = $"Item-{i}", 
                    Type = i % 2 == 0 ? "TypeA" : "TypeB",
                    Value = $"Value-{i}"
                })
                .ToList()
        },
        Steps = new[]
        {
            "validation",
            "processing", 
            "storage"
        }
    };
    
    _outputHelper.WriteLine($"Starting end-to-end workflow: {workflowRequest.WorkflowId}");
    
    // Act
    var stopwatch = Stopwatch.StartNew();
    var result = await orchestrator.ExecuteWorkflowAsync(workflowRequest);
    stopwatch.Stop();
    
    // Assert
    result.ShouldNotBeNull();
    result.Success.ShouldBeTrue();
    result.WorkflowId.ShouldBe(workflowRequest.WorkflowId);
    result.CompletedSteps.Count.ShouldBe(3);
    result.FailedSteps.ShouldBeEmpty();
    
    // Verify each agent participated correctly
    var processorState = await processor.GetStateAsync();
    processorState.ItemsProcessed.ShouldBe(50);
    processorState.BatchesProcessed.ShouldBe(5); // 50 items / 10 batch size
    
    var validatorState = await validator.GetStateAsync();
    validatorState.ItemsValidated.ShouldBe(50);
    validatorState.ValidationErrors.ShouldBeEmpty();
    
    var storageState = await storage.GetStateAsync();
    storageState.ItemsStored.ShouldBe(50);
    storageState.StorageOperations.ShouldBe(1);
    
    // Verify workflow metrics
    result.TotalDuration.ShouldBeLessThan(TimeSpan.FromSeconds(60));
    result.AverageStepDuration.ShouldBeLessThan(TimeSpan.FromSeconds(20));
    
    _outputHelper.WriteLine($"Workflow completed in {stopwatch.Elapsed.TotalSeconds:F2}s");
    _outputHelper.WriteLine($"Results: {result.CompletedSteps.Count} steps completed successfully");
}
```

### Complex Event Flow Testing

```csharp
[Fact]
public async Task Should_HandleComplexEventFlow_When_CascadingEventsTriggered()
{
    // Arrange
    var eventSourceId = Guid.NewGuid();
    var processor1Id = Guid.NewGuid();
    var processor2Id = Guid.NewGuid();
    var aggregatorId = Guid.NewGuid();
    
    var eventSource = await _agentFactory.GetGAgentAsync<IEventSourceGAgent>(eventSourceId);
    var processor1 = await _agentFactory.GetGAgentAsync<IEventProcessorGAgent>(processor1Id);
    var processor2 = await _agentFactory.GetGAgentAsync<IEventProcessorGAgent>(processor2Id);
    var aggregator = await _agentFactory.GetGAgentAsync<IEventAggregatorGAgent>(aggregatorId);
    
    // Set up event subscriptions
    await processor1.SubscribeToEventAsync("source-event");
    await processor2.SubscribeToEventAsync("processor1-event");
    await aggregator.SubscribeToEventAsync("processor1-event");
    await aggregator.SubscribeToEventAsync("processor2-event");
    
    // Configure processing rules
    await processor1.SetProcessingRulesAsync(new ProcessingRules
    {
        Filter = e => e.Priority == "High",
        Transform = e => new Event { Data = $"Processed1: {e.Data}", Priority = "Medium" }
    });
    
    await processor2.SetProcessingRulesAsync(new ProcessingRules
    {
        Filter = e => e.Priority == "Medium",
        Transform = e => new Event { Data = $"Processed2: {e.Data}", Priority = "Low" }
    });
    
    var initialEvent = new Event
    {
        Id = Guid.NewGuid(),
        Data = "initial-test-data",
        Priority = "High",
        Source = "test-source"
    };
    
    _outputHelper.WriteLine($"Starting complex event flow: {initialEvent.Id}");
    
    // Act
    var results = new List<EventProcessingResult>();
    
    // Track events as they flow through the system
    var eventTracker = new EventTracker();
    eventSource.OnEventPublished += e => eventTracker.Track("Published", e);
    processor1.OnEventProcessed += e => eventTracker.Track("Processor1", e);
    processor2.OnEventProcessed += e => eventTracker.Track("Processor2", e);
    aggregator.OnEventAggregated += e => eventTracker.Track("Aggregator", e);
    
    // Start the event flow
    await eventSource.PublishEventAsync(initialEvent);
    
    // Wait for event processing to complete
    await Task.Delay(TimeSpan.FromSeconds(5));
    
    // Assert
    var trackerResults = eventTracker.GetResults();
    
    // Verify event flow sequence
    trackerResults.ShouldContainKey("Published");
    trackerResults.ShouldContainKey("Processor1");
    trackerResults.ShouldContainKey("Processor2");
    trackerResults.ShouldContainKey("Aggregator");
    
    // Verify event transformation
    var aggregatorState = await aggregator.GetStateAsync();
    aggregatorState.ProcessedEvents.Count.ShouldBeGreaterThan(0);
    
    var highPriorityEvents = aggregatorState.ProcessedEvents
        .Where(e => e.OriginalPriority == "High")
        .ToList();
    highPriorityEvents.Count.ShouldBe(1);
    
    // Verify no events were lost
    var totalProcessed = aggregatorState.ProcessedEvents.Count;
    totalProcessed.ShouldBeGreaterThanOrEqualTo(2); // Should have at least 2 processed events
    
    _outputHelper.WriteLine($"Event flow completed. Total events processed: {totalProcessed}");
}
```

## Multi-Agent Coordination Testing

### Leader Election Testing

```csharp
[Fact]
public async Task Should_ElectLeader_When_MultipleAgentsCompete()
{
    // Arrange
    var agentCount = 5;
    var agentIds = Enumerable.Range(0, agentCount)
        .Select(_ => Guid.NewGuid())
        .ToList();
    
    var agents = new List<ICoordinatorGAgent>();
    foreach (var agentId in agentIds)
    {
        var agent = await _agentFactory.GetGAgentAsync<ICoordinatorGAgent>(agentId);
        agents.Add(agent);
    }
    
    // Create coordination group
    var groupId = Guid.NewGuid();
    foreach (var agent in agents)
    {
        await agent.JoinGroupAsync(groupId);
    }
    
    _outputHelper.WriteLine($"Starting leader election with {agentCount} agents");
    
    // Act
    var electionTasks = agents.Select(a => a.StartElectionAsync(groupId));
    await Task.WhenAll(electionTasks);
    
    // Wait for election to complete
    await Task.Delay(TimeSpan.FromSeconds(2));
    
    // Assert
    var leaderIds = new List<Guid>();
    foreach (var agent in agents)
    {
        var state = await agent.GetStateAsync();
        if (state.IsLeader)
        {
            leaderIds.Add(agent.GetPrimaryKey());
        }
    }
    
    // Should have exactly one leader
    leaderIds.Count.ShouldBe(1);
    var electedLeader = leaderIds.First();
    
    // Verify all agents recognize the same leader
    foreach (var agent in agents)
    {
        var state = await agent.GetStateAsync();
        state.CurrentLeaderId.ShouldBe(electedLeader);
        state.GroupId.ShouldBe(groupId);
        state.GroupSize.ShouldBe(agentCount);
    }
    
    // Verify leader can perform leader-specific actions
    var leaderAgent = agents.First(a => a.GetPrimaryKey() == electedLeader);
    var leaderActionResult = await leaderAgent.PerformLeaderActionAsync("test-action");
    leaderActionResult.Success.ShouldBeTrue();
    
    _outputHelper.WriteLine($"Leader election completed. Leader: {electedLeader}");
}
```

### Distributed Task Distribution Testing

```csharp
[Fact]
public async Task Should_DistributeTasks_When_WorkersAvailable()
{
    // Arrange
    var coordinatorId = Guid.NewGuid();
    var workerIds = Enumerable.Range(0, 3)
        .Select(_ => Guid.NewGuid())
        .ToList();
    
    var coordinator = await _agentFactory.GetGAgentAsync<ITaskCoordinatorGAgent>(coordinatorId);
    var workers = new List<IWorkerGAgent>();
    
    // Create and register workers
    foreach (var workerId in workerIds)
    {
        var worker = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(workerId);
        await worker.SetCapabilitiesAsync(new[] { "data-processing", "analysis", "storage" });
        await worker.SetCapacityAsync(5); // Each worker can handle 5 tasks
        workers.Add(worker);
        
        await coordinator.RegisterWorkerAsync(workerId);
    }
    
    // Create batch of tasks
    var tasks = Enumerable.Range(1, 15)
        .Select(i => new TaskItem
        {
            Id = Guid.NewGuid(),
            Type = i % 3 == 0 ? "data-processing" : i % 3 == 1 ? "analysis" : "storage",
            Data = $"task-data-{i}",
            Priority = i % 5 == 0 ? "High" : "Normal",
            EstimatedDuration = TimeSpan.FromSeconds(i % 5 + 1)
        })
        .ToList();
    
    _outputHelper.WriteLine($"Distributing {tasks.Count} tasks among {workers.Count} workers");
    
    // Act
    var distributionResult = await coordinator.DistributeTasksAsync(tasks);
    
    // Wait for task completion
    await Task.Delay(TimeSpan.FromSeconds(10));
    
    // Assert
    distributionResult.ShouldNotBeNull();
    distributionResult.Success.ShouldBeTrue();
    distributionResult.TasksDistributed.ShouldBe(tasks.Count);
    
    // Verify load balancing
    var workerStates = new List<WorkerState>();
    foreach (var worker in workers)
    {
        var state = await worker.GetStateAsync();
        workerStates.Add(state);
    }
    
    var totalTasksProcessed = workerStates.Sum(s => s.TasksCompleted);
    totalTasksProcessed.ShouldBe(tasks.Count);
    
    // Verify relatively balanced distribution
    var minTasks = workerStates.Min(s => s.TasksCompleted);
    var maxTasks = workerStates.Max(s => s.TasksCompleted);
    var difference = maxTasks - minTasks;
    difference.ShouldBeLessThanOrEqualTo(2); // Should be relatively balanced
    
    // Verify coordinator state
    var coordinatorState = await coordinator.GetStateAsync();
    coordinatorState.TotalTasks.ShouldBe(tasks.Count);
    coordinatorState.CompletedTasks.ShouldBe(tasks.Count);
    coordinatorState.FailedTasks.ShouldBe(0);
    
    _outputHelper.WriteLine($"Task distribution completed. Load balance: {minTasks}-{maxTasks} tasks per worker");
}
```

## System Health and Monitoring Testing

### Health Check Testing

```csharp
[Fact]
public async Task Should_ReportSystemHealth_When_AllComponentsHealthy()
{
    // Arrange
    var healthMonitorId = Guid.NewGuid();
    var componentIds = Enumerable.Range(0, 5)
        .Select(_ => Guid.NewGuid())
        .ToList();
    
    var healthMonitor = await _agentFactory.GetGAgentAsync<IHealthMonitorGAgent>(healthMonitorId);
    var components = new List<IHealthComponentGAgent>();
    
    // Create and register health components
    foreach (var componentId in componentIds)
    {
        var component = await _agentFactory.GetGAgentAsync<IHealthComponentGAgent>(componentId);
        await component.SetHealthStatusAsync(HealthStatus.Healthy);
        await healthMonitor.RegisterComponentAsync(componentId, $"Component-{componentId.ToString().Substring(0, 8)}");
        components.Add(component);
    }
    
    _outputHelper.WriteLine($"Setting up health monitoring for {components.Count} components");
    
    // Act
    var initialHealth = await healthMonitor.GetSystemHealthAsync();
    
    // Simulate some components becoming unhealthy
    await components[1].SetHealthStatusAsync(HealthStatus.Warning);
    await components[3].SetHealthStatusAsync(HealthStatus.Unhealthy);
    
    await Task.Delay(TimeSpan.FromSeconds(1)); // Allow health monitor to update
    
    var updatedHealth = await healthMonitor.GetSystemHealthAsync();
    
    // Recover components
    await components[1].SetHealthStatusAsync(HealthStatus.Healthy);
    await components[3].SetHealthStatusAsync(HealthStatus.Healthy);
    
    await Task.Delay(TimeSpan.FromSeconds(1)); // Allow health monitor to update
    
    var finalHealth = await healthMonitor.GetSystemHealthAsync();
    
    // Assert
    initialHealth.ShouldNotBeNull();
    initialHealth.OverallStatus.ShouldBe(HealthStatus.Healthy);
    initialHealth.ComponentHealth.Count.ShouldBe(components.Count);
    initialHealth.ComponentHealth.Values.All(c => c.Status == HealthStatus.Healthy).ShouldBeTrue();
    
    updatedHealth.ShouldNotBeNull();
    updatedHealth.OverallStatus.ShouldBe(HealthStatus.Warning);
    updatedHealth.ComponentHealth[components[1].GetPrimaryKey()].Status.ShouldBe(HealthStatus.Warning);
    updatedHealth.ComponentHealth[components[3].GetPrimaryKey()].Status.ShouldBe(HealthStatus.Unhealthy);
    
    finalHealth.ShouldNotBeNull();
    finalHealth.OverallStatus.ShouldBe(HealthStatus.Healthy);
    finalHealth.ComponentHealth.Values.All(c => c.Status == HealthStatus.Healthy).ShouldBeTrue();
    
    _outputHelper.WriteLine($"Health monitoring completed. Final status: {finalHealth.OverallStatus}");
}
```

### Performance Monitoring Testing

```csharp
[Fact]
public async Task Should_MonitorPerformance_When_SystemUnderLoad()
{
    // Arrange
    var performanceMonitorId = Guid.NewGuid();
    var targetAgentId = Guid.NewGuid();
    
    var performanceMonitor = await _agentFactory.GetGAgentAsync<IPerformanceMonitorGAgent>(performanceMonitorId);
    var targetAgent = await _agentFactory.GetGAgentAsync<IPerformanceTargetGAgent>(targetAgentId);
    
    await performanceMonitor.MonitorAgentAsync(targetAgentId);
    
    var loadTest = new LoadTestScenario
    {
        Duration = TimeSpan.FromSeconds(10),
        RequestsPerSecond = 20,
        RequestType = "CPU-Intensive"
    };
    
    _outputHelper.WriteLine($"Starting performance monitoring under load");
    
    // Act
    var monitoringTask = performanceMonitor.StartMonitoringAsync();
    var loadTask = targetAgent.ExecuteLoadTestAsync(loadTest);
    
    await Task.WhenAll(monitoringTask, loadTask);
    
    var performanceReport = await performanceMonitor.GetPerformanceReportAsync();
    
    // Assert
    performanceReport.ShouldNotBeNull();
    performanceReport.MonitoringDuration.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromSeconds(9));
    performanceReport.TotalRequests.ShouldBeGreaterThanOrEqualTo(180); // ~20 requests/sec * 10 seconds
    
    // Verify performance metrics
    performanceReport.AverageResponseTime.ShouldBeGreaterThan(TimeSpan.Zero);
    performanceReport.MaxResponseTime.ShouldBeGreaterThanOrEqualTo(performanceReport.AverageResponseTime);
    performanceReport.MinResponseTime.ShouldBeLessThanOrEqualTo(performanceReport.AverageResponseTime);
    
    // Verify resource usage
    performanceReport.AverageCPUUsage.ShouldBeGreaterThan(0);
    performanceReport.AverageMemoryUsage.ShouldBeGreaterThan(0);
    performanceReport.PeakMemoryUsage.ShouldBeGreaterThanOrEqualTo(performanceReport.AverageMemoryUsage);
    
    // Verify error rate is acceptable
    performanceReport.ErrorRate.ShouldBeLessThan(0.05); // Less than 5% error rate
    
    // Verify performance thresholds
    performanceReport.ThresholdExceededCount.ShouldBe(0);
    
    _outputHelper.WriteLine($"Performance monitoring completed");
    _outputHelper.WriteLine($"Average response time: {performanceReport.AverageResponseTime.TotalMilliseconds:F2}ms");
    _outputHelper.WriteLine($"Error rate: {performanceReport.ErrorRate:P2}");
}
```

## Event-Driven Integration Testing

### Cross-Agent Event Communication

```csharp
[Fact]
public async Task Should_HandleCrossAgentCommunication_When_EventsPublished()
{
    // Arrange
    var publisherId = Guid.NewGuid();
    var subscriber1Id = Guid.NewGuid();
    var subscriber2Id = Guid.NewGuid();
    var subscriber3Id = Guid.NewGuid();
    
    var publisher = await _agentFactory.GetGAgentAsync<IEventPublisherGAgent>(publisherId);
    var subscriber1 = await _agentFactory.GetGAgentAsync<IEventSubscriberGAgent>(subscriber1Id);
    var subscriber2 = await _agentFactory.GetGAgentAsync<IEventSubscriberGAgent>(subscriber2Id);
    var subscriber3 = await _agentFactory.GetGAgentAsync<IEventSubscriberGAgent>(subscriber3Id);
    
    // Set up event subscriptions
    await subscriber1.SubscribeToEventTypeAsync("user-action");
    await subscriber2.SubscribeToEventTypeAsync("system-event");
    await subscriber3.SubscribeToEventTypeAsync("user-action");
    await subscriber3.SubscribeToEventTypeAsync("system-event"); // Subscribe to multiple types
    
    // Configure event handlers
    await subscriber1.SetEventHandlerAsync(async e => 
    {
        await subscriber1.ProcessEventAsync(e);
        return true;
    });
    
    await subscriber2.SetEventHandlerAsync(async e => 
    {
        await subscriber2.ProcessEventAsync(e);
        return true;
    });
    
    await subscriber3.SetEventHandlerAsync(async e => 
    {
        await subscriber3.ProcessEventAsync(e);
        return true;
    });
    
    var testEvents = new[]
    {
        new Event { Type = "user-action", Data = "user-clicked-button", Priority = "Normal" },
        new Event { Type = "system-event", Data = "system-backup-completed", Priority = "Low" },
        new Event { Type = "user-action", Data = "user-submitted-form", Priority = "High" }
    };
    
    _outputHelper.WriteLine($"Testing cross-agent event communication with {testEvents.Length} events");
    
    // Act
    var publishTasks = testEvents.Select(e => publisher.PublishEventAsync(e));
    await Task.WhenAll(publishTasks);
    
    // Wait for event processing
    await Task.Delay(TimeSpan.FromSeconds(3));
    
    // Assert
    var subscriber1State = await subscriber1.GetStateAsync();
    var subscriber2State = await subscriber2.GetStateAsync();
    var subscriber3State = await subscriber3.GetStateAsync();
    
    // Verify subscriber1 received user-action events
    subscriber1State.ReceivedEvents.Count.ShouldBe(2);
    subscriber1State.ReceivedEvents.ShouldContain(e => e.Data.Contains("user-clicked-button"));
    subscriber1State.ReceivedEvents.ShouldContain(e => e.Data.Contains("user-submitted-form"));
    
    // Verify subscriber2 received system-event
    subscriber2State.ReceivedEvents.Count.ShouldBe(1);
    subscriber2State.ReceivedEvents[0].Data.ShouldBe("system-backup-completed");
    
    // Verify subscriber3 received all events (subscribed to both types)
    subscriber3State.ReceivedEvents.Count.ShouldBe(3);
    subscriber3State.ReceivedEvents.ShouldContain(e => e.Data.Contains("user-clicked-button"));
    subscriber3State.ReceivedEvents.ShouldContain(e => e.Data.Contains("system-backup-completed"));
    subscriber3State.ReceivedEvents.ShouldContain(e => e.Data.Contains("user-submitted-form"));
    
    // Verify no events were lost
    var totalReceived = subscriber1State.ReceivedEvents.Count + 
                       subscriber2State.ReceivedEvents.Count + 
                       subscriber3State.ReceivedEvents.Count;
    totalReceived.ShouldBe(6); // 2 user-action * 2 subscribers + 1 system-event * 2 subscribers
    
    _outputHelper.WriteLine($"Cross-agent communication completed. Total events received: {totalReceived}");
}
```

### Event Sourcing Integration Testing

```csharp
[Fact]
public async Task Should_MaintainEventSourcingConsistency_When_MultipleAgentsUpdateState()
{
    // Arrange
    var aggregateRootId = Guid.NewGuid();
    var updater1Id = Guid.NewGuid();
    var updater2Id = Guid.NewGuid();
    var readerId = Guid.NewGuid();
    
    var aggregateRoot = await _agentFactory.GetGAgentAsync<IAggregateRootGAgent>(aggregateRootId);
    var updater1 = await _agentFactory.GetGAgentAsync<IStateUpdaterGAgent>(updater1Id);
    var updater2 = await _agentFactory.GetGAgentAsync<IStateUpdaterGAgent>(updater2Id);
    var reader = await _agentFactory.GetGAgentAsync<IStateReaderGAgent>(readerId);
    
    // Initialize aggregate root
    await aggregateRoot.InitializeAsync(new AggregateRootConfig
    {
        Id = aggregateRootId,
        Type = "TestAggregate",
        InitialState = new Dictionary<string, object>
        {
            ["Counter"] = 0,
            ["LastUpdated"] = DateTime.UtcNow,
            ["Version"] = 1
        }
    });
    
    // Set up event sourcing relationships
    await updater1.SetAggregateRootAsync(aggregateRootId);
    await updater2.SetAggregateRootAsync(aggregateRootId);
    await reader.SetAggregateRootAsync(aggregateRootId);
    
    var updateOperations = new[]
    {
        new UpdateOperation { Type = "Increment", Value = 5, Reason = "Initial increment" },
        new UpdateOperation { Type = "Multiply", Value = 2, Reason = "Doubling" },
        new UpdateOperation { Type = "Increment", Value = 3, Reason = "Additional increment" },
        new UpdateOperation { Type = "Divide", Value = 2, Reason = "Halving" }
    };
    
    _outputHelper.WriteLine($"Testing event sourcing with {updateOperations.Length} operations");
    
    // Act
    var updateTasks = new List<Task<UpdateResult>>();
    
    // Execute updates from multiple agents
    updateTasks.Add(updater1.ExecuteUpdateAsync(updateOperations[0]));
    updateTasks.Add(updater2.ExecuteUpdateAsync(updateOperations[1]));
    updateTasks.Add(updater1.ExecuteUpdateAsync(updateOperations[2]));
    updateTasks.Add(updater2.ExecuteUpdateAsync(updateOperations[3]));
    
    var updateResults = await Task.WhenAll(updateTasks);
    
    // Wait for event processing to complete
    await Task.Delay(TimeSpan.FromSeconds(2));
    
    // Assert
    // Verify all updates succeeded
    updateResults.All(r => r.Success).ShouldBeTrue();
    
    // Verify final state consistency
    var finalState = await reader.GetCurrentStateAsync();
    finalState.ShouldNotBeNull();
    finalState["Counter"].ShouldBe(8); // ((0 + 5) * 2 + 3) / 2 = 8
    finalState["Version"].ShouldBe(5); // Initial version 1 + 4 updates
    
    // Verify event history
    var eventHistory = await reader.GetEventHistoryAsync();
    eventHistory.Count.ShouldBe(4);
    
    // Verify event ordering and consistency
    for (int i = 0; i < eventHistory.Count; i++)
    {
        var evt = eventHistory[i];
        evt.SequenceNumber.ShouldBe(i + 1);
        evt.AggregateId.ShouldBe(aggregateRootId);
        evt.Version.ShouldBe(i + 2); // Version starts at 2 (after initial)
    }
    
    // Verify state reconstruction from events
    var reconstructedState = await reader.ReconstructStateFromEventsAsync();
    reconstructedState["Counter"].ShouldBe(8);
    reconstructedState["Version"].ShouldBe(5);
    
    // Verify event sourcing consistency across readers
    var readerState = await reader.GetCurrentStateAsync();
    var aggregateState = await aggregateRoot.GetCurrentStateAsync();
    
    readerState["Counter"].ShouldBe(aggregateState["Counter"]);
    readerState["Version"].ShouldBe(aggregateState["Version"]);
    
    _outputHelper.WriteLine($"Event sourcing completed. Final counter: {finalState["Counter"]}, Version: {finalState["Version"]}");
}
```

## Performance Integration Testing

### Scalability Testing

```csharp
[Fact]
public async Task Should_ScaleHorizontally_When_LoadIncreases()
{
    // Arrange
    var loadBalancerId = Guid.NewGuid();
    var initialWorkerCount = 2;
    var maxWorkerCount = 8;
    
    var loadBalancer = await _agentFactory.GetGAgentAsync<ILoadBalancerGAgent>(loadBalancerId);
    var workers = new List<IScalableWorkerGAgent>();
    
    // Create initial workers
    for (int i = 0; i < initialWorkerCount; i++)
    {
        var workerId = Guid.NewGuid();
        var worker = await _agentFactory.GetGAgentAsync<IScalableWorkerGAgent>(workerId);
        await worker.SetCapacityAsync(10); // Each worker can handle 10 requests
        workers.Add(worker);
        await loadBalancer.RegisterWorkerAsync(workerId);
    }
    
    var scalingTest = new ScalingTestScenario
    {
        Phases = new[]
        {
            new ScalingPhase
            {
                Duration = TimeSpan.FromSeconds(10),
                RequestsPerSecond = 5,
                ExpectedWorkerCount = 2
            },
            new ScalingPhase
            {
                Duration = TimeSpan.FromSeconds(10),
                RequestsPerSecond = 15,
                ExpectedWorkerCount = 3
            },
            new ScalingPhase
            {
                Duration = TimeSpan.FromSeconds(10),
                RequestsPerSecond = 30,
                ExpectedWorkerCount = 5
            },
            new ScalingPhase
            {
                Duration = TimeSpan.FromSeconds(10),
                RequestsPerSecond = 50,
                ExpectedWorkerCount = 8
            },
            new ScalingPhase
            {
                Duration = TimeSpan.FromSeconds(10),
                RequestsPerSecond = 20,
                ExpectedWorkerCount = 4
            }
        }
    };
    
    _outputHelper.WriteLine($"Starting scalability test with {scalingTest.Phases.Length} phases");
    
    // Act
    var scalingResults = new List<ScalingPhaseResult>();
    
    foreach (var phase in scalingTest.Phases)
    {
        _outputHelper.WriteLine($"Executing phase: {phase.RequestsPerSecond} req/sec for {phase.Duration.TotalSeconds}s");
        
        var phaseResult = await loadBalancer.ExecuteScalingPhaseAsync(phase);
        scalingResults.Add(phaseResult);
        
        // Verify scaling occurred
        var currentWorkers = await loadBalancer.GetCurrentWorkersAsync();
        _outputHelper.WriteLine($"Phase completed. Workers: {currentWorkers.Count}, Expected: {phase.ExpectedWorkerCount}");
        
        // Add workers if needed (simulating auto-scaling)
        while (currentWorkers.Count < phase.ExpectedWorkerCount && workers.Count < maxWorkerCount)
        {
            var newWorkerId = Guid.NewGuid();
            var newWorker = await _agentFactory.GetGAgentAsync<IScalableWorkerGAgent>(newWorkerId);
            await newWorker.SetCapacityAsync(10);
            workers.Add(newWorker);
            await loadBalancer.RegisterWorkerAsync(newWorkerId);
            currentWorkers = await loadBalancer.GetCurrentWorkersAsync();
        }
    }
    
    // Assert
    scalingResults.Count.ShouldBe(scalingTest.Phases.Length);
    
    // Verify all phases met performance targets
    foreach (var (phase, result) in scalingTest.Phases.Zip(scalingResults))
    {
        result.Success.ShouldBeTrue();
        result.ActualWorkerCount.ShouldBeGreaterThanOrEqualTo(phase.ExpectedWorkerCount);
        result.AverageResponseTime.ShouldBeLessThan(TimeSpan.FromSeconds(1));
        result.ErrorRate.ShouldBeLessThan(0.02); // Less than 2% error rate
    }
    
    // Verify scaling efficiency
    var totalRequests = scalingResults.Sum(r => r.TotalRequests);
    var totalErrors = scalingResults.Sum(r => r.ErrorCount);
    var overallErrorRate = (double)totalErrors / totalRequests;
    overallErrorRate.ShouldBeLessThan(0.01); // Less than 1% overall error rate
    
    // Verify resource utilization
    var finalWorkers = await loadBalancer.GetCurrentWorkersAsync();
    var workerStates = new List<WorkerState>();
    
    foreach (var worker in workers.Take(finalWorkers.Count))
    {
        var state = await worker.GetStateAsync();
        workerStates.Add(state);
    }
    
    var averageUtilization = workerStates.Average(s => s.Utilization);
    averageUtilization.ShouldBeGreaterThan(0.3); // Should have reasonable utilization
    averageUtilization.ShouldBeLessThan(0.9); // Should not be overloaded
    
    _outputHelper.WriteLine($"Scalability test completed. Overall error rate: {overallErrorRate:P2}");
    _outputHelper.WriteLine($"Final worker count: {finalWorkers.Count}, Average utilization: {averageUtilization:P2}");
}
```

## Integration Testing Best Practices

### 1. Test Environment Setup
- **Isolated Environments**: Use separate test environments for integration tests
- **Realistic Data**: Use production-like data volumes and complexity
- **External Dependencies**: Mock external services but keep internal components real

### 2. Test Data Management
- **Consistent State**: Ensure tests start from known states
- **Cleanup**: Properly clean up test data and resources
- **Data Privacy**: Use anonymized or synthetic data for privacy

### 3. Performance Monitoring
- **Metrics Collection**: Collect comprehensive performance metrics
- **Threshold Monitoring**: Set and monitor performance thresholds
- **Resource Usage**: Monitor CPU, memory, and network usage

### 4. Error Handling
- **Failure Scenarios**: Test various failure modes and recovery
- **Timeout Handling**: Verify proper timeout behavior
- **Graceful Degradation**: Test system behavior under stress

### 5. Test Organization
- **Modular Tests**: Keep integration tests focused and modular
- **Clear Naming**: Use descriptive test names explaining the integration scenario
- **Documentation**: Document complex integration scenarios

## Common Integration Testing Challenges

### 1. Test Isolation
```csharp
// Bad - Tests interfere with each other
[Fact]
public async Task BadTest1()
{
    var agent = await _agentFactory.GetGAgentAsync<IMyGAgent>(Guid.NewGuid());
    await agent.SetGlobalStateAsync("shared-value");
}

[Fact]
public async Task BadTest2()
{
    var agent = await _agentFactory.GetGAgentAsync<IMyGAgent>(Guid.NewGuid());
    var state = await agent.GetGlobalStateAsync(); // May get wrong state
    state.ShouldBe("initial-value"); // May fail due to BadTest1
}

// Good - Use unique identifiers and proper cleanup
[Fact]
public async Task GoodTest1()
{
    var agentId = Guid.NewGuid();
    var agent = await _agentFactory.GetGAgentAsync<IMyGAgent>(agentId);
    await agent.SetScopedStateAsync(agentId, "test-value");
    // Cleanup handled automatically by base class
}
```

### 2. Race Conditions
```csharp
// Good - Use proper synchronization and verification
[Fact]
public async Task Should_HandleConcurrentUpdates_When_MultipleAgentsAccess()
{
    var agentId = Guid.NewGuid();
    var agent = await _agentFactory.GetGAgentAsync<IConcurrentGAgent>(agentId);
    
    var tasks = Enumerable.Range(0, 10)
        .Select(i => agent.ConcurrentUpdateAsync($"update-{i}"))
        .ToList();
    
    await Task.WhenAll(tasks);
    
    // Verify final state is consistent
    var state = await agent.GetStateAsync();
    state.UpdateCount.ShouldBe(10);
    state.LastUpdateVersion.ShouldBe(10);
}
```

## Next Steps

After mastering integration testing patterns, proceed to:

- **Part 5: AI GAgent Testing Patterns** - Explore AI-specific testing patterns
- **Part 6: Performance and Load Testing** - Master advanced performance testing
- **Part 7: Test Organization Best Practices** - Learn test maintenance strategies

## References

- [Integration Testing Best Practices](https://martinfowler.com/bliki/IntegrationTest.html)
- [End-to-End Testing](https://www.browserstack.com/guide/end-to-end-testing)
- [Distributed Systems Testing](https://www.amazon.com/Distributed-Systems-Testing-Mauricio-Nicolas-Germano/dp/1484259165)
- [Event Sourcing Patterns](https://docs.microsoft.com/en-us/azure/architecture/patterns/event-sourcing)