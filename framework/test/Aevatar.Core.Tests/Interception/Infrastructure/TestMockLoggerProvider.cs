using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Aevatar.Core.Tests.Interception.Infrastructure
{
    /// <summary>
    /// Simple mock logger provider for testing interception without console dependencies.
    /// Provides thread-safe logging capture for test scenarios.
    /// </summary>
    public class TestMockLoggerProvider : ILoggerProvider
    {
        public ConcurrentQueue<string> Logs { get; } = new();

        public ILogger CreateLogger(string categoryName)
        {
            return new TestMockLogger(Logs);
        }
        
        public ILogger<T> CreateLogger<T>()
        {
            return new TestMockLogger<T>(Logs);
        }

        public void Dispose()
        {
        }

        private class TestMockLogger : ILogger
        {
            private readonly ConcurrentQueue<string> _logs;

            public TestMockLogger(ConcurrentQueue<string> logs)
            {
                _logs = logs;
            }

            public IDisposable BeginScope<TState>(TState state) => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Debug;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                Func<TState, Exception, string> formatter)
            {
                if (IsEnabled(logLevel) && formatter != null)
                {
                    _logs.Enqueue(formatter(state, exception));
                }
            }
        }
        
        private class TestMockLogger<T> : ILogger<T>
        {
            private readonly ConcurrentQueue<string> _logs;

            public TestMockLogger(ConcurrentQueue<string> logs)
            {
                _logs = logs;
            }

            public IDisposable BeginScope<TState>(TState state) => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Debug;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                Func<TState, Exception, string> formatter)
            {
                if (IsEnabled(logLevel) && formatter != null)
                {
                    _logs.Enqueue(formatter(state, exception));
                }
            }
        }
    }
}
