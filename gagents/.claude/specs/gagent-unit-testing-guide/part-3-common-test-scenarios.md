# GAgent Unit Testing Guide - Part 3: Common Test Scenarios

## Overview

This guide covers common testing scenarios that developers frequently encounter when working with GAgents. These scenarios provide practical patterns for testing real-world GAgent functionality, including CRUD operations, validation, error handling, and complex business logic.

## CRUD Operations Testing

### Create Operations Testing

```csharp
[Fact]
public async Task Should_CreateEntity_When_ValidDataProvided()
{
    // Arrange
    var agentId = Guid.NewGuid();
    var agent = await _agentFactory.GetGAgentAsync<IEntityManagementGAgent>(agentId);
    
    var newEntity = new EntityCreationRequest
    {
        Name = "Test Entity",
        Description = "A test entity for unit testing",
        Type = "TestType",
        Properties = new Dictionary<string, object>
        {
            ["Size"] = "Large",
            ["Priority"] = "High"
        }
    };
    
    _outputHelper.WriteLine($"Creating entity: {newEntity.Name}");
    
    // Act
    var result = await agent.CreateEntityAsync(newEntity);
    
    // Assert
    result.ShouldNotBeNull();
    result.Success.ShouldBeTrue();
    result.EntityId.ShouldNotBe(Guid.Empty);
    result.Entity.Name.ShouldBe(newEntity.Name);
    result.Entity.Description.ShouldBe(newEntity.Description);
    result.Entity.CreatedAt.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1));
    
    // Verify entity was actually stored
    var state = await agent.GetStateAsync();
    state.Entities.ShouldContain(e => e.Id == result.EntityId);
    
    _outputHelper.WriteLine($"Entity created successfully with ID: {result.EntityId}");
}
```

### Read Operations Testing

```csharp
[Theory]
[InlineData("existing-id", true)]
[InlineData("non-existing-id", false)]
public async Task Should_GetEntity_When_EntityIdProvided(string entityIdStr, bool shouldExist)
{
    // Arrange
    var agentId = Guid.NewGuid();
    var agent = await _agentFactory.GetGAgentAsync<IEntityManagementGAgent>(agentId);
    
    var entityId = Guid.Parse(entityIdStr);
    
    if (shouldExist)
    {
        // Create test entity first
        var createRequest = new EntityCreationRequest
        {
            Name = "Existing Entity",
            Description = "Entity that should exist"
        };
        await agent.CreateEntityAsync(createRequest);
    }
    
    _outputHelper.WriteLine($"Looking for entity: {entityId}");
    
    // Act
    var result = await agent.GetEntityAsync(entityId);
    
    // Assert
    if (shouldExist)
    {
        result.ShouldNotBeNull();
        result.Id.ShouldBe(entityId);
        result.Name.ShouldBe("Existing Entity");
    }
    else
    {
        result.ShouldBeNull();
    }
    
    _outputHelper.WriteLine($"Entity lookup completed. Found: {result != null}");
}
```

### Update Operations Testing

```csharp
[Fact]
public async Task Should_UpdateEntity_When_ValidUpdateProvided()
{
    // Arrange
    var agentId = Guid.NewGuid();
    var agent = await _agentFactory.GetGAgentAsync<IEntityManagementGAgent>(agentId);
    
    // Create initial entity
    var createRequest = new EntityCreationRequest
    {
        Name = "Original Name",
        Description = "Original description",
        Type = "OriginalType"
    };
    var createResult = await agent.CreateEntityAsync(createRequest);
    
    var updateRequest = new EntityUpdateRequest
    {
        EntityId = createResult.EntityId,
        Name = "Updated Name",
        Description = "Updated description",
        Properties = new Dictionary<string, object>
        {
            ["Status"] = "Updated",
            ["Version"] = 2
        }
    };
    
    _outputHelper.WriteLine($"Updating entity: {createResult.EntityId}");
    
    // Act
    var updateResult = await agent.UpdateEntityAsync(updateRequest);
    
    // Assert
    updateResult.ShouldNotBeNull();
    updateResult.Success.ShouldBeTrue();
    updateResult.Entity.Name.ShouldBe("Updated Name");
    updateResult.Entity.Description.ShouldBe("Updated description");
    updateResult.Entity.UpdatedAt.ShouldBeGreaterThan(updateResult.Entity.CreatedAt);
    
    // Verify the update was persisted
    var retrievedEntity = await agent.GetEntityAsync(createResult.EntityId);
    retrievedEntity.Name.ShouldBe("Updated Name");
    retrievedEntity.Properties["Status"].ShouldBe("Updated");
    
    _outputHelper.WriteLine($"Entity updated successfully");
}
```

