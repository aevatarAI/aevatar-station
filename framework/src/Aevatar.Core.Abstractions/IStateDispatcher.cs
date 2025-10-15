namespace Aevatar.Core.Abstractions;

public interface IStateDispatcher
{
    // Original methods for backward compatibility
    Task PublishAsync<TState>(GrainId grainId, StateWrapper<TState> stateWrapper) where TState : StateBase;
    Task PublishSingleAsync<TState>(GrainId grainId, StateWrapper<TState> stateWrapper) where TState : StateBase;
    
    // Plus methods for enhanced GAgent components
    Task PublishPlusAsync<TState>(GrainId grainId, StateWrapperPlus<TState> stateWrapper) where TState : CoreStateBase;
    Task PublishSinglePlusAsync<TState>(GrainId grainId, StateWrapperPlus<TState> stateWrapper) where TState : CoreStateBase;
}