using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Aevatar.Core.Tests.Interception.Infrastructure
{
    /// <summary>
    /// Test implementation of ILogger for capturing log entries
    /// </summary>
    public class TestLogger<T> : ILogger<T>
    {
        public List<LogEntry> LogEntries { get; } = new();

        public IDisposable BeginScope<TState>(TState state) => new NoOpDisposable();

        public virtual bool IsEnabled(LogLevel logLevel) => true;

        public virtual void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception exception, Func<TState, Exception, string> formatter)
        {
            LogEntries.Add(new LogEntry
            {
                Level = logLevel,
                Message = formatter(state, exception),
                Exception = exception,
                Timestamp = DateTime.UtcNow,
                State = state
            });
        }

        public void Clear()
        {
            LogEntries.Clear();
        }

        private class NoOpDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }

    /// <summary>
    /// Represents a captured log entry
    /// </summary>
    public class LogEntry
    {
        public LogLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public Exception Exception { get; set; }
        public DateTime Timestamp { get; set; }
        public object State { get; set; }
    }
}
