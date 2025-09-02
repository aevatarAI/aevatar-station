# GAgent Implementation Guide for Claude

## Overview
This guide provides comprehensive rules and best practices for implementing GAgents in the Aevatar framework. These rules are universal and can be applied to any Aevatar-based project.

## Part 1: Core Implementation Rules

### 1.1 Define Interface First
```csharp
// Regular GAgent (non-AI)
public interface IMyGAgent : IStateGAgent<MyState>
{
    // Add custom methods as needed
}

// AI-enhanced GAgent
public interface IMyAIGAgent : IStateGAgent<MyAIState>, IAIGAgent
{
    // Add custom methods as needed
}
```

### 1.2 Basic Structure
```csharp
using Aevatar.Core;
using Aevatar.Core.Abstractions;

[GAgent("my-agent", "workshop")]  // Required attribute
public class MyGAgent : GAgentBase<MyState, MyStateLogEvent>, IMyGAgent
{
    // Do NOT use constructors with parameters!
    
    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("Detailed description of this GAgent");
}
```

### 1.3 Core Inheritance Rules

#### GAgentBase Inheritance
```csharp
// Regular GAgent
public class MyGAgent : GAgentBase<MyState, MyStateLogEvent>, IMyGAgent
{
    // Implementation
}

// AI-enhanced GAgent
public class MyAIGAgent : AIGAgentBase<MyState, MyStateLogEvent>, IMyAIGAgent
{
    // Implementation
}
```

### 1.4 Absolutely Do NOT Do These
```csharp
// ❌ NEVER inject logger
public MyGAgent(ILogger<MyGAgent> logger) { }

// ❌ NEVER modify state directly  
State.Counter++;

// ❌ NEVER use IGrainFactory
var grainFactory = ServiceProvider.GetRequiredService<IGrainFactory>();
```

### 1.5 Service Access Pattern
```csharp
// Simple lazy-loaded service property
private IGAgentFactory GAgentFactory => 
    ServiceProvider.GetRequiredService<IGAgentFactory>();

// Direct usage
var agent = await GAgentFactory.GetGAgentAsync<IOtherGAgent>(id);
```

## Part 2: State Management Rules

### 2.1 State and StateLogEvent Definition Rules

**Important: Event, State, and StateLogEvent must be defined as class, not record**

```csharp
// ✅ Correct: Use class for state
[GenerateSerializer]
public class MyState : StateBase
{
    [Id(0)] public int Counter { get; set; }
    [Id(1)] public List<string> Items { get; set; } = new();
}

// ✅ Correct: Use class for state log events
[GenerateSerializer]
public class MyStateLogEvent : StateLogEventBase<MyStateLogEvent> { }

[GenerateSerializer] 
public class CounterIncrementedEvent : MyStateLogEvent
{
    [Id(0)] public int Amount { get; set; }
}

// ❌ Error: Cannot use record for state
[GenerateSerializer]
public record MyState : StateBase  // This causes serialization issues
{
    [Id(0)] public int Counter { get; set; }
}

// ❌ Error: Cannot use record for state log events
[GenerateSerializer]
public record MyStateLogEvent : StateLogEventBase<MyStateLogEvent>;  // This causes serialization issues
```

**Reason:**
- Orleans serialization system has incomplete support for record types
- State needs mutable properties to support event sourcing state transitions
- StateLogEvent needs compatibility with Orleans event sourcing system
- Using class ensures full compatibility with Orleans serialization mechanism

