using System;

namespace Aevatar.Sandbox.Abstractions.Contracts;

public sealed class SandboxExecutionResult
{
    public required string SandboxExecutionId { get; init; }
    public bool Success { get; init; }
    public string Stdout { get; init; } = string.Empty;
    public string Stderr { get; init; } = string.Empty;
    public int ExitCode { get; init; }
    public bool TimedOut { get; init; }
    public double ExecTimeSec { get; init; }
    public int MemoryUsedMB { get; init; }
    public string ScriptHash { get; init; } = string.Empty;
    public DateTime FinishedAtUtc { get; init; }
}