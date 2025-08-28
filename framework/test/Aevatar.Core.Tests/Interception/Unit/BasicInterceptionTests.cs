using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Aevatar.Core.Interception;
using Aevatar.Core.Tests.Interception.Infrastructure;
using Aevatar.Core.Tests.Interception.TestSubjects;
using Xunit;
using FluentAssertions;

namespace Aevatar.Core.Tests.Interception.Unit;

/// <summary>
/// Tests for basic interception functionality.
/// Uses DI-based logger resolution to ensure all interceptors (static, extension, instance methods) use the same mock logger.
/// </summary>
[Collection("TraceContextTests")] // Ensure tests run sequentially to prevent static state interference
public class BasicInterceptionTests : InterceptionTestBase, IClassFixture<TraceContextFixture>
{
    private readonly BasicTestClass _testClass;
    private readonly TraceContextFixture _fixture;

    public BasicInterceptionTests(TraceContextFixture fixture)
    {
        _fixture = fixture;
        
        // Create BasicTestClass - it will get logger from DI automatically
        var logger = ServiceProvider.GetRequiredService<ILogger<BasicTestClass>>();
        _testClass = new BasicTestClass(logger);
        
        // CRITICAL: Ensure each test starts with a clean TraceContext state
        _fixture.ResetTraceContext();
    }

    public override void Dispose()
    {
        base.Dispose();
    }

    [Fact]
    public void VoidMethod_ShouldLogEntryAndExit()
    {
        _fixture.ResetTraceContext();
        
        // Act
        _testClass.VoidMethod();

        // Assert
        var logs = MockLoggerProvider.Logs.ToArray();
        logs.Should().Contain(line => line.Contains("TRACE: Entering VoidMethod"));
        logs.Should().Contain(line => line.Contains("TRACE: Exiting VoidMethod"));
    }

    [Fact]
    public void MethodWithReturnValue_ShouldLogEntryAndExit()
    {
        _fixture.ResetTraceContext();
        
        // Act
        var result = _testClass.MethodWithReturnValue();

        // Assert
        result.Should().Be("test result");
        var logs = MockLoggerProvider.Logs.ToArray();
        logs.Should().Contain(line => line.Contains("TRACE: Entering MethodWithReturnValue"));
        logs.Should().Contain(line => line.Contains("TRACE: Exiting MethodWithReturnValue"));
    }

    [Fact]
    public void MethodWithParameters_ShouldLogParameterValues()
    {
        _fixture.ResetTraceContext();
        
        // Act
        _testClass.MethodWithParameters("test", 42, true);

        // Assert
        var logs = MockLoggerProvider.Logs.ToArray();
        logs.Should().Contain(line => line.Contains("TRACE: Entering MethodWithParameters"));
        logs.Should().Contain(line => line.Contains("TRACE: Parameter input = test"));
        logs.Should().Contain(line => line.Contains("TRACE: Parameter count = 42"));
        logs.Should().Contain(line => line.Contains("TRACE: Parameter flag = True"));
        logs.Should().Contain(line => line.Contains("TRACE: Exiting MethodWithParameters"));
    }

    [Fact]
    public async Task AsyncVoidMethod_ShouldLogEntryAndAsyncCompletion()
    {
        _fixture.ResetTraceContext();
        
        // Act
        await _testClass.AsyncVoidMethod();

        // Assert
        var logs = MockLoggerProvider.Logs.ToArray();
        logs.Should().Contain(line => line.Contains("TRACE: Entering AsyncVoidMethod"));
        logs.Should().Contain(line => line.Contains("TRACE: Async completed AsyncVoidMethod"));
        logs.Should().Contain(line => line.Contains("TRACE: Exiting AsyncVoidMethod"));
        // Note: Async methods log both "Async completed" (from task continuation) and "Exiting" (from OnExit)
    }

