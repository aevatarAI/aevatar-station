using Aevatar.MongoDB;
using Aevatar.Plugins;
using Xunit;

namespace Aevatar.MongoDb.Applications.Plugins;

[Collection(AevatarTestConsts.CollectionDefinitionName)]
public class MongoDBPluginServiceTests : PluginServiceTests<AevatarMongoDbTestModule>
{

} 