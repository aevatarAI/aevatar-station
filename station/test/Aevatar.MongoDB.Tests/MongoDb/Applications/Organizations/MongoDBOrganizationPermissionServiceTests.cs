using Aevatar.MongoDB;
using Aevatar.Origanzations;
using Xunit;

namespace Aevatar.MongoDB.Applications.Organizations;

[Collection(AevatarTestConsts.CollectionDefinitionName)]
public class MongoDBOrganizationPermissionServiceTests : OrganizationPermissionServiceTests<AevatarMongoDbTestModule>
{
    
}