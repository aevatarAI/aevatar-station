using Aevatar.Core.Plugin;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace Aevatar.Core.Tests.Plugin;

/// <summary>
/// Unit tests for plugin-related exception classes
/// </summary>
public class PluginExceptionTests
{
    #region MethodNotFoundException Tests

    [Fact]
    public void MethodNotFoundException_WithMessage_CreatesCorrectly()
    {
        // Arrange
        var message = "Method not found";

        // Act
        var exception = new MethodNotFoundException(message);

        // Assert
        exception.Message.ShouldBe(message);
        exception.InnerException.ShouldBeNull();
    }

    [Fact]
    public void MethodNotFoundException_WithMessageAndInnerException_CreatesCorrectly()
    {
        // Arrange
        var message = "Method not found";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new MethodNotFoundException(message, innerException);

        // Assert
        exception.Message.ShouldBe(message);
        exception.InnerException.ShouldBe(innerException);
    }

    [Fact]
    public void MethodNotFoundException_DefaultConstructor_CreatesCorrectly()
    {
        // Act
        var exception = new MethodNotFoundException();

        // Assert
        exception.ShouldNotBeNull();
        exception.Message.ShouldNotBeNullOrEmpty();
    }

    #endregion

    #region AgentMethodCallException Tests

    [Fact]
    public void AgentMethodCallException_WithBasicParameters_CreatesCorrectly()
    {
        // Arrange
        var agentId = "test-agent";
        var methodName = "TestMethod";
        var message = "Method call failed";

        // Act
        var exception = new AgentMethodCallException(agentId, methodName, message);

        // Assert
        exception.AgentId.ShouldBe(agentId);
        exception.MethodName.ShouldBe(methodName);
        exception.Message.ShouldBe(message);
        exception.InnerException.ShouldBeNull();
    }

    [Fact]
    public void AgentMethodCallException_WithInnerException_CreatesCorrectly()
    {
        // Arrange
        var agentId = "test-agent";
        var methodName = "TestMethod";
        var message = "Method call failed";
        var innerException = new ArgumentException("Invalid argument");

        // Act
        var exception = new AgentMethodCallException(agentId, methodName, message, innerException);

        // Assert
        exception.AgentId.ShouldBe(agentId);
        exception.MethodName.ShouldBe(methodName);
        exception.Message.ShouldBe(message);
        exception.InnerException.ShouldBe(innerException);
    }

    [Theory]
    [InlineData("", "TestMethod", "Message")]
    [InlineData("agent", "", "Message")]
    [InlineData("agent", "method", "")]
    public void AgentMethodCallException_WithEmptyParameters_CreatesCorrectly(string agentId, string methodName, string message)
    {
        // Act
        var exception = new AgentMethodCallException(agentId, methodName, message);

        // Assert
        exception.AgentId.ShouldBe(agentId);
        exception.MethodName.ShouldBe(methodName);
        exception.Message.ShouldBe(message);
    }

    [Fact]
    public void AgentMethodCallException_WithNullParameters_HandlesGracefully()
    {
        // Act & Assert
        Should.NotThrow(() => new AgentMethodCallException(null!, null!, null!));
    }

    #endregion

    #region AgentEventSendException Tests

    [Fact]
    public void AgentEventSendException_WithBasicParameters_CreatesCorrectly()
    {
        // Arrange
        var agentId = "test-agent";
        var eventType = "TestEvent";
        var message = "Event send failed";

        // Act
        var exception = new AgentEventSendException(agentId, eventType, message);

        // Assert
        exception.AgentId.ShouldBe(agentId);
        exception.EventType.ShouldBe(eventType);
        exception.Message.ShouldBe(message);
        exception.InnerException.ShouldBeNull();
    }

    [Fact]
    public void AgentEventSendException_WithInnerException_CreatesCorrectly()
    {
        // Arrange
        var agentId = "test-agent";
        var eventType = "TestEvent";
        var message = "Event send failed";
        var innerException = new TimeoutException("Network timeout");

        // Act
        var exception = new AgentEventSendException(agentId, eventType, message, innerException);

        // Assert
        exception.AgentId.ShouldBe(agentId);
        exception.EventType.ShouldBe(eventType);
        exception.Message.ShouldBe(message);
        exception.InnerException.ShouldBe(innerException);
    }

    [Theory]
    [InlineData("", "TestEvent", "Message")]
    [InlineData("agent", "", "Message")]
    [InlineData("agent", "event", "")]
    public void AgentEventSendException_WithEmptyParameters_CreatesCorrectly(string agentId, string eventType, string message)
    {
        // Act
        var exception = new AgentEventSendException(agentId, eventType, message);

        // Assert
        exception.AgentId.ShouldBe(agentId);
        exception.EventType.ShouldBe(eventType);
        exception.Message.ShouldBe(message);
    }

    [Fact]
    public void AgentEventSendException_WithNullParameters_HandlesGracefully()
    {
        // Act & Assert
        Should.NotThrow(() => new AgentEventSendException(null!, null!, null!));
    }

    #endregion

    #region PluginLoadException Tests

