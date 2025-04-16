using Aevatar.Core.Abstractions;

namespace Aevatar.Application.Grains.Agents.ChatManager.Chat;

[GenerateSerializer]
public class RenameChatTitleEvent : EventBase
{
    [Id(0)] public Guid SessionId { get; set; }
    [Id(1)] public string Title { get; set; }
}


[GenerateSerializer]
public class RequestStreamChatEvent : EventBase
{
    [Id(0)] public Guid SessionId { get; set; }
    [Id(1)] public string SystemLLM { get; set; }
    [Id(2)] public string Content { get; set; }
    
}