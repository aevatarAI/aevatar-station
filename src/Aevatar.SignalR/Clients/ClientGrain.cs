using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR.Protocol;
using Orleans.Runtime;
using Orleans.Streams;
using Orleans.Concurrency;

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

    public ClientGrain(
        ILogger<ClientGrain> logger,
        IGrainContext grainContext,
        [PersistentState(CLIENT_STORAGE, SignalROrleansConstants.SignalrOrleansStorageProvider)] IPersistentState<ClientGrainState> clientState)
    {
        _logger = logger;
        _clientState = clientState;
        GrainContext = grainContext;
    }

    public async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var key = ClientKey.FromGrainPrimaryKey(this.GetPrimaryKeyString());
        _hubName = key.HubType;
        _connectionId = key.ConnectionId;

        _streamProvider = this.GetOrleansSignalRStreamProvider();

        // Resume subscriptions if we have already been "connected".
        // We know we have already been connected if the "ServerId" parameter is set.
        if (ServerId != default)
        {
            _logger.LogDebug("Resuming connection on {hubName} for connection {connectionId} to server {serverId}.",
                _hubName, _connectionId, ServerId);
            
            // We will listen to this stream to know if the server is disconnected (silo goes down) so that we can enact client disconnected procedure.
            var serverDisconnectedStream = _streamProvider.GetServerDisconnectionStream(_clientState.State.ServerId);
            var _serverDisconnectedSubscription = (await serverDisconnectedStream.GetAllSubscriptionHandles())[0];
            await _serverDisconnectedSubscription.ResumeAsync((serverId, _) => OnDisconnect("server-disconnected"));
        }
    }

    public async Task OnConnect(Guid serverId)
    {
        var serverDisconnectedStream = _streamProvider.GetServerDisconnectionStream(serverId);
        _serverDisconnectedSubscription = await serverDisconnectedStream.SubscribeAsync(_ => OnDisconnect("server-disconnected"));

        _clientState.State.ServerId = serverId;
        await _clientState.WriteStateAsync();
        
        _logger.LogDebug("Connected connection on {hubName} for connection {connectionId} to server {serverId}.",
            _hubName, _connectionId, _clientState.State.ServerId);
    }

    public async Task OnDisconnect(string? reason = null)
    {
        _logger.LogDebug("Disconnecting connection on {hubName} for connection {connectionId} from server {serverId} via reason '{reason}'.",
            _hubName, _connectionId, _clientState.State.ServerId, reason);

        if (_serverDisconnectedSubscription is not null)
        {
            await _serverDisconnectedSubscription.UnsubscribeAsync();
            _serverDisconnectedSubscription = null;
        }

        await _streamProvider.GetClientDisconnectionStream(_connectionId).OnNextAsync(_connectionId);

        await _clientState.ClearStateAsync();

        this.DeactivateOnIdle();
    }

    // NB: Interface method is marked [ReadOnly] so this method will be re-entrant/interleaved.
    public async Task Send([Immutable] InvocationMessage message)
    {
        if (ServerId != default)
        {
            _logger.LogDebug("Sending message on {hubName}.{message.Target} to connection {connectionId} on server {serverId}.",
                _hubName, message.Target, _connectionId, ServerId);

            // Routes the message to the silo (server) where the client is actually connected.
            await _streamProvider.GetServerStream(ServerId).OnNextAsync(new ClientMessage(_hubName, _connectionId, message));

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