using Aevatar.Core.Abstractions;
using Aevatar.Core.Tests.TestEvents;
using Aevatar.Core.Tests.TestGAgents;
using Aevatar.Core.Tests.TestStates;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Shouldly;

namespace Aevatar.SignalR.Tests;

// ReSharper disable InconsistentNaming
public sealed class SignalRTests : AevatarSignalRTestBase
{
    private readonly IClusterClient _clusterClient;
    private readonly IGAgentFactory _gAgentFactory;
    private readonly HubLifetimeManager<AevatarSignalRHub> _hubLifetimeManager;

    public SignalRTests()
    {
        _clusterClient = GetRequiredService<IClusterClient>();
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
        _hubLifetimeManager = new OrleansHubLifetimeManager<AevatarSignalRHub>(
            new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<AevatarSignalRHub>>(), _clusterClient);
    }

    [Fact]
    public async Task Test()
    {
        var groupGAgent = await _gAgentFactory.GetGAgentAsync<IStateGAgent<GroupGAgentState>>();
        var signalRTestGAgent = await _gAgentFactory.GetGAgentAsync<IStateGAgent<SignalRTestGAgentState>>();

        await groupGAgent.RegisterAsync(signalRTestGAgent);

        {
            var children = await groupGAgent.GetChildrenAsync();
            children.Count.ShouldBe(1);
        }

        using var client = new SignalRTestClient();
        var connection = SignalRTestHelper.CreateHubConnectionContext(client.Connection);
        await _hubLifetimeManager.OnConnectedAsync(connection);

        await _hubLifetimeManager.SendConnectionAsync(connection.ConnectionId, "PublishEventAsync",
        [
            signalRTestGAgent.GetGrainId(), typeof(NaiveTestEvent).FullName!,
            JsonConvert.SerializeObject(new NaiveTestEvent
            {
                Greeting = "Hello, World!"
            })
        ]);

        // await client.SendInvocationAsync("PublishEventAsync", signalRTestGAgent.GetGrainId(), typeof(NaiveTestEvent).FullName!,
        //     JsonConvert.SerializeObject(new NaiveTestEvent
        //     {
        //         Greeting = "Hello, World!"
        //     }));

        {
            var children = await groupGAgent.GetChildrenAsync();
            children.Count.ShouldBe(1);
        }
    }
}