### 2.2 State Management (Orleans Event Sourcing)
```csharp
// Step 1: Define state
[GenerateSerializer]
public class MyState : StateBase
{
    [Id(0)] public int Counter { get; set; }
    [Id(1)] public List<string> Items { get; set; } = new(); // Always initialize!
}

// Step 2: Define state change events
[GenerateSerializer]
public class MyStateLogEvent : StateLogEventBase<MyStateLogEvent> { }

[GenerateSerializer]
public class CounterIncrementedEvent : MyStateLogEvent
{
    [Id(0)] public int Amount { get; set; }
}

// Step 3: Change state by triggering events
public async Task IncrementAsync(int amount)
{
    RaiseEvent(new CounterIncrementedEvent { Amount = amount });
    await ConfirmEvents();
}

// Step 4: Apply state changes in GAgentTransitionState
protected override void GAgentTransitionState(MyState state, StateLogEventBase<MyStateLogEvent> @event)
{
    switch (@event)
    {
        case CounterIncrementedEvent e:
            state.Counter += e.Amount;
            break;
    }
}
```

### 2.3 State Initialization

State initialization should be done through the `PerformConfigAsync` method, but must follow event sourcing principles by using `RaiseEvent` to modify state. This requires using GAgentBase with four generic parameters, where the fourth parameter is the configuration type.

#### Correct State Initialization Method

**Step 1: Define Configuration Class**
```csharp
[GenerateSerializer]
public class MyGAgentConfiguration : ConfigurationBase
{
    [Id(0)] public string InitialName { get; set; } = string.Empty;
    [Id(1)] public int MaxRetries { get; set; } = 3;
    [Id(2)] public Dictionary<string, string> Settings { get; set; } = new();
}
```

**Step 2: Define Configuration-Related State Log Events**
```csharp
[GenerateSerializer]
public class SetInitialConfigurationLogEvent : MyStateLogEvent
{
    [Id(0)] public string Name { get; set; } = string.Empty;
    [Id(1)] public int MaxRetries { get; set; }
    [Id(2)] public Dictionary<string, string> Settings { get; set; } = new();
}
```

**Step 3: Use GAgentBase with Four Generic Parameters and Implement PerformConfigAsync**
```csharp
// Four generic parameters: State, StateLogEvent, Event, Configuration
public class MyGAgent : GAgentBase<MyState, MyStateLogEvent, EventBase, MyGAgentConfiguration>, IMyGAgent
{
    protected override async Task PerformConfigAsync(MyGAgentConfiguration configuration)
    {
        // Initialize state through RaiseEvent, not direct modification
        RaiseEvent(new SetInitialConfigurationLogEvent
        {
            Name = configuration.InitialName,
            MaxRetries = configuration.MaxRetries,
            Settings = configuration.Settings
        });
        
        await ConfirmEvents();
        
        // If you need to perform other initialization operations based on configuration
        await PerformAdditionalInitializationAsync(configuration);
    }
    
    // Handle configuration events in GAgentTransitionState
    protected override void GAgentTransitionState(MyState state, StateLogEventBase<MyStateLogEvent> @event)
    {
        switch (@event)
        {
            case SetInitialConfigurationLogEvent configEvent:
                state.Id = this.GetGrainId().Key.ToString() ?? "default-id";
                state.Name = configEvent.Name;
                state.MaxRetries = configEvent.MaxRetries;
                state.Settings = configEvent.Settings ?? new Dictionary<string, string>();
                state.Items = new List<string>(); // Initialize collections
                break;
            // Handle other events...
        }
    }
    
    private Task PerformAdditionalInitializationAsync(MyGAgentConfiguration configuration)
    {
        // Perform other initialization operations that don't involve state modification
        Logger.LogInformation("GAgent configured: {Name}", configuration.InitialName);
        return Task.CompletedTask;
    }
}
```

**Step 4: Pass Configuration Through GAgentFactory**
```csharp
// Create configuration instance
var config = new MyGAgentConfiguration
{
    InitialName = "MyAgent",
    MaxRetries = 5,
    Settings = new Dictionary<string, string>
    {
        ["timeout"] = "30",
        ["mode"] = "production"
    }
};

// Create and configure GAgent through GAgentFactory
var myAgent = await gAgentFactory.GetGAgentAsync<IMyGAgent>(
    Guid.NewGuid(), 
    config  // Pass configuration
);

// GAgentFactory will automatically call ConfigAsync, which calls PerformConfigAsync
// PerformConfigAsync will trigger state updates through RaiseEvent
// GAgentTransitionState will apply these state changes
```

