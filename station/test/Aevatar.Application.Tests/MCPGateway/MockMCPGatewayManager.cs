using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Application.Contracts.MCPGateway;
using Aevatar.Application.MCPGateway;
using Microsoft.Extensions.Logging;
using Volo.Abp.Application.Dtos;

namespace Aevatar.Application.Tests.MCPGateway;

/// <summary>
/// Mock implementation of IMCPGatewayManager for testing purposes
/// </summary>
public class MockMCPGatewayManager : IMCPGatewayManager
{
    private readonly ILogger<MockMCPGatewayManager> _logger;
    private readonly Dictionary<string, MCPAdapterDto> _adapters = new();
    private readonly Dictionary<string, MCPAdapterStatusDto> _adapterStatuses = new();
    private readonly Dictionary<string, MCPAdapterMetricsDto> _adapterMetrics = new();
    private readonly Dictionary<string, List<string>> _adapterLogs = new();

    public MockMCPGatewayManager(ILogger<MockMCPGatewayManager> logger)
    {
        _logger = logger;
        InitializeTestData();
    }

    private void InitializeTestData()
    {
        // Add some default test data
        var defaultAdapter = new MCPAdapterDto
        {
            Id = "default-adapter",
            Name = "default-adapter",
            ImageName = "default-image",
            ImageVersion = "1.0.0",
            Description = "Default test adapter",
            Status = MCPAdapterStatus.Running,
            IsHealthy = true,
            ActiveConnections = 2,
            CreationTime = DateTime.UtcNow.AddHours(-1),
            LastModificationTime = DateTime.UtcNow.AddMinutes(-30),
            Environment = new Dictionary<string, string> { ["ENV"] = "test" },
            Tags = ["test", "default"],
            Metadata = new Dictionary<string, string> { ["category"] = "test" }
        };

        _adapters["default-adapter"] = defaultAdapter;

        _adapterStatuses["default-adapter"] = new MCPAdapterStatusDto
        {
            Name = "default-adapter",
            Status = MCPAdapterStatus.Running,
            IsHealthy = true,
            ActiveConnections = 2,
            UptimeSeconds = 3600,
            LastHealthCheck = DateTime.UtcNow.AddMinutes(-1),
            ResourceUsage = new MCPResourceUsageDto
            {
                CpuUsagePercent = 15.5,
                MemoryUsageMB = 256,
                NetworkIO = new MCPNetworkIODto
                {
                    BytesReceived = 1024000,
                    BytesSent = 512000
                }
            }
        };

        _adapterMetrics["default-adapter"] = new MCPAdapterMetricsDto
        {
            Name = "default-adapter",
            FromTime = DateTime.UtcNow.AddHours(-1),
            ToTime = DateTime.UtcNow,
            RequestStats = new MCPRequestStatsDto
            {
                TotalRequests = 100,
                SuccessfulRequests = 95,
                FailedRequests = 5,
                AverageResponseTimeMs = 125.5,
                RequestsPerSecond = 2.5
            }
        };

        _adapterLogs["default-adapter"] = new List<string>
        {
            $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} INFO Starting adapter default-adapter",
            $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} INFO Adapter is healthy and ready",
            $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} DEBUG Processing request"
        };
    }

    public Task<MCPAdapterDto> CreateAdapterAsync(CreateMCPAdapterDto input)
    {
        _logger.LogInformation("Mock: Creating adapter {AdapterName}", input.Name);

        if (_adapters.ContainsKey(input.Name))
        {
            throw new InvalidOperationException($"Adapter '{input.Name}' already exists");
        }

        var adapter = new MCPAdapterDto
        {
            Id = input.Name,
            Name = input.Name,
            ImageName = input.ImageName,
            ImageVersion = input.ImageVersion,
            Description = input.Description,
            Environment = input.Environment ?? new Dictionary<string, string>(),
            Tags = input.Tags ?? new List<string>(),
            Metadata = input.Metadata ?? new Dictionary<string, string>(),
            Status = MCPAdapterStatus.Creating,
            IsHealthy = false,
            ActiveConnections = 0,
            CreationTime = DateTime.UtcNow,
            LastModificationTime = DateTime.UtcNow
        };

        _adapters[input.Name] = adapter;

        // Initialize status and metrics for new adapter
        _adapterStatuses[input.Name] = new MCPAdapterStatusDto
        {
            Name = input.Name,
            Status = MCPAdapterStatus.Creating,
            IsHealthy = false,
            ActiveConnections = 0,
            UptimeSeconds = 0,
            LastHealthCheck = DateTime.UtcNow
        };

        _adapterLogs[input.Name] = new List<string>
        {
            $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} INFO Creating adapter {input.Name}",
            $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} INFO Pulling image {input.ImageName}:{input.ImageVersion}"
        };

        return Task.FromResult(adapter);
    }

    public Task<MCPAdapterDto> UpdateAdapterAsync(string name, UpdateMCPAdapterDto input)
    {
        _logger.LogInformation("Mock: Updating adapter {AdapterName}", name);

        if (!_adapters.TryGetValue(name, out var adapter))
        {
            throw new KeyNotFoundException($"Adapter '{name}' not found");
        }

        // Update properties if provided
        if (!string.IsNullOrEmpty(input.ImageVersion))
            adapter.ImageVersion = input.ImageVersion;
        if (!string.IsNullOrEmpty(input.Description))
            adapter.Description = input.Description;
        if (input.Environment != null)
            adapter.Environment = input.Environment;
        if (input.Tags != null)
            adapter.Tags = input.Tags;
        if (input.Metadata != null)
            adapter.Metadata = input.Metadata;
        // ResourceLimits property doesn't exist in current MCPAdapterDto

        adapter.LastModificationTime = DateTime.UtcNow;
        adapter.Status = MCPAdapterStatus.Updating;

        _adapters[name] = adapter;

        return Task.FromResult(adapter);
    }

    public Task DeleteAdapterAsync(string name)
    {
        _logger.LogInformation("Mock: Deleting adapter {AdapterName}", name);

        if (!_adapters.ContainsKey(name))
        {
            throw new KeyNotFoundException($"Adapter '{name}' not found");
        }

        _adapters.Remove(name);
        _adapterStatuses.Remove(name);
        _adapterMetrics.Remove(name);
        _adapterLogs.Remove(name);

        return Task.CompletedTask;
    }

    public Task<PagedResultDto<MCPAdapterDto>> GetAdaptersAsync(GetAdaptersInput input)
    {
        _logger.LogInformation("Mock: Getting adapters with filters");

        var query = _adapters.Values.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(input.Search))
        {
            query = query.Where(a => a.Name.Contains(input.Search, StringComparison.OrdinalIgnoreCase) ||
                                   (a.Description != null && a.Description.Contains(input.Search, StringComparison.OrdinalIgnoreCase)));
        }

        if (input.Status.HasValue)
        {
            query = query.Where(a => a.Status == input.Status.Value);
        }

        if (input.IsHealthy.HasValue)
        {
            query = query.Where(a => a.IsHealthy == input.IsHealthy.Value);
        }

        if (input.Tags != null && input.Tags.Any())
        {
            query = query.Where(a => a.Tags != null && input.Tags.Any(tag => a.Tags.Contains(tag)));
        }

        var totalCount = query.Count();

        // Apply sorting
        if (!string.IsNullOrEmpty(input.Sorting))
        {
            var sortParts = input.Sorting.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var sortField = sortParts[0].ToLower();
            var sortDirection = sortParts.Length > 1 ? sortParts[1].ToLower() : "asc";

            query = sortField switch
            {
                "name" => sortDirection == "desc" ? query.OrderByDescending(a => a.Name) : query.OrderBy(a => a.Name),
                "status" => sortDirection == "desc" ? query.OrderByDescending(a => a.Status) : query.OrderBy(a => a.Status),
                "creationtime" => sortDirection == "desc" ? query.OrderByDescending(a => a.CreationTime) : query.OrderBy(a => a.CreationTime),
                _ => query.OrderBy(a => a.Name)
            };
        }
        else
        {
            query = query.OrderBy(a => a.Name);
        }

        // Apply pagination
        var items = query.Skip(input.SkipCount)
                        .Take(input.MaxResultCount > 0 ? input.MaxResultCount : 10)
                        .ToList();

        return Task.FromResult(new PagedResultDto<MCPAdapterDto>(totalCount, items));
    }

    public Task<MCPAdapterDto?> GetAdapterAsync(string name)
    {
        _logger.LogInformation("Mock: Getting adapter {AdapterName}", name);

        return Task.FromResult(_adapters.TryGetValue(name, out var adapter) ? adapter : null);
    }

    public Task<MCPAdapterStatusDto> GetAdapterStatusAsync(string name)
    {
        _logger.LogInformation("Mock: Getting adapter status {AdapterName}", name);

        if (!_adapterStatuses.TryGetValue(name, out var status))
        {
            throw new KeyNotFoundException($"Adapter '{name}' not found");
        }

        return Task.FromResult(status);
    }

    public Task<MCPGatewayHealthDto> GetGatewayHealthAsync()
    {
        _logger.LogInformation("Mock: Getting gateway health");

        var health = new MCPGatewayHealthDto
        {
            IsHealthy = true,
            Version = "1.0.0-mock",
            UptimeSeconds = 86400,
            TotalAdapters = _adapters.Count,
            HealthyAdapters = _adapters.Values.Count(a => a.IsHealthy),
            TotalActiveConnections = _adapters.Values.Sum(a => a.ActiveConnections),
            CheckedAt = DateTime.UtcNow
        };

        return Task.FromResult(health);
    }

    public Task<MCPConnectionTestResultDto> TestAdapterConnectionAsync(string name)
    {
        _logger.LogInformation("Mock: Testing adapter connection {AdapterName}", name);

        if (!_adapters.ContainsKey(name))
        {
            throw new KeyNotFoundException($"Adapter '{name}' not found");
        }

        var result = new MCPConnectionTestResultDto
        {
            IsSuccessful = true,
            LatencyMs = 125.5,
            TestedAt = DateTime.UtcNow,
            Details = new Dictionary<string, object>
            {
                ["endpoint"] = $"http://mock-gateway.com/adapters/{name}/mcp",
                ["responseCode"] = 200,
                ["responseTime"] = "125ms"
            }
        };

        return Task.FromResult(result);
    }

    public Task<MCPAdapterMetricsDto> GetAdapterMetricsAsync(string name, DateTime? from = null, DateTime? to = null)
    {
        _logger.LogInformation("Mock: Getting adapter metrics {AdapterName}", name);

        if (!_adapterMetrics.TryGetValue(name, out var metrics))
        {
            throw new KeyNotFoundException($"Adapter '{name}' not found");
        }

        // Update time range if provided
        if (from.HasValue)
            metrics.FromTime = from.Value;
        if (to.HasValue)
            metrics.ToTime = to.Value;

        return Task.FromResult(metrics);
    }

    public Task<List<string>> GetAdapterLogsAsync(string name, int? lines = null, bool? follow = null)
    {
        _logger.LogInformation("Mock: Getting adapter logs {AdapterName}", name);

        if (!_adapterLogs.TryGetValue(name, out var logs))
        {
            throw new KeyNotFoundException($"Adapter '{name}' not found");
        }

        var result = logs.ToList();
        if (lines.HasValue && lines.Value > 0)
        {
            result = result.TakeLast(lines.Value).ToList();
        }

        return Task.FromResult(result);
    }

    // Helper method for tests to add custom adapters
    public Task<List<MCPAdapterDto>> GetAllAdaptersAsync()
    {
        return Task.FromResult(_adapters.Values.ToList());
    }

    // Helper methods for test setup
    public void AddTestAdapter(MCPAdapterDto adapter)
    {
        _adapters[adapter.Name] = adapter;
    }

    public void ClearTestData()
    {
        _adapters.Clear();
        _adapterStatuses.Clear();
        _adapterMetrics.Clear();
        _adapterLogs.Clear();
        InitializeTestData();
    }
}
