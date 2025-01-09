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
        /* You can configure your AutoMapper mapping configuration here.
         * Alternatively, you can split your mapping configurations
         * into multiple profile classes for a better organization. */
        
        //Example related, can be removed
        CreateMap<CreateAtomicAgentDto, AtomicAgentDto>().ReverseMap();
        CreateMap<AtomicAgentData, CreateAtomicAgentDto>().ReverseMap();
        
        CreateMap<CombineAgentDto, CombinationAgentData>().ReverseMap();
    }
}
