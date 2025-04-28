using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR.Protocol;
using Orleans.Runtime;
using Orleans.Streams;
using System.Collections.Immutable;

namespace Aevatar.SignalR.ConnectionGroups;

/// <inheritdoc cref="IConnectionGroupGrain" />
internal sealed class ConnectionGroupGrain : IConnectionGroupGrain, IGrainBase
{
    private readonly ILogger _logger;
    private readonly ConnectionGroupKey _key;
    private readonly IGrainFactory _grainFactory;
    private readonly IPersistentState<ConnectionGroupGrainState> _state;

    private IStreamProvider _streamProvider = default!;

    public IGrainContext GrainContext { get; }

    public ConnectionGroupGrain(
        IGrainContext grainContext,
        IGrainFactory grainFactory,
        ILogger<ConnectionGroupGrain> logger,
        [PersistentState("ConnectionGroups", SignalROrleansConstants.SignalrOrleansStorageProvider)] IPersistentState<ConnectionGroupGrainState> state)
    {
        _logger = logger;
        _state = state;
        _grainFactory = grainFactory;
        _key = ConnectionGroupKey.FromPrimaryGrainKey(grainContext.GrainId.Key.ToString()!);

        GrainContext = grainContext;
    }

    public Task OnActivateAsync(CancellationToken token)
    {
        _streamProvider = this.GetOrleansSignalRStreamProvider();

        var resumeSubscriptionTasks = _state.State.ConnectionIds.Select(async connectionId =>
        {
            var clientDisconnectStream = _streamProvider.GetClientDisconnectionStream(connectionId);
            var subscriptionHandle = (await clientDisconnectStream.GetAllSubscriptionHandles())[0];
            await subscriptionHandle.ResumeAsync((connectionId, _) => Remove(connectionId));
        });

        return Task.WhenAll(resumeSubscriptionTasks);
    }

    public async Task Add(string connectionId)
    {
        if (_state.State.ConnectionIds.Add(connectionId))
        {
            var clientDisconnectStream = _streamProvider.GetClientDisconnectionStream(connectionId);
            await clientDisconnectStream.SubscribeAsync((connectionId, _) => Remove(connectionId));
            await _state.WriteStateAsync();
        }
    }

    public async Task Remove(string connectionId)
    {
        if (_state.State.ConnectionIds.Remove(connectionId))
        {
            var stream = _streamProvider.GetClientDisconnectionStream(connectionId);
            var handle = (await stream.GetAllSubscriptionHandles())[0];
            await handle.UnsubscribeAsync();

            if (_state.State.ConnectionIds.Count == 0)
            {
                await _state.ClearStateAsync();
            }
            else
            {
                await _state.WriteStateAsync();
            }
        }
    }

    public Task<int> Count()
      => Task.FromResult(_state.State.ConnectionIds.Count);

    // 在[ReadOnly]方法中使用不可变集合保证线程安全
    public Task Send([Immutable] InvocationMessage message)
    {
        // 创建一个不可变副本，确保在使用过程中不会被修改
        var connectionIds = _state.State.ConnectionIds.ToImmutableHashSet();
        return SendAll(message, connectionIds);
    }

    // 在[ReadOnly]方法中使用不可变集合保证线程安全
    public Task SendExcept(string methodName, object?[] args, IEnumerable<string> excludedConnectionIds)
    {
        var message = new InvocationMessage(methodName, args);
        
        // 创建一个不可变副本，并排除指定的连接ID
        var connectionIds = _state.State.ConnectionIds.ToImmutableHashSet()
            .Except(excludedConnectionIds);
            
        return SendAll(message, connectionIds);
    }

    private Task SendAll([Immutable] InvocationMessage message, IEnumerable<string> connectionIds)
    {
        _logger.LogDebug("Sending message to {HubName}.{MethodName} on {GroupType} group '{GroupId}'.",
            _key.HubType, message.Target, _key.GroupType, _key.GroupId);

        return Task.WhenAll(connectionIds.Select(connectionId => _grainFactory.GetClientGrain(_key.HubType, connectionId).Send(message)));
    }

    public Task SendOneWay(InvocationMessage message) => Send(message);
}
