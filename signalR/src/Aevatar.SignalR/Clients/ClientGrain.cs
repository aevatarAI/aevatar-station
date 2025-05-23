using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR.Protocol;
using Orleans.Streams;
using Aevatar.SignalR.Extensions;

namespace Aevatar.SignalR.Clients;

/// <inheritdoc cref="IClientGrain"/>
internal sealed class ClientGrain : IGrainBase, IClientGrain
{
    private const string CLIENT_STORAGE = "ClientState";
    private const int MAX_FAIL_ATTEMPTS = 3;

    private readonly ILogger<ClientGrain> _logger;
    private readonly IPersistentState<ClientGrainState> _clientState;

    private string _hubName = default!;
    private string _connectionId = default!;
    private Guid ServerId => _clientState.State.ServerId;

    public IGrainContext GrainContext { get; }

    private IStreamProvider _streamProvider = default!;
    private StreamSubscriptionHandle<Guid>? _serverDisconnectedSubscription = default;

    private int _failAttempts = 0;
    private IDisposable? _subscriptionTimer;
    private bool _isSubscriptionPending;

    public ClientGrain(
        ILogger<ClientGrain> logger,
        IGrainContext grainContext,
        [PersistentState(CLIENT_STORAGE, SignalROrleansConstants.SignalrOrleansStorageProvider)] IPersistentState<ClientGrainState> clientState)
    {
        _logger = logger;
        _clientState = clientState;
        GrainContext = grainContext;
    }

    private async Task EnsureServerDisconnectionSubscription(Guid serverId)
    {
        // TODO: Need to be sure ClientDisconnectStream's subscriptions can be called when disconnected.
        // if (_serverDisconnectedSubscription is null && !_isSubscriptionPending)
        // {
        //     _isSubscriptionPending = true;
        //     _subscriptionTimer?.Dispose();
        //     _subscriptionTimer = this.RegisterGrainTimer<object>(
        //         async _ =>
        //         {
        //             try
        //             {
        //                 if (_serverDisconnectedSubscription is null)
        //                 {
        //                     var serverDisconnectedStream = _streamProvider.GetServerDisconnectionStream(serverId);
        //                     _logger.LogDebug(
        //                         "Subscribing to server disconnection stream for server {serverId} on connection {connectionId}.",
        //                         serverId, _connectionId);
        //
        //                     _serverDisconnectedSubscription =
        //                         await serverDisconnectedStream.SubscribeAsync(_ => OnDisconnect("server-disconnected"));
        //                     
        //                     _logger.LogDebug(
        //                         "Subscribed to server disconnection stream for server {serverId} on connection {connectionId}.",
        //                         serverId, _connectionId);
        //                 }
        //             }
        //             finally
        //             {
        //                 _isSubscriptionPending = false;
        //                 _subscriptionTimer?.Dispose();
        //                 _subscriptionTimer = null;
        //             }
        //         },
        //         null,
        //         TimeSpan.FromMilliseconds(5000),
        //         TimeSpan.FromMilliseconds(-1)
        //     );
        // }
    }

    public async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var key = ClientKey.FromGrainPrimaryKey(this.GetPrimaryKeyString());
        _hubName = key.HubType;
        _connectionId = key.ConnectionId;

        _streamProvider = this.GetOrleansSignalRStreamProvider();

        if (ServerId != default)
        {
            _logger.LogDebug("Resuming connection on {hubName} for connection {connectionId} to server {serverId}.",
                _hubName, _connectionId, ServerId);
            await EnsureServerDisconnectionSubscription(ServerId);
        }
        
        _logger.LogDebug("OnActivateAsync executed, ConnectionId = {connectionId}", _connectionId);
    }

    public async Task OnConnect(Guid serverId)
    {
        _logger.LogDebug("Connecting connection on {hubName} for connection {connectionId} to server {serverId}.",
            _hubName, _connectionId, serverId);

        _clientState.State.ServerId = serverId;
        await _clientState.WriteStateAsync();

        await EnsureServerDisconnectionSubscription(serverId);

        _logger.LogDebug("Connected connection on {hubName} for connection {connectionId} to server {serverId}.",
            _hubName, _connectionId, _clientState.State.ServerId);
    }

    public async Task OnDisconnect(string? reason = null)
    {
        _logger.LogDebug("Disconnecting connection on {hubName} for connection {connectionId} from server {serverId} via reason '{reason}'.",
            _hubName, _connectionId, _clientState.State.ServerId, reason);

        _subscriptionTimer?.Dispose();
        _subscriptionTimer = null;
        _isSubscriptionPending = false;

        if (_serverDisconnectedSubscription is not null)
        {
            await _serverDisconnectedSubscription.UnsubscribeAsync();
            _serverDisconnectedSubscription = null;
        }

        await _streamProvider.GetClientDisconnectionStream(_connectionId).OnNextAsync(_connectionId);

        await _clientState.ClearStateAsync();

        this.DeactivateOnIdle();
    }

    public async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken token)
    {
        _subscriptionTimer?.Dispose();
    }

    // NB: Interface method is marked [ReadOnly] so this method will be re-entrant/interleaved.
    public async Task Send([Immutable] InvocationMessage message)
    {
        if (ServerId != default)
        {
            _logger.LogDebug("Sending message on {hubName}.{message.Target} to connection {connectionId} on server {serverId}.",
                _hubName, message.Target, _connectionId, ServerId);

            // Routes the message to the silo (server) where the client is actually connected.
            var stream = _streamProvider.GetServerStream(ServerId);

            var notification = new ClientNotification(SignalROrleansConstants.ResponseMethodName, message.Arguments!.ToStrings());
            await stream.OnNextAsync(new ClientMessage(_hubName, _connectionId, notification));

            Interlocked.Exchange(ref _failAttempts, 0);
        }
        else
        {
            _logger.LogInformation("Client not connected for connectionId '{connectionId}' and hub '{hubName}' ({targetMethod})",
                _connectionId, _hubName, message.Target);

            if (Interlocked.Increment(ref _failAttempts) >= MAX_FAIL_ATTEMPTS)
            {
                _logger.LogWarning("Force disconnect client for connectionId {connectionId} and hub {hubName} ({targetMethod}) after exceeding attempts limit",
                    _connectionId, _hubName, message.Target);

                await OnDisconnect("attempts-limit-reached");
            }
        }
    }

    public Task SendOneWay(InvocationMessage message) => Send(message);
}