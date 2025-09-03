using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aevatar.GAgents.SemanticKernel.Embeddings;

internal interface IChunk
{
    Task<IEnumerable<string>> Chunk(string input);
}