using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Projections;
using Aevatar.Silo.IdGeneration;
using Microsoft.Extensions.Logging;

namespace Aevatar.Silo.Grains.Activation
{
    /// <summary>
    /// Default implementation of IProjectionGrainActivator
    /// </summary>
    public class ProjectionGrainActivator : IProjectionGrainActivator
    {
        private readonly IGrainFactory _grainFactory;
        private readonly IDeterministicIdGenerator _idGenerator;
        private readonly ILogger<ProjectionGrainActivator> _logger;

        public ProjectionGrainActivator(
            IGrainFactory grainFactory,
            IDeterministicIdGenerator idGenerator,
            ILogger<ProjectionGrainActivator> logger)
        {
            _grainFactory = grainFactory;
            _idGenerator = idGenerator;
            _logger = logger;
        }

        public async Task ActivateProjectionGrainAsync(Type stateType, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                _logger.LogInformation("Activating StateProjectionGrain for {StateType}", stateType.Name);
                
                // Get the generic StateProjectionGrain type with this StateBase type
                var grainType = typeof(IProjectionGrain<>).MakeGenericType(stateType);
                

                // Get the grain and activate it
                for (int i = 0; i < AevatarCoreConstants.DefaultNumOfProjectorPerAgentType; i++)
                {
                    // Create a deterministic GUID based on the state type name
                    var grainId = _idGenerator.CreateDeterministicGuid(stateType.FullName + i.ToString());
                    RequestContext.Set("id", i);
                    var grain = _grainFactory.GetGrain(grainType, grainId).Cast(grainType);
                    var method = grainType.GetMethod("ActivateAsync");
                    await (Task)method?.Invoke(grain, null)!;
                
                    _logger.LogInformation("Successfully activated StateProjectionGrain for {StateType} with ID {GrainId}", 
                        stateType.Name, grainId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating StateProjectionGrain for {StateType}", stateType.Name);
                throw;
            }
        }
    }
} 