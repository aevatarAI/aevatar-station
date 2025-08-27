using Aevatar.Core.Abstractions;
using Aevatar.SignalR.Tests.Extensions;
using Aevatar.SignalR.Tests.GAgents;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
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
    private readonly ILogger<AevatarSignalRHub> _logger;

    public SignalRTests()
    {
        _clusterClient = GetRequiredService<IClusterClient>();
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
        _logger = GetRequiredService<ILogger<AevatarSignalRHub>>();
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

        var hub = new AevatarSignalRHub(_gAgentFactory, _logger);
        await hub.PublishEventAsync(signalRTestGAgent.GetGrainId(), typeof(NaiveTestEvent).FullName!,
            JsonConvert.SerializeObject(new NaiveTestEvent
            {
                Greeting = "Hello, World!"
            }));

        // Simulate server sending a response to the client
        await _hubLifetimeManager.SendConnectionAsync(connection.ConnectionId, SignalROrleansConstants.ResponseMethodName,
            new object[] { 
                new SignalRResponseEvent { 
                    Message = "Hello, World!" 
                } 
            });

        var message = Assert.IsType<InvocationMessage>(await client.ReadAsync().OrTimeout(milliseconds: 30000));

        {
            var children = await groupGAgent.GetChildrenAsync();
            children.Count.ShouldBe(2);
        }
    }
}