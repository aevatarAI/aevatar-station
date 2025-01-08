using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Tests.TestGAgents;
using Aevatar.Core.Tests.TestInitializeDtos;
using Aevatar.Core.Tests.TestStates;
using Shouldly;

namespace Aevatar.GAgents.Tests;

public class GAgentFactoryTests : AevatarGAgentsTestBase
{
    protected readonly IGAgentFactory _gAgentFactory;

    public GAgentFactoryTests()
    {
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact]
    public async Task CreateNormalGAgentTest()
    {
        var gAgent = await _gAgentFactory.GetGAgentAsync<IStateGAgent<GroupGAgentState>>(Guid.NewGuid());
        gAgent.ShouldNotBeNull();
        gAgent.GetGrainId().ShouldBe(GrainId.Create("test/group", gAgent.GetPrimaryKey().ToString("N")));
    }

    [Fact]
    public async Task CreateGAgentWithInitializeMethodTest()
    {
        var gAgent = await _gAgentFactory.GetGAgentAsync<IStateGAgent<NaiveTestGAgentState>>(Guid.NewGuid(),
            new NaiveGAgentInitializeDto
            {
                InitialGreeting = "Test"
            });
        gAgent.GetGrainId().ShouldBe(GrainId.Create("aevatar/naiveTest", gAgent.GetPrimaryKey().ToString("N")));
        await TestHelper.WaitUntilAsync(_ => CheckState(gAgent), TimeSpan.FromSeconds(20));
        var gAgentState = await gAgent.GetStateAsync();
        gAgentState.Content.Count.ShouldBe(1);
    }

    [Fact]
    public async Task CreateGAgentWithAliasTest()
    {
        {
            var gAgent = await _gAgentFactory.GetGAgentAsync("naiveTest");
            gAgent.ShouldNotBeNull();
            var subscribedEvents = await gAgent.GetAllSubscribedEventsAsync();
            subscribedEvents.ShouldNotBeNull();
            subscribedEvents.Count.ShouldBePositive();
        }

        {
            var gAgent = await _gAgentFactory.GetGAgentAsync("naiveTest", initializeDto: new NaiveGAgentInitializeDto
            {
                InitialGreeting = "Test"
            });
            gAgent.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task CreateGAgentWithInterfaceTest()
    {
        var gAgent = await _gAgentFactory.GetGAgentAsync<IPublishingGAgent>();
        gAgent.ShouldNotBeNull();
        gAgent.GetGrainId().ShouldBe(GrainId.Create("aevatar/publishing", gAgent.GetPrimaryKey().ToString("N")));
    }

    [Fact]
    public async Task GetAvailableGAgentTypesTest()
    {
        var availableGAgents = _gAgentFactory.GetAvailableGAgentTypes();
        availableGAgents.Count.ShouldBeGreaterThan(20);
    }

    private async Task<bool> CheckState(IStateGAgent<NaiveTestGAgentState> gAgent)
    {
        var state = await gAgent.GetStateAsync();
        return !state.Content.IsNullOrEmpty();
    }
}