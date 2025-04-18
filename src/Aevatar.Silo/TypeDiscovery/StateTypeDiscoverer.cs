using Microsoft.Extensions.Logging;

namespace Aevatar.Silo.TypeDiscovery
{
    /// <summary>
    /// Default implementation of IStateTypeDiscoverer
    /// </summary>
    public class StateTypeDiscoverer : IStateTypeDiscoverer
    {
        private readonly ILogger<StateTypeDiscoverer> _logger;

        public StateTypeDiscoverer(ILogger<StateTypeDiscoverer> logger)
        {
            _logger = logger;
        }

        public List<Type> GetAllInheritedTypesOf(Type baseType)
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