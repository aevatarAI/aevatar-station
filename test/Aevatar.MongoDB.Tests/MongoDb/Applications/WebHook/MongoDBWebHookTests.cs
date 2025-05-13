using Aevatar.Webhook;
using Xunit;

namespace Aevatar.MongoDB.Applications.WebHook;

[Collection(AevatarTestConsts.CollectionDefinitionName)]
public class MongoDBWebHookTests : WebHookTests<AevatarMongoDbTestModule>
{
    
}