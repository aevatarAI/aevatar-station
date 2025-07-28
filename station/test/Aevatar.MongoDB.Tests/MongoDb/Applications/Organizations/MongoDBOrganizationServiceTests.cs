using Aevatar.Origanzations;
using Xunit;

namespace Aevatar.MongoDB.Applications.Organizations;

[Collection(AevatarTestConsts.CollectionDefinitionName)]
public class MongoDBOrganizationServiceTests : OrganizationServiceTests<AevatarMongoDbTestModule>
{

}
