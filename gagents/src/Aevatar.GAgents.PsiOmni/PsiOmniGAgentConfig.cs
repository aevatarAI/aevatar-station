using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.GroupChat.Core.Dto;

namespace Aevatar.GAgents.PsiOmni;

[GenerateSerializer]
public class PsiOmniGAgentConfig : GroupMemberConfigDto
{
    [Id(0)] public int Depth { get; set; } = 0;
    [Id(1)] public LLMConfigDto? LLMConfig { get; set; }
}