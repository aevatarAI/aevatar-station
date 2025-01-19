using Orleans.Metadata;

// ReSharper disable once CheckNamespace
namespace Aevatar.Core.Abstractions;

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class GAgentAttribute : Attribute, IGrainTypeProviderAttribute
{
    private readonly string _ns;
    private readonly string _alias;

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
        return GrainType.Create($"{_ns}/{_alias}");
    }
}