    [Fact]
    public async Task AsyncMethodWithReturnValue_ShouldLogEntryAndAsyncCompletion()
    {
        _fixture.ResetTraceContext();
        
        // Act
        var result = await _testClass.AsyncMethodWithReturnValue();

        // Assert
        result.Should().Be("async result");
        var logs = MockLoggerProvider.Logs.ToArray();
        logs.Should().Contain(line => line.Contains("TRACE: Entering AsyncMethodWithReturnValue"));
        logs.Should().Contain(line => line.Contains("TRACE: Async completed AsyncMethodWithReturnValue"));
        // Note: Async methods now log "Async completed" instead of "Exiting" due to our task continuation fix
    }

    [Fact]
    public async Task GenericAsyncMethod_ShouldLogWithTypeInformation()
    {
        _fixture.ResetTraceContext();
        
        // Act
        var result = await _testClass.GenericAsyncMethod("test input");

        // Assert
        result.Should().Be("test input");
        var logs = MockLoggerProvider.Logs.ToArray();
        logs.Should().Contain(line => line.Contains("TRACE: Entering GenericAsyncMethod"));
        logs.Should().Contain(line => line.Contains("TRACE: Parameter input = test input"));
        logs.Should().Contain(line => line.Contains("TRACE: Async completed GenericAsyncMethod"));
        // Note: Async methods now log "Async completed" instead of "Exiting" due to our task continuation fix
    }

    [Fact]
    public void StaticMethod_ShouldBeIntercepted()
    {
        _fixture.ResetTraceContext();
        
        // Act
        var result = BasicTestClass.StaticMethod("test");

        // Assert
        result.Should().Be("Static: test");
        var logs = MockLoggerProvider.Logs.ToArray();
        logs.Should().Contain(line => line.Contains("TRACE: Entering StaticMethod"));
        logs.Should().Contain(line => line.Contains("TRACE: Parameter input = test"));
        logs.Should().Contain(line => line.Contains("TRACE: Exiting StaticMethod"));
    }

    [Fact]
    public void GenericMethod_ShouldLogWithTypeInformation()
    {
        _fixture.ResetTraceContext();
        
        // Act
        var result = _testClass.GenericMethod(123);

        // Assert
        result.Should().Be(123);
        var logs = MockLoggerProvider.Logs.ToArray();
        logs.Should().Contain(line => line.Contains("TRACE: Entering GenericMethod"));
        logs.Should().Contain(line => line.Contains("TRACE: Parameter input = 123"));
        logs.Should().Contain(line => line.Contains("TRACE: Exiting GenericMethod"));
    }

    [Fact]
    public void ExtensionMethod_ShouldBeIntercepted()
    {
        _fixture.ResetTraceContext();
        
        // Act
        var result = _testClass.ExtensionMethod("test");

        // Assert
        result.Should().Be("Extended: test");
        var logs = MockLoggerProvider.Logs.ToArray();
        logs.Should().Contain(line => line.Contains("TRACE: Entering ExtensionMethod"));
        logs.Should().Contain(line => line.Contains("TRACE: Parameter input = test"));
        logs.Should().Contain(line => line.Contains("TRACE: Exiting ExtensionMethod"));
    }

    [Fact]
    public async Task AsyncExtensionMethod_ShouldBeIntercepted()
    {
        _fixture.ResetTraceContext();
        
        // Act
        var result = await _testClass.AsyncExtensionMethod("test");

        // Assert
        result.Should().Be("Async Extended: test");
        var logs = MockLoggerProvider.Logs.ToArray();
        logs.Should().Contain(line => line.Contains("TRACE: Entering AsyncExtensionMethod"));
        logs.Should().Contain(line => line.Contains("TRACE: Parameter input = test"));
        logs.Should().Contain(line => line.Contains("TRACE: Async completed AsyncExtensionMethod"));
        logs.Should().Contain(line => line.Contains("TRACE: Exiting AsyncExtensionMethod"));
    }

