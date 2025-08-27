using System;
using Aevatar.ApiKey;
using Volo.Abp.Domain.Repositories;

namespace Aevatar.Notification;

public interface INotificationRepository: IRepository<NotificationInfo, Guid>
{
    
}