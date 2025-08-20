namespace Aevatar.Sandbox.Kubernetes.Models;

public class SandboxResourceLimits
{
    public int CpuMillicores { get; init; } = 1000; // 1 vCPU
    public int MemoryMB { get; init; } = 512;
    public int TimeoutSeconds { get; init; } = 30;
}