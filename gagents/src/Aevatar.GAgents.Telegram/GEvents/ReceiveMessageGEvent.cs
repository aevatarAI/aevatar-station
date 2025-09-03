using System.ComponentModel;
using Orleans;

namespace Aevatar.GAgents.Telegram.GEvents;

[Description("Receive a message from telegram.")]
[GenerateSerializer]
public class ReceiveMessageGEvent
{
    [Description("Unique identifier for the received message.")]
    [Id(0)]  public string MessageId { get; set; }
    [Description("Unique identifier for the chat from which the message was received.")]
    [Id(1)]  public string ChatId { get; set; }
    [Description("Text content of the received message.")]
    [Id(2)] public string Message { get; set; }
}