using System;

namespace Aevatar.Notification;

public class NotificationCreatForEventBusDto
{
    public NotificationTypeEnum Type { get; set; }
    public Guid Creator { get; set; }
    public Guid Target { get; set; }
    public string Content { get; set; }
}