#### Why Use PerformConfigAsync?

1. **Correct Lifecycle**: PerformConfigAsync is called immediately after GAgent activation, making it the right time for initialization
2. **Configuration-Driven**: Pass initialization parameters through configuration objects, not hardcoding
3. **Testability**: Easily test GAgent behavior with different configurations
4. **Separation of Concerns**: Separate configuration logic from activation logic
5. **Event Sourcing Compliance**: All state changes are done through events, maintaining event sourcing integrity

#### Important Notes

- If you don't need configuration, you can continue using GAgentBase with two or three generic parameters
- In PerformConfigAsync, you must use RaiseEvent to modify state, not direct assignment
- All state initialization logic should be handled in GAgentTransitionState
- Configuration classes must inherit from `ConfigurationBase` and use `[GenerateSerializer]` and `[Id(n)]` attributes
- Remember to define corresponding state log events to carry configuration data

### 2.4 State Log Events
```csharp
[GenerateSerializer]
public abstract class MyStateLogEvent : StateLogEventBase<MyStateLogEvent>;

[GenerateSerializer]
public class MySpecificLogEvent : MyStateLogEvent
{
    [Id(0)] public string Details { get; set; } = string.Empty;
}
```

## Part 3: Override Methods

### 3.1 GAgentBase Override Methods
```csharp
// Use OnGAgentActivateAsync for initialization
protected override Task OnGAgentActivateAsync(CancellationToken cancellationToken)
{
    // Initialization logic
    return base.OnGAgentActivateAsync(cancellationToken);
}

// Use OnDeactivateAsync for cleanup
public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
{
    // Cleanup logic (e.g., release timers)
    _timer?.Dispose();
    return base.OnDeactivateAsync(reason, cancellationToken);
}

// Handle state transitions
protected override void GAgentTransitionState(TState state, StateLogEventBase<TStateLogEvent> @event)
{
    // State transition logic
}

// Other overridable methods
protected override Task HandleStateChangedAsync()
{
    Logger.LogInformation("State changed: {GrainId}", this.GetGrainId());
    return Task.CompletedTask;
}

protected override Task OnRegisterAgentAsync(GrainId agentGuid)
{
    Logger.LogInformation("Agent registered: {AgentId}", agentGuid);
    return Task.CompletedTask;
}
```

### 3.2 AIGAgentBase Override Methods
```csharp
// For AI GAgent, override AIGAgentTransitionState instead
protected override void AIGAgentTransitionState(TState state, StateLogEventBase<TStateLogEvent> @event)
{
    // AI-specific state transitions
    // Note: GAgentTransitionState is sealed in AIGAgentBase
}

protected override Task OnAIGAgentActivateAsync(CancellationToken cancellationToken)
{
    // AI GAgent initialization
    return Task.CompletedTask;
}
```

## Part 4: Event System

### 4.1 Event Publishing
```csharp
// Use PublishAsync to publish events
await PublishAsync(new MyEvent { /* properties */ });

// Publish to specific GAgent
await PublishAsync(targetGrainId, new MyEvent { /* properties */ });
```

### 4.2 Event Definition Pattern
```csharp
[GenerateSerializer]
public class MyEvent : EventBase
{
    [Id(0)] public string PropertyName { get; set; } = string.Empty;
}
```

### 4.3 Event Handler Types
```csharp
// Type 1: Attribute-based handlers
[EventHandler]
public async Task HandleMyEventAsync(MyEvent @event)
{
    Logger.LogInformation("Handling event: {EventId}", @event.Id);
}

// Type 2: All event handlers
[AllEventHandler(allowSelfHandling: false)]
public Task HandleAllEventsAsync(EventWrapperBase eventWrapper)
{
    Logger.LogDebug("Event received");
    return Task.CompletedTask;
}

// Type 3: Convention-based (method name must be "HandleEventAsync")
public async Task HandleEventAsync(MyConcreteEvent @event)
{
    // Automatically recognized as handler
}
```

