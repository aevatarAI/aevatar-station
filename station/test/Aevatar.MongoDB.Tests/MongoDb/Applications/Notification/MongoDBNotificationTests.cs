using Aevatar.Notification;
using Xunit;

namespace Aevatar.MongoDB.Applications.Notification;

[Collection(AevatarTestConsts.CollectionDefinitionName)]
public class MongoDbNotificationTests : NotificationTests<AevatarMongoDbTestModule>
{
    
}