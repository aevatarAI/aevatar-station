using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;

namespace Aevatar.GAgents.SemanticKernel.Common;

internal static class EmbeddingHelper
{
    /// <summary>
    /// Add a simple retry mechanism to embedding generation.
    /// </summary>
    /// <param name="textEmbeddingGenerationService">The embedding generation service.</param>
    /// <param name="text">The text to generate the embedding for.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The generated embedding.</returns>
    public static async Task<ReadOnlyMemory<float>> GenerateEmbeddingsWithRetryAsync(
        ITextEmbeddingGenerationService textEmbeddingGenerationService, string text,
        CancellationToken cancellationToken)
    {
        var tries = 0;

        while (true)
        {
            try
            {
                return await textEmbeddingGenerationService
                    .GenerateEmbeddingAsync(text, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (HttpOperationException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                tries++;

                if (tries < 3)
                {
                    Console.WriteLine($"Failed to generate embedding. Error: {ex}");
                    Console.WriteLine("Retrying embedding generation...");
                    await Task.Delay(10_000, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}