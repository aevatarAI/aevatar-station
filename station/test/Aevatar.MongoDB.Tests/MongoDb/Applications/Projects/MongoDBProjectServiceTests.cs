using Aevatar.MongoDB;
using Aevatar.Origanzations;
using Aevatar.Projects;
using Xunit;

namespace Aevatar.MongoDB.Applications.Projects;

[Collection(AevatarTestConsts.CollectionDefinitionName)]
public class MongoDBProjectServiceTests : ProjectServiceTests<AevatarMongoDbTestModule>
{

}
