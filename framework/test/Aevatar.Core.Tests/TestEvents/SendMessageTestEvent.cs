using Aevatar.Core.Abstractions;

namespace Aevatar.Core.Tests.TestEvents;

[GenerateSerializer]
public class SendMessageTestEvent : EventBase
{
    [Id(0)] public string ChatId { get; set; }
    [Id(1)] public string Message { get; set; }
    [Id(2)] public string Photo { get; set; }
    [Id(3)] public string? Caption { get; set; }
    [Id(4)] public string? ReplyMessageId { get; set; }
    [Id(5)] public string BotName { get; set; }
}