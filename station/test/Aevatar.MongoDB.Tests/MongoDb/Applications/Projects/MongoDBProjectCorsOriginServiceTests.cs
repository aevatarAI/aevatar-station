using Aevatar.MongoDB;
using Aevatar.Projects;
using Xunit;

namespace Aevatar.MongoDb.Applications.Projects;

[Collection(AevatarTestConsts.CollectionDefinitionName)]
public class MongoDBProjectCorsOriginServiceTests : ProjectCorsOriginServiceTests<AevatarMongoDbTestModule>
{

} 