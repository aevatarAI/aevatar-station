using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Aevatar.GAgents.SemanticKernel.Embeddings;

public class ChunkAsSentence : IChunk
{
    public Task<IEnumerable<string>> Chunk(string input)
    {
        string[] sentences = Regex.Split(input, @"(?<=[.!?。？！])\s+");

        return Task.FromResult(sentences.ToList().AsEnumerable());
    }
}