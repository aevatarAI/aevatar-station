using Aevatar.ApiKey;
using MongoDB.Driver;
using Volo.Abp.MongoDB;

namespace Aevatar.Notification;

public class NotificationDbContext : AbpMongoDbContext
{
    public IMongoCollection<NotificationInfo> NotificationInfoCollection => Collection<NotificationInfo>();
}