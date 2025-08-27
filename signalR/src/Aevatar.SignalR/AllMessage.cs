using Microsoft.AspNetCore.SignalR.Protocol;

namespace Aevatar.SignalR;

[Immutable, GenerateSerializer]
public sealed record AllMessage([Immutable] InvocationMessage Message, [Immutable] IReadOnlyList<string>? ExcludedIds = null);