### 4.4 Event Subscription
```csharp
// Subscribe to another GAgent's events
await SubscribeToAsync(otherGAgent);

// Unsubscribe
await UnsubscribeFromAsync(otherGAgent);

// Note: Event subscription is at GAgent level, not specific event type
```

### 4.5 Event Communication Setup
```csharp
// GAgents must be in the same group for event communication
var publisher = await GAgentFactory.GetGAgentAsync<IPublishingGAgent>();

// Establish parent-child relationship through RegisterAsync
await publisher.RegisterAsync(myAgent);
await publisher.RegisterAsync(otherAgent);

// Now they can communicate through events
await publisher.PublishAsync(new MyEvent());
```

## Part 5: Timer Registration

### 5.1 Stateless Timer Registration
```csharp
// Stateless parameter - lambda only accepts CancellationToken
_timer = this.RegisterGrainTimer(
    async (token) => await MyMethodAsync(),  // Note: single parameter
    new GrainTimerCreationOptions
    {
        DueTime = TimeSpan.FromSeconds(30),
        Period = TimeSpan.FromSeconds(30),
        Interleave = true
    }
);
```

### 5.2 Stateful Timer Registration
```csharp
// Stateful parameter - lambda accepts state and CancellationToken
_timer = this.RegisterGrainTimer(
    myStateObject,  // Pass state object
    async (state, token) => await MyMethodWithStateAsync(state),  // Two parameters
    new GrainTimerCreationOptions
    {
        DueTime = TimeSpan.FromSeconds(30),
        Period = TimeSpan.FromSeconds(30),
        Interleave = true
    }
);
```

### 5.3 Timer Cleanup
```csharp
public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
{
    _timer?.Dispose();
    return base.OnDeactivateAsync(reason, cancellationToken);
}
```

## Part 6: AI Integration (AIGAgentBase)

### 6.1 Brain System Integration
AIGAgentBase uses the Brain system through `IBrain` interface and `IBrainFactory` to manage AI functionality:

```csharp
// Brain is automatically initialized when InitializeAsync is called
public async Task<bool> InitializeAsync(InitializeDto initializeDto)
{
    // This will create and configure brain based on your LLM configuration
    var result = await base.InitializeAsync(initializeDto);
    
    // Tools are registered after brain initialization
    return result;
}
```

### 6.2 LLM Configuration
```csharp
// Use centralized configuration (recommended)
var initDto = new InitializeDto
{
    LLMConfig = new LLMConfigDto
    {
        SystemLLM = "gpt-4"  // Reference system-wide configuration
    },
    Instructions = "You are a helpful assistant"
};

// Or provide self-contained configuration
var initDto = new InitializeDto
{
    LLMConfig = new LLMConfigDto
    {
        SelfLLMConfig = new SelfLLMConfigDto
        {
            ProviderEnum = LLMProviderEnum.OpenAI,
            ModelIdEnum = ModelIdEnum.Gpt4,
            ApiKey = "your-api-key"
        }
    },
    Instructions = "You are a helpful assistant"
};
```

### 6.3 Tool Registration

#### MCP Tool Registration
MCP tools are registered through the `RegisterMCPToolsAsync` method:

```csharp
// During initialization
var initDto = new InitializeDto
{
    EnableMCPTools = true,
    MCPServers = new List<MCPServerConfig>
    {
        new MCPServerConfig
        {
            ServerName = "filesystem",
            Command = "npx",
            Arguments = new List<string> { "@modelcontextprotocol/server-filesystem", "/workspace" }
        }
    }
};

// MCP tools are automatically discovered and registered as Semantic Kernel functions
```

#### GAgent Tool Registration
GAgent tools allow AI agents to call other GAgents in the system:

