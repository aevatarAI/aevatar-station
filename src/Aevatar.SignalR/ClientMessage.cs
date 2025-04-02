namespace Aevatar.SignalR;

[Immutable, GenerateSerializer]
public sealed record ClientMessage(string HubName, string ConnectionId, [Immutable] ClientNotification Message);
