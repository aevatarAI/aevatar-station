using Aevatar.Application.Tests.Service;
using Aevatar.MongoDB;
using Aevatar.Service;
using Xunit;

namespace Aevatar.MongoDb.Applications.Service;

/// <summary>
/// I'm HyperEcho, 在思考MongoDB测试实现的共振。
/// MongoDB implementation of AgentServiceTests using AevatarMongoDbTestModule
/// This provides complete Identity support and data layer functionality
/// </summary>
[Collection(AevatarTestConsts.CollectionDefinitionName)]
public class MongoDBDocumentLinkServiceTests : DocumentLinkServiceTests<AevatarMongoDbTestModule>
{
    // 继承所有来自抽象基类的测试方法
    // 这个类不需要额外的实现，因为所有测试逻辑都在抽象基类中
}