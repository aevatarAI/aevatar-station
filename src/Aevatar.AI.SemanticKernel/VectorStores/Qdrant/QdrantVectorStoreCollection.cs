using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.AI.Brain;
using Aevatar.AI.Model;
using Microsoft.Extensions.VectorData;

namespace Aevatar.AI.VectorStores.Qdrant;

internal class QdrantVectorStoreCollection : IVectorStoreCollection
{
    private readonly IVectorStoreRecordCollection<Guid, TextSnippet<Guid>> _vectorStoreRecordCollection;

    public QdrantVectorStoreCollection(IVectorStoreRecordCollection<Guid, TextSnippet<Guid>> vectorStoreRecordCollection)
    {
        _vectorStoreRecordCollection = vectorStoreRecordCollection;
    }

    public Task InitializeAsync(string collectionName)
    {
        _vectorStoreRecordCollection.CreateCollectionIfNotExistsAsync();
        return Task.CompletedTask;
    }

    public Task UploadRecordAsync(List<FileData> files)
    {
        throw new System.NotImplementedException();
    }
}