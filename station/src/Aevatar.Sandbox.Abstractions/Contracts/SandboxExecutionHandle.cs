using System;

namespace Aevatar.Sandbox.Abstractions.Contracts;

public sealed class SandboxExecutionHandle
{
    public required string SandboxExecutionId { get; init; }
    public required string WorkloadName { get; init; }
    public DateTime StartedAtUtc { get; init; }
}