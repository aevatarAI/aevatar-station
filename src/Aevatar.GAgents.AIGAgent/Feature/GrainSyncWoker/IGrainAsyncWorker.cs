using System.Threading;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;
using Orleans.SyncWork;

namespace Aevatar.AI.Feature.StreamSyncWoker;

public interface IGrainAsyncWorker<TRequest, TResponse> : ISyncWorker<TRequest, TResponse>, IGrainWithGuidKey
{
    Task SetLongRunTaskAsync(GrainId grainId);
}