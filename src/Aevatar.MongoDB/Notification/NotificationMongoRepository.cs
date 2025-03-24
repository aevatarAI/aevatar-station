using System;
using Volo.Abp.Domain.Repositories.MongoDB;
using Volo.Abp.MongoDB;

namespace Aevatar.Notification;

public class NotificationMongoRepository : MongoDbRepository<AbpMongoDbContext, NotificationInfo, Guid>, INotificationRepository
{
    public NotificationMongoRepository(IMongoDbContextProvider<AbpMongoDbContext> dbContextProvider) : base(dbContextProvider)
    {
    }
}