### Delete Operations Testing

```csharp
[Fact]
public async Task Should_DeleteEntity_When_EntityExists()
{
    // Arrange
    var agentId = Guid.NewGuid();
    var agent = await _agentFactory.GetGAgentAsync<IEntityManagementGAgent>(agentId);
    
    // Create entity to delete
    var createRequest = new EntityCreationRequest
    {
        Name = "Entity to Delete",
        Description = "This entity will be deleted"
    };
    var createResult = await agent.CreateEntityAsync(createRequest);
    
    var entityId = createResult.EntityId;
    _outputHelper.WriteLine($"Deleting entity: {entityId}");
    
    // Act
    var deleteResult = await agent.DeleteEntityAsync(entityId);
    
    // Assert
    deleteResult.ShouldNotBeNull();
    deleteResult.Success.ShouldBeTrue();
    deleteResult.DeletedEntityId.ShouldBe(entityId);
    
    // Verify entity was actually deleted
    var retrievedEntity = await agent.GetEntityAsync(entityId);
    retrievedEntity.ShouldBeNull();
    
    // Verify state was updated
    var state = await agent.GetStateAsync();
    state.Entities.ShouldNotContain(e => e.Id == entityId);
    state.DeletedEntities.ShouldContain(e => e.Id == entityId);
    
    _outputHelper.WriteLine($"Entity deleted successfully");
}
```

## Validation Testing Patterns

### Required Field Validation

```csharp
[Theory]
[InlineData(null, "Description", "Name is required")]
[InlineData("Name", null, "Description is required")]
[InlineData("", "Description", "Name cannot be empty")]
[InlineData("Name", "", "Description cannot be empty")]
public async Task Should_ValidateRequiredFields_When_CreatingEntity(string name, string description, string expectedError)
{
    // Arrange
    var agentId = Guid.NewGuid();
    var agent = await _agentFactory.GetGAgentAsync<IEntityManagementGAgent>(agentId);
    
    var request = new EntityCreationRequest
    {
        Name = name,
        Description = description
    };
    
    _outputHelper.WriteLine($"Testing validation - Name: '{name}', Description: '{description}'");
    
    // Act & Assert
    var exception = await Should.ThrowAsync<ValidationException>(
        () => agent.CreateEntityAsync(request)
    );
    
    exception.Message.ShouldContain(expectedError);
    
    // Verify no entity was created
    var state = await agent.GetStateAsync();
    state.Entities.ShouldBeEmpty();
    
    _outputHelper.WriteLine($"Validation failed as expected: {expectedError}");
}
```

### Business Rule Validation

```csharp
[Theory]
[InlineData("InvalidType", "Type must be one of: Standard, Premium, Enterprise")]
[InlineData("standard", "Type must be capitalized")]
[InlineData("STANDARD", "Type must be properly capitalized")]
public async Task Should_ValidateBusinessRules_When_CreatingEntity(string entityType, string expectedError)
{
    // Arrange
    var agentId = Guid.NewGuid();
    var agent = await _agentFactory.GetGAgentAsync<IEntityManagementGAgent>(agentId);
    
    var request = new EntityCreationRequest
    {
        Name = "Valid Name",
        Description = "Valid description",
        Type = entityType
    };
    
    _outputHelper.WriteLine($"Testing business rule validation for type: {entityType}");
    
    // Act & Assert
    var exception = await Should.ThrowAsync<BusinessRuleException>(
        () => agent.CreateEntityAsync(request)
    );
    
    exception.Message.ShouldContain(expectedError);
    
    // Verify validation event was logged
    var state = await agent.GetStateAsync();
    state.ValidationFailures.ShouldContain(f => f.Contains(expectedError));
    
    _outputHelper.WriteLine($"Business rule validation failed as expected: {expectedError}");
}
```

