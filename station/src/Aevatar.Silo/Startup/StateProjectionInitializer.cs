using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Projections;
using Microsoft.Extensions.Logging;

namespace Aevatar.Silo.Startup
{
    /// <summary>
    /// Startup task that initializes all StateProjectionGrains when the silo starts
    /// </summary>
    public class StateProjectionInitializer : IStartupTask
    {
        private readonly IGrainFactory _grainFactory;
        private readonly ILogger<StateProjectionInitializer> _logger;

        public StateProjectionInitializer(
            IGrainFactory grainFactory,
            ILogger<StateProjectionInitializer> logger)
        {
            _grainFactory = grainFactory;
            _logger = logger;
        }

        public async Task Execute(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting to initialize StateProjectionGrains...");
            
            try
            {
                // Get all types that inherit from StateBase
                var stateBaseTypes = GetAllInheritedTypesOf(typeof(StateBase));
                _logger.LogInformation("Found {Count} StateBase inherited types", stateBaseTypes.Count);
                
                // For each StateBase type, get the corresponding StateProjectionGrain and activate it
                foreach (var stateType in stateBaseTypes)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    try
                    {
                        _logger.LogInformation("Activating StateProjectionGrain for {StateType}", stateType.Name);
                        
                        // Get the generic StateProjectionGrain type with this StateBase type
                        var grainType = typeof(IProjectionGrain<>).MakeGenericType(stateType);
                        
                        // Create a deterministic GUID based on the state type name
                        // This ensures the same grain ID is used across all silos
                        var grainId = CreateDeterministicGuid(stateType.FullName);
                        
                        // Get the grain and activate it
                        var grain = _grainFactory.GetGrain(grainType, grainId).Cast(grainType);
                        var method = grainType.GetMethod("ActivateAsync");
                        await (Task)method?.Invoke(grain, null)!;
                        
                        _logger.LogInformation("Successfully activated StateProjectionGrain for {StateType} with ID {GrainId}", 
                            stateType.Name, grainId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error activating StateProjectionGrain for {StateType}", stateType.Name);
                    }
                }
                
                _logger.LogInformation("Completed initializing StateProjectionGrains");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during StateProjectionGrains initialization");
                throw; // Rethrow to fail the silo startup if initialization fails
            }
        }
        
        /// <summary>
        /// Creates a deterministic GUID based on a string input
        /// </summary>
        /// <param name="input">Input string to generate GUID from</param>
        /// <returns>A deterministic GUID based on the input</returns>
        private Guid CreateDeterministicGuid(string? input)
        {
            if (string.IsNullOrEmpty(input))
            {
                throw new ArgumentException("Input cannot be null or empty", nameof(input));
            }
            
            // Use a simple and consistent way to convert the type name to a GUID
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                // Get MD5 hash of the input string
                byte[] hashBytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
                
                // MD5 produces a 16-byte hash which is exactly the size of a GUID
                return new Guid(hashBytes);
            }
        }
        
        private List<Type> GetAllInheritedTypesOf(Type baseType)
        {
            var result = new List<Type>();
            
            // Get all loaded assemblies
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            foreach (var assembly in assemblies)
            {
                try
                {
                    // Skip system assemblies
                    if (assembly.FullName != null && (assembly.FullName.StartsWith("System.") ||
                                                      assembly.FullName.StartsWith("Microsoft.") ||
                                                      assembly.FullName.StartsWith("mscorlib")))
                    {
                        continue;
                    }
                    
                    // Get all types that inherit from the base type
                    var types = assembly.GetTypes()
                        .Where(t => baseType.IsAssignableFrom(t) && 
                               t != baseType && 
                               !t.IsAbstract && 
                               !t.IsInterface)
                        .ToList();
                    
                    result.AddRange(types);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error getting types from assembly {AssemblyName}", assembly.FullName);
                }
            }
            
            return result;
        }
    }
} 