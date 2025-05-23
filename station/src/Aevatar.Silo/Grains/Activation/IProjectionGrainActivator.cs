namespace Aevatar.Silo.Grains.Activation
{
    /// <summary>
    /// Interface for projection grain activator
    /// </summary>
    public interface IProjectionGrainActivator
    {
        /// <summary>
        /// Activates a projection grain for the given state type
        /// </summary>
        Task ActivateProjectionGrainAsync(Type stateType, CancellationToken cancellationToken);
    }
} 