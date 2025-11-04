using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.StateManagement;
using Microsoft.Extensions.Logging;
using Orleans.Serialization;

namespace Aevatar.Core.StateManagement;

/// <summary>
/// Implementation of IStatePublisher for Plus components using enhanced functionality.
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
            // Enhanced state publishing using Plus components for new functionality
            var snapshot = _copier.Copy(state);
            
            var singleStateWrapper = new StateWrapperPlus<TState>(grainId, snapshot, version);
            singleStateWrapper.PublishedTimestampUtc = DateTime.UtcNow;
            await _stateDispatcher.PublishSinglePlusAsync(grainId, singleStateWrapper);
            
            var batchStateWrapper = new StateWrapperPlus<TState>(grainId, snapshot, version);
            batchStateWrapper.PublishedTimestampUtc = DateTime.UtcNow;
            await _stateDispatcher.PublishPlusAsync(grainId, batchStateWrapper);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch state for grain {GrainId}", grainId);
            throw;
        }
    }
} 