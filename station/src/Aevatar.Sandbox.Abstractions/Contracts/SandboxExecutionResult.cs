using System;
using Orleans;

namespace Aevatar.Sandbox.Abstractions.Contracts;

[GenerateSerializer]
public class SandboxExecutionResult
{
    [Id(0)]
    public string ExecutionId { get; set; } = string.Empty;

    [Id(1)]
    public ExecutionStatus Status { get; set; }

    [Id(2)]
    public DateTime? StartTime { get; set; }

    [Id(3)]
    public DateTime? EndTime { get; set; }

    [Id(4)]
    public string Language { get; set; } = string.Empty;

    [Id(5)]
    public string PodName { get; set; } = string.Empty;

    [Id(6)]
    public ResourceUsage ResourceUsage { get; set; } = new();

    [Id(7)]
    public int ExitCode { get; set; }

    [Id(8)]
    public string Output { get; set; } = string.Empty;

    [Id(9)]
    public string Error { get; set; } = string.Empty;
}