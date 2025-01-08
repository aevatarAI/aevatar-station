using Microsoft.SemanticKernel;

namespace Aevatar.AI.VectorStores;

public interface IVectorStore
{
    void ConfigureCollection(IKernelBuilder kernelBuilder, string collectionName);

    void RegisterVectorStoreTextSearch(IKernelBuilder kernelBuilder);
}