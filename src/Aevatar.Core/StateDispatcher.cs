using Aevatar.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Streams;

namespace Aevatar.Core;

public class StateDispatcher : IStateDispatcher
{
    private readonly IEnumerable<IStateProjector> _stateProjectors;
    private readonly IStreamProvider _streamProvider;
    private readonly AevatarOptions _aevatarOptions;

    public StateDispatcher(IClusterClient clusterClient, IEnumerable<IStateProjector> stateProjectors)
    {
        _stateProjectors = stateProjectors;
        _streamProvider = clusterClient.GetStreamProvider(AevatarCoreConstants.StreamProvider);
        _aevatarOptions = clusterClient.ServiceProvider.GetRequiredService<IOptions<AevatarOptions>>().Value;
    }

    public async Task PublishAsync<TState>(GrainId grainId, StateWrapper<TState> stateWrapper) where TState : StateBase
    {
        // var streamId = StreamId.Create(_aevatarOptions.StreamNamespace, typeof(StateWrapper<TState>).FullName!);
        // var stream = _streamProvider.GetStream<StateWrapper<TState>>(streamId);
        // await stream.OnNextAsync(stateWrapper);
        foreach (var stateProjector in _stateProjectors)
        {
            await stateProjector.ProjectAsync(stateWrapper);
        }
    }
}