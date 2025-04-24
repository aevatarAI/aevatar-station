using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.SignalR;


namespace Aevatar.Application.Grains.Agents.ChatManager;

[GenerateSerializer]
public class RequestCreateGodChatEvent : EventBase
{
    [Id(0)] public string SystemLLM { get; set; }
    [Id(1)] public string Prompt { get; set; }
    [Id(2)] public UserProfileDto UserProfile { get; set; }
}

[GenerateSerializer]
public class ResponseCreateGod : ResponseToPublisherEventBase
{
    [Id(0)] public ResponseType ResponseType { get; set; } = ResponseType.CreateSession;
    [Id(1)] public Guid SessionId { get; set; }
    [Id(2)] public String SessionVersion { get; set; }
}

[GenerateSerializer]
public class RequestGodChatEvent : EventBase
{
    [Id(0)] public Guid SessionId { get; set; }
    [Id(1)] public string SystemLLM { get; set; }
    [Id(2)] public string Content { get; set; }
}

[GenerateSerializer]
public class ResponseGodChat : ResponseToPublisherEventBase
{
    [Id(0)] public ResponseType ResponseType { get; set; } = ResponseType.ChatResponse;
    [Id(1)] public string Response { get; set; }
    [Id(2)] public string NewTitle { get; set; }
}

[GenerateSerializer]
public class RequestStreamGodChatEvent : EventBase
{
    [Id(0)] public Guid SessionId { get; set; }
    [Id(1)] public string SystemLLM { get; set; }
    [Id(2)] public string Content { get; set; }
}

[GenerateSerializer]
public class ResponseStreamGodChat : ResponseToPublisherEventBase
{
    [Id(0)] public ResponseType ResponseType { get; set; } = ResponseType.ChatResponse;
    [Id(1)] public string Response { get; set; }
    [Id(2)] public string NewTitle { get; set; }
    [Id(3)] public string ChatId { get; set; }
    [Id(4)] public bool IsLastChunk { get; set; }

    [Id(5)] public int SerialNumber { get; set; }

    [Id(6)] public Guid SessionId { get; set; }

    public ResponseStreamGodChatForHttp ConvertToHttpResponse()
    {
        return new ResponseStreamGodChatForHttp()
        {
            Response = Response,
            ChatId = ChatId,
            IsLastChunk = IsLastChunk,
            SerialNumber = SerialNumber,
            SessionId = SessionId,
        };
    }
}

public class ResponseStreamGodChatForHttp
{
    public ResponseType ResponseType { get; set; } = ResponseType.ChatResponse;
    public string Response { get; set; }
    public string ChatId { get; set; }
    public bool IsLastChunk { get; set; }
    public int SerialNumber { get; set; }
    public int SerialChunk { get; set; }
    public Guid SessionId { get; set; }
}

[GenerateSerializer]
public class RequestGodSessionListEvent : EventBase
{
}

[GenerateSerializer]
public class ResponseGodSessionList : ResponseToPublisherEventBase
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
public class RequestClearAllEvent : EventBase
{
}

[GenerateSerializer]
public class ResponseClearAll : ResponseToPublisherEventBase
{
    [Id(0)] public ResponseType ResponseType { get; set; } = ResponseType.ClearAll;
    [Id(1)] public bool Success { get; set; }
}

[GenerateSerializer]
public class RequestSetUserProfileEvent : EventBase
{
    [Id(0)] public string Gender { get; set; }
    [Id(1)] public DateTime BirthDate { get; set; }
    [Id(2)] public string BirthPlace { get; set; }
    [Id(3)] public string FullName { get; set; }
}

[GenerateSerializer]
public class ResponseSetUserProfile : ResponseToPublisherEventBase
{
    [Id(0)] public ResponseType ResponseType { get; set; } = ResponseType.SetUserProfile;
    [Id(1)] public bool Success { get; set; }
}

[GenerateSerializer]
public class RequestGetUserProfileEvent : EventBase
{
}

[GenerateSerializer]
public class ResponseGetUserProfile : ResponseToPublisherEventBase
{
    [Id(0)] public ResponseType ResponseType { get; set; } = ResponseType.GetUserProfile;
    [Id(1)] public string Gender { get; set; }
    [Id(2)] public DateTime BirthDate { get; set; }
    [Id(3)] public string BirthPlace { get; set; }
    [Id(4)] public string FullName { get; set; }
}

[GenerateSerializer]
public class UserProfileDto
{
    [Id(0)] public string Gender { get; set; }
    [Id(1)] public DateTime BirthDate { get; set; }
    [Id(2)] public string BirthPlace { get; set; }
    [Id(3)] public string FullName { get; set; }
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
    SetUserProfile = 8,
    GetUserProfile = 9
}