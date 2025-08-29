# Aevatar.GAgents.GroupChat - Workflow Orchestration Framework

## Overview

Aevatar.GAgents.GroupChat is a powerful workflow orchestration framework built on Orleans that enables the creation and execution of complex workflows through a distributed agent-based architecture. It provides a flexible and scalable solution for building enterprise-grade workflow systems similar to Dify or LangFlow.

## Core Concepts

### 1. WorkflowCoordinatorGAgent
The central orchestrator that manages the entire workflow lifecycle:
- **State Management**: Tracks workflow status (Pending → InProgress → Finished/Failed)
- **Topology Management**: Ensures all paths can reach terminal nodes (no infinite loops)
- **Dynamic Configuration**: Supports runtime modification of workflow structure
- **Event-Driven Coordination**: Uses Orleans streaming for real-time coordination

### 2. WorkUnit (Workflow Node)
Individual processing units in the workflow:
- Each WorkUnit is a GAgent that performs specific tasks
- Can be any GAgent that inherits from `GroupMemberGAgentBase`
- Supports both synchronous and asynchronous execution
- Can produce outputs consumed by downstream nodes

### 3. Blackboard Pattern
Shared data context for the workflow:
- **BlackboardGAgent**: Centralized data storage for workflow execution
- Enables data sharing between non-connected nodes
- Maintains conversation history and intermediate results
- Thread-safe and distributed by design

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                   WorkflowCoordinatorGAgent                 │
│  ┌─────────────────────────────────────────────────────┐  │
│  │ • Workflow State Management                          │  │
│  │ • Topology Validation                                │  │
│  │ • Event Distribution                                 │  │
│  │ • Progress Tracking                                  │  │
│  └─────────────────────────────────────────────────────┘  │
└─────────────────────┬───────────────────────────────────────┘
                      │ Coordinates
     ┌────────────────┴────────────────┬─────────────────┐
     ▼                                 ▼                 ▼
┌─────────┐                      ┌─────────┐       ┌─────────┐
│WorkUnit1│                      │WorkUnit2│       │WorkUnit3│
│(GAgent) │──────Publishes──────▶│(GAgent) │       │(GAgent) │
└────┬────┘                      └────┬────┘       └────┬────┘
     │                                 │                 │
     └─────────────┬───────────────────┴─────────────────┘
                   ▼
           ┌──────────────┐
           │ Blackboard   │
           │   GAgent     │
           └──────────────┘
```

## Workflow Execution Flow

### 1. Initialization Phase
```csharp
// Configure workflow units
var config = new WorkflowCoordinatorConfigDto
{
    WorkflowUnitList = new List<WorkflowUnitDto>
    {
        new() { GrainId = "unit1", NextGrainId = "unit2" },
        new() { GrainId = "unit2", NextGrainId = "unit3" },
        new() { GrainId = "unit3", NextGrainId = null } // Terminal node
    }
};

// Apply configuration
await coordinator.ConfigAsync(config);
```

### 2. Execution Phase
1. **Start Event**: `StartWorkflowCoordinatorEvent` triggers workflow execution
2. **Node Activation**: Top-level nodes (no upstream dependencies) activate first
3. **Data Flow**: Nodes read from upstream results via Blackboard
4. **Progress Tracking**: Each node reports completion via `ChatResponseEvent`
5. **Downstream Activation**: Completed nodes trigger their downstream dependencies
6. **Completion Detection**: Workflow completes when all terminal nodes finish

### 3. Event Flow
```
StartWorkflowCoordinatorEvent
    ↓
ChatEvent (to WorkUnit)
    ↓
WorkUnit Processing
    ↓
ChatResponseEvent (from WorkUnit)
    ↓
CoordinatorConfirmChatResponse (to Blackboard)
    ↓
Next WorkUnit Activation or WorkflowFinishEvent
```

## Key Features

### 1. Loop Detection
The framework automatically detects and prevents infinite loops:
```csharp
public bool IsAllPathsCanReachTerminal(List<WorkflowUnitDto> workflowUnits)
{
    // Reverse graph traversal from terminal nodes
    // Ensures all nodes can reach at least one terminal
}
```

### 2. Parallel Execution
Multiple nodes can execute simultaneously if they have no dependencies:
- Independent branches run in parallel
- Automatic synchronization at join points
- Efficient resource utilization

### 3. Dynamic Reconfiguration
Workflows can be modified at runtime:
- Add/remove nodes
- Change connections
- Hot-swap implementations

### 4. State Persistence
All workflow state is persisted using Orleans' event sourcing:
- Automatic recovery from failures
- Complete audit trail
- Time-travel debugging capabilities

## Usage Example

### 1. Define Custom WorkUnit
```csharp
[GAgent]
public class DataProcessorGAgent : GroupMemberGAgentBase<DataProcessorState, DataProcessorLogEvent, EventBase, GroupMemberConfigDto>
{
    protected override async Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? coordinatorMessages)
    {
        // Process input data
        var inputData = coordinatorMessages?.LastOrDefault()?.Content;
        var result = await ProcessDataAsync(inputData);
        
        return new ChatResponse
        {
            Content = result,
            Continue = true, // Signal to continue workflow
            Skip = false
        };
    }
    
    protected override Task<int> GetInterestValueAsync(Guid blackboardId)
    {
        return Task.FromResult(100); // Always interested in execution
    }
}
```

### 2. Create and Start Workflow
```csharp
// Get coordinator instance
var coordinator = grainFactory.GetGrain<IWorkflowCoordinatorGAgent>(workflowId);

