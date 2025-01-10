namespace Aevatar.Core.Abstractions;

public interface IEventDispatcher
{
    Task PublishAsync(StateBase state, string grainId);
    Task PublishAsync(StateBase state, GrainId grainId);
    Task PublishAsync(Guid eventId, GrainId grainId, GEventBase eventBase);
    Task PublishAsync(Guid eventId, string grainId, GEventBase eventBase);
}