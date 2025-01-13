using System.Reflection;
using Aevatar.Core.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Aevatar.TestBase;

public class PluginGAgentManager : IPluginGAgentManager
{
    private readonly ApplicationPartManager _applicationPartManager;

    public PluginGAgentManager(ApplicationPartManager applicationPartManager)
    {
        _applicationPartManager = applicationPartManager;
    }

    public void AddPluginGAgent(byte[] pluginGAgentCode)
    {
        var assembly = Assembly.Load(pluginGAgentCode);
        _applicationPartManager.ApplicationParts.Add(new AssemblyPart(assembly));
    }
}