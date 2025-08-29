# AIGAgentBase Reference Documentation

## Overview

`AIGAgentBase` is the foundational abstract class for creating AI-powered GAgents (Grain Agents) in the Aevatar framework. It extends the core `GAgentBase` functionality with AI capabilities powered by Semantic Kernel, enabling intelligent agents that can process natural language, utilize tools, and maintain conversational state.

## Architecture

### Class Hierarchy

```
GAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>
    └── AIGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>
```

The class uses generic type parameters:
- `TState`: The state type, must inherit from `AIGAgentStateBase`
- `TStateLogEvent`: Event log type for state transitions, must inherit from `StateLogEventBase<TStateLogEvent>`
- `TEvent`: Event type for the agent, must inherit from `EventBase`
- `TConfiguration`: Configuration type, must inherit from `ConfigurationBase`

### Core Components

1. **Brain System**: Powered by `IBrain` interface, manages the AI model and Semantic Kernel integration
2. **Tool System**: Supports both GAgent tools and MCP (Model Context Protocol) tools
3. **State Management**: Event-sourced state management for persistence and recovery
4. **Streaming Support**: Real-time streaming responses for better user experience

## Key Features

### 1. AI Model Integration

AIGAgentBase supports multiple LLM providers through the `LLMConfig` system:

```csharp
public class LLMConfig
{
    public LLMProviderEnum ProviderEnum { get; set; }
    public ModelIdEnum ModelIdEnum { get; set; }
    public string ModelName { get; set; }
    public string ApiKey { get; set; }
    public string Endpoint { get; set; }
    public Dictionary<string, object>? Memo { get; set; }
}
```

Configuration can be:
- **Centralized**: Reference a system-wide LLM configuration by key
- **Self-provided**: Directly specify LLM configuration

### 2. Tool Systems

#### GAgent Tools

GAgent tools allow AI agents to invoke other GAgents in the system, enabling complex multi-agent interactions.

**Key Components:**
- `IGAgentService`: Discovers available GAgents and their event handlers
- `IGAgentExecutor`: Executes GAgent event handlers
- `GAgentToolPlugin`: Base plugin providing utility functions

**Registration Process:**
1. Discovery: `IGAgentService.GetAllAvailableGAgentInformation()` finds all GAgents
2. Function Creation: Each GAgent event becomes a Semantic Kernel function
3. Parameter Mapping: Event properties are mapped to function parameters
4. Execution Tracking: All tool calls are tracked with timing and results

**Example GAgent Tool Usage:**
```csharp
// During initialization
var initDto = new InitializeDto
{
    EnableGAgentTools = true,
    AllowedGAgentTypes = new List<string> { "Calculator", "DataProcessor" },
    // ... other configuration
};

// The AI can then call these GAgents naturally:
// "Please calculate the sum of 5 and 10 using the Calculator"
```

#### MCP Tools

MCP (Model Context Protocol) tools enable integration with external services and tools through a standardized protocol.

**Key Components:**
- `IMCPGAgent`: Interface for MCP server agents
- `MCPToolInfo`: Tool metadata including parameters
- `MCPParameterInfo`: Parameter definitions with types and requirements

**MCP Tool Structure:**
```csharp
public class MCPToolInfo
{
    public string Name { get; set; }
    public string Description { get; set; }
    public Dictionary<string, MCPParameterInfo> Parameters { get; set; }
    public string ServerName { get; set; }
}

public class MCPParameterInfo
{
    public string Name { get; set; }
    public string Type { get; set; }
    public string Description { get; set; }
    public bool Required { get; set; }
    public object? DefaultValue { get; set; }
}
```

**Configuration:**
```csharp
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
```

### 3. State Management

The `AIGAgentStateBase` maintains:

```csharp
public abstract class AIGAgentStateBase : StateBase
{
    // LLM Configuration
    public LLMConfig? LLM { get; set; }
    public string? SystemLLM { get; set; }
    public string? LLMConfigKey { get; set; }
    
    // AI Settings
    public string PromptTemplate { get; set; }
    public bool IfUpsertKnowledge { get; set; }
    
    // Token Usage Tracking
    public int InputTokenUsage { get; set; }
    public int OutTokenUsage { get; set; }
    public int TotalTokenUsage { get; set; }
    
    // Streaming Configuration
    public bool StreamingModeEnabled { get; set; }
    public StreamingConfig StreamingConfig { get; set; }
    
    // Tool Configuration
    public bool EnableGAgentTools { get; set; }
    public List<string> RegisteredGAgentFunctions { get; set; }
    public List<string>? AllowedGAgentTypes { get; set; }
    public bool EnableMCPTools { get; set; }
    public List<string> RegisteredMCPFunctions { get; set; }
    
    // Tool State
    public Dictionary<string, MCPGAgentReference> MCPAgents { get; set; }
    public List<GrainType> SelectedGAgents { get; set; }
}
```

### 4. Tool Call Tracking

AIGAgentBase provides comprehensive tracking of all tool invocations:

