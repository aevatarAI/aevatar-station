using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Orleans.Streams;

namespace Aevatar.Silo.Grains.Activation
{
    /// <summary>
    /// Helper class to check and recover stream subscriptions during K8s rolling updates
    /// Ensures stream continuity when pods are replaced
    /// </summary>
    public class StreamSubscriptionHealthChecker
    {
        private readonly ILogger<StreamSubscriptionHealthChecker> _logger;
        private readonly ILocalSiloDetails _siloDetails;

        public StreamSubscriptionHealthChecker(
            ILogger<StreamSubscriptionHealthChecker> logger, 
            ILocalSiloDetails siloDetails)
        {
            _logger = logger;
            _siloDetails = siloDetails;
        }

        /// <summary>
        /// Checks if stream subscriptions need to be recovered for a grain
        /// </summary>
        /// <param name="grain">The grain to check</param>
        /// <param name="streamProvider">Stream provider to use</param>
        /// <param name="streamId">Stream ID to check</param>
        /// <returns>True if subscription recovery is needed</returns>
        public async Task<bool> NeedsSubscriptionRecovery<T>(
            IGrain grain, 
            IStreamProvider streamProvider, 
            StreamId streamId)
        {
            try
            {
                var stream = streamProvider.GetStream<T>(streamId);
                var subscriptions = await stream.GetAllSubscriptionHandles();
                
                if (subscriptions == null || subscriptions.Count == 0)
                {
                    _logger.LogInformation("No existing subscriptions found for stream {StreamId} on silo {SiloAddress}, recovery needed", 
                        streamId, _siloDetails.SiloAddress);
                    return true;
                }

                // Check if any subscriptions are stale or need resumption
                foreach (var subscription in subscriptions)
                {
                    if (await IsSubscriptionStale(subscription))
                    {
                        _logger.LogWarning("Stale subscription detected for stream {StreamId} on silo {SiloAddress}, recovery needed", 
                            streamId, _siloDetails.SiloAddress);
                        return true;
                    }
                }

                _logger.LogDebug("All subscriptions are healthy for stream {StreamId} on silo {SiloAddress}", 
                    streamId, _siloDetails.SiloAddress);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking subscription health for stream {StreamId} on silo {SiloAddress}, assuming recovery needed", 
                    streamId, _siloDetails.SiloAddress);
                return true;
            }
        }

        /// <summary>
        /// Recovers stream subscriptions for a grain
        /// </summary>
        /// <param name="grain">The grain to recover subscriptions for</param>
        /// <param name="streamProvider">Stream provider to use</param>
        /// <param name="streamId">Stream ID to recover</param>
        /// <param name="handler">Message handler for the subscription</param>
        /// <returns>The recovered subscription handle</returns>
        public async Task<StreamSubscriptionHandle<T>> RecoverSubscription<T>(
            IGrain grain,
            IStreamProvider streamProvider,
            StreamId streamId,
            Func<T, StreamSequenceToken, Task> handler)
        {
            try
            {
                var stream = streamProvider.GetStream<T>(streamId);
                var existingSubscriptions = await stream.GetAllSubscriptionHandles();

                // Try to resume existing subscription first
                if (existingSubscriptions != null && existingSubscriptions.Count > 0)
                {
                    var existingSubscription = existingSubscriptions.First();
                    _logger.LogInformation("Resuming existing subscription for stream {StreamId} on silo {SiloAddress}", 
                        streamId, _siloDetails.SiloAddress);
                    
                    await existingSubscription.ResumeAsync(handler);
                    return existingSubscription;
                }

                // Create new subscription if none exists
                _logger.LogInformation("Creating new subscription for stream {StreamId} on silo {SiloAddress}", 
                    streamId, _siloDetails.SiloAddress);
                
                return await stream.SubscribeAsync(handler);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to recover subscription for stream {StreamId} on silo {SiloAddress}", 
                    streamId, _siloDetails.SiloAddress);
                throw;
            }
        }

        /// <summary>
        /// Checks if a subscription handle is stale and needs recovery
        /// </summary>
        private async Task<bool> IsSubscriptionStale<T>(StreamSubscriptionHandle<T> subscription)
        {
            try
            {
                // For now, we'll do basic checks
                // In a more sophisticated implementation, we could check:
                // - Last activity timestamp
                // - Connection status
                // - Silo health of the subscription owner
                
                if (subscription.HandleId == Guid.Empty)
                {
                    return true;
                }

                return false; // Conservative approach - assume healthy
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error checking subscription staleness, assuming stale");
                return true;
            }
        }

        /// <summary>
        /// Performs a complete health check and recovery for grain stream subscriptions
        /// </summary>
        public async Task EnsureHealthySubscriptions<T>(
            IGrain grain,
            IStreamProvider streamProvider,
            StreamId streamId,
            Func<T, StreamSequenceToken, Task> handler)
        {
            try
            {
                _logger.LogDebug("Performing stream subscription health check for grain {GrainId} on silo {SiloAddress}", 
                    grain.GetGrainId(), _siloDetails.SiloAddress);

                if (await NeedsSubscriptionRecovery<T>(grain, streamProvider, streamId))
                {
                    await RecoverSubscription(grain, streamProvider, streamId, handler);
                    _logger.LogInformation("Successfully recovered stream subscription for grain {GrainId} on silo {SiloAddress}", 
                        grain.GetGrainId(), _siloDetails.SiloAddress);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ensure healthy subscriptions for grain {GrainId} on silo {SiloAddress}", 
                    grain.GetGrainId(), _siloDetails.SiloAddress);
                throw;
            }
        }
    }
} 