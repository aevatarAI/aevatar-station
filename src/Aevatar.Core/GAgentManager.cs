using Aevatar.Core.Abstractions;

namespace Aevatar.Core;

public interface IGAgentManager
{
    List<Type> GetAvailableGAgentTypes();
}

public class GAgentManager : IGAgentManager
{
    public List<Type> GetAvailableGAgentTypes()
    {
        var gAgentType = typeof(IGAgent);
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var gAgentTypes = new List<Type>();

        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes()
                .Where(t => gAgentType.IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false });
            gAgentTypes.AddRange(types);
        }

        return gAgentTypes;
    }
}