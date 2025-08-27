using Aevatar.AuthServer.Grants;
using Aevatar.MongoDB;
using Xunit;

namespace Aevatar.MongoDb.AuthServerGrants;

[Collection(AevatarTestConsts.CollectionDefinitionName)]
public class MongoDBSignatureGrantHandlerTests : SignatureGrantHandlerTests<AevatarMongoDbTestModule>
{

} 