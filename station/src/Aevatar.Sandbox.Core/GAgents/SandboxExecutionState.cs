using System;
using System.Collections.Generic;
using Orleans.Serialization.GeneratedCodeHelpers;
using Orleans.Serialization.Cloning;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.WireProtocol;
using Orleans.Serialization.Serializers;
using Orleans.Serialization.Codecs;
using Orleans.Serialization.Buffers.Adaptors;
using Orleans.Serialization;

namespace Aevatar.Sandbox.Core.GAgents;

[GenerateSerializer]
public sealed class SandboxExecutionState
{
    [Id(0)]
    public int InFlightCount { get; set; }

    [Id(1)]
    public List<ExecutionRecord> History { get; set; } = new();
}

[GenerateSerializer]
public sealed class ExecutionRecord
{
    [Id(0)]
    public string SandboxExecutionId { get; init; } = string.Empty;

    [Id(1)]
    public string LanguageId { get; init; } = string.Empty;

    [Id(2)]
    public DateTime StartedAtUtc { get; init; }

    [Id(3)]
    public DateTime? FinishedAtUtc { get; init; }

    [Id(4)]
    public bool? Success { get; init; }

    [Id(5)]
    public string? Error { get; init; }

    [Id(6)]
    public string? Stdout { get; init; }

    [Id(7)]
    public string? Stderr { get; init; }

    [Id(8)]
    public int? ExitCode { get; init; }

    [Id(9)]
    public bool? TimedOut { get; init; }

    [Id(10)]
    public double? ExecTimeSec { get; init; }

    [Id(11)]
    public int? MemoryUsedMB { get; init; }

    [Id(12)]
    public string? ScriptHash { get; init; }
}