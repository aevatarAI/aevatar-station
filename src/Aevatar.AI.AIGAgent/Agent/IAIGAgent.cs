using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.AI.Brain;
using Aevatar.AI.Dtos;

namespace Aevatar.AI.Agent;

public interface IAIGAgent
{
    Task<bool> InitializeAsync(InitializeDto dto);

    Task<bool> UploadKnowledge(List<BrainContentDto>? knowledgeList);
}