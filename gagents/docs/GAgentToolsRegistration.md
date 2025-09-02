# GAgent Tools Registration in AIGAgentBase

## Overview

Starting from the latest version, AIGAgentBase has changed its default behavior for GAgent tool registration. Previously, when `EnableGAgentTools` was set to true, all available GAgent tools were automatically registered. Now, the system follows a more explicit and controlled approach.

## Default Behavior

By default, **no GAgent tools are registered automatically**, even when `EnableGAgentTools` is true. You must explicitly specify which GAgent tools you want to use.

## Registration Options

### 1. During Initialization

You can specify GAgent tools during agent initialization:

```csharp
var initDto = new InitializeDto
{
    LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" },
    Instructions = "You are a helpful assistant",
    EnableGAgentTools = true,
    SelectedGAgents = new List<GrainType>
    {
        GrainType.Create("demo/mathgagent"),
        GrainType.Create("demo/timegagent")
    }
};

await agent.InitializeAsync(initDto);
```

### 2. After Initialization

You can configure GAgent tools at any time after initialization:

```csharp
// Configure specific GAgent tools
var selectedGAgents = new List<GrainType>
{
    GrainType.Create("demo/mathgagent"),
    GrainType.Create("demo/weathergagent")
};

await agent.ConfigureGAgentToolsAsync(selectedGAgents);
```

### 3. Register All Available GAgent Tools

If you want the old behavior of registering all available GAgent tools:

```csharp
await agent.RegisterAllGAgentToolsAsync();
```

### 4. Clear GAgent Tools

To remove all registered GAgent tools:

```csharp
await agent.ClearGAgentToolsAsync();
```

## Best Practices

1. **Be Selective**: Only register the GAgent tools you actually need
2. **Dynamic Registration**: You can change registered tools during runtime
3. **Error Handling**: Always check return values from registration methods
4. **State Persistence**: Tool configuration is persisted automatically