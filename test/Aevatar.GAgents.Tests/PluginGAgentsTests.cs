using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Plugin;
using Aevatar.Plugins;
using Aevatar.TestBase;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
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

    [Fact(DisplayName = "Can load plugin gAgent by PluginGAgentManager.")]
    public async Task PluginGAgentManagerTest()
    {
        var directory = new DefaultPluginDirectoryProvider().GetDirectory();
        var pluginGAgentManager = GetRequiredService<IPluginGAgentManager>();
        var codeList = LoadDllsAsByteArrays(directory);
        var tenantId = Guid.NewGuid();
        foreach (var code in codeList)
        {
            await pluginGAgentManager.AddPluginAsync(new AddPluginDto
            {
                Code = code,
                TenantId = tenantId
            });
        }

        var gAgent = await _gAgentFactory.GetGAgentAsync("pluginTest");
        gAgent.ShouldNotBeNull();
        var subscribedEvents = await gAgent.GetAllSubscribedEventsAsync();
        subscribedEvents.ShouldNotBeNull();
        subscribedEvents.Count.ShouldBe(1);
        subscribedEvents[0].Name.ShouldBe("PluginTestEvent");
    }
    
    public static List<byte[]> LoadDllsAsByteArrays(string directory)
    {
        var dllFiles = Directory.GetFiles(directory, "*.dll");
        var dllByteArrays = new List<byte[]>();

        foreach (var dllFile in dllFiles)
        {
            var dllBytes = File.ReadAllBytes(dllFile);
            dllByteArrays.Add(dllBytes);
        }

        return dllByteArrays;
    }
}