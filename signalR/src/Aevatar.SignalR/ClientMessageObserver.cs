using Orleans.Streams;

namespace Aevatar.SignalR;

public class ClientMessageObserver : IAsyncObserver<ClientMessage>
{
    private readonly Func<ClientMessage, Task> _func;

    public ClientMessageObserver(Func<ClientMessage, Task> func)
    {
        _func = func;
    }

    public async Task OnNextAsync(ClientMessage item, StreamSequenceToken? token = null)
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