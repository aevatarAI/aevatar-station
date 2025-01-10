namespace Aevatar.Core.Abstractions;

public interface IEventDispatcher
{
    Task PublishAsync(StateBase state, GrainId grainId);
    Task PublishAsync(Guid eventId, GrainId grainId, GEventBase eventBase);
}