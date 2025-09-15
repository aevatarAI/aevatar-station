using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.GroupChat.Core.Dto;

namespace Aevatar.GAgents.PsiOmni;

[GenerateSerializer]
public class PsiOmniGAgentConfig : GroupMemberConfigDto
{
    [Id(0)] public string ParentId { get; set; } = string.Empty;
    [Id(1)] public string Name { get; set; } = string.Empty;
    [Id(2)] public int Depth { get; set; } = 0;

    // ReSharper disable once InconsistentNaming
    [Id(3)] public string SystemLLM { get; set; } = "OpenAI";
#if ENABLE_SELF_LLM_CONFIG
    [Id(4)] public SelfLLMConfig SelfLlmConfig { get; set; } = new();
#endif
    [Id(5)] public string Description { get; set; } = string.Empty;
    [Id(6)] public string Examples { get; set; } = string.Empty;
}