# Aevatar.GAgents.SemanticKernel

## Overview
The Aevatar.GAgents.SemanticKernel module is a Semantic Kernel implementation for Aevatar.GAgents.AI.Abstractions. 
It provides seamless integration with cutting-edge AI services, including Azure OpenAI, Azure AI Inference, 
and Gemini. Leveraging Semantic Kernel, developers can effortlessly create AI GAgents equipped with advanced 
capabilities like semantic understanding, context awareness, and dynamic prompt generation.

## Core Components
### Brain
BrainBase：The base class for all Brain implementations, defining common interfaces and behaviors.

The module also implements the following Brains:
- AzureOpenAIBrain
- AzureAIInferenceBrain
- GeminiBrain

### BrainFactory
BrainFactory is a factory class used to dynamically create and return different types of Brain instances based on configuration.

## Usage

### Installation
To use the `Aevatar.GAgents.SemanticKernel`, you need to add the NuGet package to your project.

```bash
Install-Package Aevatar.GAgents.SemanticKernel
```

### Using Brain in GAgent
The following example demonstrates how to use Brain for interaction within a GAgent:

```csharp
public class AIGAgent : GAgentBase<AIState, AIStateLogEvent>, IAIGAgent
{
    private readonly IBrainFactory _brainFactory;
   
    public AIGAgent()
    {
        _brainFactory = ServiceProvider.GetRequiredService<IBrainFactory>();
    }
   
    public async Task<List<ChatMessage>?> ChatAsync(string LLM string prompt, List<ChatMessage>? history = null)
    {
        var brain = _brainFactory.GetBrain(LLM);
        await brain.InitializeAsync(grainId, description);
        var invokeResponse = await brain.InvokePromptAsync(prompt, history, false);
        return invokeResponse.ChatReponseList;
    }
}
```

### Register Configurations and Services
Register the required configurations and services in Startup.cs or Program.cs:

```csharp
var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<AzureOpenAIConfig>(context.Configuration.GetSection("AIServices:AzureOpenAI"));
        services.Configure<AzureAIInferenceConfig>(context.Configuration.GetSection("AIServices:AzureAIInference"));
        services.Configure<AzureOpenAIEmbeddingsConfig>(context.Configuration.GetSection("AIServices:AzureOpenAIEmbeddings"));
        services.Configure<GeminiConfig>(context.Configuration.GetSection("AIServices:Gemini"));
        services.Configure<QdrantConfig>(context.Configuration.GetSection("VectorStores:Qdrant"));
        services.Configure<RagConfig>(context.Configuration.GetSection("Rag"));
        
        services.AddSemanticKernel()
            .AddAzureOpenAI()
            .AddQdrantVectorStore()
            .AddAzureOpenAITextEmbedding()
            .AddAzureAIInference()
            .AddGemini();
    });

using var host = builder.Build();
await host.RunAsync();
```

### Configuration
Configure the relevant parameters for AI services in appsettings.json:

```json
{
  "AIServices": {
    "AzureOpenAI": {
      "Endpoint": "",
      "ChatDeploymentName": "",
      "ApiKey": ""
    },
    "AzureAIInference": {
      "Endpoint": "",
      "ChatDeploymentName": "",
      "ApiKey": ""
    },
    "AzureOpenAIEmbeddings": {
      "Endpoint": "",
      "DeploymentName": "",
      "ApiKey": ""
    },
    "Gemini": {
      "ModelId": "",
      "ApiKey": ""
    }
  },
  "VectorStores": {
    "Qdrant": {
      "Host": "localhost",
      "Port": 6334,
      "Https": false,
      "ApiKey": ""
    }
  },
  "Rag": {
    "AIEmbeddingService": "AzureOpenAIEmbeddings",
    "VectorStoreType": "Qdrant",
    "DataLoadingBatchSize": 10,
    "DataLoadingBetweenBatchDelayInMilliseconds": 1000,
    "MaxChunkCount": 500
  }
}
```

## License

Distributed under the MIT License. See [License](../../LICENSE) for more information.




