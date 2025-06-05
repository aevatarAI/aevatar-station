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

namespace Aevatar.Silo.GrainWarmup;

/// <summary>
/// Service for automatically discovering warmup-eligible grain types from assemblies
/// </summary>
public class GrainDiscoveryService : IGrainDiscoveryService
{
    private readonly AutoDiscoveryConfiguration _config;
    private readonly ILogger<GrainDiscoveryService> _logger;
    private readonly ConcurrentDictionary<Type, Type> _grainTypeMapping = new();
    private readonly Lazy<IEnumerable<Type>> _discoveredTypes;

    public GrainDiscoveryService(
        IOptions<GrainWarmupConfiguration> options,
        ILogger<GrainDiscoveryService> logger)
    {
        _config = options.Value.AutoDiscovery;
        _logger = logger;
        _discoveredTypes = new Lazy<IEnumerable<Type>>(DiscoverTypesInternal);
    }

    public IEnumerable<Type> DiscoverWarmupEligibleGrainTypes(IEnumerable<Type>? excludedTypes = null)
    {
        var excluded = excludedTypes?.ToHashSet() ?? new HashSet<Type>();
        var configExcluded = _config.ExcludedGrainTypes
            .Select(typeName => Type.GetType(typeName))
            .Where(t => t != null)
            .ToHashSet();

        return _discoveredTypes.Value
            .Where(t => !excluded.Contains(t) && !configExcluded.Contains(t));
    }

    public bool IsWarmupEligible(Type grainType)
    {
        try
        {
            // Check if it's a grain type
            if (!IsGrainType(grainType))
                return false;

            // Check base type requirement - simple and efficient
            if (_config.BaseTypes.Any())
            {
                var hasRequiredBaseType = _config.BaseTypes.Any(baseType => grainType.IsAssignableTo(baseType));
                if (!hasRequiredBaseType)
                {
                    _logger.LogDebug("Grain type {GrainType} does not inherit from any required base types", grainType.Name);
                    return false;
                }
            }

            // Check required attributes
            if (!HasRequiredAttributes(grainType))
            {
                _logger.LogDebug("Grain type {GrainType} does not have required attributes", grainType.Name);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking warmup eligibility for type {GrainType}", grainType.Name);
            return false;
        }
    }

    public Type GetGrainIdentifierType(Type grainType)
    {
        if (_grainTypeMapping.TryGetValue(grainType, out var cachedType))
            return cachedType;

        var identifierType = DetermineIdentifierType(grainType);
        _grainTypeMapping.TryAdd(grainType, identifierType);
        return identifierType;
    }

    public Dictionary<Type, Type> GetGrainTypeMapping()
    {
        // Ensure all discovered types are processed
        var discoveredTypes = _discoveredTypes.Value.ToList();
        
        // Build mapping for all discovered types
        foreach (var grainType in discoveredTypes)
        {
            GetGrainIdentifierType(grainType);
        }

        return new Dictionary<Type, Type>(_grainTypeMapping);
    }

    private IEnumerable<Type> DiscoverTypesInternal()
    {
        _logger.LogInformation("Starting automatic grain discovery...");
        
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
                    _logger.LogInformation("Discovered {Count} warmup-eligible grain types in assembly {Assembly}", 
                        types.Count, assembly.GetName().Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error scanning assembly {Assembly} for grain types", 
                    assembly.GetName().Name);
            }
        }

        _logger.LogInformation("Grain discovery completed. Found {Count} eligible grain types", 
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

    private bool IsGrainType(Type type)
    {
        if (type.IsAbstract || type.IsInterface)
            return false;

        // Check if it implements any Orleans grain interface
        return type.GetInterfaces().Any(i => 
            typeof(IGrainWithGuidKey).IsAssignableFrom(i) ||
            typeof(IGrainWithStringKey).IsAssignableFrom(i) ||
            typeof(IGrainWithIntegerKey).IsAssignableFrom(i) ||
            typeof(IGrainWithGuidCompoundKey).IsAssignableFrom(i) ||
            typeof(IGrainWithIntegerCompoundKey).IsAssignableFrom(i));
    }

    private bool HasRequiredAttributes(Type grainType)
    {
        foreach (var requiredAttribute in _config.RequiredAttributes)
        {
            switch (requiredAttribute.ToLowerInvariant())
            {
                case "storageprovider":
                    var storageAttr = grainType.GetCustomAttribute<StorageProviderAttribute>();
                    if (storageAttr == null)
                        return false;
                    
                    if (!string.IsNullOrEmpty(_config.StorageProviderName) && 
                        storageAttr.ProviderName != _config.StorageProviderName)
                        return false;
                    break;

                default:
                    // Try to find attribute by name
                    var hasAttribute = grainType.GetCustomAttributes()
                        .Any(attr => attr.GetType().Name.Contains(requiredAttribute, StringComparison.OrdinalIgnoreCase));
                    if (!hasAttribute)
                        return false;
                    break;
            }
        }

        return true;
    }

    private Type DetermineIdentifierType(Type grainType)
    {
        var interfaces = grainType.GetInterfaces();

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
        _logger.LogWarning("Could not determine identifier type for grain {GrainType}, defaulting to Guid", 
            grainType.Name);
        return typeof(Guid);
    }
} 