using Aevatar.Core.Abstractions;
using Orleans.Streams;

namespace Aevatar.Core;

public class StateBaseAsyncObserver : IAsyncObserver<StateBase>
{
    private readonly Func<StateBase, Task> _func;

    public Guid GAgentGuid { get; set; }

    public StateBaseAsyncObserver(Func<StateBase, Task> func)
    {
        _func = func;
    }

    public async Task OnNextAsync(StateBase item, StreamSequenceToken? token = null)
    {
        await _func(item);
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