```csharp
public class ToolCallDetail
{
    public string ToolName { get; set; }
    public string ServerName { get; set; }
    public Dictionary<string, object> Arguments { get; set; }
    public string Timestamp { get; set; }
    public long DurationMs { get; set; }
    public bool Success { get; set; }
    public string Result { get; set; }
}
```

## Initialization Flow

1. **Basic Initialization**
   ```csharp
   await agent.InitializeAsync(new InitializeDto
   {
       Instructions = "You are a helpful assistant",
       LLMConfig = new LLMConfigDto { SystemLLM = "gpt-4" },
       StreamingModeEnabled = true,
       EnableGAgentTools = true,
       EnableMCPTools = true
   });
   ```

2. **Brain Initialization**
   - Creates IBrain instance via IBrainFactory
   - Initializes Semantic Kernel
   - Sets up system prompts

3. **Tool Registration**
   - If `EnableGAgentTools`: Discovers and registers GAgent functions
   - If `EnableMCPTools`: Connects to MCP servers and registers tools

4. **State Persistence**
   - All configuration changes are persisted through event sourcing
   - State can be recovered on grain reactivation

## Key Methods

### Initialization
- `InitializeAsync(InitializeDto)`: Main initialization method
- `UploadKnowledge(List<BrainContentDto>)`: Upload knowledge base content

### Tool Management
- `RegisterGAgentsAsToolsAsync()`: Register all available GAgents as tools
- `ConfigureGAgentToolsAsync(List<GrainType>)`: Configure specific GAgent tools
- `ConfigureMCPServersAsync(List<MCPServerConfig>)`: Configure MCP servers
- `UpdateKernelWithAllToolsAsync()`: Refresh all registered tools

### Chat Operations
- `ChatWithHistory(...)`: Main chat method with history support
- `InvokePromptStreamingAsync(...)`: Streaming chat responses

### Configuration
- `SetLLMConfigKeyAsync(string)`: Update LLM configuration reference
- `SetSystemLLMAsync(string)`: Set system LLM
- `SetLLMAsync(LLMConfig, string?)`: Set custom LLM configuration

## Event System

AIGAgentBase uses event sourcing for state management. Key events include:

- `SetLLMStateLogEvent`: LLM configuration changes
- `SetPromptTemplateStateLogEvent`: System prompt updates
- `SetStreamingConfigStateLogEvent`: Streaming configuration
- `SetEnableGAgentToolsStateLogEvent`: GAgent tool enablement
- `SetEnableMCPToolsStateLogEvent`: MCP tool enablement
- `TokenUsageStateLogEvent`: Token usage tracking

## Supporting Services

### IGAgentService

Provides GAgent discovery and information:

```csharp
public interface IGAgentService
{
    Task<Dictionary<GrainType, List<Type>>> GetAllAvailableGAgentInformation();
    Task<GAgentDetailInfo> GetGAgentDetailInfoAsync(GrainType grainType);
    Task<List<GrainType>> FindGAgentsByEventTypeAsync(Type eventType);
}
```

### IGAgentExecutor

Executes GAgent event handlers:

```csharp
public interface IGAgentExecutor
{
    Task<string> ExecuteGAgentEventHandler(IGAgent gAgent, EventBase @event);
    Task<string> ExecuteGAgentEventHandler(GrainId grainId, EventBase @event);
    Task<string> ExecuteGAgentEventHandler(GrainType grainType, EventBase @event);
}
```

## Best Practices

1. **Tool Selection**: Use `AllowedGAgentTypes` to limit which GAgents can be called
2. **Error Handling**: Implement proper error handling in event handlers
3. **Token Management**: Monitor token usage through state tracking
4. **Streaming**: Enable streaming for better user experience with long responses
5. **State Persistence**: Use event sourcing for all state changes

## Example Implementation

```csharp
public class MyAIAgent : AIGAgentBase<MyAIAgentState, MyStateLogEvent>
{
    protected override async Task OnAIGAgentActivateAsync(CancellationToken cancellationToken)
    {
        // Custom activation logic
        await base.OnAIGAgentActivateAsync(cancellationToken);
    }
    
    protected override void AIGAgentTransitionState(MyAIAgentState state, StateLogEventBase<MyStateLogEvent> @event)
    {
        // Handle custom state transitions
    }
}

public class MyAIAgentState : AIGAgentStateBase
{
    // Add custom state properties
}
```

## Performance Considerations

1. **Tool Discovery**: GAgent discovery happens once during initialization
2. **Function Caching**: Kernel functions are created once and reused
3. **State Updates**: Use batched events when possible
4. **Streaming Buffer**: Configure appropriate buffer size for streaming

## Troubleshooting

1. **Tool Registration Failures**: Check logs for specific GAgent/MCP errors
2. **LLM Connection Issues**: Verify API keys and endpoints
3. **State Recovery**: Ensure all events are properly serializable
4. **Tool Execution Errors**: Check tool parameter types match event properties