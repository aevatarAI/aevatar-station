using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.AI.Brain;
using Aevatar.AI.Common;
using Aevatar.AI.Embeddings;
using Aevatar.AI.Model;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;

namespace Aevatar.AI.EmbeddedDataLoader;

internal class EmbeddedStringDataLoader(
    UniqueKeyGenerator<Guid> uniqueKeyGenerator,
    IVectorStoreRecordCollection<Guid, TextSnippet<Guid>> vectorStoreCollection,
    ITextEmbeddingGenerationService textEmbeddingGenerationService,
    IChunk chunk,
    IChatCompletionService chatCompletionService) : IEmbeddedDataLoader
{
    public async Task Load(BrainContent brainContent, int batchSize, int maxChunkLength, int betweenBatchDelayInMs,
        CancellationToken cancellationToken)
    {
        // Create the collection if it doesn't exist.
        await vectorStoreCollection.CreateCollectionIfNotExistsAsync(cancellationToken).ConfigureAwait(false);

        if (brainContent.Content == null)
        {
            return;
        }

        var originalStr = Encoding.UTF8.GetString(brainContent.Content);

        var chunkList = (await chunk.Chunk(originalStr)).ToList();
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
                Key = uniqueKeyGenerator.GenerateKey($"{brainContent.Name}-{content.ChunkNum}"),
                Text = content.Text,
                ReferenceDescription = $"{brainContent.Name}#Chunk={content.ChunkNum}",
                ReferenceLink = $"{brainContent.Name}#Chunk={content.ChunkNum}",
                TextEmbedding = await EmbeddingHelper.GenerateEmbeddingsWithRetryAsync(textEmbeddingGenerationService,
                    content.Text!,
                    cancellationToken: cancellationToken).ConfigureAwait(false)
            });

            var records = await Task.WhenAll(recordTasks).ConfigureAwait(false);
            var upsertKeys =
                vectorStoreCollection.UpsertBatchAsync(records, cancellationToken: cancellationToken);
            await foreach (var key in upsertKeys.ConfigureAwait(false))
            {
                Console.WriteLine($"Upserted record '{key}' into VectorDB");
            }

            await Task.Delay(betweenBatchDelayInMs, cancellationToken).ConfigureAwait(false);
        }
    }


    private class StringRawData
    {
        public string? Text { get; init; }
        public int ChunkNum { get; init; }
    }
}