using Microsoft.SemanticKernel;

namespace Aevatar.AI.VectorStoreBuilder;

internal class QdrantVectorStoreBuilder : IVectorStoreBuilder
{
    private readonly IKernelBuilder _kernelBuilder;

    internal QdrantVectorStoreBuilder(IKernelBuilder kernelBuilder)
    {
        _kernelBuilder = kernelBuilder;
    }

    public void ConfigureCollection<TKey, TValue>(string collectionName)
        where TValue : class
    {
        _kernelBuilder.AddQdrantVectorStoreRecordCollection<TKey, TValue>(
            collectionName);
    }
}