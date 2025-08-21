using System;
using Orleans;

namespace Aevatar.Sandbox.Core.Streaming.Messages;

[GenerateSerializer]
public class SandboxExecResultMessage
{
    [Id(0)]
    public string SandboxExecutionId { get; set; } = string.Empty;

    [Id(1)]
    public bool Success { get; set; }

    [Id(2)]
    public string Stdout { get; set; } = string.Empty;

    [Id(3)]
    public string Stderr { get; set; } = string.Empty;

    [Id(4)]
    public bool TimedOut { get; set; }

    [Id(5)]
    public double ExecTimeSec { get; set; }

    [Id(6)]
    public double MemoryUsedMB { get; set; }

    [Id(7)]
    public string ScriptHash { get; set; } = string.Empty;

    [Id(8)]
    public DateTime FinishedAtUtc { get; set; }
}