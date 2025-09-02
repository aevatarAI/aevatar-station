// ABOUTME: This file defines the configuration DTO for InputGAgent
// ABOUTME: Contains the input string that will be returned as ChatResponse

using Aevatar.Core.Abstractions;
using GroupChat.GAgent.Dto;

namespace Aevatar.GAgents.InputGAgent.Dto;

[GenerateSerializer]
public class InputConfigDto : MemberConfigDto
{
    [Id(1)] public string Input { get; set; } = string.Empty;
}