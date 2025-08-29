using Orleans;

namespace Aevatar.GAgents.Telegram.Agent.GEvents;

[GenerateSerializer]
public class TelegramOptionSEvent:MessageSEvent
{
    [Id(0)] public string Webhook { get; set; }
    [Id(1)] public string EncryptionPassword { get; set; }
}