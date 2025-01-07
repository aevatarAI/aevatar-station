using System;
using Aevatar.AI.Model;
using Aevatar.AI.Options;
using Aevatar.AI.VectorStoreBuilder;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;

namespace Aevatar.AI.KernelBuilderFactory;

public class KernelBuilderFactory : IKernelBuilderFactory
{
    private readonly Func<IKernelBuilder, IVectorStoreBuilder> _vectorStoreBuilderFactory;
    
    public KernelBuilderFactory(Func<IKernelBuilder, IVectorStoreBuilder> vectorStoreBuilderFactory)
    {
        _vectorStoreBuilderFactory = vectorStoreBuilderFactory;
    }

    public IKernelBuilder GetKernelBuilder(Guid guid)
    {
        var kernelBuilder = Kernel.CreateBuilder();

        _vectorStoreBuilderFactory(kernelBuilder)
            .ConfigureCollection<Guid, TextSnippet<Guid>>(guid.ToString());
        
        RegisterVectorStore<Guid>(kernelBuilder);

        return kernelBuilder;
    }
    
    private void RegisterVectorStore<TKey>(IKernelBuilder kernelBuilder)
        where TKey : notnull
    {
        // Add a text search implementation that uses the registered vector store record collection for search.
        kernelBuilder.AddVectorStoreTextSearch<TextSnippet<TKey>>(
            new TextSearchStringMapper((result) => (result as TextSnippet<TKey>)!.Text!),
            new TextSearchResultMapper((result) =>
            {
                // Create a mapping from the Vector Store data type to the data type returned by the Text Search.
                // This text search will ultimately be used in a plugin and this TextSearchResult will be returned to the prompt template
                // when the plugin is invoked from the prompt template.
                var castResult = result as TextSnippet<TKey>;
                return new TextSearchResult(value: castResult!.Text!) { Name = castResult.ReferenceDescription, Link = castResult.ReferenceLink };
            }));
    }
}