### Complex Validation Scenarios

```csharp
[Fact]
public async Task Should_ValidateComplexRules_When_CreatingRelatedEntities()
{
    // Arrange
    var agentId = Guid.NewGuid();
    var agent = await _agentFactory.GetGAgentAsync<IEntityManagementGAgent>(agentId);
    
    // Create parent entity
    var parentRequest = new EntityCreationRequest
    {
        Name = "Parent Entity",
        Description = "Parent entity for relationship testing",
        Type = "Parent"
    };
    var parentResult = await agent.CreateEntityAsync(parentRequest);
    
    // Try to create child with invalid relationship
    var childRequest = new EntityCreationRequest
    {
        Name = "Child Entity",
        Description = "Child entity",
        Type = "Child",
        ParentId = parentResult.EntityId,
        Properties = new Dictionary<string, object>
        {
            ["IncompatibleProperty"] = "This should not be allowed with parent type"
        }
    };
    
    _outputHelper.WriteLine($"Testing complex validation for child entity");
    
    // Act & Assert
    var exception = await Should.ThrowAsync<ValidationException>(
        () => agent.CreateEntityAsync(childRequest)
    );
    
    exception.Message.ShouldContain("Child entities cannot have IncompatibleProperty when parent is Parent type");
    
    // Verify parent still exists and child was not created
    var parentStillExists = await agent.GetEntityAsync(parentResult.EntityId);
    parentStillExists.ShouldNotBeNull();
    
    var state = await agent.GetStateAsync();
    state.Entities.Count.ShouldBe(1); // Only parent should exist
    
    _outputHelper.WriteLine("Complex validation failed as expected");
}
```

## Error Handling Testing

### Exception Handling with Recovery

```csharp
[Fact]
public async Task Should_HandleAndRecoverFromTemporaryFailure_When_ServiceUnavailable()
{
    // Arrange
    var agentId = Guid.NewGuid();
    var agent = await _agentFactory.GetGAgentAsync<IResilientGAgent>(agentId);
    
    // Configure agent to simulate temporary failure
    await agent.SetFailureModeAsync(FailureMode.TemporaryServiceUnavailable);
    
    var testData = "critical-data";
    
    _outputHelper.WriteLine("Testing recovery from temporary service failure");
    
    // Act
    var result = await agent.ProcessWithRetryAsync(testData);
    
    // Assert
    result.ShouldNotBeNull();
    result.Success.ShouldBeTrue();
    result.Attempts.ShouldBeGreaterThan(1);
    result.FinalState.ShouldBe("Completed");
    
    // Verify recovery was logged
    var state = await agent.GetStateAsync();
    state.RecoveryAttempts.ShouldBeGreaterThan(0);
    state.SuccessfulRecoveries.ShouldBe(1);
    
    _outputHelper.WriteLine($"Operation succeeded after {result.Attempts} attempts");
}
```

### Graceful Degradation Testing

```csharp
[Fact]
public async Task Should_DegradeGracefully_When_DependenciesUnavailable()
{
    // Arrange
    var agentId = Guid.NewGuid();
    var agent = await _agentFactory.GetGAgentAsync<IDegradingGAgent>(agentId);
    
    // Configure agent to simulate missing dependencies
    await agent.SetDependencyAvailabilityAsync("external-service", false);
    
    var request = new ComplexRequest
    {
        Data = "test-data",
        UseExternalService = true,
        FallbackEnabled = true
    };
    
    _outputHelper.WriteLine("Testing graceful degradation with missing dependencies");
    
    // Act
    var result = await agent.ProcessWithFallbackAsync(request);
    
    // Assert
    result.ShouldNotBeNull();
    result.Success.ShouldBeTrue();
    result.Mode.ShouldBe("Fallback");
    result.UsedFallback.ShouldBeTrue();
    result.Quality.ShouldBe("Reduced"); // Degraded but acceptable
    
    // Verify degradation was logged appropriately
    var state = await agent.GetStateAsync();
    state.DegradedOperations.ShouldBe(1);
    state.FallbackOperations.ShouldBe(1);
    
    _outputHelper.WriteLine($"Operation completed in fallback mode: {result.Mode}");
}
```

