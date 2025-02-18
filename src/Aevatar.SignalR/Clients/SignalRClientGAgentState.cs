using System.Diagnostics;
using Aevatar.Core.Abstractions;

namespace Aevatar.SignalR.Clients;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
[GenerateSerializer]
internal sealed class SignalRClientGAgentState : StateBase
{
    private string DebuggerDisplay => $"ServerId: '{ServerId}'";

    [Id(0)] public Guid ServerId { get; set; }
    [Id(1)] public string HubType { get; set; }
    [Id(2)] public string ConnectionId { get; set; }
}