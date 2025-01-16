using Aevatar.CQRS.Dto;
using AutoMapper;

namespace Aevatar.CQRS;

public class AISmartCQRSAutoMapperProfile : Profile
{
    public AISmartCQRSAutoMapperProfile()
    {
        CreateMap<GetLogQuery, ChatLogQueryInputDto>();
        CreateMap<SaveLogCommand, AIChatLogIndex>();
        CreateMap<AIChatLogIndex, ChatLogPageResultDto>();
        CreateMap<AIChatLogIndex, AIChatLogIndexDto>();
    }
}
