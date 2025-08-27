namespace Aevatar.SignalR;

public static class AevatarStreamConfig
{
    public static string Prefix { get; private set; } = SignalROrleansConstants.DefaultTopicPrefix;

    public static void Initialize(string? prefix)
    {
        if (prefix != null)
        {
            Prefix = prefix;
        }
    }
}