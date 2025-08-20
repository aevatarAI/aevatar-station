using System;

namespace Aevatar.Sandbox.Core.Streaming.Messages;

[GenerateSerializer]
public sealed class SandboxExecEnqueueMessage
{
    [Id(0)] public required string SandboxExecutionId { get; init; }
    [Id(1)] public required string LanguageId { get; init; }
    [Id(2)] public required string Code { get; init; }
    [Id(3)] public int TimeoutSeconds { get; init; } = 30;
    [Id(4)] public string? TenantId { get; init; }
    [Id(5)] public string? ChatId { get; init; }
    [Id(6)] public int Priority { get; init; }
}

[GenerateSerializer]
public sealed class SandboxExecResultMessage
{
    [Id(0)] public required string SandboxExecutionId { get; init; }
    [Id(1)] public bool Success { get; init; }
    [Id(2)] public string Stdout { get; init; } = string.Empty;
    [Id(3)] public string Stderr { get; init; } = string.Empty;
    [Id(4)] public int ExitCode { get; init; }
    [Id(5)] public bool TimedOut { get; init; }
    [Id(6)] public double ExecTimeSec { get; init; }
    [Id(7)] public int MemoryUsedMB { get; init; }
    [Id(8)] public string ScriptHash { get; init; } = string.Empty;
    [Id(9)] public DateTime FinishedAtUtc { get; init; }
}