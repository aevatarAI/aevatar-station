using Orleans;

namespace Aevatar.Sandbox.Abstractions.Contracts;

[GenerateSerializer]
public class SandboxLogs
{
    [Id(0)]
    public string ExecutionId { get; set; } = string.Empty;

    [Id(1)]
    public string PodName { get; set; } = string.Empty;

    [Id(2)]
    public string Namespace { get; set; } = string.Empty;

    [Id(3)]
    public string[] Lines { get; set; } = Array.Empty<string>();

    [Id(4)]
    public bool HasMore { get; set; }

    [Id(5)]
    public string Error { get; set; } = string.Empty;
}