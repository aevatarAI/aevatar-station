using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.StateManagement;
using Microsoft.Extensions.Logging;
using Orleans.Serialization;

namespace Aevatar.Core.StateManagement;

/// <summary>
/// Implementation of IStatePublisher that wraps the original state dispatch logic from GAgentBase.
/// </summary>
public class StatePublisher : IStatePublisher
{
    private readonly ILogger<StatePublisher> _logger;
    private readonly DeepCopier _copier;
    private readonly IStateDispatcher _stateDispatcher;

    public StatePublisher(
        ILogger<StatePublisher> logger, 
        DeepCopier copier,
        IStateDispatcher stateDispatcher)
    {
        _logger = logger;
        _copier = copier;
        _stateDispatcher = stateDispatcher;
    }

    public async Task DispatchStateAsync<TState>(TState state, GrainId grainId, int version) where TState : CoreStateBase
    {
        try
        {
            // Original logic from GAgentBase - simple and clean
            var snapshot = _copier.Copy(state);
            
            var singleStateWrapper = new StateWrapper<TState>(grainId, snapshot, version);
            singleStateWrapper.PublishedTimestampUtc = DateTime.UtcNow;
            await _stateDispatcher.PublishSingleAsync(grainId, singleStateWrapper);
            
            var batchStateWrapper = new StateWrapper<TState>(grainId, snapshot, version);
            batchStateWrapper.PublishedTimestampUtc = DateTime.UtcNow;
            await _stateDispatcher.PublishAsync(grainId, batchStateWrapper);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch state for grain {GrainId}", grainId);
            throw;
        }
    }
} 