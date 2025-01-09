using Aevatar.Application.Grains.Subscription;
using Aevatar.Subscription;
using Aevatar.Agents.Atomic.Models;
using Aevatar.Agents.Combination.Models;
using Aevatar.AtomicAgent;
using Aevatar.CombinationAgent;
using AutoMapper;

namespace Aevatar;

public class AevatarApplicationAutoMapperProfile : Profile
{
    public AevatarApplicationAutoMapperProfile()
    {
        CreateMap<EventSubscriptionState, SubscriptionDto>().ReverseMap();
        CreateMap<CreateAtomicAgentDto, AtomicAgentDto>().ReverseMap();
        CreateMap<AtomicAgentData, CreateAtomicAgentDto>().ReverseMap();
        
        CreateMap<CombineAgentDto, CombinationAgentData>().ReverseMap();
    }
}
