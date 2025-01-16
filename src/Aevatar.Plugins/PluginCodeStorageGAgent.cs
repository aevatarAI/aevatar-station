using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Aevatar.Plugins;

[GenerateSerializer]
public class PluginCodeStorageGAgentState : StateBase
{
    [Id(0)] public byte[] Code { get; set; }
}

[GenerateSerializer]
public class PluginCodeStorageStateLogEvent : StateLogEventBase<PluginCodeStorageStateLogEvent>
{
    
}

[GenerateSerializer]
public class PluginCodeStorageInitializationEvent : InitializationEventBase
{
    [Id(0)] public byte[] Code { get; set; }
}

public interface IPluginCodeStorageGAgent: IStateGAgent<PluginCodeStorageGAgentState>
{
    Task<byte[]> GetPluginCodeAsync();
}

[GAgent("pluginCodeStorage")]
public class PluginCodeStorageGAgent(ILogger<PluginCodeStorageGAgent> logger)
    : GAgentBase<PluginCodeStorageGAgentState, PluginCodeStorageStateLogEvent, EventBase,
        PluginCodeStorageInitializationEvent>(logger), IPluginCodeStorageGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This GAgent is used to store plugin's code.");
    }

    protected override void GAgentTransitionState(PluginCodeStorageGAgentState state,
        StateLogEventBase<PluginCodeStorageStateLogEvent> @event)
    {
        switch (@event)
        {
            case SetPluginCodeStateLogEvent setPluginCodeStateLogEvent:
                State.Code = setPluginCodeStateLogEvent.Code;
                break;
        }
    }

    [GenerateSerializer]
    public class SetPluginCodeStateLogEvent : PluginCodeStorageStateLogEvent
    {
        [Id(0)] public byte[] Code { get; set; }
    }

    public override async Task InitializeAsync(PluginCodeStorageInitializationEvent initializationEvent)
    {
        RaiseEvent(new SetPluginCodeStateLogEvent
        {
            Code = initializationEvent.Code
        });
        await ConfirmEvents();
    }

    public Task<byte[]> GetPluginCodeAsync()
    {
        return Task.FromResult(State.Code);
    }
}