using Aevatar.Core.Abstractions.Extensions;
using Aevatar.SignalR.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Newtonsoft.Json;
using Orleans.Streams;
using System.Collections.Concurrent;

namespace Aevatar.SignalR;

// Orleans是单线程的Grain模型，但HubLifetimeManager是多线程的
public sealed class OrleansHubLifetimeManager<THub> : HubLifetimeManager<THub>, ILifecycleParticipant<ISiloLifecycle>,
    IDisposable where THub : Hub
{
    private Guid _serverId;
    private readonly ILogger _logger;
    private readonly string _hubName;
    private readonly IClusterClient _clusterClient;
    private readonly SemaphoreSlim _streamSetupLock = new(1);
    private readonly ConcurrentDictionary<string, HubConnectionContext> _connections = new();

    private IStreamProvider? _streamProvider;
    private IAsyncStream<ClientMessage> _serverStream = default!;
    private IAsyncStream<AllMessage> _allStream = default!;
    private Timer _timer = default!;

    public OrleansHubLifetimeManager(
        ILogger<OrleansHubLifetimeManager<THub>> logger,
        IClusterClient clusterClient
    )
    {
        var hubType = typeof(THub).BaseType?.GenericTypeArguments.FirstOrDefault() ?? typeof(THub);
        _hubName = hubType.IsInterface && hubType.Name[0] == 'I'
            ? hubType.Name[1..]
            : hubType.Name;

        _logger = logger;
        _clusterClient = clusterClient;

        _logger.LogDebug("Created Orleans HubLifetimeManager {hubName})", _hubName);
    }

    private async Task HeartbeatCheck()
    {
        _logger.LogInformation("Heartbeat check for Orleans HubLifetimeManager {hubName} (serverId: {serverId})",
            _hubName, _serverId);
        _clusterClient.GetServerDirectoryGrain().Heartbeat(_serverId);
    }

    private async Task EnsureStreamSetup()
    {
        if (_streamProvider is not null)
        {
            _logger.LogDebug("Stream setup already complete for Orleans HubLifetimeManager {hubName} (serverId: {serverId})",
                _hubName, _serverId);
            return;
        }

        _serverId = _serverId == Guid.Empty ? Guid.NewGuid() : _serverId;

        try
        {
            await _streamSetupLock.WaitAsync();

            if (_streamProvider is not null)
                return;

            _logger.LogInformation(
                "Initializing: Orleans HubLifetimeManager {hubName} (serverId: {serverId})...",
                _hubName, _serverId);

            _streamProvider = _clusterClient.GetOrleansSignalRStreamProvider();
            _serverStream = _streamProvider.GetServerStream(_serverId);
            _allStream = _streamProvider.GetAllStream(_hubName);

            _timer = new Timer(
                _ => Task.Run(HeartbeatCheck), null, TimeSpan.FromSeconds(0),
                TimeSpan.FromMinutes(SignalROrleansConstants.ServerHeartbeatPulseInMinutes));

            var allMessageObserver = new AllMessageObserver(ProcessAllMessage);
            var allStreamHandle = await _allStream.SubscribeAsync(allMessageObserver);
            _logger.LogDebug("Subscribed to all stream: StreamId - {streamId}, HandleId - {handleId}, ProviderName - {providerName}",
                allStreamHandle.StreamId, allStreamHandle.HandleId, allStreamHandle.ProviderName);
            var clientMessageObserver = new ClientMessageObserver(ProcessServerMessage);
            var serverStreamHandle = await _serverStream.SubscribeAsync(clientMessageObserver);
            _logger.LogDebug("Subscribed to server stream: StreamId - {streamId}, HandleId - {handleId}, ProviderName - {providerName}",
                serverStreamHandle.StreamId, serverStreamHandle.HandleId, serverStreamHandle.ProviderName);

            _logger.LogInformation(
                "Initialized complete: Orleans HubLifetimeManager {hubName} (serverId: {serverId})",
                _hubName, _serverId);
        }
        finally
        {
            _streamSetupLock.Release();
        }
    }

    private Task ProcessAllMessage(AllMessage allMessage)
    {
        var allTasks = new List<Task>();
        var payload = allMessage.Message!;
        
        // 获取当前连接的快照，避免在迭代时修改集合
        var connections = _connections.Values.ToList();
        
        foreach (var connection in connections)
        {
            if (connection.ConnectionAborted.IsCancellationRequested)
                continue;

            if (allMessage.ExcludedIds == null || !allMessage.ExcludedIds.Contains(connection.ConnectionId))
                allTasks.Add(SendLocal(connection, new ClientNotification(payload.Target, payload.Arguments!.ToStrings())));
        }

        return Task.WhenAll(allTasks);
    }

    private Task ProcessServerMessage(ClientMessage clientMessage)
    {
        // 线程安全地获取连接
        if (_connections.TryGetValue(clientMessage.ConnectionId, out var connection))
        {
            _logger.LogDebug("Processing server message for connection {connectionId} on hub {hubName} (serverId: {serverId})",
                clientMessage.ConnectionId, _hubName, _serverId);
            return SendLocal(connection, clientMessage.Message);
        }
        return Task.CompletedTask;
    }

    public override async Task OnConnectedAsync(HubConnectionContext connection)
    {
        await EnsureStreamSetup();

        try
        {
            // 使用线程安全的方式添加连接
            _connections.TryAdd(connection.ConnectionId, connection);

            var client = _clusterClient.GetClientGrain(_hubName, connection.ConnectionId);
            
            _logger.LogDebug("Handle connection {connectionId} on hub {hubName} (serverId: {serverId})",
                connection.ConnectionId, _hubName, _serverId);
            
            await client.OnConnect(_serverId);

            if (connection!.User!.Identity!.IsAuthenticated)
            {
                var user = _clusterClient.GetUserGrain(_hubName, connection.UserIdentifier!);
                await user.Add(connection.ConnectionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "An error has occurred 'OnConnectedAsync' while adding connection {connectionId} [hub: {hubName} (serverId: {serverId})]",
                connection?.ConnectionId, _hubName, _serverId);
            // 确保在异常情况下也能移除连接
            _connections.TryRemove(connection!.ConnectionId, out _);
            throw;
        }
    }

    public override async Task OnDisconnectedAsync(HubConnectionContext connection)
    {
        try
        {
            _logger.LogDebug("Handle disconnection {connectionId} on hub {hubName} (serverId: {serverId})",
                connection.ConnectionId, _hubName, _serverId);
            var client = _clusterClient.GetClientGrain(_hubName, connection.ConnectionId);
            await client.OnDisconnect("hub-disconnect");
        }
        finally
        {
            // 使用线程安全的方式移除连接
            _connections.TryRemove(connection.ConnectionId, out _);
        }
    }

    public override Task SendAllAsync(string methodName, object?[] args,
        CancellationToken cancellationToken = default)
    {
        var message = new InvocationMessage(methodName, args);
        return _allStream.OnNextAsync(new AllMessage(message));
    }

    public override Task SendAllExceptAsync(string methodName, object?[] args,
        IReadOnlyList<string> excludedConnectionIds,
        CancellationToken cancellationToken = default)
    {
        var message = new InvocationMessage(methodName, args);
        return _allStream.OnNextAsync(new AllMessage(message, excludedConnectionIds));
    }

    public override Task SendConnectionAsync(string connectionId, string methodName, object?[] args,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionId)) throw new ArgumentNullException(nameof(connectionId));
        if (string.IsNullOrWhiteSpace(methodName)) throw new ArgumentNullException(nameof(methodName));

        var message = new InvocationMessage(methodName, args);

        // 线程安全地获取连接
        if (_connections.TryGetValue(connectionId, out var connection))
        {
            return SendLocal(connection, new ClientNotification(methodName, args!.ToStrings()));
        }

        return SendExternal(connectionId, message);
    }

    public override Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName, object?[] args,
        CancellationToken cancellationToken = default)
    {
        var tasks = connectionIds.Select(c => SendConnectionAsync(c, methodName, args, cancellationToken));
        return Task.WhenAll(tasks);
    }

    public override Task SendGroupAsync(string groupName, string methodName, object?[] args,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(groupName)) throw new ArgumentNullException(nameof(groupName));
        if (string.IsNullOrWhiteSpace(methodName)) throw new ArgumentNullException(nameof(methodName));

        var group = _clusterClient.GetGroupGrain(_hubName, groupName);
        return group.Send(methodName, args);
    }

    public override Task SendGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object?[] args,
        CancellationToken cancellationToken = default)
    {
        var tasks = groupNames.Select(g => SendGroupAsync(g, methodName, args, cancellationToken));
        return Task.WhenAll(tasks);
    }

    public override Task SendGroupExceptAsync(string groupName, string methodName, object?[] args,
        IReadOnlyList<string> excludedConnectionIds,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(groupName)) throw new ArgumentNullException(nameof(groupName));
        if (string.IsNullOrWhiteSpace(methodName)) throw new ArgumentNullException(nameof(methodName));

        var group = _clusterClient.GetGroupGrain(_hubName, groupName);
        return group.SendExcept(methodName, args, excludedConnectionIds);
    }

    public override Task SendUserAsync(string userId, string methodName, object?[] args,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));
        if (string.IsNullOrWhiteSpace(methodName)) throw new ArgumentNullException(nameof(methodName));

        var user = _clusterClient.GetUserGrain(_hubName, userId);
        return user.Send(methodName, args);
    }

    public override Task SendUsersAsync(IReadOnlyList<string> userIds, string methodName, object?[] args,
        CancellationToken cancellationToken = default)
    {
        var tasks = userIds.Select(u => SendUserAsync(u, methodName, args, cancellationToken));
        return Task.WhenAll(tasks);
    }

    public override Task AddToGroupAsync(string connectionId, string groupName,
        CancellationToken cancellationToken = default)
    {
        var group = _clusterClient.GetGroupGrain(_hubName, groupName);
        return group.Add(connectionId);
    }

    public override Task RemoveFromGroupAsync(string connectionId, string groupName,
        CancellationToken cancellationToken = default)
    {
        var group = _clusterClient.GetGroupGrain(_hubName, groupName);
        return group.Remove(connectionId);
    }

    private Task SendLocal(HubConnectionContext connection, ClientNotification notification)
    {
        _logger.LogInformation(
            "Sending local message to connection {connectionId} on hub {hubName} (serverId: {serverId})",
            connection.ConnectionId, _hubName, _serverId);
        // ReSharper disable once CoVariantArrayConversion
        return connection.WriteAsync(new InvocationMessage(SignalROrleansConstants.ResponseMethodName, notification.Arguments))
            .AsTask();
    }

    private Task SendExternal(string connectionId, InvocationMessage hubMessage)
    {
        var client = _clusterClient.GetClientGrain(_hubName, connectionId);
        return client.Send(hubMessage);
    }

    public void Dispose()
    {
        _logger.LogDebug("Disposing Orleans HubLifetimeManager {hubName} (serverId: {serverId})",
            _hubName, _serverId);

        _timer?.Dispose();

        var toUnsubscribe = new List<Task>();
        if (_serverStream is not null)
        {
            toUnsubscribe.Add(Task.Factory.StartNew(async () =>
            {
                var subscriptions = await _serverStream.GetAllSubscriptionHandles();
                var subs = new List<Task>();
                subs.AddRange(subscriptions.Select(s => s.UnsubscribeAsync()));
                await Task.WhenAll(subs);
            }));
        }

        if (_allStream is not null)
        {
            toUnsubscribe.Add(Task.Factory.StartNew(async () =>
            {
                var subscriptions = await _allStream.GetAllSubscriptionHandles();
                var subs = new List<Task>();
                subs.AddRange(subscriptions.Select(s => s.UnsubscribeAsync()));
                await Task.WhenAll(subs);
            }));
        }

        var serverDirectoryGrain = _clusterClient.GetServerDirectoryGrain();
        toUnsubscribe.Add(serverDirectoryGrain.Unregister(_serverId));
        
        // 等待所有取消订阅任务完成
        try
        {
            Task.WhenAll(toUnsubscribe.ToArray()).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from streams during disposal");
        }
        
        // 释放锁资源
        _streamSetupLock.Dispose();
    }

    public void Participate(ISiloLifecycle lifecycle)
    {
        _logger.LogInformation("Participating in the lifecycle of the silo.");
        lifecycle.Subscribe(
           observerName: nameof(OrleansHubLifetimeManager<THub>),
           stage: ServiceLifecycleStage.Active,
           onStart: async cts => await Task.Run(EnsureStreamSetup, cts));
    }
}
