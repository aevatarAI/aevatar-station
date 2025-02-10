using System.Threading;
using System.Threading.Tasks;
using Aevatar.AI.Brain;

namespace Aevatar.AI.EmbeddedDataLoader;

/// <summary>
/// Interface for loading data into a data store.
/// </summary>
internal interface IEmbeddedDataSaver
{
    /// <summary>
    /// Load a file into the data store.
    /// </summary>
    /// <param name="brainContent">File data in byte array.</param>
    /// <param name="batchSize">Maximum number of parallel threads to generate embeddings and upload records.</param>
    /// <param name="betweenBatchDelayInMs">The number of milliseconds to delay between batches to avoid throttling.</param>
    /// <param name="maxChunkLength">the max length of chunk</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>An async task that completes when the loading is complete.</returns>
    Task StoreAsync(BrainContent brainContent, int batchSize, int maxChunkLength, int betweenBatchDelayInMs, CancellationToken cancellationToken);
}