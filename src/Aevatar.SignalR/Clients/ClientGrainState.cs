using System.Diagnostics;

namespace Aevatar.SignalR.Clients;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
[GenerateSerializer]
internal sealed class ClientGrainState
{
    private string DebuggerDisplay => $"ServerId: '{ServerId}'";

    [Id(0)]
    public Guid ServerId { get; set; }
}