namespace Aevatar.Core.Abstractions.Projections;

public interface IProjectionGrain<TState> : IGrainWithGuidKey
    where TState : StateBase, new()
{
    Task ActivateAsync();
}

public interface IProjectionGrainPlus<TState> : IGrainWithGuidKey
    where TState : StateBasePlus, new()
{
    Task ActivateAsync();
}