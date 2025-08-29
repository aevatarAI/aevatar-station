using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.GAgents.SemanticKernel.Common;
using Aevatar.GAgents.SemanticKernel.Embeddings;
using Aevatar.GAgents.SemanticKernel.Model;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Embeddings;

namespace Aevatar.GAgents.SemanticKernel.EmbeddedDataLoader;

internal class EmbeddedDataSaverProvider(
    UniqueKeyGenerator<Guid> uniqueKeyGenerator,
    VectorStoreCollection<Guid, TextSnippet<Guid>> vectorStoreCollection,
    ITextEmbeddingGenerationService textEmbeddingGenerationService,
    IChunk chunk) : IEmbeddedDataSaverProvider
{
    public async Task StoreAsync(string name, string content, int batchSize, int maxChunkLength,
        int betweenBatchDelayInMs,
        CancellationToken cancellationToken)
    {
        // Create the collection if it doesn't exist.
        await vectorStoreCollection.EnsureCollectionExistsAsync(cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrEmpty(content))
        {
            return;
        }

        var chunkList = (await chunk.Chunk(content)).ToList();
        var stringRawList = new List<StringRawData>();
        for (var i = 0; i < chunkList.Count(); i++)
        {
            stringRawList.Add(new StringRawData() { Text = chunkList[i], ChunkNum = i });
        }

        var batches = stringRawList.Chunk(batchSize);
        foreach (var batch in batches)
        {
            var recordTasks = batch.Select(async content => new TextSnippet<Guid>
            {
                Key = uniqueKeyGenerator.GenerateKey($"{name}-{content.ChunkNum}"),
                Text = content.Text,
                ReferenceDescription = $"{name}#Chunk={content.ChunkNum}",
                ReferenceLink = $"{name}#Chunk={content.ChunkNum}",
                TextEmbedding = await EmbeddingHelper.GenerateEmbeddingsWithRetryAsync(textEmbeddingGenerationService,
                    content.Text!,
                    cancellationToken: cancellationToken).ConfigureAwait(false)
            });

            var records = await Task.WhenAll(recordTasks).ConfigureAwait(false);
            await vectorStoreCollection.UpsertAsync(records, cancellationToken: cancellationToken).ConfigureAwait(false);
            await Task.Delay(betweenBatchDelayInMs, cancellationToken).ConfigureAwait(false);
        }
    }


    private class StringRawData
    {
        public string? Text { get; init; }
        public int ChunkNum { get; init; }
    }
}