using Aevatar.Core.Abstractions.Plugin;

namespace Aevatar.Core.Abstractions;

public interface IPluginGAgentManager
{
    Task<Guid> AddPluginGAgentAsync(AddPluginGAgentDto addPluginGAgentDto);
    Task LoadPluginGAgentsAsync(Guid tenantId);
}