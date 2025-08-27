using Aevatar.Account;
using Aevatar.ApiRequests;
using Aevatar.MongoDB;
using Xunit;

namespace Aevatar.MongoDB.Applications.ApiRequests;

[Collection(AevatarTestConsts.CollectionDefinitionName)]
public class MongoDBApiRequestServiceTests : ApiRequestServiceTests<AevatarMongoDbTestModule>
{

}
