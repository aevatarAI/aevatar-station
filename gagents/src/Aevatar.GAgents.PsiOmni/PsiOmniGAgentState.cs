using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.PsiOmni.Models;
using GroupChat.GAgent.GEvent;

namespace Aevatar.GAgents.PsiOmni;

public enum RealizationStatus
{
    Unrealized,
    Orchestrator,
    Specialized
}

[Serializable]
[GenerateSerializer]
public class PsiOmniGAgentState : GroupMemberState
{
    [Id(0)] public string Name { get; set; } = string.Empty;
    [Id(1)] public int Depth { get; set; } = 0;
    [Id(2)] public string AgentId { get; set; } = string.Empty;
    [Id(3)] public RealizationStatus RealizationStatus { get; set; } = RealizationStatus.Unrealized;
    [Id(4)] public List<ToolDefinition> Tools { get; set; } = new();
    [Id(5)] public Dictionary<string, AgentDescriptor> ChildAgents { get; set; } = new();
    [Id(6)] public string Description { get; set; } = string.Empty;
    [Id(7)] public List<AgentExample> Examples { get; set; } = new();
    [Id(8)] public AgentConfiguration? Configuration { get; set; }
    [Id(9)] public string UserAgentId { get; set; } = string.Empty;
    [Id(10)] public string CallId { get; set; } = string.Empty;
    [Id(11)] public List<PsiOmniChatMessage> ChatHistory { get; set; } = new();
    [Id(12)] public List<TodoItem> TodoList { get; set; } = new();
    [Id(13)] public FramedTask CurrentTask { get; set; } = new();
    [Id(14)] public Dictionary<string, Artifact> Artifacts { get; set; } = new();
    [Id(15)] public string DraftResponse { get; set; } = string.Empty;
    [Id(16)] public int IterationCount { get; set; }
}

[GenerateSerializer]
public class PsiOmniGAgentStateLogEvent : StateLogEventBase<PsiOmniGAgentStateLogEvent>
{
    [Id(0)] public string UniqueId { get; set; } = Guid.NewGuid().ToString();
}

[GenerateSerializer]
public class InitializeEvent : PsiOmniGAgentStateLogEvent
{
    [Id(0)] public string ParentId { get; set; } = string.Empty;
    [Id(1)] public string Name { get; set; } = string.Empty;
    [Id(2)] public int Depth { get; set; } = 0;
    [Id(3)] public string Description { get; set; } = string.Empty;
    [Id(4)] public string Examples { get; set; } = string.Empty;
}

[GenerateSerializer]
public class UpdateSendConfigEvent : PsiOmniGAgentStateLogEvent
{
    [Id(0)] public AgentConfigEvent Event { get; set; } = new();
}

[GenerateSerializer]
public class ReceiveUserMessageEvent : PsiOmniGAgentStateLogEvent
{
    [Id(0)] public UserMessageEvent Event { get; set; } = new();
}

[GenerateSerializer]
public class ReceiveAgentMessageEvent : PsiOmniGAgentStateLogEvent
{
    [Id(0)] public AgentMessageEvent Event { get; set; } = new();
}

[GenerateSerializer]
public class NewAgentsCreatedEvent : PsiOmniGAgentStateLogEvent
{
    [Id(0)] public List<AgentDescriptor> NewAgents { get; set; } = new();
}

[GenerateSerializer]
public class UpdateChildEvent : PsiOmniGAgentStateLogEvent
{
    [Id(0)] public AgentDescriptor LastChildDescriptor { get; set; } = new();
}

[GenerateSerializer]
public class GrowChatHistoryEvent : PsiOmniGAgentStateLogEvent
{
    [Id(0)] public List<PsiOmniChatMessage> NewMessages { get; set; } = new();
}

[GenerateSerializer]
public class RealizationEvent : PsiOmniGAgentStateLogEvent
{
    [Id(0)] public RealizationStatus RealizationStatus { get; set; } = RealizationStatus.Unrealized;
    [Id(1)] public string Description { get; set; } = string.Empty;
    [Id(2)] public List<ToolDefinition> Tools { get; set; } = new();
}

[GenerateSerializer]
public class UpdateSelfDescription : PsiOmniGAgentStateLogEvent
{
    [Id(0)] public string Description { get; set; } = string.Empty;
}

[GenerateSerializer]
public class AddNewAgent : PsiOmniGAgentStateLogEvent
{
    [Id(0)] public AgentDescriptor NewAgent { get; set; } = new();
}

[GenerateSerializer]
public class CallAgent : PsiOmniGAgentStateLogEvent
{
    [Id(0)] public AgentCall AgentCall { get; set; } = new();
}

[GenerateSerializer]
public class WriteArtifact : PsiOmniGAgentStateLogEvent
{
    [Id(0)] public string Name { get; set; } = string.Empty;
    [Id(1)] public string Format { get; set; } = string.Empty;
    [Id(2)] public string Content { get; set; } = string.Empty;
}

[GenerateSerializer]
public class WriteTask : PsiOmniGAgentStateLogEvent
{
    [Id(0)] public FramedTask Task { get; set; } = new();
}

[GenerateSerializer]
public class WriteDraftResponse : PsiOmniGAgentStateLogEvent
{
    [Id(0)] public string DraftResponse { get; set; } = string.Empty;
}

[GenerateSerializer]
public class UpdateTodoList : PsiOmniGAgentStateLogEvent
{
    [Id(0)] public List<TodoItem> AddedTodos { get; set; } = new();
    [Id(1)] public List<string> RemovedTodoIds { get; set; } = new();
}

[GenerateSerializer]
public class IterateEvent : PsiOmniGAgentStateLogEvent
{
    [Id(0)] public string Comment { get; set; } = string.Empty;
}