### Circuit Breaker Pattern Testing

```csharp
[Theory]
[InlineData(3, 5000, true)]  // 3 failures, 5s timeout, should open circuit
[InlineData(2, 5000, false)] // 2 failures, 5s timeout, should stay closed
public async Task Should_OpenCircuit_When_FailureThresholdReached(int failureCount, int timeoutMs, bool shouldOpen)
{
    // Arrange
    var agentId = Guid.NewGuid();
    var agent = await _agentFactory.GetGAgentAsync<ICircuitBreakerGAgent>(agentId);
    
    await agent.ConfigureCircuitBreakerAsync(failureCount, TimeSpan.FromMilliseconds(timeoutMs));
    
    // Simulate failures
    for (int i = 0; i < failureCount; i++)
    {
        try
        {
            await agent.CallFailingOperationAsync();
        }
        catch (Exception ex)
        {
            _outputHelper.WriteLine($"Failure {i + 1}: {ex.Message}");
        }
    }
    
    _outputHelper.WriteLine($"Testing circuit breaker after {failureCount} failures");
    
    // Act
    var circuitState = await agent.GetCircuitStateAsync();
    
    // Assert
    if (shouldOpen)
    {
        circuitState.ShouldBe("Open");
        var isOpen = await agent.IsCircuitOpenAsync();
        isOpen.ShouldBeTrue();
    }
    else
    {
        circuitState.ShouldBe("Closed");
        var isOpen = await agent.IsCircuitOpenAsync();
        isOpen.ShouldBeFalse();
    }
    
    _outputHelper.WriteLine($"Circuit state: {circuitState}");
}
```

## Complex Business Logic Testing

### Workflow State Machine Testing

```csharp
[Theory]
[InlineData("Draft", "Submit", "Submitted", true)]
[InlineData("Draft", "Approve", "Draft", false)]  // Cannot approve from draft
[InlineData("Submitted", "Approve", "Approved", true)]
[InlineData("Submitted", "Reject", "Rejected", true)]
[InlineData("Approved", "Submit", "Approved", false)] // Cannot resubmit approved
public async Task Should_TransitionState_When_ValidAction(string initialState, string action, 
    string expectedState, bool shouldSucceed)
{
    // Arrange
    var agentId = Guid.NewGuid();
    var agent = await _agentFactory.GetGAgentAsync<IWorkflowGAgent>(agentId);
    
    // Initialize workflow
    await agent.InitializeWorkflowAsync("TestWorkflow", initialState);
    
    var transitionRequest = new StateTransitionRequest
    {
        Action = action,
        Reason = $"Testing {action} from {initialState}"
    };
    
    _outputHelper.WriteLine($"Testing transition: {initialState} -> {action}");
    
    // Act
    var result = await agent.TransitionStateAsync(transitionRequest);
    
    // Assert
    if (shouldSucceed)
    {
        result.Success.ShouldBeTrue();
        result.NewState.ShouldBe(expectedState);
        
        var currentState = await agent.GetCurrentStateAsync();
        currentState.ShouldBe(expectedState);
    }
    else
    {
        result.Success.ShouldBeFalse();
        result.Error.ShouldContain("Cannot transition");
        
        var currentState = await agent.GetCurrentStateAsync();
        currentState.ShouldBe(initialState); // State should not change
    }
    
    _outputHelper.WriteLine($"Transition result: {result.Success}, Final state: {result.NewState ?? currentState}");
}
```

### Multi-Step Process Testing

