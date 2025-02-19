using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aevatar.AI.Embeddings;

internal interface IChunk
{
    Task<IEnumerable<string>> Chunk(string input);
}