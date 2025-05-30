using Aevatar.Core.Abstractions.Plugin;
using Aevatar.Core.Plugin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Reflection;
using Xunit;

namespace Aevatar.Core.Tests.Plugin;

public class AgentPluginLoaderTests : IDisposable
{
    private readonly Mock<ILogger<AgentPluginLoader>> _loggerMock;
    private readonly AgentPluginLoader _pluginLoader;
    private readonly string _testPluginDirectory;
    
    public AgentPluginLoaderTests()
    {
        _loggerMock = new Mock<ILogger<AgentPluginLoader>>();
        var options = Options.Create(new PluginLoadOptions
        {
            IsolateInSeparateContext = true,
            LoadTimeout = TimeSpan.FromSeconds(10)
        });
        
        _pluginLoader = new AgentPluginLoader(_loggerMock.Object, options);
        _testPluginDirectory = Path.Combine(Path.GetTempPath(), $"test-plugins-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testPluginDirectory);
    }

    public void Dispose()
    {
        _pluginLoader?.Dispose();
        if (Directory.Exists(_testPluginDirectory))
        {
            Directory.Delete(_testPluginDirectory, true);
        }
    }

    [Fact]
    public async Task LoadPluginFromBytesAsync_ValidAssembly_ReturnsPlugin()
    {
        // Arrange
        var assemblyBytes = CreateTestPluginAssembly();
        
        // Act
        var plugin = await _pluginLoader.LoadPluginFromBytesAsync(assemblyBytes, typeof(TestAgentPlugin).FullName);
        
        // Assert
        Assert.NotNull(plugin);
        Assert.Equal("TestAgent", plugin.Metadata.Name);
        Assert.Equal("1.0.0", plugin.Metadata.Version);
    }

    [Fact]
    public async Task LoadPluginFromBytesAsync_InvalidAssembly_ThrowsException()
    {
        // Arrange
        var invalidBytes = new byte[] { 1, 2, 3, 4, 5 };
        
        // Act & Assert
        await Assert.ThrowsAsync<PluginLoadException>(
            () => _pluginLoader.LoadPluginFromBytesAsync(invalidBytes));
    }

    [Fact]
    public async Task LoadPluginFromAssemblyAsync_FileNotFound_ThrowsException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testPluginDirectory, "nonexistent.dll");
        
        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _pluginLoader.LoadPluginFromAssemblyAsync(nonExistentPath));
    }

    [Fact]
    public async Task LoadPluginFromAssemblyAsync_ValidFile_ReturnsPlugin()
    {
        // Arrange
        var assemblyPath = CreateTestPluginFile();
        
        // Act
        var plugin = await _pluginLoader.LoadPluginFromAssemblyAsync(assemblyPath, typeof(TestAgentPlugin).FullName);
        
        // Assert
        Assert.NotNull(plugin);
        Assert.Equal("TestAgent", plugin.Metadata.Name);
    }

    [Fact]
    public async Task GetAvailablePluginsAsync_ScansCorrectly()
    {
        // Arrange
        CreateTestPluginFileInPluginsDirectory();
        
        // Act
        var plugins = await _pluginLoader.GetAvailablePluginsAsync();
        
        // Assert
        Assert.NotEmpty(plugins);
        Assert.Contains(plugins, p => p.Name == "TestAgent");
    }

    [Fact]
    public async Task UnloadPluginAsync_DoesNotThrow()
    {
        // Arrange
        var pluginName = "TestPlugin";
        
        // Act & Assert
        await _pluginLoader.UnloadPluginAsync(pluginName);
        // Should not throw
    }

    private byte[] CreateTestPluginAssembly()
    {
        // Get the current assembly that contains our test plugin
        var currentAssembly = Assembly.GetExecutingAssembly();
        var assemblyPath = currentAssembly.Location;
        return File.ReadAllBytes(assemblyPath);
    }

    private string CreateTestPluginFile()
    {
        var assemblyBytes = CreateTestPluginAssembly();
        var pluginPath = Path.Combine(_testPluginDirectory, "TestAgent.dll");
        File.WriteAllBytes(pluginPath, assemblyBytes);
        return pluginPath;
    }

    private void CreateTestPluginFileInPluginsDirectory()
    {
        // Create plugins directory structure that the scanner expects
        var pluginsDir = Path.Combine(Environment.CurrentDirectory, "plugins");
        var testAgentDir = Path.Combine(pluginsDir, "TestAgent");
        Directory.CreateDirectory(testAgentDir);
        
        var assemblyBytes = CreateTestPluginAssembly();
        var pluginPath = Path.Combine(testAgentDir, "TestAgent.dll");
        File.WriteAllBytes(pluginPath, assemblyBytes);
    }
}

public class AgentPluginRegistryTests
{
    private readonly Mock<ILogger<AgentPluginRegistry>> _loggerMock;
    private readonly AgentPluginRegistry _registry;
    
    public AgentPluginRegistryTests()
    {
        _loggerMock = new Mock<ILogger<AgentPluginRegistry>>();
        _registry = new AgentPluginRegistry(_loggerMock.Object);
    }

    [Fact]
    public void RegisterPlugin_ValidPlugin_RegistersSuccessfully()
    {
        // Arrange
        var plugin = new Mock<IAgentPlugin>();
        var agentId = "test-agent-123";
        
        // Act
        _registry.RegisterPlugin(agentId, plugin.Object);
        
        // Assert
        var retrievedPlugin = _registry.GetPlugin(agentId);
        Assert.Equal(plugin.Object, retrievedPlugin);
    }

    [Fact]
    public void GetPlugin_NonExistentAgent_ReturnsNull()
    {
        // Arrange
        var agentId = "non-existent-agent";
        
        // Act
        var plugin = _registry.GetPlugin(agentId);
        
        // Assert
        Assert.Null(plugin);
    }

    [Fact]
    public void UnregisterPlugin_ExistingAgent_ReturnsTrue()
    {
        // Arrange
        var plugin = new Mock<IAgentPlugin>();
        var agentId = "test-agent-123";
        _registry.RegisterPlugin(agentId, plugin.Object);
        
        // Act
        var result = _registry.UnregisterPlugin(agentId);
        
        // Assert
        Assert.True(result);
        Assert.Null(_registry.GetPlugin(agentId));
    }

    [Fact]
    public void UnregisterPlugin_NonExistentAgent_ReturnsFalse()
    {
        // Arrange
        var agentId = "non-existent-agent";
        
        // Act
        var result = _registry.UnregisterPlugin(agentId);
        
        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetAllPlugins_MultiplePlugins_ReturnsAll()
    {
        // Arrange
        var plugin1 = new Mock<IAgentPlugin>();
        var plugin2 = new Mock<IAgentPlugin>();
        _registry.RegisterPlugin("agent1", plugin1.Object);
        _registry.RegisterPlugin("agent2", plugin2.Object);
        
        // Act
        var allPlugins = _registry.GetAllPlugins().ToList();
        
        // Assert
        Assert.Equal(2, allPlugins.Count);
        Assert.Contains(allPlugins, p => p.AgentId == "agent1");
        Assert.Contains(allPlugins, p => p.AgentId == "agent2");
    }
}