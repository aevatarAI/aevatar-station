// ABOUTME: This file implements the configuration DTO for group member agents
// ABOUTME: Defines the configuration structure for initializing member agents

using System.ComponentModel.DataAnnotations;
using Aevatar.Core.Abstractions;

namespace GroupChat.GAgent.Dto;

[GenerateSerializer]
public class MemberConfigDto:ConfigurationBase
{
    [Id(0)] 
    [Required(ErrorMessage = "Member Name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Member Name must be between 1 and 100 characters")]
    public string MemberName { get; set; }
}