```csharp
[Fact]
public async Task Should_ExecuteMultiStepProcess_When_AllStepsSucceed()
{
    // Arrange
    var agentId = Guid.NewGuid();
    var agent = await _agentFactory.GetGAgentAsync<IMultiStepGAgent>(agentId);
    
    var processRequest = new MultiStepProcessRequest
    {
        ProcessId = Guid.NewGuid(),
        Steps = new List<ProcessStep>
        {
            new ProcessStep { Id = 1, Name = "Validation", Type = "Validation" },
            new ProcessStep { Id = 2, Name = "Processing", Type = "Processing" },
            new ProcessStep { Id = 3, Name = "Notification", Type = "Notification" }
        },
        Data = new Dictionary<string, object>
        {
            ["InputData"] = "test-data",
            ["Priority"] = "High"
        }
    };
    
    _outputHelper.WriteLine($"Starting multi-step process: {processRequest.ProcessId}");
    
    // Act
    var result = await agent.ExecuteMultiStepProcessAsync(processRequest);
    
    // Assert
    result.ShouldNotBeNull();
    result.Success.ShouldBeTrue();
    result.ProcessId.ShouldBe(processRequest.ProcessId);
    result.CompletedSteps.Count.ShouldBe(3);
    result.FailedSteps.ShouldBeEmpty();
    
    // Verify each step completed successfully
    foreach (var step in processRequest.Steps)
    {
        var stepResult = result.CompletedSteps.FirstOrDefault(s => s.StepId == step.Id);
        stepResult.ShouldNotBeNull();
        stepResult.Status.ShouldBe("Completed");
        stepResult.Duration.ShouldBeGreaterThan(TimeSpan.Zero);
    }
    
    // Verify overall process state
    var processState = await agent.GetProcessStateAsync(processRequest.ProcessId);
    processState.Status.ShouldBe("Completed");
    processState.Progress.ShouldBe(100);
    
    _outputHelper.WriteLine($"Multi-step process completed successfully. Duration: {result.TotalDuration}");
}
```

### Conditional Logic Testing

```csharp
[Theory]
[InlineData("Premium", 1000, 0.2, 800)]   // 20% discount for Premium
[InlineData("Standard", 1000, 0.1, 900)] // 10% discount for Standard
[InlineData("Basic", 1000, 0.05, 950)]    // 5% discount for Basic
[InlineData("None", 1000, 0, 1000)]       // No discount
public async Task Should_ApplyCorrectDiscount_When_CustomerTierProvided(
    string customerTier, decimal basePrice, decimal expectedDiscountRate, decimal expectedFinalPrice)
{
    // Arrange
    var agentId = Guid.NewGuid();
    var agent = await _agentFactory.GetGAgentAsync<IPricingGAgent>(agentId);
    
    var pricingRequest = new PricingRequest
    {
        CustomerTier = customerTier,
        BasePrice = basePrice,
        ProductCategory = "Electronics",
        Quantity = 1
    };
    
    _outputHelper.WriteLine($"Testing pricing for {customerTier} customer, base price: {basePrice}");
    
    // Act
    var result = await agent.CalculatePriceAsync(pricingRequest);
    
    // Assert
    result.ShouldNotBeNull();
    result.BasePrice.ShouldBe(basePrice);
    result.DiscountRate.ShouldBe(expectedDiscountRate);
    result.FinalPrice.ShouldBe(expectedFinalPrice);
    result.AppliedDiscount.ShouldBe(basePrice * expectedDiscountRate);
    
    // Verify discount was calculated correctly
    var expectedDiscount = basePrice * expectedDiscountRate;
    result.AppliedDiscount.ShouldBe(expectedDiscount);
    
    _outputHelper.WriteLine($"Price calculated: {basePrice} - {expectedDiscount} = {expectedFinalPrice}");
}
```

## Edge Case Testing

### Boundary Value Testing

