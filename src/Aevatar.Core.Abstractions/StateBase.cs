namespace Aevatar.Core.Abstractions;

[GenerateSerializer]
public abstract class StateBase
{
    [Id(0)] public List<GrainId> Children { get; set; } = [];
    [Id(1)] public GrainId? Parent { get; set; }
    [Id(2)] public Type? ConfigurationType { get; set; }
    [Id(3)] public string? GAgentCreator { get; set; }
}