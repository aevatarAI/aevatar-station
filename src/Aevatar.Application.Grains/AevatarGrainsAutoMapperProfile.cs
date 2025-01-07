using Aevatar.Domain.Grains.Event;
using AutoMapper;

namespace Aevatar.Application.Grains;

public class AevatarGrainsAutoMapperProfile : Profile
{
    public AevatarGrainsAutoMapperProfile()
    {
        // User AutoMap
        CreateMap<BasicEvent, TelegramEvent>().ReverseMap();
        CreateMap<AgentTaskState, AgentTaskDto>().ReverseMap();
    }
}