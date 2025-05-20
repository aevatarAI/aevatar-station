using Aevatar.MongoDB;
using Aevatar.Origanzations;
using Xunit;

namespace Aevatar.MongoDb.Applications.Organizations;

[Collection(AevatarTestConsts.CollectionDefinitionName)]
public class MongoDBOrganizationServiceTests : OrganizationServiceTests<AevatarMongoDbTestModule>
{

}
