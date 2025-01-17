using Aevatar.CQRS.Dto;
using AutoMapper;

namespace Aevatar.CQRS;

public class AISmartCQRSAutoMapperProfile : Profile
{
    public AISmartCQRSAutoMapperProfile()
    {
        CreateMap<GetDataQuery, ChatLogQueryInputDto>();
        CreateMap<AIChatLogIndex, ChatLogPageResultDto>();
        CreateMap<AIChatLogIndex, AIChatLogIndexDto>();
    }
}
