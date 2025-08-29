// ABOUTME: This file defines the configuration DTO for InputGAgent
// ABOUTME: Contains the input string that will be returned as ChatResponse

using System.ComponentModel.DataAnnotations;
using Aevatar.Core.Abstractions;
using GroupChat.GAgent.Dto;

namespace Aevatar.GAgents.InputGAgent.Dto;

[GenerateSerializer]
public class InputConfigDto : MemberConfigDto
{
    [Id(1)] 
    [Required(ErrorMessage = "Input is required")]
    [StringLength(5000, MinimumLength = 1, ErrorMessage = "Input must be between 1 and 5000 characters")]
    public string Input { get; set; } = string.Empty;
}