using System;
using Aevatar.AI.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using Qdrant.Client;

namespace Aevatar.AI.VectorStores.Qdrant;

internal class QdrantVectorStore : IVectorStore
{
    private readonly QdrantClient _qdrantClient;

    public QdrantVectorStore(QdrantClient qdrantClient)
    {
        _qdrantClient = qdrantClient;
    }

    public void ConfigureCollection(IKernelBuilder kernelBuilder, string collectionName)
    {
        // Propogate QdrantClient
        kernelBuilder.Services.AddSingleton(_qdrantClient);
        
        kernelBuilder.AddQdrantVectorStoreRecordCollection<Guid, TextSnippet<Guid>>(
            collectionName);
        
        kernelBuilder.Services.AddSingleton<IVectorStoreCollection, QdrantVectorStoreCollection>();
    }

    public void RegisterVectorStoreTextSearch(IKernelBuilder kernelBuilder)
    {
        kernelBuilder.AddVectorStoreTextSearch<TextSnippet<Guid>>(
            new TextSearchStringMapper((result) => (result as TextSnippet<Guid>)!.Text!),
            new TextSearchResultMapper((result) =>
            {
                // Create a mapping from the Vector Store data type to the data type returned by the Text Search.
                // This text search will ultimately be used in a plugin and this TextSearchResult will be returned to the prompt template
                // when the plugin is invoked from the prompt template.
                var castResult = result as TextSnippet<Guid>;
                return new TextSearchResult(value: castResult!.Text!) { Name = castResult.ReferenceDescription, Link = castResult.ReferenceLink };
            }));
    }
}