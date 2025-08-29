# PsiOmniGAgent - Brain-Based AI Agent

## Overview

PsiOmniGAgent is an advanced AI agent that operates using the AIGAgentBase Brain architecture. It no longer supports the legacy IKernelFactory mode and exclusively uses the Brain abstraction for all AI operations.

## Key Features

- **Brain-Based Architecture**: All AI operations are handled through the centralized Brain system
- **Automatic Brain Initialization**: Brain is automatically initialized during agent configuration
- **Multiple Operation Modes**: Supports Analyzer, Orchestrator, and Specialized modes
- **Hierarchical Agent Structure**: Can create and manage child agents

## Configuration

The agent now requires LLM configuration during initialization:

```csharp
var config = new PsiOmniGAgentConfig
{
    Depth = 0, // Agent depth in hierarchy
    LLMConfig = new LLMConfigDto
    {
        SystemLLM = "gpt-4" // Required: specify the system LLM to use
    }
};

await agent.ConfigureAsync(config);
```

If no LLMConfig is provided, the agent will default to using "gpt-4".

## Brain Initialization

The Brain is automatically initialized in the `PerformConfigAsync` method with:
- Default instructions for intelligent task analysis and coordination
- Streaming disabled by default
- GAgent and MCP tools disabled by default

## Usage Example

```csharp
// Create the agent
var agent = await gAgentFactory.GetGAgentAsync<IPsiOmniGAgent>();

// Configure with Brain support
var config = new PsiOmniGAgentConfig
{
    Depth = 0,
    LLMConfig = new LLMConfigDto
    {
        SystemLLM = "gpt-4" // or any other configured system LLM
    }
};

await agent.ConfigureAsync(config);

// Send a message
await agent.PublishEventAsync(new UserMessageEvent
{
    Content = "Your task here",
    CallId = Guid.NewGuid().ToString()
});
```

## Migration from Legacy Mode

If you were previously using PsiOmniGAgent without Brain configuration:

1. **Update Configuration**: Always provide `LLMConfig` in `PsiOmniGAgentConfig`
2. **Remove IKernelFactory**: The agent no longer uses or requires IKernelFactory
3. **Update Dependencies**: Ensure your project references the AIGAgentBase package

## Technical Details

### Kernel Access

The agent uses reflection to access the Brain's internal Kernel through the `GetBrainKernel()` method. This ensures compatibility with the Semantic Kernel while maintaining the Brain abstraction.

### Mode-Specific Kernels

All operation modes (Analyzer, Orchestrator, Specialized, Introspector) now get their kernels from the same Brain instance, ensuring consistent behavior across all modes.

## Troubleshooting

### "Cannot get kernel from brain" Error

This error occurs when the Brain is not properly initialized. Ensure:
1. You've provided a valid `LLMConfig` in the configuration
2. The specified `SystemLLM` exists in your system configuration
3. The agent's `ConfigureAsync` method has completed successfully

### Brain Not Initialized

The Brain is automatically initialized during `PerformConfigAsync`. If you encounter initialization issues:
1. Check your LLM configuration is valid
2. Verify the system LLM name matches your configuration
3. Check logs for any initialization errors
