using System;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.Twitter.GAgents.ChatAIAgent;

[GenerateSerializer]
public class ChatAIGAgentEvent : StateLogEventBase<ChatAIGAgentEvent>
{
    // Base event for chat functionality
}

[GenerateSerializer]
public class ChatResponseEvent : ChatAIGAgentEvent
{
    [Id(0)]
    public string Response { get; set; } = "";
    
    [Id(1)]
    public DateTime Timestamp { get; set; }
}

[GenerateSerializer]
public class SetInitialPromptEvent : ChatAIGAgentEvent
{
    [Id(0)]
    public string? InitialPrompt { get; set; }
} 