using Orleans;

namespace Aevatar.Sandbox.Core.Streaming.Messages;

[GenerateSerializer]
public class SandboxExecStartMessage
{
    [Id(0)]
    public string ExecutionId { get; set; } = string.Empty;

    [Id(1)]
    public string Code { get; set; } = string.Empty;

    [Id(2)]
    public string Language { get; set; } = string.Empty;

    [Id(3)]
    public int Timeout { get; set; }

    [Id(4)]
    public string CpuLimit { get; set; } = string.Empty;

    [Id(5)]
    public string MemoryLimit { get; set; } = string.Empty;

    [Id(6)]
    public string DiskLimit { get; set; } = string.Empty;
}

[GenerateSerializer]
public class SandboxExecStatusMessage
{
    [Id(0)]
    public string ExecutionId { get; set; } = string.Empty;

    [Id(1)]
    public string Status { get; set; } = string.Empty;

    [Id(2)]
    public string Output { get; set; } = string.Empty;

    [Id(3)]
    public string Error { get; set; } = string.Empty;

    [Id(4)]
    public int ExitCode { get; set; }

    [Id(5)]
    public string PodName { get; set; } = string.Empty;

    [Id(6)]
    public string CpuUsage { get; set; } = string.Empty;

    [Id(7)]
    public string MemoryUsage { get; set; } = string.Empty;

    [Id(8)]
    public string DiskUsage { get; set; } = string.Empty;
}