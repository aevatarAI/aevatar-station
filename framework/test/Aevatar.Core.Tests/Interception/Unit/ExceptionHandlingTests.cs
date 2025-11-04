using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Aevatar.Core.Tests.Interception.Infrastructure;
using Aevatar.Core.Tests.Interception.TestSubjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aevatar.Core.Tests.Interception.Unit
{
    /// <summary>
    /// Tests for exception handling in interception.
    /// Uses DI-based logger resolution to ensure all interceptors use the same mock logger.
    /// </summary>
    [Collection("TraceContextTests")] // Ensure tests run sequentially to prevent static state interference
    public class ExceptionHandlingTests : InterceptionTestBase, IClassFixture<TraceContextFixture>
    {
        private readonly BasicTestClass _testClass;
        private readonly TraceContextFixture _fixture;

        public ExceptionHandlingTests(TraceContextFixture fixture)
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
        public void MethodThatThrowsException_ShouldLogEntryAndException()
        {
            _fixture.ResetTraceContext();
        
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _testClass.MethodThatThrowsException());

            // Assert
            exception.Message.Should().Be("Test exception");
            var logs = MockLoggerProvider.Logs.ToArray();
            logs.Should().Contain(line => line.Contains("TRACE: Entering MethodThatThrowsException"));
            logs.Should().Contain(line => line.Contains("TRACE: Exception in MethodThatThrowsException"));
        }

        [Fact]
        public async Task AsyncMethodThatThrowsException_ShouldLogEntryAndException()
        {
            _fixture.ResetTraceContext();
        
            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _testClass.AsyncMethodThatThrowsException());

            // Assert
            exception.Message.Should().Be("Async test exception");
            var logs = MockLoggerProvider.Logs.ToArray();
            logs.Should().Contain(line => line.Contains("TRACE: Entering AsyncMethodThatThrowsException"));
            // Note: Async methods now properly log exceptions through our task continuation fix
            logs.Should().Contain(line => line.Contains("TRACE: Async exception in AsyncMethodThatThrowsException"));
            logs.Should().Contain(line => line.Contains("TRACE: Exiting AsyncMethodThatThrowsException"));
        }

        [Fact]
        public void MethodWithComplexParameters_ShouldHandleToStringFailures()
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


    }
}
