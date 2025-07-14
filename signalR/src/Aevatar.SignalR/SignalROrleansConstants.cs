namespace Aevatar.SignalR;

public static class SignalROrleansConstants
{
    /// <summary>
    /// Name of the state storage provider used by signalR orleans backplane grains.
    /// </summary>
    public const string SignalrOrleansStorageProvider = "PubSubStore";

    /// <summary>
    /// The number of minutes that each signalR hub server must heartbeat the server directory grain.
    /// There's just one server directory grain (id 0) per cluster.
    /// </summary>
    internal const int ServerHeartbeatPulseInMinutes = 30;

    public const string ResponseMethodName = "ReceiveResponse";
    public const string DefaultTopicPrefix = "AevatarSignalR";
}
