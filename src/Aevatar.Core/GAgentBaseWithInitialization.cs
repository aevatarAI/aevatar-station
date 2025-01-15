using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Aevatar.Core;

[GAgent("baseWithInitialization")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public abstract class GAgentBase<TState, TStateLogEvent, TEvent, TInitializationEvent> : GAgentBase<TState, TStateLogEvent, TEvent>
    where TState : StateBase, new()
    where TStateLogEvent: StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
    where TInitializationEvent : InitializationEventBase
{
    protected GAgentBase(ILogger logger) : base(logger)
    {
    }

    public abstract Task InitializeAsync(TInitializationEvent initializationEvent);

    public override Task<Type?> GetInitializationTypeAsync()
    {
        return Task.FromResult(typeof(TInitializationEvent))!;
    }
}