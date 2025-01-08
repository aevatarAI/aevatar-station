using Aevatar.Application.Grains.Subscription;
using Aevatar.Subscription;
using AutoMapper;

namespace Aevatar;

public class AevatarApplicationAutoMapperProfile : Profile
{
    public AevatarApplicationAutoMapperProfile()
    {
        CreateMap<EventSubscriptionState, SubscriptionDto>().ReverseMap();
    }
}
