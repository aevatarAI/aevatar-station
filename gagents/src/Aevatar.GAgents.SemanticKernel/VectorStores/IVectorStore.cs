using Microsoft.SemanticKernel;

namespace Aevatar.GAgents.SemanticKernel.VectorStores;

public interface IVectorStore
{
    void ConfigureCollection(IKernelBuilder kernelBuilder, string collectionName);

    void RegisterVectorStoreTextSearch(IKernelBuilder kernelBuilder);
}