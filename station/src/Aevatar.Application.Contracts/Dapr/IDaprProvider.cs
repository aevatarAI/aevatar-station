using System.Threading.Tasks;

namespace Aevatar.Dapr;

public interface IDaprProvider
{
    Task PublishEventAsync<T>(string pubsubName, string topicName, T message);

}