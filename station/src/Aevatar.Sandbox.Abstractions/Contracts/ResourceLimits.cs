using Orleans;

namespace Aevatar.Sandbox.Abstractions.Contracts;

[GenerateSerializer]
public class ResourceLimits
{
    [Id(0)]
    public double CpuLimitCores { get; set; } = 1.0;

    [Id(1)]
    public long MemoryLimitBytes { get; set; } = 512 * 1024 * 1024;

    [Id(2)]
    public int TimeoutSeconds { get; set; } = 30;
}