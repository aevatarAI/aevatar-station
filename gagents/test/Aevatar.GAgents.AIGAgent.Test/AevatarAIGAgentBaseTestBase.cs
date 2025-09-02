using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.AIGAgent.Test;

public class AevatarAIGAgentBaseTestBase : AevatarAIGAgentTestBase
{
    protected IGAgentFactory GAgentFactory => GetRequiredService<IGAgentFactory>();
}