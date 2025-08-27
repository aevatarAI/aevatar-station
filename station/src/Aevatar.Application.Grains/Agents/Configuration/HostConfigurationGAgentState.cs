// ABOUTME: This file defines the simplified state and events for Host Business Configuration Agent
// ABOUTME: Stores business configuration as JSON strings for persistent storage and retrieval

using System;
using Aevatar.Core.Abstractions;
using Aevatar.Enum;
using Orleans;

namespace Aevatar.Domain.Shared.Configuration;

[GenerateSerializer]
public class BusinessConfigurationResult
{
    [Id(0)] public string ConfigurationJson { get; set; } = "{}";
    [Id(1)] public DateTime UpdatedAt { get; set; }
}

[GenerateSerializer]
public class HostConfigurationGAgentState : StateBase
{
    [Id(0)] public string HostId { get; set; }
    [Id(1)] public HostTypeEnum HostType { get; set; }
    [Id(2)] public string BusinessConfigurationJson { get; set; } = "{}";
    [Id(3)] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [Id(4)] public string UpdatedBy { get; set; } = "System";

    public void Apply(UpdateBusinessConfigurationGEvent updateEvent)
    {
        HostId = updateEvent.HostId;
        HostType = updateEvent.HostType;
        BusinessConfigurationJson = updateEvent.BusinessConfigurationJson ?? "{}";
        UpdatedAt = updateEvent.UpdatedAt;
        UpdatedBy = updateEvent.UpdatedBy;
    }
}

[GenerateSerializer]
public class HostConfigurationGEvent : StateLogEventBase<HostConfigurationGEvent>
{
    [Id(0)] public override Guid Id { get; set; } = Guid.NewGuid();
}

[GenerateSerializer]
public class UpdateBusinessConfigurationGEvent : HostConfigurationGEvent
{
    [Id(0)] public string HostId { get; set; }
    [Id(1)] public HostTypeEnum HostType { get; set; }
    [Id(2)] public string BusinessConfigurationJson { get; set; } = "{}";
    [Id(3)] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [Id(4)] public string UpdatedBy { get; set; } = "System";
}