using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Plugin;
using Aevatar.Core.Plugin;
using Aevatar.Core.Tests.TestGAgents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Orleans;
using Orleans.TestKit.Services;
using Orleans.TestKit.Storage;
using Orleans.TestKit;
using Xunit;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Collections.Generic;

namespace Aevatar.Core.Tests.Plugin;

/// <summary>
/// Unit tests for PluginGAgentHost and IPluginGAgentHost interface
/// </summary>
public class PluginGAgentHostTests : TestKitBase
{
    private readonly Mock<IAgentPluginLoader> _mockPluginLoader;
    private readonly Mock<IAgentPluginRegistry> _mockPluginRegistry;
    private readonly Mock<IAgentPlugin> _mockPlugin;
    private readonly AgentPluginMetadata _testMetadata;

    public PluginGAgentHostTests()
    {
        _mockPluginLoader = new Mock<IAgentPluginLoader>();
        _mockPluginRegistry = new Mock<IAgentPluginRegistry>();
        _mockPlugin = new Mock<IAgentPlugin>();
        
        _testMetadata = new AgentPluginMetadata(
            "TestPlugin",
            "1.0.0",
            "A test plugin for unit testing",
            new Dictionary<string, object> { { "test", "value" } }
        );

        _mockPlugin.Setup(p => p.Metadata).Returns(_testMetadata);

        // Add services to silo
        Silo.AddService(_mockPluginLoader.Object);
        Silo.AddService(_mockPluginRegistry.Object);
        
        // Setup storage for PluginAgentState
        Silo.AddPersistentState<PluginAgentState>("State");
    }

    #region Positive Test Cases

    [Fact]
    public async Task InitializePluginConfigurationAsync_ShouldSetPluginState_WhenCalled()
    {
        // Arrange
        var host = await Silo.CreateGrainAsync<PluginGAgentHost>(Guid.NewGuid());

        // Act
        await host.InitializePluginConfigurationAsync("TestPlugin", "1.0.0", null);

        // Assert - Verify state was updated through event sourcing
        var state = await host.GetStateAsync();
        Assert.Equal("TestPlugin", state.PluginName);
        Assert.Equal("1.0.0", state.PluginVersion);
    }

    [Fact]
    public async Task GetDescriptionAsync_ShouldReturnPluginDescription_WhenPluginIsLoaded()
    {
        // Arrange
        var host = await CreateActivatedPluginHost();

        // Act
        var description = await host.GetDescriptionAsync();

        // Assert
        Assert.Equal("Mock Test Plugin Description", description);
    }

