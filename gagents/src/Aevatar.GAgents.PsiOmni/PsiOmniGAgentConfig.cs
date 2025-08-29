using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.GroupChat.Core.Dto;

namespace Aevatar.GAgents.PsiOmni;

[GenerateSerializer]
public class PsiOmniGAgentConfig : GroupMemberConfigDto
{
    [Id(0)] public string ParentId { get; set; } = string.Empty;
    [Id(1)] public string Name { get; set; } = string.Empty;
    [Id(2)] public int Depth { get; set; } = 0;
    [Id(3)] public LLMConfigDto? LLMConfig { get; set; }
    [Id(4)] public string Description { get; set; } = string.Empty;
    [Id(5)] public string Examples { get; set; } = string.Empty;
}