```csharp
[Theory]
[InlineData(0, false)]      // Below minimum
[InlineData(1, true)]       // Minimum valid value
[InlineData(50, true)]      // Middle range
[InlineData(100, true)]     // Maximum valid value
[InlineData(101, false)]    // Above maximum
public async Task Should_ValidateBoundaryValues_When_ProcessingQuantity(int quantity, bool shouldBeValid)
{
    // Arrange
    var agentId = Guid.NewGuid();
    var agent = await _agentFactory.GetGAgentAsync<IValidationGAgent>(agentId);
    
    var request = new QuantityRequest
    {
        ProductId = Guid.NewGuid(),
        Quantity = quantity
    };
    
    _outputHelper.WriteLine($"Testing boundary value: {quantity}");
    
    // Act
    var result = await agent.ValidateQuantityAsync(request);
    
    // Assert
    if (shouldBeValid)
    {
        result.IsValid.ShouldBeTrue();
        result.Error.ShouldBeNull();
    }
    else
    {
        result.IsValid.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.ShouldContain("Quantity must be between 1 and 100");
    }
    
    _outputHelper.WriteLine($"Validation result: {result.IsValid}");
}
```

### Concurrent Operations Testing

```csharp
[Fact]
public async Task Should_HandleConcurrentOperations_When_MultipleThreadsAccess()
{
    // Arrange
    var agentId = Guid.NewGuid();
    var agent = await _agentFactory.GetGAgentAsync<IConcurrentGAgent>(agentId);
    
    var concurrentTasks = new List<Task<ConcurrentOperationResult>>();
    var operationCount = 10;
    
    _outputHelper.WriteLine($"Testing {operationCount} concurrent operations");
    
    // Act - Start multiple concurrent operations
    for (int i = 0; i < operationCount; i++)
    {
        var taskId = i;
        var task = agent.ExecuteConcurrentOperationAsync($"operation-{taskId}");
        concurrentTasks.Add(task);
    }
    
    // Wait for all operations to complete
    var results = await Task.WhenAll(concurrentTasks);
    
    // Assert
    results.Length.ShouldBe(operationCount);
    
    var successfulOperations = results.Count(r => r.Success);
    var failedOperations = results.Count(r => !r.Success);
    
    // All operations should succeed (agent should handle concurrency)
    successfulOperations.ShouldBe(operationCount);
    failedOperations.ShouldBe(0);
    
    // Verify no data corruption occurred
    var state = await agent.GetStateAsync();
    state.TotalOperations.ShouldBe(operationCount);
    state.ConcurrentConflicts.ShouldBe(0); // Should be resolved internally
    
    _outputHelper.WriteLine($"Concurrent operations completed: {successfulOperations} successful, {failedOperations} failed");
}
```

### Large Dataset Testing

```csharp
[Fact]
public async Task Should_HandleLargeDataset_When_ProcessingBulkData()
{
    // Arrange
    var agentId = Guid.NewGuid();
    var agent = await _agentFactory.GetGAgentAsync<IBulkProcessingGAgent>(agentId);
    
    // Generate large dataset
    var largeDataset = Enumerable.Range(1, 10000)
        .Select(i => new DataItem 
        { 
            Id = i, 
            Value = $"Item-{i}", 
            Category = i % 2 == 0 ? "Even" : "Odd"
        })
        .ToList();
    
    var bulkRequest = new BulkProcessingRequest
    {
        Items = largeDataset,
        BatchSize = 100,
        ParallelProcessing = true
    };
    
    _outputHelper.WriteLine($"Processing large dataset: {largeDataset.Count} items");
    
    // Act
    var stopwatch = Stopwatch.StartNew();
    var result = await agent.ProcessBulkDataAsync(bulkRequest);
    stopwatch.Stop();
    
    // Assert
    result.ShouldNotBeNull();
    result.Success.ShouldBeTrue();
    result.ProcessedCount.ShouldBe(largeDataset.Count);
    result.FailedCount.ShouldBe(0);
    result.BatchesProcessed.ShouldBe(100); // 10000 items / 100 batch size
    
    // Verify performance
    var processingTime = stopwatch.Elapsed;
    processingTime.ShouldBeLessThan(TimeSpan.FromSeconds(30)); // Should complete within 30 seconds
    
    // Verify data integrity
    var state = await agent.GetStateAsync();
    state.TotalProcessed.ShouldBe(largeDataset.Count);
    state.CategoriesProcessed["Even"].ShouldBe(5000);
    state.CategoriesProcessed["Odd"].ShouldBe(5000);
    
    _outputHelper.WriteLine($"Bulk processing completed in {processingTime.TotalSeconds:F2}s");
    _outputHelper.WriteLine($"Results: {result.ProcessedCount} processed, {result.FailedCount} failed");
}
```

