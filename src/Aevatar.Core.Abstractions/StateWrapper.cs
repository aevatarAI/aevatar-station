namespace Aevatar.Core.Abstractions;

[GenerateSerializer]
public class StateWrapper<T>(GrainId grainId, T state) : StateWrapperBase
    where T : StateBase
{
    [Id(0)] public GrainId GrainId { get; private set; } = grainId;
    [Id(1)] public T State { get; private set; } = state;
}