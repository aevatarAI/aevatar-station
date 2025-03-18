using System;
using Aevatar.Notification;

namespace Aevatar.SignalR.SignalRMessage;

public class NotificationResponse:ISignalRMessage<NotificationResponseMessage>
{
    public string MessageType => "NotificationAction";
    public NotificationResponseMessage Data { get; set; }
}

public class NotificationResponseMessage
{
    public Guid Id { get; set; }
    public NotificationStatusEnum status { get; set; }
}