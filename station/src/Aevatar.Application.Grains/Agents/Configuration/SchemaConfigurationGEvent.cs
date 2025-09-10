using System;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.Application.Grains.Agents.Configuration;

/// <summary>
/// Base event for Schema Configuration Agent - minimal events for configuration reading
/// </summary>
[GenerateSerializer]
public abstract class SchemaConfigurationGEvent : StateLogEventBase<SchemaConfigurationGEvent>
{
    [Id(0)] public override Guid Id { get; set; } = Guid.NewGuid();
}

/// <summary>
/// Event raised when schema context is retrieved - simplified for configuration reading
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
