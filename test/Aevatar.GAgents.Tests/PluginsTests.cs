using Aevatar.Core;
using Aevatar.Core.Abstractions.Plugin;
using Shouldly;

namespace Aevatar.GAgents.Tests;

public sealed class PluginsTests : AevatarGAgentsTestBase
{
    private readonly IGAgentFactory _gAgentFactory;

    public PluginsTests()
    {
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact]
    public void Test()
    {
        var provider = new DefaultPluginDirectoryProvider();
        var directory = provider.GetDirectory();
        directory.ShouldNotBeNull();
    }

    [Fact]
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