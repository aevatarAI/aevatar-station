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
        private readonly IGrainFactory _grainFactory;
        private readonly ILocalSiloDetails _siloDetails;

        public StateProjectionInitializer(
            IStateTypeDiscoverer typeDiscoverer,
            IProjectionGrainActivator grainActivator,
            ILogger<StateProjectionInitializer> logger,
            IGrainFactory grainFactory,
            ILocalSiloDetails siloDetails)
        {
            _typeDiscoverer = typeDiscoverer;
            _grainActivator = grainActivator;
            _logger = logger;
            _grainFactory = grainFactory;
            _siloDetails = siloDetails;
        }

        public async Task Execute(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting StateProjectionGrains initialization for silo {SiloAddress}...", 
                _siloDetails.SiloAddress);
            
            try
            {
                // Add delay to stagger initialization across pods during rolling updates
                var staggerDelay = CalculateStaggerDelay();
                if (staggerDelay > TimeSpan.Zero)
                {
                    _logger.LogInformation("Applying stagger delay of {Delay}ms to avoid K8s rolling update conflicts", 
                        staggerDelay.TotalMilliseconds);
                    await Task.Delay(staggerDelay, cancellationToken);
                }

                // Get all types that inherit from StateBase
                var stateBaseTypes = _typeDiscoverer.GetAllInheritedTypesOf(typeof(StateBase));
                _logger.LogInformation("🔍 Found {Count} StateBase inherited types for stream processing", stateBaseTypes.Count);
                
                // Log each state type for monitoring purposes
                foreach (var stateType in stateBaseTypes)
                {
                    _logger.LogInformation("📋 State type discovered: {StateType} - will initialize stream subscriptions", stateType.Name);
                }
                
                // Process types with retry and coordination logic
                var tasks = stateBaseTypes.Select(stateType => 
                    InitializeStateProjectionWithRetry(stateType, cancellationToken));
                
                await Task.WhenAll(tasks);

                _logger.LogInformation("🎉 Completed initializing StateProjectionGrains for silo {SiloAddress} - All stream subscriptions are ready", 
                    _siloDetails.SiloAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during StateProjectionGrains initialization on silo {SiloAddress}", 
                    _siloDetails.SiloAddress);
                throw; // Rethrow to fail the silo startup if initialization fails
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
                    
                    _logger.LogInformation("🎯 Successfully initialized StateProjectionGrain for {StateType} on attempt {Attempt} - Stream subscriptions are now active", 
                        stateType.Name, attempt);
                    return;
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    var delay = TimeSpan.FromMilliseconds(baseDelayMs * Math.Pow(2, attempt - 1));
                    _logger.LogWarning(ex, "Failed to initialize StateProjectionGrain for {StateType} on attempt {Attempt}, retrying in {Delay}ms", 
                        stateType.Name, attempt, delay.TotalMilliseconds);
                    
                    await Task.Delay(delay, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize StateProjectionGrain for {StateType} after {MaxRetries} attempts", 
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