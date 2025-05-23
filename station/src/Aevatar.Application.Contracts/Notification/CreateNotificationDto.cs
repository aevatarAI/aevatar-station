using System;

namespace Aevatar.Notification;

public class CreateNotificationDto
{
    public NotificationTypeEnum Type { get; set; }
    public Guid Target { get; set; }
    public string? TargetEmail { get; set; }
    public string Content { get; set; }
}