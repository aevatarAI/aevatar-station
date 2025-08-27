using System;
using Aevatar.MongoDB;
using Volo.Abp.Domain.Repositories.MongoDB;
using Volo.Abp.MongoDB;

namespace Aevatar.Notification;

public class NotificationMongoRepository : MongoDbRepository<AevatarMongoDbContext, NotificationInfo, Guid>, INotificationRepository
{
    public NotificationMongoRepository(IMongoDbContextProvider<AevatarMongoDbContext> dbContextProvider) : base(dbContextProvider)
    {
    }
}