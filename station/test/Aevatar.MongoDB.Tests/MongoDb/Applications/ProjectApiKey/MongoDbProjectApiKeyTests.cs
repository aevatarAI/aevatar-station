using Aevatar.ProjectApiKey;
using Xunit;

namespace Aevatar.MongoDB.Applications.ProjectApiKey;

[Collection(AevatarTestConsts.CollectionDefinitionName)]
public class MongoDbProjectApiKeyTests : ProjectApiKeyTests<AevatarMongoDbTestModule>
{
    
}