namespace Aevatar.SignalR;

public class AevatarSignalRConstants
{
    public static string[] AevatarSignalRStreamNamespaces { get; } =
    [
        "ServerDisconnectStream",
        "ServerStream",
        "ClientDisconnectStream",
        "AllStream"
    ];
}