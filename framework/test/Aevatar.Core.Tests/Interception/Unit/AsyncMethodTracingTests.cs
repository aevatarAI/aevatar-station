using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Aevatar.Core.Interception;
using Aevatar.Core.Interception.Context;
using Aevatar.Core.Interception.Configurations;
using Aevatar.Core.Tests.Interception.Infrastructure;
using Xunit;

namespace Aevatar.Core.Tests.Interception.Unit
{
    /// <summary>
    /// Tests for async method tracing functionality.
    /// Uses TestMockLoggerProvider instead of console capture to avoid race conditions.
    /// </summary>
    [Collection("TraceContextTests")] // Ensure tests run sequentially to prevent static state interference
    public class AsyncMethodTracingTests : IClassFixture<TraceContextFixture>, IDisposable
    {
        private readonly TestMockLoggerProvider _mockLoggerProvider;
        private readonly ILogger<TestAsyncClass> _logger;
        private readonly TraceContextFixture _fixture;

        public AsyncMethodTracingTests(TraceContextFixture fixture)
        {
            _fixture = fixture;
            
            // Create TestMockLoggerProvider to capture logs without console race conditions
            _mockLoggerProvider = new TestMockLoggerProvider();
            _logger = _mockLoggerProvider.CreateLogger<TestAsyncClass>();
            
            // CRITICAL: Ensure each test starts with a clean TraceContext state
            _fixture.ResetTraceContext();
        }

        public void Dispose()
        {
            _mockLoggerProvider?.Dispose();
        }

        private void ClearLogs()
        {
            _mockLoggerProvider.Logs.Clear();
        }

        [Fact]
        public async Task WhenTracingEnabled_ShouldCaptureAsyncMethodCanceled()
        {
            // Arrange
            ClearLogs();
            var testClass = new TestAsyncClass(_logger);
            
            // Enable tracing for a specific trace ID
            TraceContext.EnableTracing("test-trace-canceled");

            // Create a cancellation token that will be canceled
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act
            try
            {
                await testClass.AsyncMethodThatCanBeCanceled(cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

            // Wait a bit for the task continuation to complete
            await Task.Delay(100);

            // Assert
            var logs = _mockLoggerProvider.Logs.ToArray();
            Assert.Contains(logs, line => line.Contains("TRACE: Entering AsyncMethodThatCanBeCanceled"));
            Assert.Contains(logs, line => line.Contains("TRACE: Async canceled AsyncMethodThatCanBeCanceled"));
        }

        [Fact]
        public async Task WhenTracingEnabled_ShouldCaptureAsyncMethodFaulted()
        {
            // Arrange
            ClearLogs();
            var testClass = new TestAsyncClass(_logger);
            
            // Enable tracing for a specific trace ID
            TraceContext.EnableTracing("test-trace-faulted");

            // Act
            try
            {
                await testClass.AsyncMethodThatThrowsException();
            }
            catch (InvalidOperationException)
            {
                // Expected
            }

            // Wait a bit for the task continuation to complete
            await Task.Delay(100);

            // Assert
            var logs = _mockLoggerProvider.Logs.ToArray();
            Assert.Contains(logs, line => line.Contains("TRACE: Entering AsyncMethodThatThrowsException"));
            Assert.Contains(logs, line => line.Contains("TRACE: Async exception in AsyncMethodThatThrowsException"));
            Assert.Contains(logs, line => line.Contains("InvalidOperationException"));
            Assert.Contains(logs, line => line.Contains("Async test exception"));
        }

        [Fact]
        public async Task WhenTracingEnabled_ShouldCaptureAsyncMethodCompleted()
        {
            // Arrange
            ClearLogs();
            var testClass = new TestAsyncClass(_logger);
            
            // Enable tracing for a specific trace ID
            TraceContext.EnableTracing("test-trace-completed");

            // Act
            var result = await testClass.AsyncMethodWithReturnValue();

            // Wait a bit for the task continuation to complete
            await Task.Delay(100);

            // Assert
            var logs = _mockLoggerProvider.Logs.ToArray();
            Assert.Contains(logs, line => line.Contains("TRACE: Entering AsyncMethodWithReturnValue"));
            Assert.Contains(logs, line => line.Contains("TRACE: Async completed AsyncMethodWithReturnValue"));
            Assert.Equal("async result", result);
        }

        [Fact]
        public async Task WhenTracingEnabled_ShouldCaptureAsyncVoidMethod()
        {
            // Arrange
            ClearLogs();
            var testClass = new TestAsyncClass(_logger);
            
            // Enable tracing for a specific trace ID
            TraceContext.EnableTracing("test-trace-void");

            // Act
            testClass.AsyncVoidMethod();
            
            // Wait a bit for the task continuation to complete
            await Task.Delay(100);

            // Assert
            var logs = _mockLoggerProvider.Logs.ToArray();
            Assert.Contains(logs, line => line.Contains("TRACE: Entering AsyncVoidMethod"));
            Assert.Contains(logs, line => line.Contains("TRACE: Exiting AsyncVoidMethod"));
        }

        [Fact]
        public async Task WhenTracingEnabled_ShouldCaptureAsyncExtensionMethod()
        {
            // Arrange
            ClearLogs();
            var testClass = new TestAsyncClass(_logger);
            
            // Enable tracing for a specific trace ID
            TraceContext.EnableTracing("test-trace-extension");

            // Act
            var result = await testClass.AsyncExtensionMethod("test input");
            
            // Wait a bit for the task continuation to complete
            await Task.Delay(100);

            // Assert
            var logs = _mockLoggerProvider.Logs.ToArray();
            Assert.Contains(logs, line => line.Contains("TRACE: Entering AsyncExtensionMethod"));
            Assert.Contains(logs, line => line.Contains("TRACE: Async completed AsyncExtensionMethod"));
            Assert.Equal("test input processed", result);
        }

        private class TestAsyncClass
        {
            private readonly ILogger<TestAsyncClass> _logger;

            public TestAsyncClass(ILogger<TestAsyncClass> logger)
            {
                _logger = logger;
            }

            public ILogger<TestAsyncClass> Logger => _logger;
            [Interceptor]
            public async Task AsyncMethodThatCanBeCanceled(CancellationToken cancellationToken)
            {
                await Task.Delay(1000, cancellationToken); // This will be canceled
            }

            [Interceptor]
            public async Task AsyncMethodThatThrowsException()
            {
                await Task.Delay(1); // Small delay to make it async
                throw new InvalidOperationException("Async test exception");
            }

            [Interceptor]
            public async Task<string> AsyncMethodWithReturnValue()
            {
                await Task.Delay(1); // Small delay to make it async
                return "async result";
            }

            [Interceptor]
            public async void AsyncVoidMethod()
            {
                await Task.Delay(1); // Small delay to make it async
            }

            [Interceptor]
            public async Task<string> AsyncExtensionMethod(string input)
            {
                await Task.Delay(1); // Small delay to make it async
                return $"{input} processed";
            }
        }
    }
}
