using Aevatar.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Metadata;
using System.Reflection;
using Aevatar.Core;

namespace Aevatar.GAgents.Executor;

public class GAgentService : IGAgentService
{
    private readonly IGAgentManager _gAgentManager;
    private readonly ILogger<GAgentService> _logger;
    private readonly IGAgentFactory _gAgentFactory;
    private readonly GrainTypeResolver _grainTypeResolver;

    // Cache GAgent information to avoid repeated calculations
    private Dictionary<GrainType, List<Type>>? _cachedGAgentInfo;
    private DateTime _lastCacheTime = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    public GAgentService(
        IGAgentManager gAgentManager,
        IClusterClient clusterClient,
        ILogger<GAgentService> logger)
    {
        _gAgentManager = gAgentManager;
        _logger = logger;
        _gAgentFactory = new GAgentFactory(clusterClient);
        _grainTypeResolver = clusterClient.ServiceProvider.GetRequiredService<GrainTypeResolver>();
    }

    public async Task<Dictionary<GrainType, List<Type>>> GetAllAvailableGAgentInformation()
    {
        // Check if cache is valid
        if (_cachedGAgentInfo != null && DateTime.UtcNow - _lastCacheTime < _cacheExpiration)
        {
            return _cachedGAgentInfo;
        }

        var gAgentInfo = new Dictionary<GrainType, List<Type>>();

        try
        {
            // Get all GAgent types
            var gAgentTypes = _gAgentManager.GetAvailableGAgentGrainTypes();

            foreach (var grainType in gAgentTypes)
            {
                try
                {
                    if (grainType.ToString()!.StartsWith("proxy") || grainType.ToString()!.StartsWith("Aevatar.Core.ArtifactGAgent"))
                    {
                        continue;
                    }
                    var grainId = GrainId.Create(grainType, Guid.NewGuid().ToString());
                    // Create temporary instance to get event types
                    var tempGAgent = await _gAgentFactory.GetGAgentAsync(grainId);
                    var subscribedEvents = await tempGAgent.GetAllSubscribedEventsAsync(includeBaseHandlers: false);

                    if (subscribedEvents != null && subscribedEvents.Any())
                    {
                        if (!gAgentInfo.ContainsKey(grainType))
                        {
                            gAgentInfo[grainType] = [];
                        }

                        gAgentInfo[grainType].AddRange(subscribedEvents);

                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while trying to get GAgent of type {GrainType}", grainType.ToString());
                }
            }

            // Update cache
            _cachedGAgentInfo = gAgentInfo;
            _lastCacheTime = DateTime.UtcNow;

            return gAgentInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while trying to get all available GAgent types");
            throw;
        }
    }

    /// <summary>
    /// Get detailed information of GAgent, including description
    /// </summary>
    public async Task<GAgentDetailInfo> GetGAgentDetailInfoAsync(GrainType grainType)
    {
        try
        {
            // Create GAgent instance based on GrainType
            var grainId = GrainId.Create(grainType, Guid.NewGuid().ToString());
            var gAgent = await _gAgentFactory.GetGAgentAsync(grainId);

            // Get description
            var description = await gAgent.GetDescriptionAsync();

            // Get supported event types
            var eventTypes = await gAgent.GetAllSubscribedEventsAsync(includeBaseHandlers: false);

            // Get configuration type
            var configurationType = await gAgent.GetConfigurationTypeAsync();

            return new GAgentDetailInfo
            {
                GrainType = grainType,
                Description = description,
                SupportedEventTypes = eventTypes ?? new List<Type>(),
                ConfigurationType = configurationType
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting GAgent detail info for {grainType}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Find GAgents that support the given event type
    /// </summary>
    public async Task<List<GrainType>> FindGAgentsByEventTypeAsync(Type eventType)
    {
        var allGAgentInfo = await GetAllAvailableGAgentInformation();

        return allGAgentInfo
            .Where(kvp => kvp.Value.Contains(eventType))
            .Select(kvp => kvp.Key)
            .ToList();
    }
}

/// <summary>
/// Detailed information of GAgent
/// </summary>
[GenerateSerializer]
public class GAgentDetailInfo
{
    [Id(0)] public GrainType GrainType { get; set; }
    [Id(1)] public string Description { get; set; } = string.Empty;
    [Id(2)] public List<Type> SupportedEventTypes { get; set; } = new();
    [Id(3)] public Type? ConfigurationType { get; set; }
}