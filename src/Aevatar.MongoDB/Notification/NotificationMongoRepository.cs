using System;
using Volo.Abp.Domain.Repositories.MongoDB;
using Volo.Abp.MongoDB;

namespace Aevatar.Notification;

public class NotificationMongoRepository : MongoDbRepository<NotificationDbContext, NotificationInfo, Guid>, INotificationRepository
{
    public NotificationMongoRepository(IMongoDbContextProvider<NotificationDbContext> dbContextProvider) : base(dbContextProvider)
    {
    }
}