using System;
using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Telegram.Agent.GEvents;
using Orleans;

namespace Aevatar.GAgents.Telegram.Agent;

public class TelegramGAgentState : StateBase
{
    [Id(0)] public Guid Id { get; set; } = Guid.NewGuid();

    [Id(1)]
    public Dictionary<string, ReceiveMessageSEvent> PendingMessages { get; set; } =
        new Dictionary<string, ReceiveMessageSEvent>();

    [Id(3)] public string BotName { get; set; }

    [Id(4)] public string Token { get; set; }
    [Id(5)] public List<Guid> SocialRequestList { get; set; } = new List<Guid>();
   
    [Id(6)] public string Webhook { get; set; }
    [Id(7)] public string EncryptionPassword { get; set; }
}
