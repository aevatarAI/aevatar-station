using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.SignalR;
using Nest;

namespace Aevatar.Application.Grains.Agents.ChatGAgentManager;

[GenerateSerializer]
public class RequestQuantumChatEvent:EventBase
{
    [Id(0)] public Guid SessionId { get; set; }
    [Id(1)] public string SystemLLM { get; set; }
    [Id(2)] public string Content { get; set; } 
}

[GenerateSerializer]
public class ResponseQuantumChat : ResponseToPublisherEventBase
{
    [Id(0)] public string Response { get; set; }
}

[GenerateSerializer]
public class RequestQuantumSessionListEvent : EventBase
{
    
}

[GenerateSerializer]
public class ResponseQuantumSessionList : ResponseToPublisherEventBase
{
    [Id(0)] public List<SessionInfoDto> SessionList { get; set; } 
}

[GenerateSerializer]
public class RequestSessionChatHistoryEvent : EventBase
{
    [Id(0)] public Guid SessionId { get; set; }
}

[GenerateSerializer]
public class ResponseSessionChatHistory : ResponseToPublisherEventBase
{
    [Id(0)] public List<ChatMessage> ChatHistory { get; set; }
}

[GenerateSerializer]
public class RequestDeleteSessionEvent : EventBase
{
    [Id(0)] public Guid SessionId { get; set; }
}

[GenerateSerializer]
public class ResponseDeleteSession : ResponseToPublisherEventBase
{
    [Id(0)] public bool IfSuccess { get; set; }
}

[GenerateSerializer]
public class RequestRenameSessionEvent : EventBase
{
    [Id(0)] public Guid SessionId { get; set; }
    [Id(1)] public string Title { get; set; }
}

[GenerateSerializer]
public class ResponseRenameSession : ResponseToPublisherEventBase
{
    
    [Id(0)] public Guid SessionId { get; set; }
    [Id(1)] public string Title { get; set; }
}