## Complex Test Data Setup

### Complex Relationship Testing

```csharp
[Fact]
public async Task Should_ManageComplexRelationships_When_EntitiesAreInterconnected()
{
    // Arrange
    var agentId = Guid.NewGuid();
    var agent = await _agentFactory.GetGAgentAsync<IRelationshipGAgent>(agentId);
    
    // Create main entity
    var mainEntity = await agent.CreateEntityAsync(new EntityCreationRequest
    {
        Name = "Main Entity",
        Type = "Main"
    });
    
    // Create related entities with different relationship types
    var child1 = await agent.CreateEntityAsync(new EntityCreationRequest
    {
        Name = "Child 1",
        Type = "Child",
        ParentId = mainEntity.EntityId
    });
    
    var child2 = await agent.CreateEntityAsync(new EntityCreationRequest
    {
        Name = "Child 2",
        Type = "Child",
        ParentId = mainEntity.EntityId
    });
    
    var reference1 = await agent.CreateEntityAsync(new EntityCreationRequest
    {
        Name = "Reference 1",
        Type = "Reference"
    });
    
    // Create relationships
    await agent.AddRelationshipAsync(mainEntity.EntityId, reference1.EntityId, "references");
    await agent.AddRelationshipAsync(child1.EntityId, reference1.EntityId, "uses");
    
    _outputHelper.WriteLine($"Created complex relationship structure");
    
    // Act
    var relationshipMap = await agent.GetRelationshipMapAsync(mainEntity.EntityId);
    
    // Assert
    relationshipMap.ShouldNotBeNull();
    relationshipMap.EntityId.ShouldBe(mainEntity.EntityId);
    
    // Verify parent-child relationships
    relationshipMap.Children.Count.ShouldBe(2);
    relationshipMap.Children.ShouldContain(c => c.EntityId == child1.EntityId);
    relationshipMap.Children.ShouldContain(c => c.EntityId == child2.EntityId);
    
    // Verify reference relationships
    relationshipMap.References.Count.ShouldBe(1);
    relationshipMap.References[0].EntityId.ShouldBe(reference1.EntityId);
    relationshipMap.References[0].RelationshipType.ShouldBe("references");
    
    // Verify indirect relationships
    var indirectRelationships = await agent.GetIndirectRelationshipsAsync(mainEntity.EntityId);
    indirectRelationships.ShouldContain(r => 
        r.FromEntityId == child1.EntityId && 
        r.ToEntityId == reference1.EntityId && 
        r.RelationshipType == "uses");
    
    _outputHelper.WriteLine($"Complex relationship verification completed");
}
```

## Performance and Load Testing

### Load Testing Patterns

```csharp
[Fact]
public async Task Should_HandleHighLoad_When_MultipleRequestsConcurrent()
{
    // Arrange
    var agentId = Guid.NewGuid();
    var agent = await _agentFactory.GetGAgentAsync<ILoadTestGAgent>(agentId);
    
    var concurrentRequests = 50;
    var requestsPerSecond = 10;
    var testData = Enumerable.Range(1, concurrentRequests)
        .Select(i => new LoadTestRequest 
        { 
            RequestId = i, 
            Data = $"load-test-data-{i}",
            Complexity = "Medium"
        })
        .ToList();
    
    _outputHelper.WriteLine($"Starting load test: {concurrentRequests} concurrent requests");
    
    // Act
    var stopwatch = Stopwatch.StartNew();
    
    // Start requests with controlled rate
    var tasks = new List<Task<LoadTestResult>>();
    var taskDelay = TimeSpan.FromMilliseconds(1000 / requestsPerSecond);
    
    foreach (var request in testData)
    {
        tasks.Add(agent.ProcessLoadTestRequestAsync(request));
        await Task.Delay(taskDelay);
    }
    
    var results = await Task.WhenAll(tasks);
    stopwatch.Stop();
    
    // Assert
    results.Length.ShouldBe(concurrentRequests);
    
    var successfulRequests = results.Count(r => r.Success);
    var failedRequests = results.Count(r => !r.Success);
    var averageResponseTime = results.Average(r => r.ResponseTime.TotalMilliseconds);
    
    // Performance assertions
    successfulRequests.ShouldBe(concurrentRequests);
    failedRequests.ShouldBe(0);
    averageResponseTime.ShouldBeLessThan(1000); // Average response time under 1 second
    
    // Verify system handled load gracefully
    var state = await agent.GetStateAsync();
    state.TotalRequests.ShouldBe(concurrentRequests);
    state.PeakConcurrentRequests.ShouldBeGreaterThan(0);
    state.AverageResponseTime.ShouldBeLessThan(1000);
    
    _outputHelper.WriteLine($"Load test completed in {stopwatch.Elapsed.TotalSeconds:F2}s");
    _outputHelper.WriteLine($"Results: {successfulRequests} successful, {failedRequests} failed");
    _outputHelper.WriteLine($"Average response time: {averageResponseTime:F2}ms");
}
```

