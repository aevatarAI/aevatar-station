using Aevatar.Core.Abstractions.Extensions;
using Aevatar.SignalR.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Newtonsoft.Json;
using Orleans.Streams;

namespace Aevatar.SignalR;

// TODO: Is this thing called in a threadsafe manner by signalR? 
public sealed class OrleansHubLifetimeManager<THub> : HubLifetimeManager<THub>, ILifecycleParticipant<ISiloLifecycle>,
    IDisposable where THub : Hub
{
    private Guid _serverId;
    private readonly ILogger _logger;
    private readonly string _hubName;
    private readonly IClusterClient _clusterClient;
    private readonly SemaphoreSlim _streamSetupLock = new(1);
    private readonly HubConnectionStore _connections = new();

    private IStreamProvider? _streamProvider;
    private IAsyncStream<ClientMessage> _serverStream = default!;
    private IAsyncStream<AllMessage> _allStream = default!;
    private Timer _timer = default!;

    private readonly string _instanceId = Guid.NewGuid().ToString("N")[..8];

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

        _logger.LogDebug("Created Orleans HubLifetimeManager - Instance: {InstanceId}, Hub: {HubName}",
            _instanceId, _hubName);
    }

    private async Task HeartbeatCheck()
    {
        _logger.LogInformation(
            "Heartbeat check - Instance: {InstanceId}, Hub: {HubName}, ServerId: {ServerId}",
            _instanceId, _hubName, _serverId);
        _clusterClient.GetServerDirectoryGrain().Heartbeat(_serverId);
    }

    private async Task EnsureStreamSetup()
    {
        if (_streamProvider is not null)
        {
            _logger.LogDebug(
                "Stream setup already complete - Instance: {InstanceId}, Hub: {HubName}, ServerId: {ServerId}",
                _instanceId, _hubName, _serverId);
            return;
        }

        _serverId = _serverId == Guid.Empty ? Guid.NewGuid() : _serverId;

        try
        {
            await _streamSetupLock.WaitAsync();

            if (_streamProvider is not null)
                return;

            _logger.LogInformation(
                "Initializing Orleans HubLifetimeManager - Instance: {InstanceId}, Hub: {HubName}, ServerId: {ServerId}",
                _instanceId, _hubName, _serverId);

            _streamProvider = _clusterClient.GetOrleansSignalRStreamProvider();
            _serverStream = _streamProvider.GetServerStream(_serverId);
            _allStream = _streamProvider.GetAllStream(_hubName);

            _timer = new Timer(
                _ => Task.Run(HeartbeatCheck), null, TimeSpan.FromSeconds(0),
                TimeSpan.FromMinutes(SignalROrleansConstants.ServerHeartbeatPulseInMinutes));

            var allMessageObserver = new AllMessageObserver(ProcessAllMessage);
            var allStreamHandle = await _allStream.SubscribeAsync(allMessageObserver);
            _logger.LogDebug(
                "Subscribed to all stream - Instance: {InstanceId}, StreamId: {StreamId}, HandleId: {HandleId}, ProviderName: {ProviderName}",
                _instanceId, allStreamHandle.StreamId, allStreamHandle.HandleId, allStreamHandle.ProviderName);
            var clientMessageObserver = new ClientMessageObserver(ProcessServerMessage);
            var serverStreamHandle = await _serverStream.SubscribeAsync(clientMessageObserver);
            _logger.LogDebug(
                "Subscribed to server stream - Instance: {InstanceId}, StreamId: {StreamId}, HandleId: {HandleId}, ProviderName: {ProviderName}",
                _instanceId, serverStreamHandle.StreamId, serverStreamHandle.HandleId, serverStreamHandle.ProviderName);

            _logger.LogInformation(
                "Initialization complete - Instance: {InstanceId}, Hub: {HubName}, ServerId: {ServerId}",
                _instanceId, _hubName, _serverId);
        }
        finally
        {
            _streamSetupLock.Release();
        }
    }

    private Task ProcessAllMessage(AllMessage allMessage)
    {
        var allTasks = new List<Task>(_connections.Count);
        var payload = allMessage.Message!;

        foreach (var connection in _connections)
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
        var connection = _connections[clientMessage.ConnectionId];
        _logger.LogDebug(
            "Processing server message - Instance: {InstanceId}, Hub: {HubName}, ServerId: {ServerId}, ConnectionId: {ConnectionId}, Available: {ConnectionAvailable}",
            _instanceId,
            _hubName,
            _serverId,
            clientMessage.ConnectionId,
            connection != null);
        return connection == null ? Task.CompletedTask : SendLocal(connection, clientMessage.Message);
    }

    public override async Task OnConnectedAsync(HubConnectionContext connection)
    {
        await EnsureStreamSetup();

        try
        {
            var httpContext = connection.GetHttpContext();
            var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown IP";
            
            _connections.Add(connection);

            var userAgent = httpContext?.Request?.Headers["User-Agent"].ToString() ?? "Unknown Agent";

            _logger.LogDebug(
                "Orleans Hub - New client connection - Instance: {InstanceId}, Hub: {HubName}, ServerId: {ServerId}, ConnectionId: {ConnectionId}, IP: {IpAddress}, UserAgent: {UserAgent}, Identity: {UserIdentity}, IsAuthenticated: {IsAuthenticated}, UserIdentifier: {UserIdentifier}, Items: {ItemsCount}, Claims: {Claims}",
                _instanceId,
                _hubName,
                _serverId,
                connection.ConnectionId,
                ipAddress,
                userAgent,
                connection.User?.Identity?.Name ?? "Anonymous",
                connection.User?.Identity?.IsAuthenticated ?? false,
                connection.UserIdentifier ?? "None",
                connection.Items.Count,
                connection.User?.Claims != null
                    ? string.Join(", ", connection.User.Claims.Select(c => $"{c.Type}: {c.Value}"))
                    : "No claims");

            var client = _clusterClient.GetClientGrain(_hubName, connection.ConnectionId);

            _logger.LogDebug(
                "Orleans Hub - Client grain - Instance: {InstanceId}, Hub: {HubName}, ServerId: {ServerId}, ConnectionId: {ConnectionId}",
                _instanceId,
                _hubName,
                _serverId,
                connection.ConnectionId);

            await client.OnConnect(_serverId);

            if (connection!.User!.Identity!.IsAuthenticated)
            {
                _logger.LogDebug(
                    "Orleans Hub - Authenticated user connected - Instance: {InstanceId}, Hub: {HubName}, ConnectionId: {ConnectionId}, User: {UserIdentity}, UserIdentifier: {UserIdentifier}",
                    _instanceId,
                    _hubName,
                    connection.ConnectionId,
                    connection.User.Identity.Name,
                    connection.UserIdentifier);

                var user = _clusterClient.GetUserGrain(_hubName, connection.UserIdentifier!);
                await user.Add(connection.ConnectionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "An error has occurred 'OnConnectedAsync' while adding connection - Instance: {InstanceId}, ConnectionId: {ConnectionId}, Hub: {HubName}, ServerId: {ServerId}",
                _instanceId,
                connection?.ConnectionId,
                _hubName,
                _serverId);
            _connections.Remove(connection!);
            throw;
        }
    }

    public override async Task OnDisconnectedAsync(HubConnectionContext connection)
    {
        try
        {
            _logger.LogDebug(
                "Handle disconnection - Instance: {InstanceId}, Hub: {HubName}, ServerId: {ServerId}, ConnectionId: {ConnectionId}",
                _instanceId,
                _hubName,
                _serverId,
                connection.ConnectionId);
            var client = _clusterClient.GetClientGrain(_hubName, connection.ConnectionId);
            await client.OnDisconnect("hub-disconnect");
        }
        finally
        {
            _connections.Remove(connection);
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

        var connection = _connections[connectionId];
        if (connection != null)
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
        var tasks = userIds.Select(u => SendGroupAsync(u, methodName, args, cancellationToken));
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
            "Sending local message - Instance: {InstanceId}, Hub: {HubName}, ServerId: {ServerId}, ConnectionId: {ConnectionId}",
            _instanceId,
            _hubName,
            _serverId,
            connection.ConnectionId);
        return connection
            .WriteAsync(new InvocationMessage(SignalROrleansConstants.ResponseMethodName, notification.Arguments))
            .AsTask();
    }

    private Task SendExternal(string connectionId, InvocationMessage hubMessage)
    {
        var client = _clusterClient.GetClientGrain(_hubName, connectionId);
        return client.Send(hubMessage);
    }

    public void Dispose()
    {
        _logger.LogDebug(
            "Disposing Orleans HubLifetimeManager - Instance: {InstanceId}, Hub: {HubName}, ServerId: {ServerId}",
            _instanceId,
            _hubName,
            _serverId);

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

        try
        {
            Task.WhenAll(toUnsubscribe.ToArray()).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from streams during disposal");
        }
    }

    public void Participate(ISiloLifecycle lifecycle)
    {
        _logger.LogDebug(
            "Participating in silo lifecycle - Instance: {InstanceId}, Hub: {HubName}",
            _instanceId,
            _hubName);
        lifecycle.Subscribe(
            observerName: nameof(OrleansHubLifetimeManager<THub>),
            stage: ServiceLifecycleStage.Active,
            onStart: async cts => await Task.Run(EnsureStreamSetup, cts));
    }
}
