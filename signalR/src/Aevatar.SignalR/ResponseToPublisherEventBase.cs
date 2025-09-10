using Aevatar.Core.Abstractions;

namespace Aevatar.SignalR;

[GenerateSerializer]
public abstract class ResponseToPublisherEventBase : EventBase
{
    [Id(0)] public string ConnectionId { get; set; } = string.Empty;
}