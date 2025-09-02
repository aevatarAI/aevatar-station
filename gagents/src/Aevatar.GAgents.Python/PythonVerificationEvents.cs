using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.Python;

/// <summary>
/// Base event for theory reasoning system
/// </summary>
[GenerateSerializer]
public class PythonVerificationGAgentEventBase : EventBase
{
    [Id(0)] public string ReasoningSessionId { get; set; } = string.Empty;
    [Id(1)] public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Event triggered when Python verification is completed
/// </summary>
[GenerateSerializer]
public class VerificationCompletedEventBase : PythonVerificationGAgentEventBase
{
    [Id(0)] public string TheoryId { get; set; } = string.Empty;
    [Id(1)] public string PythonCode { get; set; } = string.Empty;
    [Id(2)] public bool TestsPassed { get; set; }
    [Id(3)] public string TestResults { get; set; } = string.Empty;
    [Id(4)] public List<string> TestCases { get; set; } = new();
    [Id(5)] public double ExecutionTime { get; set; }
}