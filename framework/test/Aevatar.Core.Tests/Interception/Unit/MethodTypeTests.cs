using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using FluentAssertions;
using Aevatar.Core.Tests.Interception.Infrastructure;
using Aevatar.Core.Tests.Interception.TestSubjects;

namespace Aevatar.Core.Tests.Interception.Unit
{

    /// <summary>
    /// Tests for method type interception functionality.
    /// Uses DI-based logger resolution to ensure all interceptors use the same mock logger.
    /// </summary>
    [Collection("TraceContextTests")] // Ensure tests run sequentially to prevent static state interference
    public class MethodTypeTests : InterceptionTestBase, IClassFixture<TraceContextFixture>
    {
        private readonly BasicTestClass _testClass;
        private readonly TraceContextFixture _fixture;

        public MethodTypeTests(TraceContextFixture fixture)
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
        public void PrivateMethod_ShouldBeInterceptedWhenCalledFromPublic()
        {
            _fixture.ResetTraceContext();

        // Act
            _testClass.PublicMethodCallingPrivate();

            // Assert
            var logs = MockLoggerProvider.Logs.ToArray();
            logs.Should().Contain(line => line.Contains("TRACE: Entering PrivateMethod"));
            logs.Should().Contain(line => line.Contains("TRACE: Exiting PrivateMethod"));
        }

        [Fact]
        public void ProtectedMethod_ShouldBeInterceptedWhenCalledFromPublic()
        {
            _fixture.ResetTraceContext();

            // Act
            _testClass.PublicMethodCallingProtected();

            // Assert
            var logs = MockLoggerProvider.Logs.ToArray();
            logs.Should().Contain(line => line.Contains("TRACE: Entering ProtectedMethod"));
            logs.Should().Contain(line => line.Contains("TRACE: Exiting ProtectedMethod"));
        }

        [Fact]
        public void InternalMethod_ShouldBeInterceptedWhenCalledFromPublic()
        {
            _fixture.ResetTraceContext();

            // Act
            _testClass.PublicMethodCallingInternal();

            // Assert
            var logs = MockLoggerProvider.Logs.ToArray();
            logs.Should().Contain(line => line.Contains("TRACE: Entering InternalMethod"));
            logs.Should().Contain(line => line.Contains("TRACE: Exiting InternalMethod"));
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
    }
}
