using System;
using Orleans.Serialization.GeneratedCodeHelpers;
using Orleans.Serialization.Cloning;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.WireProtocol;
using Orleans.Serialization.Serializers;
using Orleans.Serialization.Codecs;
using Orleans.Serialization.Buffers.Adaptors;
using Orleans.Serialization;
using System.Text;

namespace Aevatar.Sandbox.Core.GAgents.Events;

[GenerateSerializer]
public abstract record SandboxExecutionStateLogEvent
{
    public string SandboxExecutionId { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

[GenerateSerializer]
public sealed record ExecutionStartedEvent(string SandboxExecutionId, string LanguageId, DateTime StartedAtUtc) : SandboxExecutionStateLogEvent;

[GenerateSerializer]
public sealed record ExecutionCompletedEvent(string SandboxExecutionId, bool Success, string Stdout, string Stderr, int ExitCode, bool TimedOut, double ExecTimeSec, int MemoryUsedMB, string ScriptHash, DateTime FinishedAtUtc) : SandboxExecutionStateLogEvent;

[GenerateSerializer]
public sealed record ExecutionFailedEvent(string SandboxExecutionId, string Error, DateTime FailedAtUtc) : SandboxExecutionStateLogEvent;

[GenerateSerializer]
public sealed record ExecutionCancelledEvent(string SandboxExecutionId, DateTime CancelledAtUtc) : SandboxExecutionStateLogEvent;