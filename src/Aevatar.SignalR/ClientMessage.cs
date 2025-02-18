using Microsoft.AspNetCore.SignalR.Protocol;

namespace Aevatar.SignalR;

[Immutable, GenerateSerializer]
public sealed record ClientMessage(string HubName, string ConnectionId, [Immutable] InvocationMessage Message);
