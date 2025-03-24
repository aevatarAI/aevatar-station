using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aevatar.Notification;

public interface INotificationService
{
    Task<bool> CreateAsync(NotificationTypeEnum notificationTypeEnum, Guid target, string? targetEmail, string input);
    Task<bool> WithdrawAsync(Guid notificationId);
    Task<bool> Response(Guid notificationId, NotificationStatusEnum status);
    Task<List<NotificationDto>> GetNotificationList(int pageIndex, int pageSize);
}
