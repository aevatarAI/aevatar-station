using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Aevatar.Plugins;

[GenerateSerializer]
public class TenantPluginCodeGAgentState : StateBase
{
    [Id(0)] public List<Guid> CodeStorageGuids { get; set; }
}

[GenerateSerializer]
public class TenantPluginStateLogEvent : StateLogEventBase<TenantPluginStateLogEvent>
{
    
}

public interface ITenantPluginCodeGAgent : IStateGAgent<TenantPluginCodeGAgentState>
{
    Task AddPluginCode(IEnumerable<Guid> codeStorageGuid);
}

[GAgent("pluginTenant")]
public class TenantPluginCodeGAgent(ILogger<TenantPluginCodeGAgent> logger)
    : GAgentBase<TenantPluginCodeGAgentState, TenantPluginStateLogEvent>(logger), ITenantPluginCodeGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This GAgent is used to store plugin list for a specific tenant.");
    }

    protected override void GAgentTransitionState(TenantPluginCodeGAgentState state,
        StateLogEventBase<TenantPluginStateLogEvent> @event)
    {
        switch (@event)
        {
            case AddPluginCodeStateLogEvent addPluginCodeStateLogEvent:
                if (State.CodeStorageGuids.IsNullOrEmpty())
                {
                    State.CodeStorageGuids = [];
                }

                State.CodeStorageGuids.AddRange(addPluginCodeStateLogEvent.CodeStorageGuids);
                break;
        }

        base.GAgentTransitionState(state, @event);
    }

    public async Task AddPluginCode(IEnumerable<Guid> codeStorageGuids)
    {
        RaiseEvent(new AddPluginCodeStateLogEvent
        {
            CodeStorageGuids = codeStorageGuids.ToList()
        });
        await ConfirmEvents();
    }

    [GenerateSerializer]
    public class AddPluginCodeStateLogEvent : TenantPluginStateLogEvent
    {
        [Id(0)] public List<Guid> CodeStorageGuids { get; set; }
    }
}