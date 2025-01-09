using Aevatar.Core.Abstractions;
using Aevatar.Domain.Grains.Subscription;

namespace Aevatar.Application.Grains.Subscription;

public interface ISubscriptionGAgent :  IStateGAgent<EventSubscriptionState>
{
    Task<EventSubscriptionState> SubscribeAsync(SubscribeEventInputDto input);
    Task UnsubscribeAsync();
}