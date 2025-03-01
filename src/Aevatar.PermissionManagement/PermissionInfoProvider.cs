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
            var grainType = _grainTypeResolver.GetGrainType(gAgentType).ToString()!;

            var classAttributes = gAgentType.GetCustomAttributes<PermissionAttribute>(inherit: true);
            permissionInfos.AddRange(
                classAttributes.Select(attr => new PermissionInfo
                {
                    Type = gAgentType.FullName!,
                    GrainType = grainType,
                    Name = attr.Name,
                    GroupName = attr.GroupName ?? string.Empty,
                    DisplayName = attr.DisplayName ?? string.Empty
                })
            );

            var methods =
                gAgentType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            permissionInfos.AddRange(
                methods
                    .SelectMany(method => method.GetCustomAttributes<PermissionAttribute>(inherit: true))
                    .Select(attr => new PermissionInfo
                    {
                        Type = gAgentType.FullName!,
                        GrainType = grainType,
                        Name = attr.Name,
                        GroupName = attr.GroupName ?? string.Empty,
                        DisplayName = attr.DisplayName ?? string.Empty
                    })
            );
        }

        return permissionInfos;
    }
}