using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Extensions;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.Basic.BasicGAgents;

namespace Aevatar.GAgents.AIGAgent.Core.Extensions;

public static class GAgentFactoryExtensions
{
    public static Guid SystemLLMConfigGuid = typeof(SystemLLMConfigOptions).FullName!.ToGuid();

    public static async Task<IConfigManagerGAgent> GetSystemLLMConfigGAgent(this IGAgentFactory gAgentFactory)
    {
        return await gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>(SystemLLMConfigGuid);
    }
}