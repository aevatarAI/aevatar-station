using Aevatar.Core.Abstractions;
using Aevatar.Silo.Grains.Activation;
using Aevatar.Silo.TypeDiscovery;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;

namespace Aevatar.Silo.Startup
{
    /// <summary>
    /// Startup task that initializes all StateProjectionGrains when the silo starts
    /// Enhanced with K8s rolling update support and distributed coordination
    /// </summary>
    public class StateProjectionInitializer : IStartupTask
    {
        private readonly IStateTypeDiscoverer _typeDiscoverer;
        private readonly IProjectionGrainActivator _grainActivator;
        private readonly ILogger<StateProjectionInitializer> _logger;
        private readonly ILocalSiloDetails _siloDetails;

        public StateProjectionInitializer(
            IStateTypeDiscoverer typeDiscoverer,
            IProjectionGrainActivator grainActivator,
            ILogger<StateProjectionInitializer> logger,
            ILocalSiloDetails siloDetails)
        {
            _typeDiscoverer = typeDiscoverer;
            _grainActivator = grainActivator;
            _logger = logger;
            _siloDetails = siloDetails;
        }

        public async Task Execute(CancellationToken cancellationToken)
        {
            try
            {
                // Add delay to stagger initialization across pods during rolling updates
                var staggerDelay = CalculateStaggerDelay();
                if (staggerDelay > TimeSpan.Zero)
                {
                    await Task.Delay(staggerDelay, cancellationToken);
                }

                // Get all types that inherit from StateBase
                var stateBaseTypes = _typeDiscoverer.GetAllInheritedTypesOf(typeof(StateBase));
                _logger.LogInformation("Initializing StateProjectionGrains for {Count} state types", stateBaseTypes.Count);
                
                // Process types with retry and coordination logic
                var tasks = stateBaseTypes.Select(stateType => 
                    InitializeStateProjectionWithRetry(stateType, cancellationToken));
                
                await Task.WhenAll(tasks);

                _logger.LogInformation("StateProjectionGrains initialization completed for silo {SiloAddress}", 
                    _siloDetails.SiloAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StateProjectionGrains initialization failed on silo {SiloAddress}", 
                    _siloDetails.SiloAddress);
                throw;
            }
        }

        private async Task InitializeStateProjectionWithRetry(Type stateType, CancellationToken cancellationToken)
        {
            const int maxRetries = 3;
            const int baseDelayMs = 1000;
            
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    // Orleans ActivateAsync is idempotent - if grain is already active, it won't process again
                    await _grainActivator.ActivateProjectionGrainAsync(stateType, cancellationToken);
                    
                    if (attempt > 1)
                    {
                        _logger.LogInformation("StateProjectionGrain initialized for {StateType} on attempt {Attempt}", 
                            stateType.Name, attempt);
                    }
                    return;
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    var delay = TimeSpan.FromMilliseconds(baseDelayMs * Math.Pow(2, attempt - 1));
                    _logger.LogWarning(ex, "StateProjectionGrain initialization failed for {StateType}, attempt {Attempt}/{MaxRetries}, retrying in {Delay}ms", 
                        stateType.Name, attempt, maxRetries, delay.TotalMilliseconds);
                    
                    await Task.Delay(delay, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "StateProjectionGrain initialization failed for {StateType} after {MaxRetries} attempts", 
                        stateType.Name, maxRetries);
                    throw;
                }
            }
        }



        private TimeSpan CalculateStaggerDelay()
        {
            try
            {
                // Use silo address hash to create consistent but different delays across pods
                var hash = _siloDetails.SiloAddress.GetHashCode();
                var delayMs = Math.Abs(hash % 5000); // 0-5 second stagger
                return TimeSpan.FromMilliseconds(delayMs);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to calculate stagger delay, using no delay");
                return TimeSpan.Zero;
            }
        }
    }
} 