## Best Practices

### 1. Test Organization
- **Scenario-Based Testing**: Group tests by business scenarios
- **Data-Driven Testing**: Use theories for parameterized tests
- **Clear Test Names**: Describe the scenario and expected outcome

### 2. Test Data Management
- **Realistic Data**: Use realistic but manageable test data
- **Data Builders**: Use builders for complex object creation
- **Cleanup**: Ensure proper cleanup of test data

### 3. Error Handling
- **Comprehensive Coverage**: Test both success and failure scenarios
- **Recovery Testing**: Test error recovery mechanisms
- **Logging**: Verify proper error logging and monitoring

### 4. Performance Considerations
- **Performance Metrics**: Track and assert performance metrics
- **Load Testing**: Test under various load conditions
- **Resource Management**: Monitor resource usage

### 5. Maintainability
- **Modular Tests**: Keep tests focused and modular
- **Reusable Components**: Create reusable test utilities
- **Documentation**: Document complex test scenarios

## Common Pitfalls

### 1. Testing Implementation Details
```csharp
// Bad - Testing internal implementation
[Fact]
public async Task BadTest()
{
    var agent = await _agentFactory.GetGAgentAsync<IMyGAgent>(Guid.NewGuid());
    var internalState = agent.GetInternalState(); // Bad practice
    internalState.SomeInternalProperty.ShouldBe("value");
}

// Good - Testing observable behavior
[Fact]
public async Task Should_ReturnExpectedResult_When_InputProvided()
{
    var agent = await _agentFactory.GetGAgentAsync<IMyGAgent>(Guid.NewGuid());
    var result = await agent.ProcessAsync("input");
    result.ShouldBe("expected-output");
}
```

### 2. Overly Complex Tests
```csharp
// Bad - Too many assertions and setup
[Fact]
public async Task BadTest()
{
    var agent = await _agentFactory.GetGAgentAsync<IMyGAgent>(Guid.NewGuid());
    // 20 lines of setup
    // Multiple complex operations
    // 15 assertions
}

// Good - Focused test with clear purpose
[Fact]
public async Task Should_SingleOperation_When_ConditionMet()
{
    var agent = await _agentFactory.GetGAgentAsync<IMyGAgent>(Guid.NewGuid());
    // Minimal setup
    var result = await agent.SingleOperationAsync();
    result.ShouldBeTrue();
}
```

## Next Steps

After mastering common test scenarios, proceed to:

- **Part 4: Integration Testing Patterns** - Learn advanced integration testing techniques
- **Part 5: AI GAgent Testing Patterns** - Explore AI-specific testing patterns
- **Part 6: Performance and Load Testing** - Master performance testing methodologies

## References

- [Boundary Value Analysis](https://en.wikipedia.org/wiki/Boundary-value_analysis)
- [Equivalence Partitioning](https://en.wikipedia.org/wiki/Equivalence_partitioning)
- [State Machine Testing](https://www.testplant.com/state-machine-testing/)
- [Load Testing Best Practices](https://docs.microsoft.com/en-us/azure/architecture/framework/scalability/performance-testing)