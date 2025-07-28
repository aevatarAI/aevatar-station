namespace Aevatar.Core.Abstractions;

using Orleans.Streams;

[GenerateSerializer]
public class BroadcastGState : StateBase
{
    [Id(0)]public Dictionary<string, Guid> Subscription = [];
}