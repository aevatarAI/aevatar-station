using System;
using Aevatar.Notification;
using Orleans;

namespace Aevatar.SignalR.SignalRMessage;

public class NotificationResponse:ISignalRMessage<NotificationResponseMessage>
{
    public string MessageType => "NotificationAction";
    public NotificationResponseMessage Data { get; set; }
}


[GenerateSerializer]
public class NotificationResponseMessage
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public NotificationStatusEnum Status { get; set; }
}