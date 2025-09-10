namespace Aevatar.TestBase;

public static class InterfaceImplementationsFinder
{
    public static IEnumerable<TInterface> GetImplementations<TInterface>()
    {
        var interfaceType = typeof(TInterface);
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => interfaceType.IsAssignableFrom(type) && type is { IsClass: true, IsAbstract: false })
            .Select(type => (TInterface)Activator.CreateInstance(type)!);
    }
}