using System.Threading.Tasks;
using Orleans;

namespace Aevatar.AI.Feature.StreamSyncWoker;

public interface IGrainAsyncHandler<T> : IGrainWithGuidKey
{
    Task HandleStreamAsync(T arg);
}