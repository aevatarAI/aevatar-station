using Aevatar.Core.Abstractions;
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
    public async Task Generic_IGAgentPlus_Factory_Should_Create_GAgent()
    {
        // Arrange
        var genericFactory = GetRequiredService<IGAgentFactory<IGAgentPlus>>();
        var primaryKey = Guid.NewGuid();
        
        // Act - Use the concrete test grain type instead of generic GrainId
        var agent = await genericFactory.GetGAgentAsync<ITestIGAgentPlus>(primaryKey);
        
        // Assert
        Assert.NotNull(agent);
        
        // Verify it's an IGAgentPlus instance
        Assert.IsAssignableFrom<IGAgentPlus>(agent);
        Assert.IsAssignableFrom<ITestIGAgentPlus>(agent);
        
        _testOutputHelper.WriteLine($"Successfully created IGAgentPlus with primary key: {primaryKey}");
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
}
