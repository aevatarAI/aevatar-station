using Orleans.Metadata;

// ReSharper disable once CheckNamespace
namespace Aevatar.Core.Abstractions;

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class GAgentAttribute : Attribute, IGrainTypeProviderAttribute
{
    private readonly string? _ns;
    private readonly string? _alias;

    public GAgentAttribute()
    {

    }

    public GAgentAttribute(string alias)
    {
        _alias = alias;
        _ns = "aevatar";
    }

    public GAgentAttribute(string alias, string ns = "aevatar")
    {
        _alias = alias.ToLower();
        _ns = ns.ToLower();
    }

    public GrainType GetGrainType(IServiceProvider services, Type type)
    {
        if (_alias == null) // Use ctor with 0 parameters.
        {
            var className = type.Name.ToLower();
            var ns = type.Namespace!.ToLower().Replace('.', '/');
            return GrainType.Create($"{ns}/{className}");
        }

        return GrainType.Create($"{_ns}/{_alias}");
    }
}