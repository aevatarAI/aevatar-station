using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.SignalR;
using Orleans.Concurrency;

namespace Aevatar.Application.Grains.Agents.ChatManager;

[GenerateSerializer]
[Immutable]
public class RequestCreateGodChatEvent : EventBase
{
    [Id(0)] public string SystemLLM { get; set; }
    [Id(1)] public string Prompt { get; set; }
}

[GenerateSerializer]
[Immutable]
public class ResponseCreateGod : ResponseToPublisherEventBase
{
    [Id(0)] public ResponseType ResponseType { get; set; } = ResponseType.CreateSession;
    [Id(1)] public Guid SessionId { get; set; }
}

[GenerateSerializer]
[Immutable]
public class RequestGodChatEvent : EventBase
{
    [Id(0)] public Guid SessionId { get; set; }
    [Id(1)] public string SystemLLM { get; set; }
    [Id(2)] public string Content { get; set; }
}

[GenerateSerializer]
[Immutable]
public class ResponseGodChat : ResponseToPublisherEventBase
{
    [Id(0)] public ResponseType ResponseType { get; set; } = ResponseType.ChatResponse;
    [Id(1)] public string Response { get; set; }
    [Id(2)] public string NewTitle { get; set; }
}

[GenerateSerializer]
[Immutable]
public class RequestStreamGodChatEvent : EventBase
{
    [Id(0)] public Guid SessionId { get; set; }
    [Id(1)] public string SystemLLM { get; set; }
    [Id(2)] public string Content { get; set; }
    
}

[GenerateSerializer]
[Immutable]
public class ResponseStreamGodChat : ResponseToPublisherEventBase
{
    [Id(0)] public ResponseType ResponseType { get; set; } = ResponseType.ChatResponse;
    [Id(1)] public string Response { get; set; }
    [Id(2)] public string NewTitle { get; set; }
    [Id(3)] public string ChatId { get; set; }
    [Id(4)] public bool IsLastChunk { get; set; }
    
    [Id(5)]
    public int SerialNumber { get; set; }
}

[GenerateSerializer]
[Immutable]
public class RequestGodSessionListEvent : EventBase
{
}

[GenerateSerializer]
[Immutable]
public class ResponseGodSessionList : ResponseToPublisherEventBase
{
    [Id(0)] public ResponseType ResponseType { get; set; } = ResponseType.SessionListResponse;
    [Id(1)] public List<SessionInfoDto> SessionList { get; set; }
}

[GenerateSerializer]
[Immutable]
public class RequestSessionChatHistoryEvent : EventBase
{
    [Id(0)] public Guid SessionId { get; set; }
}

[GenerateSerializer]
[Immutable]
public class ResponseSessionChatHistory : ResponseToPublisherEventBase
{
    [Id(0)] public ResponseType ResponseType { get; set; } = ResponseType.SessionChatHistory;
    [Id(1)] public List<ChatMessage> ChatHistory { get; set; }
}

[GenerateSerializer]
[Immutable]
public class RequestDeleteSessionEvent : EventBase
{
    [Id(0)] public Guid SessionId { get; set; }
}

[GenerateSerializer]
[Immutable]
public class ResponseDeleteSession : ResponseToPublisherEventBase
{
    [Id(0)] public ResponseType ResponseType { get; set; } = ResponseType.SessionDelete;
    [Id(1)] public bool IfSuccess { get; set; }
}

[GenerateSerializer]
[Immutable]
public class RequestRenameSessionEvent : EventBase
{
    [Id(0)] public Guid SessionId { get; set; }
    [Id(1)] public string Title { get; set; }
}

[GenerateSerializer]
[Immutable]
public class ResponseRenameSession : ResponseToPublisherEventBase
{
    [Id(0)] public ResponseType ResponseType { get; set; } = ResponseType.SessionRename;
    [Id(1)] public Guid SessionId { get; set; }
    [Id(2)] public string Title { get; set; }
}

[GenerateSerializer]
[Immutable]
public class RequestClearAllEvent : EventBase
{
}

[GenerateSerializer]
[Immutable]
public class ResponseClearAll : ResponseToPublisherEventBase
{
    [Id(0)] public ResponseType ResponseType { get; set; } = ResponseType.ClearAll;
    [Id(1)] public bool Success { get; set; }
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
    ClearAll = 7,
}