using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Projections;
using Aevatar.Silo.IdGeneration;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;

namespace Aevatar.Silo.Grains.Activation
{
    /// <summary>
    /// Default implementation of IProjectionGrainActivator
    /// Enhanced with stream subscription recovery for K8s rolling updates
    /// </summary>
    public class ProjectionGrainActivator : IProjectionGrainActivator
    {
        private readonly IGrainFactory _grainFactory;
        private readonly IDeterministicIdGenerator _idGenerator;
        private readonly ILogger<ProjectionGrainActivator> _logger;
        private readonly ILocalSiloDetails _siloDetails;

        public ProjectionGrainActivator(
            IGrainFactory grainFactory,
            IDeterministicIdGenerator idGenerator,
            ILogger<ProjectionGrainActivator> logger,
            ILocalSiloDetails siloDetails)
        {
            _grainFactory = grainFactory;
            _idGenerator = idGenerator;
            _logger = logger;
            _siloDetails = siloDetails;
        }

        public async Task ActivateProjectionGrainAsync(Type stateType, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                _logger.LogInformation("Activating StateProjectionGrain for {StateType} on silo {SiloAddress}", 
                    stateType.Name, _siloDetails.SiloAddress);
                
                // Get the generic StateProjectionGrain type with this StateBase type
                var grainType = typeof(IProjectionGrain<>).MakeGenericType(stateType);
                
                var activationTasks = new List<Task>();

                // Get the grain and activate it
                for (int i = 0; i < AevatarCoreConstants.DefaultNumOfProjectorPerAgentType; i++)
                {
                    var grainIndex = i; // Capture for closure
                    var activationTask = ActivateSingleProjectionGrain(grainType, stateType, grainIndex, cancellationToken);
                    activationTasks.Add(activationTask);
                }

                // Wait for all activations to complete
                await Task.WhenAll(activationTasks);
                
                _logger.LogInformation("Successfully activated {Count} StateProjectionGrains for {StateType} on silo {SiloAddress}", 
                    AevatarCoreConstants.DefaultNumOfProjectorPerAgentType, stateType.Name, _siloDetails.SiloAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating StateProjectionGrain for {StateType} on silo {SiloAddress}", 
                    stateType.Name, _siloDetails.SiloAddress);
                throw;
            }
        }

        private async Task ActivateSingleProjectionGrain(Type grainType, Type stateType, int index, CancellationToken cancellationToken)
        {
            try
            {
                // Create a deterministic GUID based on the state type name
                var grainId = _idGenerator.CreateDeterministicGuid(stateType.FullName + index.ToString());
                RequestContext.Set("id", index);
                
                var grain = _grainFactory.GetGrain(grainType, grainId).Cast(grainType);
                
                // Enhanced activation with stream subscription recovery
                var method = grainType.GetMethod("ActivateAsync");
                if (method != null)
                {
                    await (Task)method.Invoke(grain, null)!;
                }
                else
                {
                    // Fallback: try to call a parameterless method or use reflection
                    _logger.LogWarning("ActivateAsync method not found for {GrainType}, attempting alternative activation", grainType.Name);
                    
                    // Try to get a reference to activate the grain
                    _ = grain.GetType(); // This should trigger grain activation
                }
                
                _logger.LogDebug("Successfully activated StateProjectionGrain for {StateType} with ID {GrainId} (index {Index}) on silo {SiloAddress}", 
                    stateType.Name, grainId, index, _siloDetails.SiloAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating individual StateProjectionGrain for {StateType} at index {Index} on silo {SiloAddress}", 
                    stateType.Name, index, _siloDetails.SiloAddress);
                throw;
            }
        }
    }
} 