namespace Aevatar.Silo.TypeDiscovery
{
    /// <summary>
    /// Interface for discovering state types in assemblies
    /// </summary>
    public interface IStateTypeDiscoverer
    {
        /// <summary>
        /// Gets all non-abstract types that inherit from the specified base type
        /// </summary>
        List<Type> GetAllInheritedTypesOf(Type baseType);
    }
} 