using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.Twitter.GEvents;

[GenerateSerializer]
public class BindTwitterAccountGEvent : EventBase
{
    [Id(0)] public string UserName { get; set; }
    [Id(1)] public string UserId { get; set; }
    [Id(2)] public string Token { get; set; }
    [Id(3)] public string TokenSecret { get; set; }
}