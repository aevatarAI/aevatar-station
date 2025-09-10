using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Application.Contracts.MCPGateway;
using Aevatar.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;

namespace Aevatar.Application.MCPGateway;

/// <summary>
/// Application service for MCP Gateway management
/// </summary>
[RemoteService(false)]
public class MCPGatewayAppService : ApplicationService, IMCPGatewayAppService
{
    private readonly IMCPGatewayManager _gatewayManager;
    private readonly ILogger<MCPGatewayAppService> _logger;

    public MCPGatewayAppService(
        IMCPGatewayManager gatewayManager,
        ILogger<MCPGatewayAppService> logger)
    {
        _gatewayManager = gatewayManager;
        _logger = logger;
    }

    /// <summary>
    /// Create a new MCP adapter
    /// </summary>
    [Audited]
    public async Task<MCPAdapterDto> CreateAdapterAsync(CreateMCPAdapterDto input)
    {
        _logger.LogInformation("User {UserId} creating MCP adapter {AdapterName}",
            CurrentUser.Id, input.Name);

        try
        {
            await ValidateCreateAdapterInputAsync(input);

            var result = await _gatewayManager.CreateAdapterAsync(input);

            _logger.LogInformation("Successfully created MCP adapter {AdapterName} for user {UserId}",
                input.Name, CurrentUser.Id);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create MCP adapter {AdapterName} for user {UserId}: {Error}",
                input.Name, CurrentUser.Id, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Update an existing MCP adapter
    /// </summary>
    [Audited]
    public async Task<MCPAdapterDto> UpdateAdapterAsync(string name, UpdateMCPAdapterDto input)
    {
        _logger.LogInformation("User {UserId} updating MCP adapter {AdapterName}",
            CurrentUser.Id, name);

        try
        {
            await ValidateUpdateAdapterInputAsync(name, input);

            var result = await _gatewayManager.UpdateAdapterAsync(name, input);

            _logger.LogInformation("Successfully updated MCP adapter {AdapterName} for user {UserId}",
                name, CurrentUser.Id);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update MCP adapter {AdapterName} for user {UserId}: {Error}",
                name, CurrentUser.Id, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Delete an MCP adapter
    /// </summary>
    [Audited]
    public async Task DeleteAdapterAsync(string name)
    {
        _logger.LogInformation("User {UserId} deleting MCP adapter {AdapterName}",
            CurrentUser.Id, name);

        try
        {
            await ValidateAdapterExistsAsync(name);

            await _gatewayManager.DeleteAdapterAsync(name);

            _logger.LogInformation("Successfully deleted MCP adapter {AdapterName} for user {UserId}",
                name, CurrentUser.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete MCP adapter {AdapterName} for user {UserId}: {Error}",
                name, CurrentUser.Id, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Get all MCP adapters with pagination
    /// </summary>
    public async Task<PagedResultDto<MCPAdapterDto>> GetAdaptersAsync(GetAdaptersInput input)
    {
        _logger.LogDebug("User {UserId} fetching MCP adapters with filter: {Filter}",
            CurrentUser.Id, input.Search);

        try
        {
            var allAdapters = await _gatewayManager.GetAllAdaptersAsync();

            // Apply filtering
            var filteredAdapters = ApplyFilters(allAdapters, input);

            // Apply sorting
            var sortedAdapters = ApplySorting(filteredAdapters, input.Sorting);

            // Apply pagination
            var totalCount = sortedAdapters.Count();
            var pagedAdapters = sortedAdapters
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
                .ToList();

            _logger.LogDebug("Returning {Count} of {Total} MCP adapters for user {UserId}",
                pagedAdapters.Count, totalCount, CurrentUser.Id);

            return new PagedResultDto<MCPAdapterDto>(totalCount, pagedAdapters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch MCP adapters for user {UserId}: {Error}",
                CurrentUser.Id, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Get a specific MCP adapter by name
    /// </summary>
    public async Task<MCPAdapterDto> GetAdapterAsync(string name)
    {
        _logger.LogDebug("User {UserId} fetching MCP adapter {AdapterName}",
            CurrentUser.Id, name);

        try
        {
            var adapter = await _gatewayManager.GetAdapterAsync(name);

            if (adapter == null)
            {
                throw new KeyNotFoundException($"Adapter '{name}' not found");
            }

            return adapter;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch MCP adapter {AdapterName} for user {UserId}: {Error}",
                name, CurrentUser.Id, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Get the status of a specific MCP adapter
    /// </summary>
    public async Task<MCPAdapterStatusDto> GetAdapterStatusAsync(string name)
    {
        _logger.LogDebug("User {UserId} fetching status for MCP adapter {AdapterName}",
            CurrentUser.Id, name);

        try
        {
            return await _gatewayManager.GetAdapterStatusAsync(name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch status for MCP adapter {AdapterName} for user {UserId}: {Error}",
                name, CurrentUser.Id, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Get health status of the MCP Gateway
    /// </summary>
    public async Task<MCPGatewayHealthDto> GetGatewayHealthAsync()
    {
        _logger.LogDebug("User {UserId} fetching MCP Gateway health", CurrentUser.Id);

        try
        {
            return await _gatewayManager.GetGatewayHealthAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch MCP Gateway health for user {UserId}: {Error}",
                CurrentUser.Id, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Test connection to a specific adapter
    /// </summary>
    [Audited]
    public async Task<MCPConnectionTestResultDto> TestAdapterConnectionAsync(string name)
    {
        _logger.LogInformation("User {UserId} testing connection to MCP adapter {AdapterName}",
            CurrentUser.Id, name);

        try
        {
            return await _gatewayManager.TestAdapterConnectionAsync(name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test connection for MCP adapter {AdapterName} for user {UserId}: {Error}",
                name, CurrentUser.Id, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Get adapter metrics and usage statistics
    /// </summary>
    public async Task<MCPAdapterMetricsDto> GetAdapterMetricsAsync(string name, DateTime? from = null,
        DateTime? to = null)
    {
        _logger.LogDebug("User {UserId} fetching metrics for MCP adapter {AdapterName}",
            CurrentUser.Id, name);

        try
        {
            return await _gatewayManager.GetAdapterMetricsAsync(name, from, to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch metrics for MCP adapter {AdapterName} for user {UserId}: {Error}",
                name, CurrentUser.Id, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Get adapter logs
    /// </summary>
    public async Task<List<string>> GetAdapterLogsAsync(string name, int? lines = null, bool? follow = null)
    {
        _logger.LogDebug("User {UserId} fetching logs for MCP adapter {AdapterName}",
            CurrentUser.Id, name);

        try
        {
            return await _gatewayManager.GetAdapterLogsAsync(name, lines, follow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch logs for MCP adapter {AdapterName} for user {UserId}: {Error}",
                name, CurrentUser.Id, ex.Message);
            throw;
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Validate create adapter input
    /// </summary>
    private async Task ValidateCreateAdapterInputAsync(CreateMCPAdapterDto input)
    {
        // Check if adapter already exists
        var existingAdapter = await _gatewayManager.GetAdapterAsync(input.Name);
        if (existingAdapter != null)
        {
            throw new InvalidOperationException($"Adapter '{input.Name}' already exists");
        }

        // Validate resource limits
        if (input.ResourceLimits != null)
        {
            ValidateResourceLimits(input.ResourceLimits);
        }
    }

    /// <summary>
    /// Validate update adapter input
    /// </summary>
    private async Task ValidateUpdateAdapterInputAsync(string name, UpdateMCPAdapterDto input)
    {
        await ValidateAdapterExistsAsync(name);

        // Validate resource limits if provided
        if (input.ResourceLimits != null)
        {
            ValidateResourceLimits(input.ResourceLimits);
        }
    }

    /// <summary>
    /// Validate that adapter exists
    /// </summary>
    private async Task ValidateAdapterExistsAsync(string name)
    {
        var adapter = await _gatewayManager.GetAdapterAsync(name);
        if (adapter == null)
        {
            throw new KeyNotFoundException($"Adapter '{name}' not found");
        }
    }

    /// <summary>
    /// Validate resource limits
    /// </summary>
    private void ValidateResourceLimits(MCPResourceLimitsDto resourceLimits)
    {
        if (resourceLimits.CpuLimit.HasValue && (resourceLimits.CpuLimit <= 0 || resourceLimits.CpuLimit > 32))
        {
            throw new ArgumentException("CPU limit must be between 0.1 and 32 cores");
        }

        if (resourceLimits.MemoryLimitMB.HasValue &&
            (resourceLimits.MemoryLimitMB <= 0 || resourceLimits.MemoryLimitMB > 32768))
        {
            throw new ArgumentException("Memory limit must be between 64 MB and 32 GB");
        }

        if (resourceLimits.MaxConnections.HasValue &&
            (resourceLimits.MaxConnections <= 0 || resourceLimits.MaxConnections > 10000))
        {
            throw new ArgumentException("Max connections must be between 1 and 10,000");
        }
    }

    /// <summary>
    /// Apply filters to adapter list
    /// </summary>
    private IEnumerable<MCPAdapterDto> ApplyFilters(List<MCPAdapterDto> adapters, GetAdaptersInput input)
    {
        var query = adapters.AsEnumerable();

        if (input.Status.HasValue)
        {
            query = query.Where(a => a.Status == input.Status.Value);
        }

        if (input.IsHealthy.HasValue)
        {
            query = query.Where(a => a.IsHealthy == input.IsHealthy.Value);
        }

        if (!string.IsNullOrEmpty(input.Search))
        {
            var searchLower = input.Search.ToLowerInvariant();
            query = query.Where(a =>
                a.Name.ToLowerInvariant().Contains(searchLower) ||
                a.Description.ToLowerInvariant().Contains(searchLower) ||
                a.ImageName.ToLowerInvariant().Contains(searchLower));
        }

        if (input.Tags != null && input.Tags.Any())
        {
            query = query.Where(a => input.Tags.Any(tag => a.Tags.Contains(tag)));
        }

        return query;
    }

    /// <summary>
    /// Apply sorting to adapter list
    /// </summary>
    private IEnumerable<MCPAdapterDto> ApplySorting(IEnumerable<MCPAdapterDto> adapters, string? sorting)
    {
        if (string.IsNullOrEmpty(sorting))
        {
            return adapters.OrderBy(a => a.Name);
        }

        var sortParts = sorting.Split(' ');
        var sortField = sortParts[0].ToLowerInvariant();
        var sortDirection = sortParts.Length > 1 && sortParts[1].ToLowerInvariant() == "desc" ? "desc" : "asc";

        return sortField switch
        {
            "name" => sortDirection == "desc"
                ? adapters.OrderByDescending(a => a.Name)
                : adapters.OrderBy(a => a.Name),
            "status" => sortDirection == "desc"
                ? adapters.OrderByDescending(a => a.Status)
                : adapters.OrderBy(a => a.Status),
            "creationtime" => sortDirection == "desc"
                ? adapters.OrderByDescending(a => a.CreationTime)
                : adapters.OrderBy(a => a.CreationTime),
            "activeconnections" => sortDirection == "desc"
                ? adapters.OrderByDescending(a => a.ActiveConnections)
                : adapters.OrderBy(a => a.ActiveConnections),
            "lasthealthcheck" => sortDirection == "desc"
                ? adapters.OrderByDescending(a => a.LastHealthCheck ?? DateTime.MinValue)
                : adapters.OrderBy(a => a.LastHealthCheck ?? DateTime.MinValue),
            _ => adapters.OrderBy(a => a.Name)
        };
    }

    #endregion
}