using Orleans;

namespace Aevatar.GAgents.Telegram.Agent.GEvents;
[GenerateSerializer]
public class ReceiveMessageSEvent : MessageSEvent
{
    [Id(0)]  public string MessageId { get; set; }
    [Id(1)]  public string ChatId { get; set; }
    [Id(2)] public string Message { get; set; }
    [Id(3)] public string NeedReplyBotName { get; set; }
}