using System;
using System.Collections.Generic;
using Orleans;

namespace Aevatar.Sandbox.Core.GAgents;

[GenerateSerializer]
public class SandboxExecutionState
{
    [Id(0)]
    public string ExecutionId { get; set; } = string.Empty;

    [Id(1)]
    public string Status { get; set; } = string.Empty;

    [Id(2)]
    public DateTime? StartTime { get; set; }

    [Id(3)]
    public DateTime? EndTime { get; set; }

    [Id(4)]
    public string? Output { get; set; }

    [Id(5)]
    public string? Error { get; set; }

    [Id(6)]
    public int ExitCode { get; set; }

    [Id(7)]
    public string PodName { get; set; } = string.Empty;

    [Id(8)]
    public string CpuUsage { get; set; } = string.Empty;

    [Id(9)]
    public string MemoryUsage { get; set; } = string.Empty;

    [Id(10)]
    public string DiskUsage { get; set; } = string.Empty;

    [Id(11)]
    public List<string> Logs { get; set; } = new List<string>();
}