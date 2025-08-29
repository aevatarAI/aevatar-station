using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.SemanticKernel.Model;
using Microsoft.Extensions.VectorData;

namespace Aevatar.GAgents.SemanticKernel.VectorStores.Qdrant;

internal class QdrantVectorStoreCollection : IVectorStoreCollection
{
    private readonly VectorStoreCollection<Guid, TextSnippet<Guid>> _vectorStoreCollection;

    public QdrantVectorStoreCollection(VectorStoreCollection<Guid, TextSnippet<Guid>> vectorStoreCollection)
    {
        _vectorStoreCollection = vectorStoreCollection;
    }

    public async Task InitializeAsync(string collectionName)
    {
        await _vectorStoreCollection.EnsureCollectionExistsAsync();
    }

    public Task UploadRecordAsync(List<BrainContent> files)
    {
        throw new System.NotImplementedException();
    }
}