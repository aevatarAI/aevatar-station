using Aevatar.Application.Grains.Subscription;
using Aevatar.Subscription;
using Aevatar.Agents.Atomic.Models;
using Aevatar.Agents.Combination.Models;
using Aevatar.AtomicAgent;
using Aevatar.CombinationAgent;
using Aevatar.Domain.Grains.Subscription;
using AutoMapper;
using Volo.Abp.AutoMapper;

namespace Aevatar;

public class AevatarApplicationAutoMapperProfile : Profile
{
    public AevatarApplicationAutoMapperProfile()
    {
        CreateMap<EventSubscriptionState, SubscriptionDto>().ReverseMap();
        CreateMap<CreateAtomicAgentDto, AtomicAgentDto>().ReverseMap();
        CreateMap<AtomicAgentData, CreateAtomicAgentDto>().ReverseMap();
        
        CreateMap<CombineAgentDto, CombinationAgentData>()
            .ForMember(t => t.AgentComponent, m => m.Ignore())
            .ReverseMap();
        CreateMap<CreateSubscriptionDto, SubscribeEventInputDto>().ReverseMap();
        CreateMap<EventSubscriptionState, SubscriptionDto>()
            .ForMember(t => t.SubscriptionId, m => m.MapFrom(f => f.Id))
            .ForMember(t => t.CreatedAt, m => m.MapFrom(f => f.CreateTime));
    }
}
