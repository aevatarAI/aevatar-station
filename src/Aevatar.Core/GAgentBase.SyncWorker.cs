using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.SyncWorker;
using Orleans.SyncWork;

namespace Aevatar.Core;

public abstract partial class GAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>
{
    protected async Task CreateLongRunTaskAsync<TRequest, TResponse>(TRequest request)
        where TRequest : EventBase
        where TResponse : EventBase
    {
        var syncWorker = GrainFactory.GetGrain<IAevatarSyncWorker<TRequest, TResponse>>(Guid.NewGuid());
        await syncWorker.SetLongRunTaskAsync(GetEventBaseStream(GrainId));
        await syncWorker.StartWorkAndPollUntilResult(request);
    }
}