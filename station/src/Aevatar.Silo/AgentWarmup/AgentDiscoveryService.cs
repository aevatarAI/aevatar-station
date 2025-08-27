using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Storage;
using Orleans.Providers;

namespace Aevatar.Silo.AgentWarmup;

/// <summary>
/// Service for automatically discovering warmup-eligible agent types from assemblies
/// </summary>
public class AgentDiscoveryService : IAgentDiscoveryService
{
    private readonly AutoDiscoveryConfiguration _config;
    private readonly ILogger<AgentDiscoveryService> _logger;
    private readonly ConcurrentDictionary<Type, Type> _agentTypeMapping = new();
    private readonly Lazy<IEnumerable<Type>> _discoveredTypes;

    public AgentDiscoveryService(
        IOptions<AgentWarmupConfiguration> options,
        ILogger<AgentDiscoveryService> logger)
    {
        _config = options.Value.AutoDiscovery;
        _logger = logger;
        _discoveredTypes = new Lazy<IEnumerable<Type>>(DiscoverTypesInternal);
    }

    public IEnumerable<Type> DiscoverWarmupEligibleAgentTypes(IEnumerable<Type>? excludedTypes = null)
    {
        var excluded = excludedTypes?.ToHashSet() ?? new HashSet<Type>();
        var configExcluded = _config.ExcludedAgentTypes
            .Select(typeName => Type.GetType(typeName))
            .Where(t => t != null)
            .ToHashSet();

        return _discoveredTypes.Value
            .Where(t => !excluded.Contains(t) && !configExcluded.Contains(t));
    }

    public bool IsWarmupEligible(Type agentType)
    {
        try
        {
            // Check if it's a agent type
            if (!IsAgentType(agentType))
                return false;

            // Check base type requirement - simple and efficient
            if (_config.BaseTypes.Any())
            {
                var hasRequiredBaseType = _config.BaseTypes.Any(baseType => agentType.IsAssignableTo(baseType));
                if (!hasRequiredBaseType)
                {
                    _logger.LogDebug("Agent type {AgentType} does not inherit from any required base types", agentType.Name);
                    return false;
                }
            }

            // Check required attributes
            if (!HasRequiredAttributes(agentType))
            {
                _logger.LogDebug("Agent type {AgentType} does not have required attributes", agentType.Name);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking warmup eligibility for type {AgentType}", agentType.Name);
            return false;
        }
    }

    public Type GetAgentIdentifierType(Type agentType)
    {
        if (_agentTypeMapping.TryGetValue(agentType, out var cachedType))
            return cachedType;

        var identifierType = DetermineIdentifierType(agentType);
        _agentTypeMapping.TryAdd(agentType, identifierType);
        return identifierType;
    }

    public Dictionary<Type, Type> GetAgentTypeMapping()
    {
        // Ensure all discovered types are processed
        var discoveredTypes = _discoveredTypes.Value.ToList();
        
        // Build mapping for all discovered types
        foreach (var agentType in discoveredTypes)
        {
            GetAgentIdentifierType(agentType);
        }

        return new Dictionary<Type, Type>(_agentTypeMapping);
    }

    private IEnumerable<Type> DiscoverTypesInternal()
    {
        _logger.LogInformation("Starting automatic agent discovery...");
        
        var assemblies = GetTargetAssemblies();
        var discoveredTypes = new List<Type>();

        foreach (var assembly in assemblies)
        {
            try
            {
                var types = assembly.GetTypes()
                    .Where(IsWarmupEligible)
                    .ToList();

                discoveredTypes.AddRange(types);
                
                if (types.Any())
                {
                    _logger.LogInformation("Discovered {Count} warmup-eligible agent types in assembly {Assembly}", 
                        types.Count, assembly.GetName().Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error scanning assembly {Assembly} for agent types", 
                    assembly.GetName().Name);
            }
        }

        _logger.LogInformation("Agent discovery completed. Found {Count} eligible agent types", 
            discoveredTypes.Count);

        return discoveredTypes;
    }

    private IEnumerable<Assembly> GetTargetAssemblies()
    {
        if (_config.IncludedAssemblies.Any())
        {
            // Use specific assemblies if configured
            foreach (var assemblyName in _config.IncludedAssemblies)
            {
                Assembly? assembly = null;
                try
                {
                    assembly = Assembly.LoadFrom(assemblyName);
                }
                catch
                {
                    try
                    {
                        assembly = Assembly.Load(assemblyName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not load assembly {AssemblyName}", assemblyName);
                    }
                }

                if (assembly != null)
                    yield return assembly;
            }
        }
        else
        {
            // Use all loaded assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // Skip system assemblies
                var name = assembly.GetName().Name;
                if (name != null && !name.StartsWith("System.") && !name.StartsWith("Microsoft."))
                {
                    yield return assembly;
                }
            }
        }
    }

    private bool IsAgentType(Type type)
    {
        if (type.IsAbstract || type.IsInterface)
            return false;

        // Check if it implements any Orleans agent interface
        return type.GetInterfaces().Any(i => 
            typeof(IGrainWithGuidKey).IsAssignableFrom(i) ||
            typeof(IGrainWithStringKey).IsAssignableFrom(i) ||
            typeof(IGrainWithIntegerKey).IsAssignableFrom(i) ||
            typeof(IGrainWithGuidCompoundKey).IsAssignableFrom(i) ||
            typeof(IGrainWithIntegerCompoundKey).IsAssignableFrom(i));
    }

    private bool HasRequiredAttributes(Type agentType)
    {
        foreach (var requiredAttribute in _config.RequiredAttributes)
        {
            switch (requiredAttribute.ToLowerInvariant())
            {
                case "storageprovider":
                    var storageAttr = agentType.GetCustomAttribute<StorageProviderAttribute>();
                    if (storageAttr == null)
                        return false;
                    
                    if (!string.IsNullOrEmpty(_config.StorageProviderName) && 
                        storageAttr.ProviderName != _config.StorageProviderName)
                        return false;
                    break;

                default:
                    // Try to find attribute by name
                    var hasAttribute = agentType.GetCustomAttributes()
                        .Any(attr => attr.GetType().Name.Contains(requiredAttribute, StringComparison.OrdinalIgnoreCase));
                    if (!hasAttribute)
                        return false;
                    break;
            }
        }

        return true;
    }

    private Type DetermineIdentifierType(Type agentType)
    {
        var interfaces = agentType.GetInterfaces();

        if (interfaces.Any(i => typeof(IGrainWithGuidKey).IsAssignableFrom(i)))
            return typeof(Guid);
        
        if (interfaces.Any(i => typeof(IGrainWithStringKey).IsAssignableFrom(i)))
            return typeof(string);
        
        if (interfaces.Any(i => typeof(IGrainWithIntegerKey).IsAssignableFrom(i)))
            return typeof(long);
        
        if (interfaces.Any(i => typeof(IGrainWithGuidCompoundKey).IsAssignableFrom(i)))
            return typeof(Guid);
        
        if (interfaces.Any(i => typeof(IGrainWithIntegerCompoundKey).IsAssignableFrom(i)))
            return typeof(long);

        // Default to Guid if we can't determine
        _logger.LogWarning("Could not determine identifier type for agent {AgentType}, defaulting to Guid", 
            agentType.Name);
        return typeof(Guid);
    }
} 