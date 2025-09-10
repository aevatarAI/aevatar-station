using Aevatar.Agents.Group;
using Aevatar.Core;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Aevatar.Application.Grains.Agents.Group;

[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class GroupGAgent : GAgentBase<GroupAgentState, GroupGEvent>
{

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("An agent to inform other agents when a social event is published.");
    }
    
    protected override async Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        State.RegisteredAgents = 0;
    }
}