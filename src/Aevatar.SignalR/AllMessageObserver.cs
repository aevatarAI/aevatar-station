using Orleans.Streams;

namespace Aevatar.SignalR;

public class AllMessageObserver : IAsyncObserver<AllMessage>
{
    private readonly Func<AllMessage, Task> _func;

    public AllMessageObserver(Func<AllMessage, Task> func)
    {
        _func = func;
    }

    public async Task OnNextAsync(AllMessage item, StreamSequenceToken? token = null)
    {
        await _func.Invoke(item);
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