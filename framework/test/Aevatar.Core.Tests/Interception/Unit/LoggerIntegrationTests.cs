using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using FluentAssertions;
using Aevatar.Core.Interception;
using Aevatar.Core.Tests.Interception.Infrastructure;
using Aevatar.Core.Tests.Interception.TestSubjects;

namespace Aevatar.Core.Tests.Interception.Unit
{
    /// <summary>
    /// Tests for logger integration functionality.
    /// </summary>
    [Collection("TraceContextTests")] // Ensure tests run sequentially to prevent static state interference
    public class LoggerIntegrationTests : IClassFixture<TraceContextFixture>, IDisposable
    {
        private readonly BasicTestClass _testClass;
        private readonly TestLogger<BasicTestClass> _testLogger;
        private readonly TraceContextFixture _fixture;

        public LoggerIntegrationTests(TraceContextFixture fixture)
        {
            _fixture = fixture;
            _testLogger = new TestLogger<BasicTestClass>();
            _testClass = new BasicTestClass(_testLogger);
            
            // CRITICAL: Ensure each test starts with a clean TraceContext state
            _fixture.ResetTraceContext();
        }

        public void Dispose()
        {

        }

        [Fact]
        public void MethodWithLogger_ShouldUseProvidedLogger()
        {
            _fixture.ResetTraceContext();
        
        // Act
            _testClass.VoidMethod();

            // Assert
            _testLogger.LogEntries.Should().NotBeEmpty();
            // Note: The actual logging would depend on the interception implementation
            // This test verifies that the logger is properly injected
        }

        [Fact]
        public void MethodWithReturnValue_ShouldUseProvidedLogger()
        {
            _fixture.ResetTraceContext();
        
        // Act
            var result = _testClass.MethodWithReturnValue();

            // Assert
            result.Should().Be("test result");
            _testLogger.LogEntries.Should().NotBeEmpty();
        }

        [Fact]
        public void MethodWithParameters_ShouldUseProvidedLogger()
        {
            _fixture.ResetTraceContext();
        
        // Act
            _testClass.MethodWithParameters("test", 42, true);

            // Assert
            _testLogger.LogEntries.Should().NotBeEmpty();
        }

        [Fact]
        public async Task AsyncMethod_ShouldUseProvidedLogger()
        {
            _fixture.ResetTraceContext();
        
        // Act
            var result = await _testClass.AsyncMethodWithReturnValue();

            // Assert
            result.Should().Be("async result");
            _testLogger.LogEntries.Should().NotBeEmpty();
        }

        [Fact]
        public void ConstructorWithLogger_ShouldUseProvidedLogger()
        {
            _fixture.ResetTraceContext();
        
        // Act
            var obj = new BasicTestClass(_testLogger);

            // Assert
            obj.Logger.Should().Be(_testLogger);
        }

        [Fact]
        public void MethodWithoutLogger_ShouldHandleNullLogger()
        {
            _fixture.ResetTraceContext();
        
        // Act
            var obj = new BasicTestClass(null);

            // Assert
            obj.Logger.Should().BeNull();
            // Should not throw when calling methods
            obj.VoidMethod(); // Should not throw
        }
    }
}
