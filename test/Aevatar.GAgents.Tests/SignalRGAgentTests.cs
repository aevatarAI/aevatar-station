using Aevatar.Core.Abstractions;
using Aevatar.Core.Tests.TestEvents;
using Aevatar.Core.Tests.TestGAgents;
using Aevatar.Core.Tests.TestStates;
using Aevatar.SignalR;
using Aevatar.SignalR.GAgents;
using Newtonsoft.Json;
using Shouldly;

namespace Aevatar.GAgents.Tests;

// ReSharper disable InconsistentNaming
public sealed class SignalRGAgentTests : AevatarGAgentsTestBase
{
    private readonly IGAgentFactory _gAgentFactory;
    private readonly AevatarSignalRHub<NaiveTestEvent> _signalRHub;

    public SignalRGAgentTests()
    {
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
        _signalRHub = new AevatarSignalRHub<NaiveTestEvent>(_gAgentFactory);
    }

    [Fact]
    public async Task SignalRGAgentTest()
    {
        var signalRGAgent = await _gAgentFactory.GetGAgentAsync<ISignalRGAgent<NaiveTestEvent>>(
            new SignalRGAgentConfiguration
            {
                ConnectionId = "test-connection-id"
            });
        (await signalRGAgent.GetDescriptionAsync()).ShouldNotBeNull();
    }

    [Fact]
    public async Task SignalRHubTest()
    {
        var groupGAgent = await _gAgentFactory.GetGAgentAsync<IStateGAgent<GroupGAgentState>>();
        var naiveGAgent = await _gAgentFactory.GetGAgentAsync<IStateGAgent<NaiveTestGAgentState>>();
        await groupGAgent.RegisterAsync(naiveGAgent);

        var naiveTestEvent = new NaiveTestEvent
        {
            Greeting = "Hello, SignalR!"
        };

        await _signalRHub.PublishEventAsync("Aevatar.Core.Tests.TestGAgents/naiveTest",
            naiveGAgent.GetPrimaryKey().ToString("N"),
            JsonConvert.SerializeObject(naiveTestEvent));

        var children = await groupGAgent.GetChildrenAsync();
        children.Count.ShouldBe(2);
        children.Last().Type
            .ShouldBe(GrainType.Create(
                "Aevatar.SignalR.GAgents/SignalRGAgent`1[[Aevatar.Core.Tests.TestEvents.NaiveTestEvent,Aevatar.Core.Tests]]"));
    }
}