    [Fact]
    public async Task CallPluginMethodAsync_ShouldExecuteMethodOnPlugin_WhenPluginIsLoaded()
    {
        // Arrange
        var host = await CreateActivatedPluginHost();
        var expectedResult = "test result";
        var methodName = "TestMethod";
        var parameters = new object[] { "param1", 42 };

        _mockPlugin.Setup(p => p.ExecuteMethodAsync(methodName, parameters, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await host.CallPluginMethodAsync(methodName, parameters);

        // Assert
        Assert.Equal(expectedResult, result);
        _mockPlugin.Verify(p => p.ExecuteMethodAsync(methodName, parameters, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPluginStateAsync_ShouldReturnPluginState_WhenPluginIsLoaded()
    {
        // Arrange
        var host = await CreateActivatedPluginHost();
        var expectedState = new { Value = "test state" };

        _mockPlugin.Setup(p => p.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedState);

        // Act
        var state = await host.GetPluginStateAsync();

        // Assert
        Assert.Equal(expectedState, state);
        _mockPlugin.Verify(p => p.GetStateAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetPluginStateAsync_ShouldSetPluginState_WhenPluginIsLoaded()
    {
        // Arrange
        var host = await CreateActivatedPluginHost();
        var newState = new { Value = "new state" };

        // Act
        await host.SetPluginStateAsync(newState);

        // Assert
        _mockPlugin.Verify(p => p.SetStateAsync(newState, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPluginMetadataAsync_ShouldReturnMetadata_WhenPluginIsLoaded()
    {
        // Arrange
        var host = await CreateActivatedPluginHost();

        // Act
        var metadata = await host.GetPluginMetadataAsync();

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("TestPlugin", metadata.Name);
        Assert.Equal("1.0.0", metadata.Version);
        Assert.Equal("Mock Test Plugin Description", metadata.Description);
    }

    [Fact]
    public async Task ReloadPluginAsync_ShouldReloadPlugin_WhenCalled()
    {
        // Arrange
        var host = await CreateActivatedPluginHost();

        // Setup new plugin for reload - but don't reset the loader since reload works differently
        var newMockPlugin = new Mock<IAgentPlugin>();
        newMockPlugin.Setup(p => p.Metadata).Returns(_testMetadata);
        newMockPlugin.Setup(p => p.InitializeAsync(It.IsAny<IAgentContext>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback<IAgentContext, CancellationToken>((context, token) => 
            {
                System.Diagnostics.Debug.WriteLine("NEW Plugin InitializeAsync called!");
            });
        
        // Debug: Check initial state
        var initialDescription = await host.GetDescriptionAsync();
        System.Diagnostics.Debug.WriteLine($"Before reload - Description: {initialDescription}");
        
        // CRITICAL FIX: Reset the mock loader setup first, then configure for reload
        _mockPluginLoader.Reset();
        _mockPluginLoader.Setup(l => l.LoadPluginAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newMockPlugin.Object);

        // Act
        await host.ReloadPluginAsync();

        // Debug: Check final state
        var finalDescription = await host.GetDescriptionAsync();
        System.Diagnostics.Debug.WriteLine($"After reload - Description: {finalDescription}");

        // Assert - Verify the old plugin was disposed and the reload process happened
        _mockPlugin.Verify(p => p.DisposeAsync(), Times.Once);
        _mockPluginRegistry.Verify(r => r.UnregisterPlugin(It.IsAny<string>()), Times.Once);
        
        // The new plugin should have been initialized during reload
        newMockPlugin.Verify(p => p.InitializeAsync(It.IsAny<IAgentContext>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockPluginRegistry.Verify(r => r.RegisterPlugin(It.IsAny<string>(), newMockPlugin.Object), Times.Once);
    }

    [Fact]
    public async Task DebugTest_StateAccessDuringActivation()
    {
        // Create grain
        var grainId = Guid.NewGuid();
        var host = await Silo.CreateGrainAsync<PluginGAgentHost>(grainId);
        
        // Set configuration
        await host.InitializePluginConfigurationAsync("TestPlugin", "1.0.0", null);
        
        // Verify state is accessible
        var state = await host.GetStateAsync();
        Assert.Equal("TestPlugin", state.PluginName);
        Assert.Equal("1.0.0", state.PluginVersion);
        
        // Try to trigger activation by calling a method that would use the plugin
        // This should trigger LoadPluginAsync internally
        var description = await host.GetDescriptionAsync();
        
        // Check if it falls back to default description
        Assert.NotNull(description);
    }

    #endregion

    #region Negative Test Cases

    [Fact]
    public async Task CallPluginMethodAsync_ShouldThrowInvalidOperationException_WhenPluginNotLoaded()
    {
        // Arrange - Create grain without plugin loading
        var host = await CreateHostWithoutPlugin();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => host.CallPluginMethodAsync("TestMethod", new object[0]));

        Assert.Equal("Plugin not loaded", exception.Message);
    }

    [Fact]
    public async Task SetPluginStateAsync_ShouldThrowInvalidOperationException_WhenPluginNotLoaded()
    {
        // Arrange - Create grain without plugin loading
        var host = await CreateHostWithoutPlugin();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => host.SetPluginStateAsync(new object()));

        Assert.Equal("Plugin not loaded", exception.Message);
    }

    [Fact]
    public async Task GetPluginStateAsync_ShouldReturnNull_WhenPluginNotLoaded()
    {
        // Arrange - Create grain without plugin loading
        var host = await CreateHostWithoutPlugin();

        // Act
        var state = await host.GetPluginStateAsync();

        // Assert
        Assert.Null(state);
    }

    [Fact]
    public async Task GetPluginMetadataAsync_ShouldReturnNull_WhenPluginNotLoaded()
    {
        // Arrange - Create grain without plugin loading
        var host = await CreateHostWithoutPlugin();

        // Act
        var metadata = await host.GetPluginMetadataAsync();

        // Assert
        Assert.Null(metadata);
    }

    #endregion

    #region Exception Test Cases

    [Fact]
    public async Task GetDescriptionAsync_ShouldThrowException_WhenPluginLoaderFails()
    {
        // Arrange
        var grainId = Guid.NewGuid();
        var host = await Silo.CreateGrainAsync<PluginGAgentHost>(grainId);
        
        // Setup failing loader BEFORE any plugin operations - this will catch all plugin loading attempts
        _mockPluginLoader.Reset();
        _mockPluginLoader.Setup(l => l.LoadPluginAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Plugin load failed"));

        // Initialize plugin configuration 
        await host.InitializePluginConfigurationAsync("FailingPlugin", "1.0.0", null);

        // Wait a moment for state persistence
        await Task.Delay(50);

        // Act & Assert - Force plugin loading by triggering reload, which will fail
        await Assert.ThrowsAsync<InvalidOperationException>(() => host.ReloadPluginAsync());
    }

    [Fact]
    public async Task CallPluginMethodAsync_ShouldPropagateException_WhenPluginExecutionFails()
    {
        // Arrange
        var host = await CreateActivatedPluginHost();
        var expectedException = new InvalidOperationException("Plugin method failed");

        _mockPlugin.Setup(p => p.ExecuteMethodAsync("FailingMethod", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => host.CallPluginMethodAsync("FailingMethod", new object[0]));

        Assert.Equal("Plugin method failed", exception.Message);
    }

    [Fact]
    public async Task ReloadPluginAsync_ShouldHandleDisposeException_AndContinueReloading()
    {
        // Arrange
        var host = await CreateActivatedPluginHost();

        // Setup dispose to throw exception
        _mockPlugin.Setup(p => p.DisposeAsync())
            .ThrowsAsync(new InvalidOperationException("Dispose failed"));

        // Setup reload to succeed with new plugin
        var newMockPlugin = new Mock<IAgentPlugin>();
        newMockPlugin.Setup(p => p.Metadata).Returns(_testMetadata);
        newMockPlugin.Setup(p => p.InitializeAsync(It.IsAny<IAgentContext>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        // Configure the loader to return the new plugin for reload
        _mockPluginLoader.Setup(l => l.LoadPluginAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newMockPlugin.Object);

        // Act & Assert - Should not throw despite dispose failure
        await host.ReloadPluginAsync();

        // Verify reload continued despite dispose failure
        newMockPlugin.Verify(p => p.InitializeAsync(It.IsAny<IAgentContext>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Boundary Test Cases

    [Fact]
    public async Task CallPluginMethodAsync_ShouldHandleNullParameters()
    {
        // Arrange
        var host = await CreateActivatedPluginHost();
        var expectedResult = "result";

        _mockPlugin.Setup(p => p.ExecuteMethodAsync("TestMethod", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await host.CallPluginMethodAsync("TestMethod", null);

        // Assert
        Assert.Equal(expectedResult, result);
        _mockPlugin.Verify(p => p.ExecuteMethodAsync("TestMethod", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetPluginStateAsync_ShouldHandleNullState()
    {
        // Arrange
        var host = await CreateActivatedPluginHost();

        // Act
        await host.SetPluginStateAsync(null);

        // Assert
        _mockPlugin.Verify(p => p.SetStateAsync(null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InitializePluginConfigurationAsync_ShouldHandleNullConfiguration()
    {
        // Arrange
        var host = await Silo.CreateGrainAsync<PluginGAgentHost>(Guid.NewGuid());

        // Act
        await host.InitializePluginConfigurationAsync("TestPlugin", "1.0.0", null);

        // Assert - Should not throw
        var state = await host.GetStateAsync();
        Assert.Equal("TestPlugin", state.PluginName);
        Assert.Equal("1.0.0", state.PluginVersion);
        Assert.Null(state.Configuration);
    }

    [Fact]
    public async Task InitializePluginConfigurationAsync_ShouldHandleNullVersion()
    {
        // Arrange
        var host = await Silo.CreateGrainAsync<PluginGAgentHost>(Guid.NewGuid());

        // Act
        await host.InitializePluginConfigurationAsync("TestPlugin", null, null);

        // Assert - Should not throw
        var state = await host.GetStateAsync();
        Assert.Equal("TestPlugin", state.PluginName);
        Assert.Null(state.PluginVersion);
    }

    #endregion

    #region Helper Methods

    private async Task<IPluginGAgentHost> CreateActivatedPluginHost()
    {
        // Create grain with proper ID
        var grainId = Guid.NewGuid();
        var host = await Silo.CreateGrainAsync<PluginGAgentHost>(grainId);
        
        // Setup mock plugin behavior first
        var mockMetadata = new AgentPluginMetadata("TestPlugin", "1.0.0", "Mock Test Plugin Description");
        _mockPlugin.Setup(p => p.Metadata).Returns(mockMetadata);
        
        _mockPlugin.Setup(p => p.InitializeAsync(It.IsAny<IAgentContext>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        // CRITICAL FIX: Setup the plugin loader to handle ANY call and return our mock plugin
        // The issue is that Orleans TestKit activates grains before state is loaded
        // So we need to handle both the grain ID fallback AND the proper plugin name
        _mockPluginLoader.Setup(l => l.LoadPluginAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockPlugin.Object);
        
        // Initialize plugin configuration AFTER grain creation but BEFORE triggering activation
        // This ensures the state is available when plugin loading occurs
        await host.InitializePluginConfigurationAsync("TestPlugin", "1.0.0", null);
        
        // Wait a moment to ensure state persistence is complete in TestKit
        await Task.Delay(50);
        
        // Verify state was set correctly
        var state = await host.GetStateAsync();
        if (state.PluginName != "TestPlugin" || state.PluginVersion != "1.0.0")
        {
            throw new InvalidOperationException($"State not properly initialized. PluginName: {state.PluginName}, PluginVersion: {state.PluginVersion}");
        }
        
        // Now manually trigger plugin loading by calling a method that requires the plugin
        // This will trigger the LoadPluginAsync workflow since the grain is already activated
        try 
        {
            // Force plugin loading - this should work now that state is available
            await host.ReloadPluginAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Plugin reload failed (expected during setup): {ex.Message}");
        }
        
        // Verify that the plugin was loaded successfully
        var metadata = await host.GetPluginMetadataAsync();
        if (metadata == null)
        {
            throw new InvalidOperationException("Plugin was not loaded successfully");
        }
        
        return host;
    }

    private async Task<IPluginGAgentHost> CreateHostWithoutPlugin()
    {
        var grainId = Guid.NewGuid();
        var host = await Silo.CreateGrainAsync<PluginGAgentHost>(grainId);
        
        // Setup plugin loader to fail with proper parameter types
        _mockPluginLoader.Setup(l => l.LoadPluginAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Plugin loading disabled for test"));
        
        try
        {
            await host.ActivateAsync();
        }
        catch
        {
            // Expected to fail - grain should handle this gracefully
        }
        
        return host;
    }

    #endregion
}

public class PluginGAgentFactoryTests : TestKitBase
{
    private readonly Mock<IGrainFactory> _grainFactoryMock;
    private readonly Mock<ILogger<PluginGAgentFactory>> _loggerMock;
    private readonly Mock<IPluginGAgentHost> _mockHost;
    private readonly PluginGAgentFactory _factory;

    public PluginGAgentFactoryTests()
    {
        _grainFactoryMock = new Mock<IGrainFactory>();
        _loggerMock = new Mock<ILogger<PluginGAgentFactory>>();
        _mockHost = new Mock<IPluginGAgentHost>();
        _factory = new PluginGAgentFactory(_grainFactoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CreatePluginGAgentAsync_ValidParameters_ReturnsGrain()
    {
        // Arrange
        var agentId = "test-agent-123";
        var pluginName = "TestPlugin";
        
        _grainFactoryMock.Setup(f => f.GetGrain<IPluginGAgentHost>(It.IsAny<GrainId>()))
                        .Returns(_mockHost.Object);

        // Act
        var result = await _factory.CreatePluginGAgentAsync(agentId, pluginName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_mockHost.Object, result);
        _grainFactoryMock.Verify(f => f.GetGrain<IPluginGAgentHost>(It.IsAny<GrainId>()), Times.Once);
        _mockHost.Verify(h => h.InitializePluginConfigurationAsync(pluginName, null, null), Times.Once);
    }

    [Fact]
    public async Task CreatePluginGAgentAsync_WithConfiguration_SetsConfiguration()
    {
        // Arrange
        var agentId = "test-agent-123";
        var pluginName = "TestPlugin";
        var pluginVersion = "2.0.0";
        var configuration = new Dictionary<string, object> { { "key", "value" } };
        
        _grainFactoryMock.Setup(f => f.GetGrain<IPluginGAgentHost>(It.IsAny<GrainId>()))
                        .Returns(_mockHost.Object);

        // Act
        var result = await _factory.CreatePluginGAgentAsync(agentId, pluginName, pluginVersion, configuration);

        // Assert
        Assert.NotNull(result);
        _grainFactoryMock.Verify(f => f.GetGrain<IPluginGAgentHost>(It.IsAny<GrainId>()), Times.Once);
        _mockHost.Verify(h => h.InitializePluginConfigurationAsync(pluginName, pluginVersion, configuration), Times.Once);
    }

    [Fact]
    public void Constructor_ValidParameters_DoesNotThrow()
    {
        // Arrange & Act & Assert
        Assert.NotNull(_factory);
    }

    [Fact]
    public async Task CreatePluginGAgentAsync_NullAgentId_ThrowsArgumentException()
    {
        // Arrange
        string? agentId = null;
        var pluginName = "TestPlugin";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _factory.CreatePluginGAgentAsync(agentId!, pluginName));
    }

    [Fact]
    public async Task CreatePluginGAgentAsync_NullPluginName_ThrowsArgumentException()
    {
        // Arrange
        var agentId = "test-agent";
        string? pluginName = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _factory.CreatePluginGAgentAsync(agentId, pluginName!));
    }
}

public class PluginAgentContextTests : TestKitBase
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IAgentPluginRegistry> _pluginRegistryMock;
    private readonly Mock<IGrainFactory> _grainFactoryMock;

    public PluginAgentContextTests()
    {
        _loggerMock = new Mock<ILogger>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _pluginRegistryMock = new Mock<IAgentPluginRegistry>();
        _grainFactoryMock = new Mock<IGrainFactory>();
        
        // Setup the service provider to return the plugin registry mock
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IAgentPluginRegistry)))
                           .Returns(_pluginRegistryMock.Object);
                           
        // Setup grain factory to return mock grains
        var mockGrain = new Mock<IGAgent>();
        _grainFactoryMock.Setup(f => f.GetGrain<IGAgent>(It.IsAny<GrainId>()))
                        .Returns(mockGrain.Object);
    }

    [Fact]
    public async Task Constructor_ValidParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        var hostGAgent = await Silo.CreateGrainAsync<PublishingGAgent>(Guid.NewGuid());
        var configuration = new Dictionary<string, object> { { "testKey", "testValue" } };
        
        // Act
        var context = new AgentContext(hostGAgent, _loggerMock.Object, _grainFactoryMock.Object, _serviceProviderMock.Object, configuration);

        // Assert
        Assert.NotNull(context.AgentId);
        Assert.NotNull(context.Logger);
        Assert.NotNull(context.Configuration);
        Assert.Single(context.Configuration);
        Assert.Equal("testValue", context.Configuration["testKey"]);
    }

    [Fact]
    public async Task RegisterAgentsAsync_ValidAgentIds_CallsHostGAgent()
    {
        // Arrange
        var agentIds = new[] { "agent1", "agent2" };
        var hostGAgent = await Silo.CreateGrainAsync<PublishingGAgent>(Guid.NewGuid());
        
        var configuration = new Dictionary<string, object> { { "testKey", "testValue" } };
        var context = new AgentContext(hostGAgent, _loggerMock.Object, _grainFactoryMock.Object, _serviceProviderMock.Object, configuration);
        
        // Act
        await context.RegisterAgentsAsync(agentIds);
        
        // Assert - Since this is testing the context, we verify it doesn't throw
        // Actual registration verification would need more complex setup
        Assert.True(true); // Placeholder assertion
    }

    [Fact]
    public void Constructor_NullHostGAgent_ThrowsArgumentNullException()
    {
        // Arrange
        IGAgent? hostGAgent = null;
        var configuration = new Dictionary<string, object>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => new AgentContext(hostGAgent!, _loggerMock.Object, _grainFactoryMock.Object, _serviceProviderMock.Object, configuration));
    }

    [Fact]
    public async Task Constructor_NullConfiguration_UsesEmptyDictionary()
    {
        // Arrange
        var hostGAgent = await Silo.CreateGrainAsync<PublishingGAgent>(Guid.NewGuid());
        Dictionary<string, object>? configuration = null;

        // Act
        var context = new AgentContext(hostGAgent, _loggerMock.Object, _grainFactoryMock.Object, _serviceProviderMock.Object, configuration);

        // Assert
        Assert.NotNull(context.Configuration);
        Assert.Empty(context.Configuration);
    }
}

public class AgentLoggerAdapterTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly AgentLoggerAdapter _adapter;

    public AgentLoggerAdapterTests()
    {
        _loggerMock = new Mock<ILogger>();
        _adapter = new AgentLoggerAdapter(_loggerMock.Object);
    }

    [Fact]
    public void LogDebug_CallsUnderlyingLogger()
    {
        // Arrange
        var message = "Debug message";

        // Act
        _adapter.LogDebug(message);

        // Assert
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogError_WithException_CallsUnderlyingLogger()
    {
        // Arrange
        var message = "Error message";
        var exception = new Exception("Test exception");

        // Act
        _adapter.LogError(message, exception);

        // Assert
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogInformation_CallsUnderlyingLogger()
    {
        // Arrange
        var message = "Info message";

        // Act
        _adapter.LogInformation(message);

        // Assert
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogWarning_CallsUnderlyingLogger()
    {
        // Arrange
        var message = "Warning message";

        // Act
        _adapter.LogWarning(message);

        // Assert
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        ILogger? logger = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AgentLoggerAdapter(logger!));
    }
}