using Aevatar.Core.Abstractions;
using Shouldly;

namespace Aevatar.GAgents.Tests;

public sealed class PluginGAgentsTests : AevatarGAgentsTestBase
{
    private readonly IGAgentFactory _gAgentFactory;

    public PluginGAgentsTests()
    {
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact(DisplayName = "Can load plugin gAgent from dll.")]
    public async Task LoadPluginGAgentTest()
    {
        var gAgent = await _gAgentFactory.GetGAgentAsync("pluginTest");
        gAgent.ShouldNotBeNull();
        var subscribedEvents = await gAgent.GetAllSubscribedEventsAsync();
        subscribedEvents.ShouldNotBeNull();
        subscribedEvents.Count.ShouldBe(1);
        subscribedEvents[0].Name.ShouldBe("PluginTestEvent");
    }
}