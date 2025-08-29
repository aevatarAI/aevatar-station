using System;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using System.Text;
using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.SemanticKernel.Common;
using Aevatar.GAgents.SemanticKernel.EmbeddedDataLoader;
using Aevatar.GAgents.SemanticKernel.Embeddings;
using Aevatar.GAgents.SemanticKernel.ExtractContent;
using Aevatar.GAgents.SemanticKernel.Model;
using Qdrant.Client;

namespace Aevatar.GAgents.SemanticKernel.VectorStores.Qdrant;

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
            collectionName: collectionName);
        
        kernelBuilder.Services.AddSingleton<IVectorStoreCollection, QdrantVectorStoreCollection>();
        
        //add the embedded data loaders here
        kernelBuilder.Services.AddTransient<IEmbeddedDataSaverProvider, EmbeddedDataSaverProvider>();

        kernelBuilder.Services.AddTransient<IChunk, ChunkAsSentence>();
        kernelBuilder.Services.AddKeyedSingleton<IExtractContent, ExtractPdf>(BrainContentType.Pdf.ToString());
        kernelBuilder.Services.AddKeyedSingleton<IExtractContent, ExtractString>(BrainContentType.String.ToString());
        
        kernelBuilder.Services.AddSingleton<UniqueKeyGenerator<Guid>>(sp =>
        {
            return new UniqueKeyGenerator<Guid>((input) =>
            {
                using var md5 = MD5.Create();
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));

                return new Guid(hash);
            });
        });

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