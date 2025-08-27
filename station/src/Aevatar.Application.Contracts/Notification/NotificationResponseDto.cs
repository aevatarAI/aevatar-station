using System;

namespace Aevatar.Notification;

public class NotificationResponseDto
{
    public Guid Id { get; set; }
    public NotificationStatusEnum Status { get; set; }
}