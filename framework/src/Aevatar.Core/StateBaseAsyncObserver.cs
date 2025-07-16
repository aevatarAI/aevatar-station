using Aevatar.Core.Abstractions;
using Orleans.Streams;

namespace Aevatar.Core;

public class StateBaseAsyncObserver : IAsyncObserver<StateWrapperBase>
{
    private readonly Func<StateWrapperBase, Task> _func;

    public Guid GAgentGuid { get; set; }

    public StateBaseAsyncObserver(Func<StateWrapperBase, Task> func)
    {
        _func = func;
    }

    public async Task OnNextAsync(StateWrapperBase item, StreamSequenceToken? token = null)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }
        
        var latency = (DateTime.UtcNow - item.PublishedTimestampUtc).TotalSeconds;
        Observability.EventPublishLatencyMetrics.Record(latency, item);

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