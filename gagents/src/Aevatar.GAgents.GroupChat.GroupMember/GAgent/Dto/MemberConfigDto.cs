// ABOUTME: This file implements the configuration DTO for group member agents
// ABOUTME: Defines the configuration structure for initializing member agents

using Aevatar.Core.Abstractions;

namespace GroupChat.GAgent.Dto;

[GenerateSerializer]
public class MemberConfigDto:ConfigurationBase
{
    [Id(0)] public string MemberName { get; set; }
}