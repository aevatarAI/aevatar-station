using System;
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
    /// Tests for parameter capture functionality.
    /// Uses TestMockLoggerProvider instead of console capture to avoid race conditions.
    /// </summary>
    [Collection("TraceContextTests")] // Ensure tests run sequentially to prevent console redirection interference
    public class ParameterCaptureTests : InterceptionTestBase, IClassFixture<TraceContextFixture>
    {
        private readonly BasicTestClass _testClass;
        private readonly TraceContextFixture _fixture;

        public ParameterCaptureTests(TraceContextFixture fixture)
        {
            _fixture = fixture;
            _fixture.ResetTraceContext();
            
            // Create BasicTestClass - it will get logger from DI automatically
            var logger = ServiceProvider.GetRequiredService<ILogger<BasicTestClass>>();
            _testClass = new BasicTestClass(logger);
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        [Fact]
        public void MethodWithStringParameters_ShouldLogStringValues()
        {
            _fixture.ResetTraceContext();
            // Act
            _testClass.MethodWithParameters("hello world", 42, true);

            // Assert
            var logs = MockLoggerProvider.Logs.ToArray();
            logs.Should().Contain(line => line.Contains("TRACE: Parameter input = hello world"));
            logs.Should().Contain(line => line.Contains("TRACE: Parameter count = 42"));
            logs.Should().Contain(line => line.Contains("TRACE: Parameter flag = True"));
        }

        [Fact]
        public void MethodWithNumericParameters_ShouldLogNumericValues()
        {
            _fixture.ResetTraceContext();
            // Act
            _testClass.MethodWithParameters("test", 100, false);

            // Assert
            var logs = MockLoggerProvider.Logs.ToArray();
            logs.Should().Contain(line => line.Contains("TRACE: Parameter input = test"));
            logs.Should().Contain(line => line.Contains("TRACE: Parameter count = 100"));
            logs.Should().Contain(line => line.Contains("TRACE: Parameter flag = False"));
        }

        [Fact]
        public void MethodWithNullParameters_ShouldHandleNullValues()
        {
            _fixture.ResetTraceContext();
            // Act
            _testClass.MethodWithParameters(null, 0, false);

            // Assert
            var logs = MockLoggerProvider.Logs.ToArray();
            logs.Should().Contain(line => line.Contains("TRACE: Parameter input = "));
            logs.Should().Contain(line => line.Contains("TRACE: Parameter count = 0"));
            logs.Should().Contain(line => line.Contains("TRACE: Parameter flag = False"));
        }

        [Fact]
        public void GenericMethod_ShouldLogGenericTypeParameters()
        {
            _fixture.ResetTraceContext();
            // Act
            var result = _testClass.GenericMethod("generic test");

            // Assert
            result.Should().Be("generic test");
            var logs = MockLoggerProvider.Logs.ToArray();
            logs.Should().Contain(line => line.Contains("TRACE: Entering GenericMethod"));
            logs.Should().Contain(line => line.Contains("TRACE: Parameter input = generic test"));
            logs.Should().Contain(line => line.Contains("TRACE: Exiting GenericMethod"));
        }

        [Fact]
        public void Constructor_ShouldLogConstructorParameters()
        {
            _fixture.ResetTraceContext();
            // Act
            var obj = new ConstructorTestClass("test name", 123);

            // Assert
            obj.Name.Should().Be("test name");
            obj.Value.Should().Be(123);
            var logs = MockLoggerProvider.Logs.ToArray();
            logs.Should().Contain(line => line.Contains("TRACE: Entering .ctor"));
            logs.Should().Contain(line => line.Contains("TRACE: Parameter name = test name"));
            logs.Should().Contain(line => line.Contains("TRACE: Parameter value = 123"));
            logs.Should().Contain(line => line.Contains("TRACE: Exiting .ctor"));
        }
    }
}