// Configure workflow
var units = new List<WorkflowUnitDto>
{
    new() { GrainId = "data-fetcher", NextGrainId = "data-processor" },
    new() { GrainId = "data-processor", NextGrainId = "data-saver" },
    new() { GrainId = "data-saver", NextGrainId = null }
};

await coordinator.ConfigAsync(new WorkflowCoordinatorConfigDto 
{ 
    WorkflowUnitList = units,
    InitContent = "Start processing customer data"
});

// Start execution
await coordinator.PublishAsync(new StartWorkflowCoordinatorEvent());
```

### 3. Monitor Progress
```csharp
// Subscribe to workflow events
var stream = streamProvider.GetStream<EventBase>("workflow-events", workflowId);
await stream.SubscribeAsync(async (evt, token) =>
{
    switch (evt)
    {
        case ChatResponseEvent response:
            Console.WriteLine($"Node {response.MemberName} completed");
            break;
        case WorkflowFinishEvent:
            Console.WriteLine("Workflow completed!");
            break;
    }
});
```

## Advanced Patterns

### 1. Conditional Branching
```csharp
protected override async Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? messages)
{
    var condition = EvaluateCondition(messages);
    
    return new ChatResponse
    {
        Content = JsonSerializer.Serialize(new { branch = condition ? "A" : "B" }),
        Continue = true,
        // Use ExtendedData to signal which branch to take
        ExtendedData = new Dictionary<string, object> { ["nextNode"] = condition ? "nodeA" : "nodeB" }
    };
}
```

### 2. Human-in-the-Loop
```csharp
public class ApprovalGAgent : GroupMemberGAgentBase<ApprovalState, ApprovalLogEvent, EventBase, GroupMemberConfigDto>
{
    protected override async Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? messages)
    {
        // Send approval request
        await SendApprovalRequestAsync(messages);
        
        // Wait for human response
        var approved = await WaitForApprovalAsync();
        
        return new ChatResponse
        {
            Content = approved ? "Approved" : "Rejected",
            Continue = approved
        };
    }
}
```

### 3. Error Handling and Retry
```csharp
protected override async Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? messages)
{
    try
    {
        var result = await ProcessWithRetryAsync(messages);
        return new ChatResponse { Content = result, Continue = true };
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Processing failed");
        return new ChatResponse 
        { 
            Content = $"Error: {ex.Message}", 
            Continue = false // Stop workflow on error
        };
    }
}
```

## Best Practices

1. **Keep WorkUnits Focused**: Each WorkUnit should have a single responsibility
2. **Use Blackboard Wisely**: Store only necessary shared data
3. **Handle Failures Gracefully**: Implement proper error handling and recovery
4. **Monitor Performance**: Use Orleans Dashboard for monitoring
5. **Test Thoroughly**: Unit test individual WorkUnits and integration test workflows

## Configuration Options

### WorkflowCoordinatorConfigDto
```csharp
public class WorkflowCoordinatorConfigDto
{
    // List of workflow units and their connections
    public List<WorkflowUnitDto> WorkflowUnitList { get; set; }
    
    // Initial content/context for the workflow
    public string? InitContent { get; set; }
}
```

### WorkflowUnitDto
```csharp
public class WorkflowUnitDto
{
    // Unique identifier for the work unit (GrainId)
    public string GrainId { get; set; }
    
    // Next unit in the workflow (null for terminal nodes)
    public string? NextGrainId { get; set; }
    
    // Additional configuration data
    public Dictionary<string, object>? ExtendedData { get; set; }
}
```

## Troubleshooting

### Common Issues

1. **Workflow Stuck**: Check if all nodes are properly responding to ChatEvent
2. **Loop Detected Error**: Ensure all paths lead to terminal nodes
3. **Node Not Activating**: Verify upstream dependencies are completing
4. **Data Not Shared**: Confirm Blackboard is being updated correctly

### Debugging Tips

1. Enable detailed logging:
```csharp
services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));
```

2. Use Orleans Dashboard for visual monitoring
3. Implement custom event handlers for detailed tracking
4. Check grain activation logs for initialization issues

## Performance Considerations

1. **Grain Activation**: Minimize grain activation overhead by reusing instances
2. **Message Size**: Keep messages small; use references for large data
3. **Parallelism**: Design workflows to maximize parallel execution
4. **State Size**: Limit state size to improve persistence performance

## Conclusion

Aevatar.GAgents.GroupChat provides a robust foundation for building complex workflow systems. Its event-driven architecture, combined with Orleans' distributed computing capabilities, makes it suitable for enterprise-scale workflow orchestration needs. 