using Orleans;

namespace Aevatar.GAgents.Telegram.Agent.GEvents;
[GenerateSerializer]
public class SendMessageSEvent : MessageSEvent
{
    [Id(1)] public string ChatId { get; set; }
    [Id(2)] public string ReplyMessageId { get; set; }
    [Id(3)] public string Message { get; set; } 
    [Id(4)] public string Photo { get; set; } 
    [Id(5)] public string Caption { get; set; } 
}