    [Fact]
    public async Task AsyncMethodCancellation_ShouldLogCancellation()
    {
        _fixture.ResetTraceContext();
        
        // Arrange - Use a pre-canceled token to ensure cancellation happens
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel(); // Cancel immediately
        var cancellationToken = cancellationTokenSource.Token;

        // Act - Start the async method with already canceled token
        var task = _testClass.AsyncMethodThatCanBeCanceled(cancellationToken);

        // Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => task);
        var logs = MockLoggerProvider.Logs.ToArray();
        logs.Should().Contain(line => line.Contains("TRACE: Entering AsyncMethodThatCanBeCanceled"));
        logs.Should().Contain(line => line.Contains("TRACE: Async canceled AsyncMethodThatCanBeCanceled"));
        logs.Should().Contain(line => line.Contains("TRACE: Exiting AsyncMethodThatCanBeCanceled"));
    }

    [Fact]
    public async Task AsyncMethods_ShouldLogProperAsyncMessages()
    {
        _fixture.ResetTraceContext();
        
        // Test 1: Normal completion
        var result1 = await _testClass.AsyncMethodWithReturnValue();
        result1.Should().Be("async result");
        
        // Test 2: Exception
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _testClass.AsyncMethodThatThrowsException());
        exception.Message.Should().Be("Async test exception");
        
        // Test 3: Cancellation
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel(); // Cancel immediately
        var cancellationToken = cancellationTokenSource.Token;
        var task = _testClass.AsyncMethodThatCanBeCanceled(cancellationToken);
        await Assert.ThrowsAsync<OperationCanceledException>(() => task);

        // Assert all async logging patterns
        var logs = MockLoggerProvider.Logs.ToArray();
        
        // Normal completion
        logs.Should().Contain(line => line.Contains("TRACE: Async completed AsyncMethodWithReturnValue"));
        
        // Exception
        logs.Should().Contain(line => line.Contains("TRACE: Async exception in AsyncMethodThatThrowsException"));
        
        // Cancellation
        logs.Should().Contain(line => line.Contains("TRACE: Async canceled AsyncMethodThatCanBeCanceled"));
        
        // Verify async methods log both "Exiting" and appropriate async messages
        logs.Should().Contain(line => line.Contains("TRACE: Exiting AsyncMethodWithReturnValue"));
        logs.Should().Contain(line => line.Contains("TRACE: Exiting AsyncMethodThatThrowsException"));
        logs.Should().Contain(line => line.Contains("TRACE: Exiting AsyncMethodThatCanBeCanceled"));
    }

    [Fact]
    public void Constructor_ShouldBeIntercepted()
    {
        _fixture.ResetTraceContext();
        
        // Act
        var obj = new ConstructorTestClass("test name");

        // Assert
        obj.Name.Should().Be("test name");
        var logs = MockLoggerProvider.Logs.ToArray();
        logs.Should().Contain(line => line.Contains("TRACE: Entering .ctor"));
        logs.Should().Contain(line => line.Contains("TRACE: Parameter name = test name"));
        logs.Should().Contain(line => line.Contains("TRACE: Exiting .ctor"));
    }

    [Fact]
    public void ConstructorWithMultipleParameters_ShouldBeIntercepted()
    {
        _fixture.ResetTraceContext();
        
        // Act
        var obj = new ConstructorTestClass("test name", 42);

        // Assert
        obj.Name.Should().Be("test name");
        obj.Value.Should().Be(42);
        var logs = MockLoggerProvider.Logs.ToArray();
        logs.Should().Contain(line => line.Contains("TRACE: Entering .ctor"));
        logs.Should().Contain(line => line.Contains("TRACE: Parameter name = test name"));
        logs.Should().Contain(line => line.Contains("TRACE: Parameter value = 42"));
        logs.Should().Contain(line => line.Contains("TRACE: Exiting .ctor"));
    }
}
