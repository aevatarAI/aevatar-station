using System.Threading.Tasks;
using Aevatar.Core.Abstractions;

namespace Aevatar.Sender;

public interface IPublishingGAgent : IGAgent
{
    Task PublishEventAsync<T>(T @event) where T : EventBase;
}