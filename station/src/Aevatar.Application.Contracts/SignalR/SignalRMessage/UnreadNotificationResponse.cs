using Orleans;

namespace Aevatar.SignalR.SignalRMessage;

public class UnreadNotificationResponse : ISignalRMessage<UnreadNotification>
{
    public string MessageType => "NotificationUnread";
    public UnreadNotification Data { get; set; }
}


[GenerateSerializer]
public class UnreadNotification
{
    public UnreadNotification()
    {
    }

    public UnreadNotification(int unreadCount)
    {
        UnreadCount = unreadCount;
    }

    [Id(0)] public int UnreadCount { get; set; }
}