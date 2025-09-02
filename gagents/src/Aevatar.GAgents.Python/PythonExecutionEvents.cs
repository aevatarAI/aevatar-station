using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.Python;

/// <summary>
/// Base event for Python execution system
/// </summary>
[GenerateSerializer]
public class PythonExecutionGAgentEventBase : EventBase
{
    [Id(0)] public string ExecutionSessionId { get; set; } = string.Empty;
    [Id(1)] public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Event triggered when Python script execution is completed
/// </summary>
[GenerateSerializer]
public class ScriptExecutionCompletedEvent : PythonExecutionGAgentEventBase
{
    [Id(0)] public ScriptExecutionResult Result { get; set; } = new();
    [Id(1)] public string EnvironmentName { get; set; } = string.Empty;
}

/// <summary>
/// Event triggered when Python environment is created
/// </summary>
[GenerateSerializer]
public class EnvironmentCreatedEvent : PythonExecutionGAgentEventBase
{
    [Id(0)] public string EnvironmentName { get; set; } = string.Empty;
    [Id(1)] public PythonEnvironmentConfig Config { get; set; } = new();
}

/// <summary>
/// Event triggered when Python environment is deleted
/// </summary>
[GenerateSerializer]
public class EnvironmentDeletedEvent : PythonExecutionGAgentEventBase
{
    [Id(0)] public string EnvironmentName { get; set; } = string.Empty;
}

/// <summary>
/// Event triggered when dependencies are installed
/// </summary>
[GenerateSerializer]
public class DependenciesInstalledEvent : PythonExecutionGAgentEventBase
{
    [Id(0)] public string EnvironmentName { get; set; } = string.Empty;
    [Id(1)] public List<string> Dependencies { get; set; } = new();
    [Id(2)] public bool InstallationSuccess { get; set; }
    [Id(3)] public int SuccessfulCount { get; set; }
    [Id(4)] public int TotalCount { get; set; }
}

/// <summary>
/// Event triggered when script security validation fails
/// </summary>
[GenerateSerializer]
public class SecurityValidationFailedEvent : PythonExecutionGAgentEventBase
{
    [Id(0)] public string ScriptHash { get; set; } = string.Empty;
    [Id(1)] public string Reason { get; set; } = string.Empty;
    [Id(2)] public List<string> ViolatedRules { get; set; } = new();
}

/// <summary>
/// Event triggered when execution limit is reached
/// </summary>
[GenerateSerializer]
public class ExecutionLimitReachedEvent : PythonExecutionGAgentEventBase
{
    [Id(0)] public int CurrentExecutions { get; set; }
    [Id(1)] public int MaxExecutions { get; set; }
}