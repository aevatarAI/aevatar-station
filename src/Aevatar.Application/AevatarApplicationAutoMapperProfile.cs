using Aevatar.Application.Grains.Subscription;
using Aevatar.Subscription;
using Aevatar.Agents.Atomic.Models;
using Aevatar.Agents.Creator;
using Aevatar.AtomicAgent;
using Aevatar.Domain.Grains.Subscription;
using AutoMapper;

namespace Aevatar;

public class AevatarApplicationAutoMapperProfile : Profile
{
    public AevatarApplicationAutoMapperProfile()
    {
        CreateMap<EventSubscriptionState, SubscriptionDto>().ReverseMap();
        CreateMap<CreateAtomicAgentDto, AtomicAgentDto>().ReverseMap();
        CreateMap<AtomicAgentData, CreateAtomicAgentDto>().ReverseMap();
        
        CreateMap<CreateSubscriptionDto, SubscribeEventInputDto>().ReverseMap();
        CreateMap<EventSubscriptionState, SubscriptionDto>()
            .ForMember(t => t.SubscriptionId, m => m.MapFrom(f => f.Id))
            .ForMember(t => t.CreatedAt, m => m.MapFrom(f => f.CreateTime));

        CreateMap<EventDescription, EventDescriptionDto>()
            .ForMember(t => t.EventType, m => m.MapFrom(f => f.EventType.Name));
    }
}
