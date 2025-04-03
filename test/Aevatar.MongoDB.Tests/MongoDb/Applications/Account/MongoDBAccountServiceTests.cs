using Aevatar.Account;
using Aevatar.MongoDB;
using Xunit;

namespace Aevatar.MongoDb.Applications.Account;

[Collection(AevatarTestConsts.CollectionDefinitionName)]
public class MongoDBAccountServiceTests : AccountServiceTests<AevatarMongoDbTestModule>
{

}
