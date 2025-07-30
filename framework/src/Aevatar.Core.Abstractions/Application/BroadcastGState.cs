namespace Aevatar.Core.Abstractions;

using Orleans.Streams;

[GenerateSerializer]
public class BroadcastGState : CoreStateBase
{
    [Id(0)]public Dictionary<string, Guid> Subscription = [];
}