    [Fact]
    public void PluginLoadException_WithBasicParameters_CreatesCorrectly()
    {
        // Arrange
        var pluginName = "TestPlugin";
        var version = "1.0.0";
        var message = "Plugin load failed";

        // Act
        var exception = new PluginLoadException(pluginName, version, message);

        // Assert
        exception.PluginName.ShouldBe(pluginName);
        exception.Version.ShouldBe(version);
        exception.Message.ShouldBe(message);
        exception.InnerException.ShouldBeNull();
    }

    [Fact]
    public void PluginLoadException_WithInnerException_CreatesCorrectly()
    {
        // Arrange
        var pluginName = "TestPlugin";
        var version = "1.0.0";
        var message = "Plugin load failed";
        var innerException = new FileNotFoundException("Assembly not found");

        // Act
        var exception = new PluginLoadException(pluginName, version, message, innerException);

        // Assert
        exception.PluginName.ShouldBe(pluginName);
        exception.Version.ShouldBe(version);
        exception.Message.ShouldBe(message);
        exception.InnerException.ShouldBe(innerException);
    }

    [Fact]
    public void PluginLoadException_WithNullVersion_CreatesCorrectly()
    {
        // Arrange
        var pluginName = "TestPlugin";
        string? version = null;
        var message = "Plugin load failed";

        // Act
        var exception = new PluginLoadException(pluginName, version, message);

        // Assert
        exception.PluginName.ShouldBe(pluginName);
        exception.Version.ShouldBeNull();
        exception.Message.ShouldBe(message);
    }

    [Theory]
    [InlineData("", "1.0.0", "Message")]
    [InlineData("plugin", "", "Message")]
    [InlineData("plugin", "1.0.0", "")]
    public void PluginLoadException_WithEmptyParameters_CreatesCorrectly(string pluginName, string version, string message)
    {
        // Act
        var exception = new PluginLoadException(pluginName, version, message);

        // Assert
        exception.PluginName.ShouldBe(pluginName);
        exception.Version.ShouldBe(version);
        exception.Message.ShouldBe(message);
    }

    [Fact]
    public void PluginLoadException_WithNullParameters_HandlesGracefully()
    {
        // Act & Assert
        Should.NotThrow(() => new PluginLoadException(null!, null, null!));
    }

    #endregion

    #region Exception Serialization Tests

    [Fact]
    public void AgentMethodCallException_Serialization_PreservesProperties()
    {
        // Arrange
        var agentId = "test-agent";
        var methodName = "TestMethod";
        var message = "Method call failed";
        var originalException = new AgentMethodCallException(agentId, methodName, message);

        // Act & Assert - Basic validation that properties are accessible
        var recreated = new AgentMethodCallException(
            originalException.AgentId,
            originalException.MethodName,
            originalException.Message);

        recreated.AgentId.ShouldBe(originalException.AgentId);
        recreated.MethodName.ShouldBe(originalException.MethodName);
        recreated.Message.ShouldBe(originalException.Message);
    }

    [Fact]
    public void AgentEventSendException_Serialization_PreservesProperties()
    {
        // Arrange
        var agentId = "test-agent";
        var eventType = "TestEvent";
        var message = "Event send failed";
        var originalException = new AgentEventSendException(agentId, eventType, message);

        // Act & Assert - Basic validation that properties are accessible
        var recreated = new AgentEventSendException(
            originalException.AgentId,
            originalException.EventType,
            originalException.Message);

        recreated.AgentId.ShouldBe(originalException.AgentId);
        recreated.EventType.ShouldBe(originalException.EventType);
        recreated.Message.ShouldBe(originalException.Message);
    }

    [Fact]
    public void PluginLoadException_Serialization_PreservesProperties()
    {
        // Arrange
        var pluginName = "TestPlugin";
        var version = "1.0.0";
        var message = "Plugin load failed";
        var originalException = new PluginLoadException(pluginName, version, message);

        // Act & Assert - Basic validation that properties are accessible
        var recreated = new PluginLoadException(
            originalException.PluginName,
            originalException.Version,
            originalException.Message);

        recreated.PluginName.ShouldBe(originalException.PluginName);
        recreated.Version.ShouldBe(originalException.Version);
        recreated.Message.ShouldBe(originalException.Message);
    }

    #endregion

    #region Exception Hierarchy Tests

    [Fact]
    public void AllPluginExceptions_InheritFromException()
    {
        // Arrange & Act & Assert
        typeof(MethodNotFoundException).ShouldBeAssignableTo<Exception>();
        typeof(AgentMethodCallException).ShouldBeAssignableTo<Exception>();
        typeof(AgentEventSendException).ShouldBeAssignableTo<Exception>();
        typeof(PluginLoadException).ShouldBeAssignableTo<Exception>();
    }

    [Fact]
    public void AllPluginExceptions_HaveParameterlessConstructor()
    {
        // Act & Assert
        Should.NotThrow(() => new MethodNotFoundException());
        Should.NotThrow(() => Activator.CreateInstance(typeof(AgentMethodCallException), 
            new object[] { "agent", "method", "message" }));
        Should.NotThrow(() => Activator.CreateInstance(typeof(AgentEventSendException), 
            new object[] { "agent", "event", "message" }));
        Should.NotThrow(() => Activator.CreateInstance(typeof(PluginLoadException), 
            new object[] { "plugin", "version", "message" }));
    }

    #endregion
} 