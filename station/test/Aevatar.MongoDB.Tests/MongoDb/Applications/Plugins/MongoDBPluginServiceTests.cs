using Aevatar.MongoDB;
using Aevatar.Plugins;
using Xunit;

namespace Aevatar.MongoDB.Applications.Plugins;

[Collection(AevatarTestConsts.CollectionDefinitionName)]
public class MongoDBPluginServiceTests : PluginServiceTests<AevatarMongoDbTestModule>
{

} 