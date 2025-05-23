using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SignalRSample.GAgents;

namespace SignalRSample.Host;

public class HostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public HostedService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var gAgentFactory = scope.ServiceProvider.GetRequiredService<IGAgentFactory>();
        // var groupGAgent = await gAgentFactory.GetGAgentAsync<IStateGAgent<GroupGAgentState>>();
        var signalRTestGAgent =
            await gAgentFactory.GetGAgentAsync<IStateGAgent<SignalRTestGAgentState>>("test".ToGuid());

        // await groupGAgent.RegisterAsync(signalRTestGAgent);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}