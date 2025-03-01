using Aevatar.Core.Abstractions;
using Orleans.Streams;

namespace Aevatar.Core;

public class StateProjectionAsyncObserver : IAsyncObserver<StateBase>
{
    private readonly IEnumerable<IStateProjector> _stateProjectors;

    public StateProjectionAsyncObserver(IEnumerable<IStateProjector> stateProjectors)
    {
        _stateProjectors = stateProjectors;
    }
    
    public Task OnNextAsync(StateBase item, StreamSequenceToken? token = null)
    {
        return Task.WhenAll(_stateProjectors.Select(projector => projector.ProjectAsync(item)));
    }

    public Task OnCompletedAsync()
    {
        return Task.CompletedTask;
    }

    public Task OnErrorAsync(Exception ex)
    {
        return Task.CompletedTask;
    }
}