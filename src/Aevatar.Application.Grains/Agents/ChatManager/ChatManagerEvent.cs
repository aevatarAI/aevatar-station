using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.SignalR;

namespace Aevatar.Application.Grains.Agents.ChatManager;

[GenerateSerializer]
public class RequestCreateQuantumChatEvent : EventBase
{
    [Id(0)] public string SystemLLM { get; set; }
    [Id(1)] public string Prompt { get; set; }
}

[GenerateSerializer]
public class ResponseCreateQuantum : ResponseToPublisherEventBase
{
    [Id(0)] public ResponseType ResponseType { get; set; } = ResponseType.CreateSession;
    [Id(1)] public Guid SessionId { get; set; }
}

[GenerateSerializer]
public class RequestQuantumChatEvent : EventBase
{
    [Id(0)] public Guid SessionId { get; set; }
    [Id(1)] public string SystemLLM { get; set; }
    [Id(2)] public string Content { get; set; }
}

[GenerateSerializer]
public class ResponseQuantumChat : ResponseToPublisherEventBase
{
    [Id(0)] public ResponseType ResponseType { get; set; } = ResponseType.ChatResponse;
    [Id(1)] public string Response { get; set; }
    [Id(2)] public string NewTitle { get; set; }
}

[GenerateSerializer]
public class RequestQuantumSessionListEvent : EventBase
{
}

[GenerateSerializer]
public class ResponseQuantumSessionList : ResponseToPublisherEventBase
{
    [Id(0)] public ResponseType ResponseType { get; set; } = ResponseType.SessionListResponse;
    [Id(1)] public List<SessionInfoDto> SessionList { get; set; }
}

[GenerateSerializer]
public class RequestSessionChatHistoryEvent : EventBase
{
    [Id(0)] public Guid SessionId { get; set; }
}

[GenerateSerializer]
public class ResponseSessionChatHistory : ResponseToPublisherEventBase
{
    [Id(0)] public ResponseType ResponseType { get; set; } = ResponseType.SessionChatHistory;
    [Id(1)] public List<ChatMessage> ChatHistory { get; set; }
}

[GenerateSerializer]
public class RequestDeleteSessionEvent : EventBase
{
    [Id(0)] public Guid SessionId { get; set; }
}

[GenerateSerializer]
public class ResponseDeleteSession : ResponseToPublisherEventBase
{
    [Id(0)] public ResponseType ResponseType { get; set; } = ResponseType.SessionDelete;
    [Id(1)] public bool IfSuccess { get; set; }
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
    [Id(0)] public ResponseType ResponseType { get; set; } = ResponseType.SessionRename;
    [Id(1)] public Guid SessionId { get; set; }
    [Id(2)] public string Title { get; set; }
}

[GenerateSerializer]
public enum ResponseType
{
    CreateSession = 1,
    ChatResponse = 2,
    SessionListResponse = 3,
    SessionChatHistory = 4,
    SessionDelete = 5,
    SessionRename = 6,
}