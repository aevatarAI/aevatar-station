using System;

namespace Aevatar.Notification;

public class NotificationDto
{
    public Guid Id { get; set; }
    public NotificationTypeEnum Type { get; set; }
    public string Content { get; set; }
    public Guid Receiver { get; set; }
    public Guid CreatorId { get; set; }
    public NotificationStatusEnum Status { get; set; }
    public DateTime CreationTime { get; set; }
}