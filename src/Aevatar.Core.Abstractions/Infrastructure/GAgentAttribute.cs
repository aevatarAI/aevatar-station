using Orleans.Metadata;

// ReSharper disable once CheckNamespace
namespace Aevatar.Core.Abstractions;

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class GAgentAttribute : Attribute, IGrainTypeProviderAttribute
{
    private const char Separator = '.';
    private readonly string? _ns;
    private readonly string? _alias;

    public GAgentAttribute()
    {

    }

    public GAgentAttribute(string alias)
    {
        _alias = alias;
    }

    public GAgentAttribute(string alias, string ns)
    {
        _alias = alias;
        _ns = ns;
    }

    public GrainType GetGrainType(IServiceProvider services, Type type)
    {
        if (_alias == null) // Use ctor with 0 parameters.
        {
            return GrainType.Create($"{type.Namespace}{Separator}{type.Name}");
        }

        if (_ns == null)
        {
            return GrainType.Create($"{type.Namespace}{Separator}{_alias}");
        }

        return GrainType.Create($"{_ns}{Separator}{_alias}");
    }
}