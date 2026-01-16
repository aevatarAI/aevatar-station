using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.Python;

/// <summary>
/// Python execution environment configuration
/// </summary>
[GenerateSerializer]
public class PythonEnvironmentConfig
{
    [Id(0)] public string EnvironmentName { get; set; } = string.Empty;
    [Id(1)] public string PythonVersion { get; set; } = "3.9";
    [Id(2)] public List<string> Dependencies { get; set; } = new();
    [Id(3)] public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
    [Id(4)] public int MaxExecutionTimeSeconds { get; set; } = 30;
    [Id(5)] public long MaxMemoryMB { get; set; } = 512;
    [Id(6)] public bool EnableNetworkAccess { get; set; } = false;
    [Id(7)] public List<string> AllowedModules { get; set; } = new();
    [Id(8)] public string WorkingDirectory { get; set; } = string.Empty;
    [Id(9)] public bool IsSandboxed { get; set; } = true;
    [Id(10)] public string PythonCommand { get; set; } = string.Empty; // Auto-detect if empty
    [Id(11)] public bool AutoDetectPython { get; set; } = true;
    [Id(12)] public List<string> PreferredPythonCommands { get; set; } = new() { "python3", "python" };
}

/// <summary>
/// Script execution result with detailed runtime information
/// </summary>
[GenerateSerializer]
public class ScriptExecutionResult
{
    [Id(0)] public bool Success { get; set; }
    [Id(1)] public string StandardOutput { get; set; } = string.Empty;
    [Id(2)] public string ErrorOutput { get; set; } = string.Empty;
    [Id(3)] public int ExitCode { get; set; }
    [Id(4)] public double ExecutionTimeSeconds { get; set; }
    [Id(5)] public long MemoryUsedMB { get; set; }
    [Id(6)] public bool TimedOut { get; set; }
    [Id(7)] public Exception? Exception { get; set; }
    [Id(8)] public DateTime StartTime { get; set; }
    [Id(9)] public DateTime EndTime { get; set; }
    [Id(10)] public string ScriptHash { get; set; } = string.Empty;
} 