// ABOUTME: This file implements the simplified Host Business Configuration Agent
// ABOUTME: Provides persistent storage and retrieval for business configuration JSON

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Domain.Shared.Configuration;
using Aevatar.Enum;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Aevatar.Application.Grains.Agents.Configuration;

[Description("Simplified Host Business Configuration Storage")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class HostConfigurationGAgent : GAgentBase<HostConfigurationGAgentState, HostConfigurationGEvent>, IHostConfigurationGAgent
{
    private readonly ILogger<HostConfigurationGAgent> _logger;

    public HostConfigurationGAgent(ILogger<HostConfigurationGAgent> logger)
    {
        _logger = logger;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "Simplified agent for persistent storage and retrieval of business configuration JSON.");
    }

    public async Task SetBusinessConfigurationJsonAsync(string configurationJson, string updatedBy = "System")
    {
        if (string.IsNullOrWhiteSpace(configurationJson))
        {
            configurationJson = "{}";
        }

        var updateEvent = new UpdateBusinessConfigurationGEvent
        {
            Ctime = DateTime.UtcNow,
            HostId = ExtractHostIdFromGrainKey(),
            HostType = ExtractHostTypeFromGrainKey(), 
            BusinessConfigurationJson = configurationJson,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = updatedBy
        };

        RaiseEvent(updateEvent);
        await ConfirmEvents();

        _logger.LogInformation("Business configuration JSON stored for {HostId}:{HostType} by {UpdatedBy}", 
            State.HostId, State.HostType, updatedBy);
    }

    public async Task<BusinessConfigurationResult> GetBusinessConfigurationJsonAsync()
    {
        return new BusinessConfigurationResult
        {
            ConfigurationJson = State.BusinessConfigurationJson ?? "{}",
            UpdatedAt = State.UpdatedAt
        };
    }

    public async Task ClearBusinessConfigurationAsync(string updatedBy = "System")
    {
        var updateEvent = new UpdateBusinessConfigurationGEvent
        {
            Ctime = DateTime.UtcNow,
            HostId = ExtractHostIdFromGrainKey(),
            HostType = ExtractHostTypeFromGrainKey(),
            BusinessConfigurationJson = "{}",
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = updatedBy
        };

        RaiseEvent(updateEvent);
        await ConfirmEvents();

        _logger.LogInformation("Business configuration cleared for {HostId}:{HostType} by {UpdatedBy}", 
            State.HostId, State.HostType, updatedBy);
    }

    private string ExtractHostIdFromGrainKey()
    {
        var grainKey = this.GetPrimaryKeyString();
        var parts = grainKey.Split(':');
        return parts.Length > 0 ? parts[0] : "unknown";
    }

    private HostTypeEnum ExtractHostTypeFromGrainKey()
    {
        var grainKey = this.GetPrimaryKeyString();
        var parts = grainKey.Split(':');
        if (parts.Length > 1 && System.Enum.TryParse<HostTypeEnum>(parts[1], true, out var hostType))
        {
            return hostType;
        }
        return HostTypeEnum.Client; // Default fallback
    }
}

public interface IHostConfigurationGAgent : IStateGAgent<HostConfigurationGAgentState>
{
    /// <summary>
    /// Store business configuration JSON for this host-type combination
    /// </summary>
    /// <param name="configurationJson">Business configuration as JSON string</param>
    /// <param name="updatedBy">User who performed the update</param>
    Task SetBusinessConfigurationJsonAsync(string configurationJson, string updatedBy = "System");

    /// <summary>
    /// Retrieve business configuration JSON for this host-type combination
    /// </summary>
    /// <returns>Business configuration with last modified time</returns>
    Task<BusinessConfigurationResult> GetBusinessConfigurationJsonAsync();

    /// <summary>
    /// Clear business configuration for this host-type combination
    /// </summary>
    /// <param name="updatedBy">User who performed the clear operation</param>
    Task ClearBusinessConfigurationAsync(string updatedBy = "System");
}