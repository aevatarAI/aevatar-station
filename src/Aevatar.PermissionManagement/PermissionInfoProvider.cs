using System.Reflection;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Metadata;
using Volo.Abp.DependencyInjection;

namespace Aevatar.PermissionManagement;

public class PermissionInfoProvider : IPermissionInfoProvider, ITransientDependency
{
    private readonly GrainTypeResolver _grainTypeResolver;

    public PermissionInfoProvider(IClusterClient clusterClient)
    {
        _grainTypeResolver = clusterClient.ServiceProvider.GetRequiredService<GrainTypeResolver>();
    }

    public List<PermissionInfo> GetAllPermissionInfos()
    {
        var permissionInfos = new List<PermissionInfo>();
        var agentType = typeof(IGAgent);
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var gAgentTypes = new List<Type>();

        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes()
                .Where(t => agentType.IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false });
            gAgentTypes.AddRange(types);
        }

        foreach (var gAgentType in gAgentTypes)
        {
            var methods = gAgentType.GetMethods();
            permissionInfos.AddRange(methods.Select(method => method.GetCustomAttribute<PermissionAttribute>())
                .OfType<PermissionAttribute>().Select(permissionAttribute => new PermissionInfo
                {
                    Type = gAgentType.FullName!,
                    GrainType = _grainTypeResolver.GetGrainType(gAgentType).ToString()!,
                    Name = permissionAttribute.Name,
                    GroupName = permissionAttribute.GroupName ?? string.Empty,
                    DisplayName = permissionAttribute.DisplayName ?? string.Empty
                }));
        }

        return permissionInfos;
    }
}