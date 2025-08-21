using Orleans;

namespace Aevatar.Sandbox.Abstractions.Contracts;

[GenerateSerializer]
public class LogQueryOptions
{
    [Id(0)]
    public int MaxLines { get; set; } = 1000;

    [Id(1)]
    public bool Tail { get; set; } = true;

    [Id(2)]
    public string? Since { get; set; }

    [Id(3)]
    public string? Until { get; set; }

    [Id(4)]
    public bool Follow { get; set; }
}