using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace SignalRSample.Host;

public class TestStateProjector : IStateProjector
{
    private readonly ILogger<TestStateProjector> _logger;

    public TestStateProjector(ILogger<TestStateProjector> logger)
    {
        _logger = logger;
    }

    public Task ProjectAsync<T>(T state) where T : StateWrapperBase
    {
        _logger.LogInformation("TestStateProjector works. {State}", state);
        return Task.CompletedTask;
    }
}