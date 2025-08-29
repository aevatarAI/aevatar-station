using Aevatar.Core.Abstractions;

public interface ICoordinatorGAgent : IGAgent
{
    Task StartAsync(Guid blackboardId);
}