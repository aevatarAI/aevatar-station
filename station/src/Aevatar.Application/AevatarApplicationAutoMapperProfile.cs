using Aevatar.Application.Contracts.MCPGateway;
using Aevatar.Application.Grains.Subscription;
using Aevatar.Subscription;
using Aevatar.ApiRequests;
using Aevatar.Domain.Grains.Subscription;
using Aevatar.Notification;
using Aevatar.Organizations;
using Aevatar.Plugins;
using Aevatar.Projects;
using AutoMapper;
using Volo.Abp.Identity;

namespace Aevatar;

public class AevatarApplicationAutoMapperProfile : Profile
{
    public AevatarApplicationAutoMapperProfile()
    {
        CreateMap<EventSubscriptionState, SubscriptionDto>().ReverseMap();

        CreateMap<CreateSubscriptionDto, SubscribeEventInputDto>().ReverseMap();
        CreateMap<NotificationInfo, NotificationDto>()
            .ForMember(d => d.CreationTime, m => m.MapFrom(s => DateTimeHelper.ToUnixTimeMilliseconds(s.CreationTime)));
        CreateMap<EventSubscriptionState, SubscriptionDto>()
            .ForMember(t => t.SubscriptionId, m => m.MapFrom(f => f.Id))
            .ForMember(t => t.CreatedAt, m => m.MapFrom(f => f.CreateTime));
        CreateMap<OrganizationUnit, OrganizationDto>()
            .ForMember(d => d.CreationTime, m => m.MapFrom(s => DateTimeHelper.ToUnixTimeMilliseconds(s.CreationTime)));
        CreateMap<IdentityUser, OrganizationMemberDto>();
        CreateMap<OrganizationUnit, ProjectDto>()
            .ForMember(d => d.CreationTime, m => m.MapFrom(s => DateTimeHelper.ToUnixTimeMilliseconds(s.CreationTime)));
        
        CreateMap<ApiRequestSnapshot, ApiRequestDto>()
            .ForMember(d => d.Time, m => m.MapFrom(s => DateTimeHelper.ToUnixTimeMilliseconds(s.Time)));

        CreateMap<Plugin, PluginDto>()
            .ForMember(d => d.CreationTime, m => m.MapFrom(s => DateTimeHelper.ToUnixTimeMilliseconds(s.CreationTime)))
            .ForMember(d => d.LastModificationTime,
                m => m.MapFrom(s =>
                    s.LastModificationTime.HasValue
                        ? DateTimeHelper.ToUnixTimeMilliseconds(s.LastModificationTime.Value)
                        : 0));
        
        CreateMap<ProjectCorsOrigin, ProjectCorsOriginDto>()
            .ForMember(d => d.CreationTime, m => m.MapFrom(s => DateTimeHelper.ToUnixTimeMilliseconds(s.CreationTime)));
        
        // MCP Gateway mappings
        ConfigureMCPGatewayMappings();
    }
    
    /// <summary>
    /// Configure MCP Gateway AutoMapper mappings
    /// </summary>
    private void ConfigureMCPGatewayMappings()
    {
        // Note: Since we're using DTOs that directly match the gateway API responses,
        // most mappings are straightforward or not needed.
        // Add specific mappings here if needed for data transformation.
        
        CreateMap<CreateMCPAdapterDto, MCPAdapterDto>()
            .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Name))
            .ForMember(d => d.Status, opt => opt.Ignore())
            .ForMember(d => d.ActiveConnections, opt => opt.Ignore())
            .ForMember(d => d.LastHealthCheck, opt => opt.Ignore())
            .ForMember(d => d.IsHealthy, opt => opt.Ignore())
            .ForMember(d => d.ResourceUsage, opt => opt.Ignore())
            .ForMember(d => d.CreationTime, opt => opt.Ignore())
            .ForMember(d => d.CreatorId, opt => opt.Ignore())
            .ForMember(d => d.LastModificationTime, opt => opt.Ignore())
            .ForMember(d => d.LastModifierId, opt => opt.Ignore());

        CreateMap<UpdateMCPAdapterDto, MCPAdapterDto>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.Name, opt => opt.Ignore())
            .ForMember(d => d.Status, opt => opt.Ignore())
            .ForMember(d => d.ActiveConnections, opt => opt.Ignore())
            .ForMember(d => d.LastHealthCheck, opt => opt.Ignore())
            .ForMember(d => d.IsHealthy, opt => opt.Ignore())
            .ForMember(d => d.ResourceUsage, opt => opt.Ignore())
            .ForMember(d => d.CreationTime, opt => opt.Ignore())
            .ForMember(d => d.CreatorId, opt => opt.Ignore())
            .ForMember(d => d.LastModificationTime, opt => opt.Ignore())
            .ForMember(d => d.LastModifierId, opt => opt.Ignore())
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}