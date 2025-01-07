using Aevatar.AtomicAgent.Dtos;
using Aevatar.AtomicAgent.Models;
using AutoMapper;

namespace Aevatar.AtomicAgent;

public class AevatarAtomicAgentAutoMapperProfile : Profile
{
    public AevatarAtomicAgentAutoMapperProfile()
    {
        CreateMap<CreateAtomicAgentDto, AtomicAgentDto>().ReverseMap();
        CreateMap<AtomicAgentData, CreateAtomicAgentDto>().ReverseMap();
    }
}