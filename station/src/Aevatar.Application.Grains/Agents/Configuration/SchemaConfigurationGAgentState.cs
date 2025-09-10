using System;
using Aevatar.Core.Abstractions;
using Aevatar.Enum;
using Orleans;

namespace Aevatar.Application.Grains.Agents.Configuration;

/// <summary>
/// State for Schema Configuration Agent
/// Simple state to track when the schema context was last retrieved
/// </summary>
[GenerateSerializer]
public class SchemaConfigurationGAgentState : StateBase
{
    /// <summary>
    /// Last time the schema context was retrieved
    /// </summary>
    [Id(0)] public DateTime LastRetrievedAt { get; set; } = DateTime.MinValue;
    
    /// <summary>
    /// Number of times the schema context has been retrieved
    /// </summary>
    [Id(1)] public int RetrievalCount { get; set; } = 0;
    
    /// <summary>
    /// Agent ID associated with this configuration
    /// </summary>
    [Id(2)] public string AgentId { get; set; } = string.Empty;
    
    /// <summary>
    /// Host type for this configuration
    /// </summary>
    [Id(3)] public HostTypeEnum HostType { get; set; } = HostTypeEnum.Client;

    public void Apply(SchemaContextRetrievedGEvent retrievalEvent)
    {
        LastRetrievedAt = retrievalEvent.Ctime;
        RetrievalCount++;
        AgentId = retrievalEvent.AgentId;
        HostType = retrievalEvent.HostType;
    }
}
