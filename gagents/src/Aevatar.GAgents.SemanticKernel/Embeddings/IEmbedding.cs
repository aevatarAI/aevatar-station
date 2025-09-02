using Microsoft.SemanticKernel;

namespace Aevatar.GAgents.SemanticKernel.Embeddings;

public interface IEmbedding
{
    void Configure(IKernelBuilder kernelBuilder);
}