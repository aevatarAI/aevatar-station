using Orleans;

namespace Aevatar.Sandbox.Core.Streaming.Messages;

[GenerateSerializer]
public class SandboxExecEnqueueMessage
{
    [Id(0)]
    public string ExecutionId { get; set; } = string.Empty;

    [Id(1)]
    public string Language { get; set; } = string.Empty;

    [Id(2)]
    public string TenantId { get; set; } = string.Empty;

    [Id(3)]
    public string ChatId { get; set; } = string.Empty;

    [Id(4)]
    public int Timeout { get; set; }

    [Id(5)]
    public string Code { get; set; } = string.Empty;

    [Id(6)]
    public string CpuLimit { get; set; } = "1.0";

    [Id(7)]
    public string MemoryLimit { get; set; } = "512M";
}