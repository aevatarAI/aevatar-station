using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;

public class MockLoggerProvider : ILoggerProvider
{
    public ConcurrentQueue<string> Logs { get; } = new();
    private readonly string _namespace;

    public MockLoggerProvider(string @namespace)
    {
        _namespace = @namespace;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new MockLogger(Logs, categoryName, _namespace);
    }

    public void Dispose()
    {
    }

    private class MockLogger : ILogger
    {
        private readonly ConcurrentQueue<string> _logs;
        private readonly string _categoryName;
        private readonly string _namespace;

        public MockLogger(ConcurrentQueue<string> logs, string categoryName, string @namespace)
        {
            _logs = logs;
            _categoryName = categoryName;
            _namespace = @namespace;
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (IsEnabled(logLevel) && _categoryName.StartsWith(_namespace))
            {
                _logs.Enqueue(formatter(state, exception));
            }
        }
    }
}