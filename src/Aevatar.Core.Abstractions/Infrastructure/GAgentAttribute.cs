// ReSharper disable once CheckNamespace

using Orleans.Metadata;

namespace Aevatar.Core.Abstractions;

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public class GAgentAttribute : Attribute, IGrainTypeProviderAttribute
{
    private readonly string _ns;
    private readonly string _alias;

    public GAgentAttribute(string alias, string ns = "Aevatar")
    {
        _alias = alias;
        _ns = ns;
    }

    public GrainType GetGrainType(IServiceProvider services, Type type)
    {
        return GrainType.Create($"{_ns}_{_alias}");
    }
}