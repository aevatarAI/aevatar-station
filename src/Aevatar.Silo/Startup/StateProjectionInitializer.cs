using Aevatar.Core.Abstractions;
using Aevatar.Silo.Grains.Activation;
using Aevatar.Silo.TypeDiscovery;
using Microsoft.Extensions.Logging;

namespace Aevatar.Silo.Startup
{
    /// <summary>
    /// Startup task that initializes all StateProjectionGrains when the silo starts
    /// </summary>
    public class StateProjectionInitializer : IStartupTask
    {
        private readonly IStateTypeDiscoverer _typeDiscoverer;
        private readonly IProjectionGrainActivator _grainActivator;
        private readonly List<IStateProjector> _stateProjectors;
        private readonly ILogger<StateProjectionInitializer> _logger;

        public StateProjectionInitializer(
            IStateTypeDiscoverer typeDiscoverer,
            IProjectionGrainActivator grainActivator,
            IEnumerable<IStateProjector> stateProjectors,
            ILogger<StateProjectionInitializer> logger)
        {
            _typeDiscoverer = typeDiscoverer;
            _grainActivator = grainActivator;
            _stateProjectors = stateProjectors.ToList();
            _logger = logger;
        }

        public async Task Execute(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting to initialize StateProjectionGrains...");
            
            try
            {
                // Get all types that inherit from StateBase
                var stateBaseTypes = _typeDiscoverer.GetAllInheritedTypesOf(typeof(StateBase));
                _logger.LogInformation("Found {Count} StateBase inherited types", stateBaseTypes.Count);
                
                // For each StateBase type, get the corresponding StateProjectionGrain and activate it
                foreach (var stateType in stateBaseTypes)
                {
                    try
                    {
                        await _grainActivator.ActivateProjectionGrainAsync(stateType, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error activating StateProjectionGrain for {StateType}", stateType.Name);
                    }
                }
                
                _logger.LogInformation("Completed initializing StateProjectionGrains with {Count} StateProjectors",
                    _stateProjectors.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during StateProjectionGrains initialization");
                throw; // Rethrow to fail the silo startup if initialization fails
            }
        }
    }
} 