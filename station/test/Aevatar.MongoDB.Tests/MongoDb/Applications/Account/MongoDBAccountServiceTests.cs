using Aevatar.Account;
using Xunit;

namespace Aevatar.MongoDB.Applications.Account;

[Collection(AevatarTestConsts.CollectionDefinitionName)]
public class MongoDBAccountServiceTests : AccountServiceTests<AevatarMongoDbTestModule>
{

}
