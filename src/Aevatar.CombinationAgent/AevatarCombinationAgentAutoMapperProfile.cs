using Aevatar.CombinationAgent.Dtos;
using Aevatar.CombinationAgent.Models;
using AutoMapper;

namespace Aevatar.CombinationAgent;

public class AevatarCombinationAgentAutoMapperProfile : Profile
{
    public AevatarCombinationAgentAutoMapperProfile()
    {
        CreateMap<CombineAgentDto, CombinationAgentDto>().ReverseMap();
        CreateMap<CombineAgentDto, CombinationAgentData>().ReverseMap();
    }
}