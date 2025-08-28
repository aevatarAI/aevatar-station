using Orleans;

namespace Aevatar.Sandbox.Abstractions.Contracts;

[GenerateSerializer]
public class ResourceUsage
{
    [Id(0)]
    public double CpuUsageCores { get; set; }

    [Id(1)]
    public long MemoryUsageBytes { get; set; }

    [Id(2)]
    public long NetworkInBytes { get; set; }

    [Id(3)]
    public long NetworkOutBytes { get; set; }

    [Id(4)]
    public long DiskReadBytes { get; set; }

    [Id(5)]
    public long DiskWriteBytes { get; set; }
}