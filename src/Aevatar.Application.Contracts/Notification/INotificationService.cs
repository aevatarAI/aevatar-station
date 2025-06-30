using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aevatar.Notification;

public interface INotificationService
{
    Task<Guid> CreateAsync(NotificationTypeEnum notificationTypeEnum, Guid? creator, Guid target, string input);
    Task<bool> WithdrawAsync(Guid? creator, Guid notificationId);
    Task<bool> Response(Guid notificationId, Guid? receiver, NotificationStatusEnum status);
    Task<List<NotificationDto>> GetNotificationList(Guid? creator, int pageIndex, int pageSize);
    Task<List<OrganizationVisitDto>> GetOrganizationVisitInfo(Guid userId, int pageIndex, int pageSize);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task ReadAsync(Guid userId, ReadNotificationDto input);
}