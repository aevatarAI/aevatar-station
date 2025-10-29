using Aevatar.MongoDB;
using Aevatar.Service;
using Xunit;

namespace Aevatar.MongoDb.Applications.Service;

/// <summary>
/// MongoDB implementation of WorkflowViewService integration tests
/// </summary>
[Collection(AevatarTestConsts.CollectionDefinitionName)]
public class MongoDBWorkflowViewServiceTests : WorkflowViewServiceTests<AevatarMongoDbTestModule>
{
}

