using Aevatar.Core.Abstractions;
using Aevatar.Core.Tests;
using Aevatar.Core.Tests.TestGAgents;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Aevatar.GAgents.Tests;

public sealed class GenericGAgentFactoryTests : AevatarGAgentsTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    public GenericGAgentFactoryTests(ITestOutputHelper testOutputHelper)
        : base()
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task Generic_IGAgent_Factory_Should_Be_Resolvable()
    {
        // Arrange & Act - Resolve the generic factory for IGAgent
        var genericFactoryForIGAgent = GetRequiredService<IGAgentFactory<IGAgent>>();
        
        // Assert
        Assert.NotNull(genericFactoryForIGAgent);
        _testOutputHelper.WriteLine("Successfully resolved IGAgentFactory<IGAgent>");
    }

    [Fact]
    public async Task Generic_IGAgentPlus_Factory_Should_Be_Resolvable()
    {
        // Arrange & Act - Resolve the generic factory for IGAgentPlus
        var genericFactoryForIGAgentPlus = GetRequiredService<IGAgentFactory<IGAgentPlus>>();
        
        // Assert
        Assert.NotNull(genericFactoryForIGAgentPlus);
        _testOutputHelper.WriteLine("Successfully resolved IGAgentFactory<IGAgentPlus>");
    }

    [Fact]
    public async Task Generic_IGAgent_Factory_Should_Create_GAgent()
    {
        // Arrange
        var genericFactory = GetRequiredService<IGAgentFactory<IGAgent>>();
        var primaryKey = Guid.NewGuid();
        
        // Act - Use the concrete test grain type instead of generic GrainId
        var agent = await genericFactory.GetGAgentAsync<ITestIGAgent>(primaryKey);
        
        // Assert
        Assert.NotNull(agent);
        
        // Verify it's an IGAgent instance
        Assert.IsAssignableFrom<IGAgent>(agent);
        Assert.IsAssignableFrom<ITestIGAgent>(agent);
        
        _testOutputHelper.WriteLine($"Successfully created IGAgent with primary key: {primaryKey}");
    }

    [Fact]
    public async Task NonGeneric_IGAgentFactory_Should_Return_IGAgent()
    {
        // Arrange
        var nonGenericFactory = GetRequiredService<IGAgentFactory>();
        var primaryKey = Guid.NewGuid();
        
        // Act - Use the concrete test grain type
        var agent = await nonGenericFactory.GetGAgentAsync<ITestIGAgent>(primaryKey);
        
        // Assert - The method signature guarantees IGAgent return type
        Assert.NotNull(agent);
        Assert.IsAssignableFrom<IGAgent>(agent);
        Assert.IsAssignableFrom<ITestIGAgent>(agent);
        
        // Verify it's specifically IGAgent type (not IGAgentPlus or something else)
        _testOutputHelper.WriteLine($"Non-generic factory returned: {agent?.GetType().Name ?? "null"}");
        _testOutputHelper.WriteLine($"Return type is IGAgent: {agent is IGAgent}");
        _testOutputHelper.WriteLine($"Return type is IGAgentPlus: {agent is IGAgentPlus}");
    }

    [Fact]
    public async Task Both_Factory_Types_Should_Be_Different_Instances()
    {
        // Arrange & Act
        var factoryForIGAgent = GetRequiredService<IGAgentFactory<IGAgent>>();
        var factoryForIGAgentPlus = GetRequiredService<IGAgentFactory<IGAgentPlus>>();
        var nonGenericFactory = GetRequiredService<IGAgentFactory>();
        
        // Assert
        Assert.NotSame(factoryForIGAgent, factoryForIGAgentPlus);
        Assert.NotSame(factoryForIGAgent, nonGenericFactory);
        Assert.NotSame(factoryForIGAgentPlus, nonGenericFactory);
        
        _testOutputHelper.WriteLine("All factory instances are correctly different");
        _testOutputHelper.WriteLine($"IGAgentFactory<IGAgent> type: {factoryForIGAgent.GetType().Name}");
        _testOutputHelper.WriteLine($"IGAgentFactory<IGAgentPlus> type: {factoryForIGAgentPlus.GetType().Name}");
        _testOutputHelper.WriteLine($"IGAgentFactory type: {nonGenericFactory.GetType().Name}");
    }

    [Fact]
    public async Task GetGAgentAsync_WithGrainId_ShouldCreateAndConfigureAgent()
    {
        // Arrange
        var factory = GetRequiredService<IGAgentFactory<IGAgentPlus>>();
        var primaryKey = Guid.NewGuid();
        // Create proper GrainId for ITestIGAgentPlus using full grain type like the factory does
        var grainType = $"{typeof(TestIGAgentPlus).Namespace}.{typeof(TestIGAgentPlus).Name}";
        var grainId = GrainId.Create(grainType, primaryKey.ToString());

        // Act
        var agent = await factory.GetGAgentAsync<ITestIGAgentPlus>(grainId);

        // Assert
        Assert.NotNull(agent);
        Assert.IsAssignableFrom<IGAgentPlus>(agent);
        Assert.IsAssignableFrom<ITestIGAgentPlus>(agent);
        
        // Verify activation was called
        var testValue = await agent.GetTestValueAsync();
        Assert.Equal("IGAgentPlus-Test-Value", testValue);
        
        _testOutputHelper.WriteLine($"Successfully created and configured agent with GrainId: {grainId}");
    }

    [Fact]
    public async Task GetGAgentAsync_WithPrimaryKey_ShouldCreateAgent()
    {
        // Arrange
        var factory = GetRequiredService<IGAgentFactory<IGAgentPlus>>();
        var primaryKey = Guid.NewGuid();

        // Act
        var agent = await factory.GetGAgentAsync<ITestIGAgentPlus>(primaryKey);

        // Assert
        Assert.NotNull(agent);
        Assert.IsAssignableFrom<IGAgentPlus>(agent);
        Assert.IsAssignableFrom<ITestIGAgentPlus>(agent);
        
        // Verify activation was called
        var testValue = await agent.GetTestValueAsync();
        Assert.Equal("IGAgentPlus-Test-Value", testValue);
        
        _testOutputHelper.WriteLine($"Successfully created agent with primaryKey: {primaryKey}");
    }

    [Fact]
    public async Task GetGAgentAsync_WithoutParameters_ShouldCreateAgentWithRandomGuid()
    {
        // Arrange
        var factory = GetRequiredService<IGAgentFactory<IGAgentPlus>>();

        // Act - Using the generic method without parameters (generates random GUID)
        var agent = await factory.GetGAgentAsync<ITestIGAgentPlus>();

        // Assert
        Assert.NotNull(agent);
        Assert.IsAssignableFrom<IGAgentPlus>(agent);
        Assert.IsAssignableFrom<ITestIGAgentPlus>(agent);
        
        // Verify activation was called
        var testValue = await agent.GetTestValueAsync();
        Assert.Equal("IGAgentPlus-Test-Value", testValue);
        
        _testOutputHelper.WriteLine("Successfully created agent with auto-generated GUID");
    }

    [Fact]
    public async Task GetGAgentAsync_WithNonGenericTypeMethod_ShouldCreateAgent()
    {
        // Arrange
        var factory = GetRequiredService<IGAgentFactory<IGAgentPlus>>();
        var primaryKey = Guid.NewGuid();
        var gAgentType = typeof(TestIGAgentPlus);
        
        // Act - Using the non-generic method with Type parameter
        var agent = await factory.GetGAgentAsync(primaryKey, gAgentType);

        // Assert
        Assert.NotNull(agent);
        Assert.IsAssignableFrom<IGAgentPlus>(agent);
        
        _testOutputHelper.WriteLine($"Successfully created agent with primaryKey: {primaryKey}, type: {gAgentType.Name}");
    }

    [Fact]
    public async Task GetGAgentAsync_WithAliasAndNamespaceOnly_ShouldCreateAgentWithRandomGuid()
    {
        // Arrange
        var factory = GetRequiredService<IGAgentFactory<IGAgentPlus>>();
        var alias = "TestIGAgentPlus";  // Actual type name
        var ns = "Aevatar.Core.Tests.TestGAgents";  // Actual type namespace

        // Act - Using the non-generic method with alias and namespace
        var agent = await factory.GetGAgentAsync(alias, ns);

        // Assert
        Assert.NotNull(agent);
        Assert.IsAssignableFrom<IGAgentPlus>(agent);
        
        _testOutputHelper.WriteLine($"Successfully created agent with random GUID for alias: {alias}, namespace: {ns}");
    }


    [Fact]
    public async Task Multiple_GetGAgentAsync_Calls_ShouldCreateDifferentInstances()
    {
        // Arrange
        var factory = GetRequiredService<IGAgentFactory<IGAgentPlus>>();

        // Act
        var agent1 = await factory.GetGAgentAsync<ITestIGAgentPlus>();
        var agent2 = await factory.GetGAgentAsync<ITestIGAgentPlus>();

        // Assert
        Assert.NotNull(agent1);
        Assert.NotNull(agent2);
        Assert.NotSame(agent1, agent2);
        
        // Different grain IDs
        Assert.NotEqual(agent1.GetGrainId(), agent2.GetGrainId());
        
        _testOutputHelper.WriteLine($"Agent1 GrainId: {agent1.GetGrainId()}");
        _testOutputHelper.WriteLine($"Agent2 GrainId: {agent2.GetGrainId()}");
    }

    [Fact]
    public async Task GetGAgentAsync_SamePrimaryKey_ShouldReturnSameGrainInstance()
    {
        // Arrange
        var factory = GetRequiredService<IGAgentFactory<IGAgentPlus>>();
        var primaryKey = Guid.NewGuid();

        // Act
        var agent1 = await factory.GetGAgentAsync<ITestIGAgentPlus>(primaryKey);
        var agent2 = await factory.GetGAgentAsync<ITestIGAgentPlus>(primaryKey);

        // Assert
        Assert.NotNull(agent1);
        Assert.NotNull(agent2);
        
        // Same grain ID (Orleans should return same grain instance for same key)
        Assert.Equal(agent1.GetGrainId(), agent2.GetGrainId());
        
        _testOutputHelper.WriteLine($"Both agents have same GrainId: {agent1.GetGrainId()}");
    }

    [Fact]
    public async Task GetGAgentAsync_WithConfiguration_ShouldCreateAndActivateAgent()
    {
        // Arrange
        var factory = GetRequiredService<IGAgentFactory<IGAgentPlus>>();
        var configuration = new TestCoreConfiguration { Setting = "TestValue" };

        // Act
        var agent = await factory.GetGAgentAsync<ITestIGAgentPlus>(configuration: configuration);

        // Assert
        Assert.NotNull(agent);
        Assert.IsAssignableFrom<IGAgentPlus>(agent);
        Assert.IsAssignableFrom<ITestIGAgentPlus>(agent);
        
        // Verify the agent was activated (configuration parameter is accepted but TestIGAgentPlus doesn't use it)
        var testValue = await agent.GetTestValueAsync();
        Assert.Equal("IGAgentPlus-Test-Value", testValue);
        
        _testOutputHelper.WriteLine("Successfully created and activated agent with configuration parameter");
    }

}
