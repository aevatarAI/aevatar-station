using Orleans;

namespace Aevatar.Sandbox.Abstractions.Contracts;

[GenerateSerializer]
public class SandboxExecutionClientParams
{
    [Id(0)]
    public string Code { get; set; } = string.Empty;

    [Id(1)]
    public int Timeout { get; set; }

    [Id(2)]
    public string Language { get; set; } = string.Empty;

    [Id(3)]
    public ResourceLimits Resources { get; set; } = new();
}