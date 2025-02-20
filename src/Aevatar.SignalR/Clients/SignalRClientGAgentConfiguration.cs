using Aevatar.Core.Abstractions;

namespace Aevatar.SignalR.Clients;

[GenerateSerializer]
public class SignalRClientGAgentConfiguration : ConfigurationBase
{
    [Id(1)] public string? HubType { get; set; }
    [Id(2)] public string? ConnectionId { get; set; }
}