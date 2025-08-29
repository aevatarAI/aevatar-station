# Aevatar.GAgents.AI.Abstractions

## Overview
The Aevatar.GAgents.AI.Abstractions module is a core library that provides essential abstractions for 
building AI Agents within the Aevatar ecosystem. This module defines key interfaces such as IBrain and 
IBrainFactory, along with related options, enabling developers to implement custom AI implementation. 
By leveraging these abstractions, developers can focus on creating specific AI behaviors while adhering to 
a consistent framework.

## Core Components

### IBrain
The IBrain interface defines the core functionality for an AI brain, including methods for processing inputs, 
generating outputs, and managing internal states. This interface serves as the foundation for implementing 
custom AI logic.

### IBrainFactory
The IBrainFactory interface provides a factory pattern for creating instances of IBrain. This allows for 
flexible and dynamic instantiation of AI brains based on runtime requirements.

### Options
The module includes various options classes that allow developers to configure AI behavior, such as
setting up model parameters settings.

## Usage

### Installation
To use the `Aevatar.GAgents.AI.Abstractions`, you need to add the NuGet package to your project.

```bash
Install-Package Aevatar.GAgents.AI.Abstractions
```

### Implementing IBrain
To create a custom AI Brain, implement the IBrain interface and implement the corresponding logic.
```csharp
public class MyCustomBrain : IBrain
{
    public async Task InitializeAsync(string id, string description)
    {
        
    }

    public async Task<bool> UpsertKnowledgeAsync(List<BrainContent>? files = null)
    {
        
    }

    public async Task<InvokePromptResponse?> InvokePromptAsync(string content, List<ChatMessage>? history = null, bool ifUseKnowledge = false)
    {
        
    }
}
```

### Implementing IBrainFactory
Create a BrainFactory instantiate Brain.

```csharp
public class BrainFactory : IBrainFactory
{
    private readonly IServiceProvider _serviceProvider;

    public BrainFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IBrain? GetBrain(string llm)
    {
        return _serviceProvider.GetRequiredKeyedService<IBrain>(llm);
    }
}
```

## License

Distributed under the MIT License. See [License](../../LICENSE) for more information.
