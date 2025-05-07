using Aevatar.Application.Grains.Subscription;
using Aevatar.Subscription;
using Aevatar.Agents.Creator;
using Aevatar.ApiKey;
using Aevatar.ApiKeys;
using Aevatar.CQRS;
using Aevatar.CQRS.Dto;
using Aevatar.Domain.Grains.Subscription;
using Aevatar.Notification;
using Aevatar.Organizations;
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
        CreateMap<NotificationInfo, NotificationDto>();
        CreateMap<EventSubscriptionState, SubscriptionDto>()
            .ForMember(t => t.SubscriptionId, m => m.MapFrom(f => f.Id))
            .ForMember(t => t.CreatedAt, m => m.MapFrom(f => f.CreateTime));
        CreateMap<OrganizationUnit, OrganizationDto>()
            .ForMember(d => d.CreationTime, m => m.MapFrom(s => DateTimeHelper.ToUnixTimeMilliseconds(s.CreationTime)));
        CreateMap<IdentityUser, OrganizationMemberDto>();
        CreateMap<OrganizationUnit, ProjectDto>()
            .ForMember(d => d.CreationTime, m => m.MapFrom(s => DateTimeHelper.ToUnixTimeMilliseconds(s.CreationTime)))
            .ForMember(d => d.DomainName,
                m => m.MapFrom(s =>
                    s.ExtraProperties.ContainsKey(AevatarConsts.ProjectDomainNameKey)
                        ? s.ExtraProperties[AevatarConsts.ProjectDomainNameKey].ToString()
                        : null));
    }
}