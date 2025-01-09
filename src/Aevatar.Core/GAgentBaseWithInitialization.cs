using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Aevatar.Core;

[GAgent("baseWithInitialization")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public abstract class GAgentBaseWithInitialization<TState, TEvent, TInitializeDto> : GAgentBase<TState, TEvent>
    where TState : StateBase, new()
    where TEvent : GEventBase
    where TInitializeDto : InitializeDtoBase
{
    protected GAgentBaseWithInitialization(ILogger logger) : base(logger)
    {
    }
    
    public abstract Task InitializeAsync(TInitializeDto initializeDto);

    public override Task<Type?> GetInitializeDtoTypeAsync()
    {
        return Task.FromResult(typeof(TInitializeDto))!;
    }
}