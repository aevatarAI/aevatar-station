using Aevatar.Core.Abstractions;

namespace Aevatar.SignalR.Clients;

[GenerateSerializer]
public class SignalRClientGAgentConfiguration : ConfigurationBase
{
    [Id(0)] public required Guid ServerId { get; set; }
    [Id(1)] public required string HubType { get; set; }
    [Id(2)] public required string ConnectionId { get; set; }
}