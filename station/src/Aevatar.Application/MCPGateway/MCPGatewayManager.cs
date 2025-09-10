using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Aevatar.Application.Contracts.MCPGateway;
using Aevatar.GAgents.MCP.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Application.MCPGateway;

/// <summary>
/// Manager for MCP Gateway operations
/// </summary>
public class MCPGatewayManager : IMCPGatewayManager, ITransientDependency
{
    private readonly HttpClient _httpClient;
    private readonly MCPGatewayConfig _config;
    private readonly ILogger<MCPGatewayManager> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public MCPGatewayManager(
        HttpClient httpClient,
        IOptions<MCPGatewayConfig> config,
        ILogger<MCPGatewayManager> logger)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        ConfigureHttpClient();
    }

    /// <summary>
    /// Configure HTTP client with gateway settings
    /// </summary>
    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_config.GatewayBaseUrl);
        _httpClient.Timeout = _config.RequestTimeout;

        var headers = _config.GetAuthenticatedHeaders();
        foreach (var (key, value) in headers)
        {
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(key, value);
        }

        _logger.LogInformation(
            "Configured HTTP client for gateway {GatewayUrl} with timeout {Timeout}",
            _config.GatewayBaseUrl, _config.RequestTimeout);
    }

    /// <summary>
    /// Create a new adapter in the gateway
    /// </summary>
    public async Task<MCPAdapterDto> CreateAdapterAsync(CreateMCPAdapterDto input)
    {
        try
        {
            _logger.LogInformation("Creating adapter {AdapterName} with image {ImageName}:{ImageVersion}",
                input.Name, input.ImageName, input.ImageVersion);

            var createRequest = new
            {
                name = input.Name,
                imageName = input.ImageName,
                imageVersion = input.ImageVersion,
                description = input.Description,
                environment = input.Environment,
                tags = input.Tags,
                metadata = input.Metadata,
                resourceLimits = input.ResourceLimits
            };

            var response = await _httpClient.PostAsJsonAsync("/adapters", createRequest, _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<MCPAdapterDto>(_jsonOptions);
                _logger.LogInformation("Successfully created adapter {AdapterName}", input.Name);
                return result!;
            }

            await HandleErrorResponseAsync(response, $"Failed to create adapter {input.Name}");
            throw new InvalidOperationException($"Failed to create adapter {input.Name}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error creating adapter {AdapterName}: {Error}", input.Name, ex.Message);
            throw new InvalidOperationException($"Gateway communication error: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout creating adapter {AdapterName}", input.Name);
            throw new TimeoutException($"Gateway request timeout for adapter {input.Name}", ex);
        }
    }

    /// <summary>
    /// Update an existing adapter
    /// </summary>
    public async Task<MCPAdapterDto> UpdateAdapterAsync(string name, UpdateMCPAdapterDto input)
    {
        try
        {
            _logger.LogInformation("Updating adapter {AdapterName}", name);

            var updateRequest = new
            {
                imageName = input.ImageName,
                imageVersion = input.ImageVersion,
                description = input.Description,
                environment = input.Environment,
                tags = input.Tags,
                metadata = input.Metadata,
                resourceLimits = input.ResourceLimits
            };

            var response = await _httpClient.PutAsJsonAsync($"/adapters/{name}", updateRequest, _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<MCPAdapterDto>(_jsonOptions);
                _logger.LogInformation("Successfully updated adapter {AdapterName}", name);
                return result!;
            }

            await HandleErrorResponseAsync(response, $"Failed to update adapter {name}");
            throw new InvalidOperationException($"Failed to update adapter {name}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error updating adapter {AdapterName}: {Error}", name, ex.Message);
            throw new InvalidOperationException($"Gateway communication error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Delete an adapter from the gateway
    /// </summary>
    public async Task DeleteAdapterAsync(string name)
    {
        try
        {
            _logger.LogInformation("Deleting adapter {AdapterName}", name);

            var response = await _httpClient.DeleteAsync($"/adapters/{name}");

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully deleted adapter {AdapterName}", name);
                return;
            }

            await HandleErrorResponseAsync(response, $"Failed to delete adapter {name}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error deleting adapter {AdapterName}: {Error}", name, ex.Message);
            throw new InvalidOperationException($"Gateway communication error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get all adapters from the gateway
    /// </summary>
    public async Task<List<MCPAdapterDto>> GetAllAdaptersAsync()
    {
        try
        {
            _logger.LogDebug("Fetching all adapters from gateway");

            var response = await _httpClient.GetAsync("/adapters");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<MCPAdapterDto>>(_jsonOptions);
                _logger.LogDebug("Successfully fetched {Count} adapters", result?.Count ?? 0);
                return result ?? new List<MCPAdapterDto>();
            }

            await HandleErrorResponseAsync(response, "Failed to fetch adapters");
            return new List<MCPAdapterDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching adapters: {Error}", ex.Message);
            throw new InvalidOperationException($"Gateway communication error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get a specific adapter by name
    /// </summary>
    public async Task<MCPAdapterDto?> GetAdapterAsync(string name)
    {
        try
        {
            _logger.LogDebug("Fetching adapter {AdapterName}", name);

            var response = await _httpClient.GetAsync($"/adapters/{name}");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<MCPAdapterDto>(_jsonOptions);
                _logger.LogDebug("Successfully fetched adapter {AdapterName}", name);
                return result;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug("Adapter {AdapterName} not found", name);
                return null;
            }

            await HandleErrorResponseAsync(response, $"Failed to fetch adapter {name}");
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching adapter {AdapterName}: {Error}", name, ex.Message);
            throw new InvalidOperationException($"Gateway communication error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get adapter status and health information
    /// </summary>
    public async Task<MCPAdapterStatusDto> GetAdapterStatusAsync(string name)
    {
        try
        {
            _logger.LogDebug("Fetching adapter status for {AdapterName}", name);

            var response = await _httpClient.GetAsync($"/adapters/{name}/status");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<MCPAdapterStatusDto>(_jsonOptions);
                _logger.LogDebug("Successfully fetched status for adapter {AdapterName}", name);
                return result!;
            }

            await HandleErrorResponseAsync(response, $"Failed to fetch status for adapter {name}");
            throw new InvalidOperationException($"Failed to fetch status for adapter {name}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching adapter status {AdapterName}: {Error}", name, ex.Message);
            throw new InvalidOperationException($"Gateway communication error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get gateway health status
    /// </summary>
    public async Task<MCPGatewayHealthDto> GetGatewayHealthAsync()
    {
        try
        {
            _logger.LogDebug("Fetching gateway health status");

            var response = await _httpClient.GetAsync(_config.HealthCheckPath);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<MCPGatewayHealthDto>(_jsonOptions);
                _logger.LogDebug("Successfully fetched gateway health status");
                return result!;
            }

            await HandleErrorResponseAsync(response, "Failed to fetch gateway health");
            throw new InvalidOperationException("Failed to fetch gateway health");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching gateway health: {Error}", ex.Message);
            throw new InvalidOperationException($"Gateway communication error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Test connection to a specific adapter
    /// </summary>
    public async Task<MCPConnectionTestResultDto> TestAdapterConnectionAsync(string name)
    {
        try
        {
            _logger.LogInformation("Testing connection to adapter {AdapterName}", name);

            var startTime = DateTime.UtcNow;
            var response = await _httpClient.PostAsync($"/adapters/{name}/test", null);
            var latency = (DateTime.UtcNow - startTime).TotalMilliseconds;

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<MCPConnectionTestResultDto>(_jsonOptions);
                if (result != null)
                {
                    result.LatencyMs = latency;
                    result.TestedAt = DateTime.UtcNow;
                }

                _logger.LogInformation("Connection test successful for adapter {AdapterName}, latency: {Latency}ms",
                    name, latency);
                return result!;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Connection test failed for adapter {AdapterName}: {Error}", name, errorContent);

            return new MCPConnectionTestResultDto
            {
                IsSuccessful = false,
                LatencyMs = latency,
                ErrorMessage = $"HTTP {response.StatusCode}: {errorContent}",
                TestedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception testing connection to adapter {AdapterName}: {Error}", name, ex.Message);

            return new MCPConnectionTestResultDto
            {
                IsSuccessful = false,
                LatencyMs = -1,
                ErrorMessage = ex.Message,
                TestedAt = DateTime.UtcNow,
                Details = new Dictionary<string, object>
                {
                    ["ExceptionType"] = ex.GetType().Name,
                    ["StackTrace"] = ex.StackTrace ?? "N/A"
                }
            };
        }
    }

    /// <summary>
    /// Get adapter metrics and usage statistics
    /// </summary>
    public async Task<MCPAdapterMetricsDto> GetAdapterMetricsAsync(string name, DateTime? from = null,
        DateTime? to = null)
    {
        try
        {
            _logger.LogDebug("Fetching metrics for adapter {AdapterName}", name);

            var queryParams = new List<string>();
            if (from.HasValue)
                queryParams.Add($"from={from.Value:yyyy-MM-ddTHH:mm:ssZ}");
            if (to.HasValue)
                queryParams.Add($"to={to.Value:yyyy-MM-ddTHH:mm:ssZ}");

            var query = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
            var response = await _httpClient.GetAsync($"/adapters/{name}/metrics{query}");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<MCPAdapterMetricsDto>(_jsonOptions);
                _logger.LogDebug("Successfully fetched metrics for adapter {AdapterName}", name);
                return result!;
            }

            await HandleErrorResponseAsync(response, $"Failed to fetch metrics for adapter {name}");
            throw new InvalidOperationException($"Failed to fetch metrics for adapter {name}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching adapter metrics {AdapterName}: {Error}", name, ex.Message);
            throw new InvalidOperationException($"Gateway communication error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get adapter logs (corresponds to GET /adapters/{name}/logs)
    /// </summary>
    public async Task<List<string>> GetAdapterLogsAsync(string name, int? lines = null, bool? follow = null)
    {
        try
        {
            _logger.LogInformation("Fetching logs for adapter {AdapterName}", name);

            var queryParams = new List<string>();
            if (lines.HasValue)
                queryParams.Add($"lines={lines.Value}");
            if (follow.HasValue)
                queryParams.Add($"follow={follow.Value.ToString().ToLower()}");

            var query = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
            var response = await _httpClient.GetAsync($"/adapters/{name}/logs{query}");

            if (response.IsSuccessStatusCode)
            {
                var logsContent = await response.Content.ReadAsStringAsync();
                var logLines = logsContent.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();

                _logger.LogDebug("Successfully fetched {Count} log lines for adapter {AdapterName}",
                    logLines.Count, name);

                return logLines;
            }

            await HandleErrorResponseAsync(response, $"Failed to fetch logs for adapter {name}");
            return new List<string>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching adapter logs {AdapterName}: {Error}", name, ex.Message);
            throw new InvalidOperationException($"Gateway communication error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Handle error responses from the gateway
    /// </summary>
    private async Task HandleErrorResponseAsync(HttpResponseMessage response, string operation)
    {
        var errorContent = await response.Content.ReadAsStringAsync();
        var statusCode = response.StatusCode;

        _logger.LogError("Gateway API error: {Operation} failed with {StatusCode}: {Error}",
            operation, statusCode, errorContent);

        var errorMessage = statusCode switch
        {
            System.Net.HttpStatusCode.BadRequest => $"Invalid request: {errorContent}",
            System.Net.HttpStatusCode.Unauthorized => "Authentication failed. Please check your gateway credentials.",
            System.Net.HttpStatusCode.Forbidden => "Access denied. Insufficient permissions for this operation.",
            System.Net.HttpStatusCode.NotFound => "Resource not found in the gateway.",
            System.Net.HttpStatusCode.Conflict => $"Resource conflict: {errorContent}",
            System.Net.HttpStatusCode.TooManyRequests => "Rate limit exceeded. Please try again later.",
            System.Net.HttpStatusCode.InternalServerError => $"Gateway internal error: {errorContent}",
            System.Net.HttpStatusCode.BadGateway => "Gateway is temporarily unavailable.",
            System.Net.HttpStatusCode.ServiceUnavailable => "Gateway service is temporarily unavailable.",
            System.Net.HttpStatusCode.GatewayTimeout => "Gateway request timeout.",
            _ => $"Gateway error ({statusCode}): {errorContent}"
        };

        throw statusCode switch
        {
            System.Net.HttpStatusCode.BadRequest => new ArgumentException(errorMessage),
            System.Net.HttpStatusCode.Unauthorized => new UnauthorizedAccessException(errorMessage),
            System.Net.HttpStatusCode.Forbidden => new UnauthorizedAccessException(errorMessage),
            System.Net.HttpStatusCode.NotFound => new KeyNotFoundException(errorMessage),
            System.Net.HttpStatusCode.Conflict => new InvalidOperationException(errorMessage),
            System.Net.HttpStatusCode.TooManyRequests => new InvalidOperationException(errorMessage),
            System.Net.HttpStatusCode.GatewayTimeout => new TimeoutException(errorMessage),
            _ => new InvalidOperationException(errorMessage)
        };
    }

    /// <summary>
    /// Validate gateway configuration and connectivity
    /// </summary>
    public async Task<bool> ValidateGatewayConnectivityAsync()
    {
        try
        {
            var health = await GetGatewayHealthAsync();
            return health.IsHealthy;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get gateway connection statistics
    /// </summary>
    public Dictionary<string, object> GetConnectionStats()
    {
        return new Dictionary<string, object>
        {
            ["GatewayUrl"] = _config.GatewayBaseUrl,
            ["RequestTimeout"] = _config.RequestTimeout.TotalSeconds,
            ["MaxRetryAttempts"] = _config.MaxRetryAttempts,
            ["RetryDelay"] = _config.RetryDelay.TotalMilliseconds,
            ["SessionAffinity"] = _config.EnableSessionAffinity,
            ["DetailedLogging"] = _config.EnableDetailedLogging
        };
    }
}