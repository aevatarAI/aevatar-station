namespace Aevatar.Core.Abstractions;

public interface IStateDispatcher
{
    Task PublishAsync<TState>(GrainId grainId, TState state) where TState : StateBase;
}