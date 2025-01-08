using Microsoft.SemanticKernel;

namespace Aevatar.AI.Embeddings;

public interface IEmbedding
{
    void Configure(IKernelBuilder kernelBuilder);
}