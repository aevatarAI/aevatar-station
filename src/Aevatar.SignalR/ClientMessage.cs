namespace Aevatar.SignalR;

[Immutable, GenerateSerializer]
public class ClientMessage(string hubName, string connectionId, ClientNotification message)
{
    [Id(0)] public string HubName { get; set; } = hubName;
    [Id(1)] public string ConnectionId { get; set; } = connectionId;
    [Immutable] [Id(2)] public ClientNotification Message { get; set; } = message;
}