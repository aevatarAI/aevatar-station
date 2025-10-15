# Frontend Compatibility Interfaces

## ⚠️ CRITICAL: These interfaces MUST NOT be changed during refactoring

These interfaces are consumed by the workflow frontend UI and any changes will break the frontend.

## 1. Core Agent Interfaces

### IInputGAgent
```csharp
namespace Aevatar.GAgents.InputGAgent.GAgent;

public interface IInputGAgent : IGAgent
{
    // ✅ PRESERVE: Frontend depends on this for workflow setup
    Task ConfigureWorkflowAsync(Guid workflowCoordinatorId, Guid workflowId);
}
```

### IChatAIGAgent  
```csharp
namespace Aevatar.GAgents.Twitter.GAgents.ChatAIAgent;

public interface IChatAIGAgent : IAIGAgent, IStateGAgent<ChatAIGAgentState>
{
    // ✅ PRESERVE: Frontend calls this for chat responses
    Task<string> GetLastResponseAsync();
    
    // ✅ PRESERVE: Frontend uses this for enhanced workflow setup
    Task ConfigureEnhancedWorkflowAsync(Guid workflowCoordinatorId, Guid workflowId, Guid blackboardId, List<Guid> inputAgentIds);
}
```

### IAIGAgent
```csharp
namespace Aevatar.GAgents.AIGAgent.Agent;

public interface IAIGAgent
{
    // ✅ PRESERVE: Frontend initializes AI agents
    Task<bool> InitializeAsync(InitializeDto dto);
    Task<bool> UploadKnowledge(List<BrainContentDto>? knowledgeList);
    
    // ✅ PRESERVE: Frontend manages LLM configurations
    Task<LLMConfig?> GetLLMConfigAsync();
    Task SetLLMConfigKeyAsync(string llmConfigKey);
    Task SetSystemLLMAsync(string systemLLM);
    Task SetLLMAsync(LLMConfig llmConfig, string? systemLLM);
    
    // ✅ PRESERVE: Frontend configures tools
    Task<bool> ConfigureMCPServersAsync(List<MCPServerConfig> servers);
    Task<List<MCPToolInfo>> GetAvailableMCPToolsAsync();
    Task<bool> ConfigureGAgentToolsAsync(List<GrainType> toolGAgentTypes);
}
```

## 2. Workflow Coordination Interfaces

### IWorkflowCoordinatorGAgent
```csharp
public interface IWorkflowCoordinatorGAgent : IStateGAgent<WorkflowCoordinatorState>
{
    // ✅ PRESERVE: Used for point-to-point workflow communication
    Task HandleWorkflowEventAsync(WorkflowLifecycleEvent workflowEvent);
}
```

### IWorkflowExecutionRecordGAgent
```csharp
namespace Aevatar.GAgents.GroupChat.Core;

public interface IWorkflowExecutionRecordGAgent : IStateGAgent<WorkflowExecutionRecordState>
{
    // ✅ PRESERVE: Frontend reads execution records
}
```

### IBlackboardGAgent
```csharp
public interface IBlackboardGAgent : IGAgent
{
    // ✅ PRESERVE: Frontend manages blackboard data
    Task<bool> SetTopic(string topic);
    Task<List<ChatMessage>> GetContent();
    Task<List<ChatMessage>> GetLastChatMessageAsync(List<Guid> talkerList);
    Task SetMessageAsync(CoordinatorConfirmChatResponse confirmChatResponse);
    Task ResetAsync();
}
```

### ICoordinatorGAgent
```csharp
public interface ICoordinatorGAgent : IGAgent
{
    // ✅ PRESERVE: Frontend starts coordination
    Task StartAsync(Guid blackboardId);
}
```

## 3. State Types (Frontend Dependencies)

### Key State Classes
- `ChatAIGAgentState` - Frontend reads chat AI state
- `WorkflowCoordinatorState` - Frontend monitors workflow status
- `WorkflowExecutionRecordState` - Frontend displays execution history
- `BlackboardState` - Frontend accesses blackboard data

### Key Configuration DTOs
- `InitializeDto` - Frontend initializes AI agents
- `LLMConfigDto` - Frontend configures LLM settings
- `ChatAIGAgentConfigDto` - Frontend configures chat agents
- `InputConfigDto` - Frontend configures input agents

### Key Event Types
- `WorkflowLifecycleEvent` - Workflow coordination
- `ChatMessage` - Chat communication
- `ChatResponse` - Chat responses

## 4. Core Base Interfaces (Orleans/Aevatar)

### From Aevatar.Core.Abstractions
- `IGAgent` - All agents implement this
- `IStateGAgent<TState>` - Stateful agents implement this
- `ConfigurationBase` - All configurations inherit this
- `StateBase` - All states inherit this
- `EventBase` - All events inherit this

## ⚠️ REFACTORING RULES

### ✅ SAFE TO CHANGE
- Internal implementation details
- Private/protected methods
- Inheritance hierarchy (as long as interfaces are preserved)
- State field IDs (Orleans handles inheritance properly)
- Event forwarding mechanisms
- Internal communication patterns

### 🚫 NEVER CHANGE
- Public interface method signatures
- Public interface names
- State class names that frontend references
- Configuration DTO field names/types
- Event class names/fields that frontend uses

### 🔄 CHANGE WITH CAUTION
- State field additions (add new fields, don't remove existing)
- Configuration additions (extend, don't break existing)
- New interface methods (add with default implementations)

## ✅ REFACTORING VALIDATION CHECKLIST

Before completing any refactoring:

1. **Interface Preservation**: Verify all interfaces above are unchanged
2. **State Compatibility**: Ensure state classes maintain same public properties  
3. **Configuration Compatibility**: Verify configuration DTOs are backward compatible
4. **Build Success**: All projects build without errors
5. **Method Signatures**: All public methods have same signatures
6. **Namespace Consistency**: No namespace changes for public interfaces

## 📝 CURRENT REFACTORING STATUS

✅ **COMPLETED SAFELY**:
- Inheritance hierarchy: `GAgentBase → BusinessAgentBase → AIGAgentBase`
- State ID numbering: All start from 0 (Orleans handles inheritance)
- Moved shared types to `Aevatar.Core.Abstractions`
- Preserved all public interfaces listed above

✅ **VERIFIED COMPATIBLE**:
- `IInputGAgent.ConfigureWorkflowAsync()` - ✅ Preserved
- `IChatAIGAgent.GetLastResponseAsync()` - ✅ Preserved  
- `IChatAIGAgent.ConfigureEnhancedWorkflowAsync()` - ✅ Preserved
- `IAIGAgent.InitializeAsync()` - ✅ Preserved
- `IWorkflowCoordinatorGAgent.HandleWorkflowEventAsync()` - ✅ Preserved
- All blackboard and coordination interfaces - ✅ Preserved

The refactoring has been completed **WITHOUT BREAKING** any frontend dependencies.
