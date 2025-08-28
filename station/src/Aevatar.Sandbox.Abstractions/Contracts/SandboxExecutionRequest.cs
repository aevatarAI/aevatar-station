using Orleans;

namespace Aevatar.Sandbox.Abstractions.Contracts;

[GenerateSerializer]
public class SandboxExecutionRequest
{
    [Id(0)]
    public string Code { get; set; } = string.Empty;

    [Id(1)]
    public int Timeout { get; set; } = 30;

    [Id(2)]
    public string Language { get; set; } = "python";

    [Id(3)]
    public ExecutionResourceLimits Resources { get; set; }
}

[GenerateSerializer]
public class ExecutionResourceLimits
{
    [Id(0)]
    public string CpuLimit { get; set; } = "100m";

    [Id(1)]
    public string MemoryLimit { get; set; } = "256Mi";

    [Id(2)]
    public string DiskLimit { get; set; } = "1Gi";
}