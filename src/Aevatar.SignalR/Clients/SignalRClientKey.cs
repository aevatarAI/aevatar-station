using Aevatar.Core.Abstractions.Extensions;

namespace Aevatar.SignalR.Clients;

internal readonly record struct SignalRClientKey
{
    public required string HubType { get; init; }
    public required string ConnectionId { get; init; }

    public Guid ToGrainPrimaryKey() => $"{HubType}:{ConnectionId}".ToGuid();
}