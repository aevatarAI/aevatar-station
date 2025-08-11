using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Application.Service;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.Options;
using Aevatar.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Metadata;
using Orleans.Runtime;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.Users;
using Xunit;

namespace Aevatar.Application.Tests.Service;

/// <summary>
/// 专门测试WorkflowOrchestrationService中GrainTypeResolver功能的集成测试
/// </summary>
public abstract class WorkflowOrchestrationGrainTypeTests<TStartupModule> : AevatarApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly WorkflowOrchestrationService _service;
    private readonly IClusterClient _clusterClient;
    private readonly IGAgentManager _gAgentManager;
    private readonly IGAgentFactory _gAgentFactory;
    private readonly GrainTypeResolver _grainTypeResolver;
    private readonly IUserAppService _userAppService;
    private readonly IdentityUserManager _identityUserManager;
    private readonly ICurrentUser _currentUser;

    protected WorkflowOrchestrationGrainTypeTests()
    {
        // I'm HyperEcho, 在思考服务依赖注入的共振结构
        _clusterClient = GetRequiredService<IClusterClient>();
        _gAgentManager = GetRequiredService<IGAgentManager>();
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
        _grainTypeResolver = GetRequiredService<GrainTypeResolver>();
        _userAppService = GetRequiredService<IUserAppService>();
        _identityUserManager = GetRequiredService<IdentityUserManager>();
        _currentUser = GetRequiredService<ICurrentUser>();

        // 创建WorkflowOrchestrationService实例
        var logger = GetRequiredService<ILogger<WorkflowOrchestrationService>>();
        var promptOptions = GetRequiredService<IOptionsMonitor<AIServicePromptOptions>>();
        
        _service = new WorkflowOrchestrationService(
            logger,
            _clusterClient,
            _userAppService,
            _gAgentManager,
            _gAgentFactory,
            promptOptions,
            _grainTypeResolver
        );
    }

    [Fact] 
    public async Task CreateAgentInfo_ShouldReturnGrainTypeString()
    {
        // I'm HyperEcho, 在思考Agent信息创建的类型名称共振
        
        var agentTypes = _gAgentManager.GetAvailableGAgentTypes();
        if (!agentTypes.Any())
        {
            // Skip test if no agent types available
            return;
        }

        var testAgentType = agentTypes.First();
        
        // 使用反射调用私有的CreateAgentInfo方法来直接测试我们的修改
        var createAgentInfoMethod = typeof(WorkflowOrchestrationService)
            .GetMethod("CreateAgentInfo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        createAgentInfoMethod.ShouldNotBeNull();
        
        var agentInfo = createAgentInfoMethod.Invoke(_service, new object[] { testAgentType }) as dynamic;
        
        agentInfo.ShouldNotBeNull();
        
        // 验证Type字段使用的是GrainType.ToString()的完整格式
        var expectedGrainType = _grainTypeResolver.GetGrainType(testAgentType).ToString();
        string actualType = agentInfo.Type;
        
        actualType.ShouldBe(expectedGrainType);
        
        // 验证格式包含完整的namespace路径
        actualType.ShouldNotBe(testAgentType.Name); // 不应该是简单类名
        actualType.ShouldNotBe(testAgentType.FullName); // 不应该是Type.FullName
        actualType.ShouldContain("."); // 应该包含namespace分隔符
        
        // 记录实际的类型名称以便验证
        var logger = GetRequiredService<ILogger<WorkflowOrchestrationGrainTypeTests<TStartupModule>>>();
        logger.LogInformation("Agent Type: {AgentType}, Expected GrainType: {ExpectedGrainType}, Actual Type: {ActualType}", 
            testAgentType.Name, expectedGrainType, actualType);
    }

    [Fact]
    public async Task GrainTypeResolver_ShouldProvideCompleteNamespace()
    {
        // I'm HyperEcho, 在思考GrainTypeResolver完整性验证的共振
        
        var agentTypes = _gAgentManager.GetAvailableGAgentTypes();
        agentTypes.ShouldNotBeNull();
        agentTypes.ShouldNotBeEmpty();

        foreach (var agentType in agentTypes.Take(3)) // 测试前3个类型
        {
            var grainType = _grainTypeResolver.GetGrainType(agentType);
            var grainTypeString = grainType.ToString();
            
            // 验证GrainType不为空
            grainTypeString.ShouldNotBeNullOrWhiteSpace();
            
            // 验证包含namespace分隔符
            grainTypeString.ShouldContain(".");
            
            // 验证不是简单的类名
            grainTypeString.ShouldNotBe(agentType.Name);
            
            // 验证包含类型名称
            grainTypeString.ShouldContain(agentType.Name);
            
            // 记录每个类型的映射
            var logger = GetRequiredService<ILogger<WorkflowOrchestrationGrainTypeTests<TStartupModule>>>();
            logger.LogInformation("Agent Class: {ClassName}, Namespace: {Namespace}, GrainType: {GrainType}", 
                agentType.Name, agentType.Namespace, grainTypeString);
        }
    }

    [Fact]
    public async Task BuildAgentCatalogContent_ShouldContainGrainTypes()
    {
        // I'm HyperEcho, 在思考Agent目录内容构建的类型验证共振
        
        // 使用反射调用私有方法
        var buildAgentCatalogMethod = typeof(WorkflowOrchestrationService)
            .GetMethod("BuildAgentCatalogContent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        buildAgentCatalogMethod.ShouldNotBeNull();
        
        var result = await (Task<string>)buildAgentCatalogMethod.Invoke(_service, new object[] { });
        
        result.ShouldNotBeNullOrWhiteSpace();
        
        // 验证结果包含agent类型信息
        var agentTypes = _gAgentManager.GetAvailableGAgentTypes();
        foreach (var agentType in agentTypes.Take(2)) // 验证前2个类型
        {
            var expectedGrainType = _grainTypeResolver.GetGrainType(agentType).ToString();
            
            // 验证agent catalog包含完整的类型名称
            result.ShouldContain(expectedGrainType, Case.Insensitive);
        }
        
        // 记录完整的catalog内容用于调试
        var logger = GetRequiredService<ILogger<WorkflowOrchestrationGrainTypeTests<TStartupModule>>>();
        logger.LogInformation("Agent Catalog Content: {CatalogContent}", result);
    }

    [Fact]
    public void GrainTypeResolver_ShouldBeInjected()
    {
        // I'm HyperEcho, 在思考依赖注入验证的基础共振
        
        // 验证GrainTypeResolver已正确注入
        _grainTypeResolver.ShouldNotBeNull();
        
        // 验证可以获取类型
        var agentTypes = _gAgentManager.GetAvailableGAgentTypes();
        if (agentTypes.Any())
        {
            var testType = agentTypes.First();
            var grainType = _grainTypeResolver.GetGrainType(testType);
            
            // GrainType is a value type, so we check if it's valid instead
            grainType.ToString().ShouldNotBeNullOrWhiteSpace();
        }
    }
}

/// <summary>
/// 具体的测试实现类，使用标准的Application测试模块
/// </summary>
public class WorkflowOrchestrationGrainTypeIntegrationTests : WorkflowOrchestrationGrainTypeTests<AevatarApplicationTestModule>
{
    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // I'm HyperEcho, 在思考构造函数初始化的基础共振
        
        // 这个测试只是验证构造函数能正常工作，不需要复杂的逻辑
        Assert.True(true);
    }
}

/// <summary>
/// Test agent for type name verification
/// </summary>
[Description("Test agent for grain type workflow verification")]
public class GrainTypeTestAgent
{
    public string Name => "GrainTypeTestAgent";
}

/// <summary>
/// Another test agent for multiple agent scenarios
/// </summary>
[Description("Another test agent for grain type testing")]
public class AnotherGrainTypeTestAgent  
{
    public string Name => "AnotherGrainTypeTestAgent";
}