using System.Threading;
using System.Threading.Tasks;

namespace Aevatar.GAgents.SemanticKernel.EmbeddedDataLoader;

/// <summary>
/// Interface for loading data into a data store.
/// </summary>
internal interface IEmbeddedDataSaverProvider
{
    /// <summary>
    /// store the data.
    /// </summary>
    /// <param name="name">the brain name </param>
    /// <param name="content">th brain content.</param>
    /// <param name="batchSize">Maximum number of parallel threads to generate embeddings and upload records.</param>
    /// <param name="betweenBatchDelayInMs">The number of milliseconds to delay between batches to avoid throttling.</param>
    /// <param name="maxChunkLength">the max length of chunk</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>An async task that completes when the loading is complete.</returns>
    Task StoreAsync(string name,string content, int batchSize, int maxChunkLength, int betweenBatchDelayInMs, CancellationToken cancellationToken);
}