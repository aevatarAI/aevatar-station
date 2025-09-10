using System;
using Aevatar.Core.Abstractions;
using Aevatar.Enum;
using Orleans;

namespace Aevatar.Application.Grains.Agents.Configuration;

/// <summary>
/// Base event for Schema Configuration Agent
/// </summary>
[GenerateSerializer]
public abstract class SchemaConfigurationGEvent : StateLogEventBase<SchemaConfigurationGEvent>
{
    [Id(0)] public override Guid Id { get; set; } = Guid.NewGuid();
    [Id(1)] public string AgentId { get; set; } = string.Empty;
    [Id(2)] public HostTypeEnum HostType { get; set; } = HostTypeEnum.Client;
}

/// <summary>
/// Event raised when schema context is retrieved
/// </summary>
[GenerateSerializer]
public class SchemaContextRetrievedGEvent : SchemaConfigurationGEvent
{
    /// <summary>
    /// Number of AI model configurations retrieved
    /// </summary>
    [Id(0)] public int ConfigurationCount { get; set; }
    
    /// <summary>
    /// Source of the configuration (Silo, Default, etc.)
    /// </summary>
    [Id(1)] public string ConfigurationSource { get; set; } = "Silo";
}