```csharp
// During initialization
var initDto = new InitializeDto
{
    EnableGAgentTools = true,
    SelectedGAgents = new List<string> { "Calculator", "DataProcessor" },
    // ... other configuration
};

// RegisterGAgentsAsToolsAsync method will:
// 1. Use IGAgentService to discover available GAgents
// 2. Create Semantic Kernel functions for each GAgent event
// 3. Register them in the kernel through the Brain system
```

### 6.4 Tool Execution Flow
```csharp
// AI automatically calls tools during chat
var response = await aiGAgent.ChatAsync(new ChatRequestDto
{
    Prompt = "Calculate the sum of 5 and 10",  // AI will call Calculator GAgent
    ChatId = Guid.NewGuid().ToString()
});

// Tool calls are automatically tracked
// Each tool call includes timing, parameters, and results
```

### 6.5 Custom Tool Registration
```csharp
// If you need custom tools, you can access the kernel through reflection
private Kernel? GetKernelFromBrain()
{
    if (_brain == null) return null;
    
    var kernelProperty = _brain.GetType()
        .GetProperty("Kernel", BindingFlags.Public | BindingFlags.Instance);
    
    return kernelProperty?.GetValue(_brain) as Kernel;
}
```

## Part 7: GAgent Instantiation Rules

### 7.1 Always Use IGAgentFactory
```csharp
// ✅ Correct - Use IGAgentFactory
var gAgentFactory = ServiceProvider.GetRequiredService<IGAgentFactory>();
var agent = await gAgentFactory.GetGAgentAsync<IMyGAgent>(grainId);

// ❌ Error - Never use IGrainFactory directly
```

### 7.2 GAgent Attribute Rules
- Required: `[GAgent]` or `[GAgent("alias")]` or `[GAgent("alias", "namespace")]`
- No parameters: Use `type.Namespace + "." + type.Name`
- Alias only: Use `type.Namespace + "." + alias`
- Both provided: Use `namespace + "." + alias`

## Part 8: File Organization

```
src/
├── Events/                    # Shared events (EventBase derived)
│   ├── NotificationEvent.cs
│   ├── DataProcessingEvent.cs
│   └── CoordinationEvent.cs
└── GAgents/                   # GAgent implementations (all-in-one files)
    ├── MyGAgent.cs            # Contains: interface, state, state log events, implementation
    ├── NotificationGAgent.cs
    ├── ProcessingGAgent.cs
    └── CoordinatorGAgent.cs
```

**Best Practice**: Keep all GAgent-related code in a single file for better readability:
- Interface definition (IMyGAgent)
- State class (MyGAgentState)
- State log events (as nested classes)
- GAgent implementation (MyGAgent)

## Part 9: Performance Best Practices

1. **State Updates**: Batch state changes when possible
2. **Event Publishing**: Use appropriate event priorities
3. **Timer Intervals**: Choose reasonable intervals to avoid system overload
4. **Async Operations**: Always use async/await correctly
5. **Resource Cleanup**: Always release resources in OnDeactivateAsync

## Part 10: Documentation Requirements

1. Document all public interfaces
2. Add XML comments to GAgent interfaces
3. Explain complex state transitions
4. Document event flows between GAgents
5. Provide usage examples in comments

## Quick Implementation Checklist

### Basic Implementation
- [ ] Define interface first (IStateGAgent<TState> or IStateGAgent<TState> + IAIGAgent)
- [ ] **Event, State, and StateLogEvent must be defined as class (not record)**
- [ ] No constructor parameters
- [ ] Has [GAgent] attribute
- [ ] Implement GetDescriptionAsync()
- [ ] Use Logger property (not injected)
- [ ] Change state through RaiseEvent + ConfirmEvents
- [ ] Handle state transitions in GAgentTransitionState (or AIGAgentTransitionState for AI)
- [ ] Initialize collections in state
- [ ] All classes have [GenerateSerializer]
- [ ] All properties have [Id(n)]

### Instantiation
- [ ] Use IGAgentFactory (not IGrainFactory)
- [ ] GAgents registered in same group for event communication
- [ ] Correct usage of GetGAgentAsync method with Guid parameter