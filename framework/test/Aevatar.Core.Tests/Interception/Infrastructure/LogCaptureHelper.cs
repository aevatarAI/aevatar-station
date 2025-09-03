using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Aevatar.Core.Tests.Interception.Infrastructure
{
    /// <summary>
    /// Helper class for capturing console output during tests
    /// Uses test-scoped console isolation to prevent interference between tests
    /// </summary>
    public class LogCaptureHelper : IDisposable
    {
        private readonly TextWriter _originalConsole;
        private readonly StringWriter _captureWriter;
        private readonly object _lock = new object();
        private bool _disposed;
        
        public LogCaptureHelper()
        {
            lock (_lock)
            {
                _originalConsole = Console.Out;
                _captureWriter = new StringWriter();
                Console.SetOut(_captureWriter);
            }
        }

        public string GetCapturedLogs()
        {
            lock (_lock)
            {
                if (_disposed) return string.Empty;
                return _captureWriter.ToString();
            }
        }

        public List<string> GetCapturedLogLines()
        {
            lock (_lock)
            {
                if (_disposed) return new List<string>();
                
                var logs = _captureWriter.ToString();
                var lines = new List<string>();
                using var reader = new StringReader(logs);
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        lines.Add(line.Trim());
                    }
                }
                return lines;
            }
        }

        public void ClearCapturedLogs()
        {
            lock (_lock)
            {
                if (_disposed) return;
                _captureWriter.GetStringBuilder().Clear();
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (!_disposed)
                {
                    Console.SetOut(_originalConsole);
                    _captureWriter.Dispose();
                    _disposed = true;
                }
            }
        }
    }

}
