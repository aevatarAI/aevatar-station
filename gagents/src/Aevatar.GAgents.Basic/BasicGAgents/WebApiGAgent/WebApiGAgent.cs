using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.Basic.WebApiGAgent;

/// <summary>
/// WebAPI GAgent implementation for HTTP API interactions
/// </summary>
[GAgent("webapi-gagent", "basic")]
public class WebApiGAgent : GAgentBase<WebApiGAgentState, WebApiStateLogEvent, EventBase, WebApiGAgentConfiguration>, IWebApiGAgent
{
    private HttpClient? _httpClient;
    private readonly object _httpClientLock = new();
    private readonly SemaphoreSlim _rateLimitSemaphore = new(1, 1);
    private IDisposable? _healthCheckTimer;

    #region Service Access

    private HttpClient HttpClient
    {
        get
        {
            if (_httpClient == null)
            {
                lock (_httpClientLock)
                {
                    if (_httpClient == null)
                    {
                        CreateHttpClient();
                    }
                }
            }
            return _httpClient!;
        }
    }

    #endregion

    #region GAgent Overrides

    public override Task<string> GetDescriptionAsync()
    {
        var baseUrl = State.Configuration?.BaseUrl ?? "Not configured";
        var authType = State.Configuration?.Authentication.Type.ToString() ?? "None";
        var totalRequests = State.Metrics.TotalRequests;
        var successRate = State.Metrics.TotalRequests > 0 
            ? (State.Metrics.SuccessfulRequests * 100.0 / State.Metrics.TotalRequests).ToString("F1") + "%"
            : "N/A";
        
        return Task.FromResult(
            $"WebAPI GAgent - Base URL: {baseUrl}\n" +
            $"Authentication: {authType}\n" +
            $"Total Requests: {totalRequests}\n" +
            $"Success Rate: {successRate}\n" +
            $"Health Status: {(State.HealthStatus.IsHealthy ? "Healthy" : "Unhealthy")}\n" +
            $"Last Activity: {State.LastActivityAt:yyyy-MM-dd HH:mm:ss} UTC"
        );
    }

    protected override async Task PerformConfigAsync(WebApiGAgentConfiguration configuration)
    {
        RaiseEvent(new ConfigurationUpdatedLogEvent
        {
            ConfigurationJson = JsonSerializer.Serialize(configuration),
            UpdatedAt = DateTime.UtcNow
        });

        await ConfirmEvents();
        
        // Re-create HTTP client with new configuration
        RecreateHttpClient();
        
        Logger.LogInformation("WebAPI GAgent configured: Base URL = {BaseUrl}, Auth = {AuthType}", 
            configuration.BaseUrl, configuration.Authentication.Type);
    }

    protected override void GAgentTransitionState(WebApiGAgentState state, StateLogEventBase<WebApiStateLogEvent> @event)
    {
        switch (@event)
        {
            case ConfigurationUpdatedLogEvent configEvent:
                var config = JsonSerializer.Deserialize<WebApiGAgentConfiguration>(configEvent.ConfigurationJson);
                state.Configuration = config;
                state.LastActivityAt = configEvent.UpdatedAt;
                
                // Initialize health status
                if (state.HealthStatus.LastCheckAt == DateTime.MinValue)
                {
                    state.HealthStatus = new WebApiHealthStatus
                    {
                        IsHealthy = true,
                        StatusMessage = "Initialized",
                        LastCheckAt = DateTime.UtcNow,
                        ConsecutiveFailures = 0
                    };
                }
                break;

            case RequestExecutedLogEvent requestEvent:
                // Add to history (keep only latest N records)
                state.RequestHistory.Add(requestEvent.RequestRecord);
                if (state.RequestHistory.Count > 100) // Keep last 100 records
                {
                    state.RequestHistory.RemoveRange(0, state.RequestHistory.Count - 100);
                }
                
                // Update metrics
                UpdateMetrics(state, requestEvent.RequestRecord);
                
                // Update health status
                UpdateHealthStatus(state, requestEvent.RequestRecord.IsSuccess);
                
                state.LastActivityAt = requestEvent.RequestRecord.ResponseTime;
                break;

            case MetricsUpdatedLogEvent metricsEvent:
                state.Metrics = metricsEvent.Metrics;
                break;

            case HealthStatusChangedLogEvent healthEvent:
                state.HealthStatus = healthEvent.HealthStatus;
                state.LastActivityAt = healthEvent.ChangedAt;
                break;

            case RequestHistoryCleanupLogEvent cleanupEvent:
                // Remove old records from history
                if (state.RequestHistory.Count > cleanupEvent.RecordsRemoved)
                {
                    state.RequestHistory.RemoveRange(0, cleanupEvent.RecordsRemoved);
                }
                state.LastActivityAt = cleanupEvent.CleanedAt;
                break;
        }
    }

    protected override async Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("WebAPI GAgent activated: {GrainId}", this.GetGrainId());
        
        // Start periodic health checks if configured
        if (State.Configuration != null)
        {
            StartPeriodicHealthCheck();
        }
        
        await base.OnGAgentActivateAsync(cancellationToken);
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _httpClient?.Dispose();
        _rateLimitSemaphore.Dispose();
        _healthCheckTimer?.Dispose();
        Logger.LogInformation("WebAPI GAgent deactivated: {GrainId}, Reason: {Reason}", this.GetGrainId(), reason);
        return base.OnDeactivateAsync(reason, cancellationToken);
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handle all WebAPI related events for monitoring and logging
    /// </summary>
    [AllEventHandler(allowSelfHandling: false)]
    public Task HandleAllEventsAsync(EventWrapperBase eventWrapper)
    {
        if (State.Configuration?.EnableLogging == true)
        {
            Logger.LogDebug("Received event wrapper: {EventType}", eventWrapper.GetType().Name);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handle configuration update requests
    /// </summary>
    [EventHandler]
    public async Task HandleWebApiConfigurationUpdatedEventAsync(WebApiConfigurationUpdatedEvent @event)
    {
        Logger.LogInformation("Received configuration update event for base URL: {BaseUrl}", @event.BaseUrl);
        
        // Optionally trigger health check after configuration update
        if (State.Configuration != null)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1)); // Small delay to let config settle
                await TestConnectionAsync();
            });
        }
        
        await Task.CompletedTask; // Make it properly async
    }

    /// <summary>
    /// Handle request events to update internal metrics
    /// </summary>
    public async Task HandleEventAsync(WebApiRequestEvent @event)
    {
        // Convention-based handler for WebApiRequestEvent
        Logger.LogInformation("WebAPI request started: {Method} {Endpoint}", @event.Method, @event.Endpoint);
        
        // Could trigger additional monitoring or alerting here
        if (State.Configuration?.EnableMetrics == true)
        {
            // Publish internal monitoring event if needed
            await PublishAsync(new WebApiHealthCheckEvent
            {
                IsHealthy = State.HealthStatus.IsHealthy,
                StatusMessage = "Request in progress",
                ConsecutiveFailures = State.HealthStatus.ConsecutiveFailures,
                CheckTime = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Handle response events for additional processing
    /// </summary>
    public async Task HandleEventAsync(WebApiResponseEvent @event)
    {
        // Convention-based handler for WebApiResponseEvent
        Logger.LogInformation("WebAPI response received: RequestId={RequestId}, StatusCode={StatusCode}, Duration={Duration}ms", 
            @event.RequestId, @event.StatusCode, @event.ElapsedMilliseconds);

        // Alert on consecutive failures
        if (!@event.IsSuccess && State.HealthStatus.ConsecutiveFailures >= 3)
        {
            Logger.LogWarning("WebAPI experiencing consecutive failures: {ConsecutiveFailures}", 
                State.HealthStatus.ConsecutiveFailures);
            
            // Could send alert event to monitoring system
            await PublishAsync(new WebApiErrorEvent
            {
                RequestId = @event.RequestId,
                ErrorType = "ConsecutiveFailures",
                ErrorMessage = $"API has {State.HealthStatus.ConsecutiveFailures} consecutive failures",
                Endpoint = "Health Check",
                RetryCount = 0,
                ErrorTime = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Handle error events for alerting and recovery
    /// </summary>
    [EventHandler]
    public async Task HandleWebApiErrorEventAsync(WebApiErrorEvent @event)
    {
        Logger.LogError("WebAPI error occurred: {ErrorType} - {ErrorMessage}, Endpoint: {Endpoint}, RetryCount: {RetryCount}", 
            @event.ErrorType, @event.ErrorMessage, @event.Endpoint, @event.RetryCount);

        // Implement recovery logic for specific error types
        switch (@event.ErrorType.ToLowerInvariant())
        {
            case "httprequestexception":
            case "timeoutexception":
                // Network-related errors - could trigger connection health check
                if (@event.RetryCount >= (State.Configuration?.Retry.MaxAttempts ?? 3))
                {
                    Logger.LogWarning("Max retries exceeded for endpoint: {Endpoint}", @event.Endpoint);
                    await TestConnectionAsync(); // Test overall connection health
                }
                break;

            case "unauthorizedaccessexception":
            case "forbiddenexception":
                // Auth-related errors - could trigger token refresh if supported
                Logger.LogWarning("Authentication/Authorization error detected for endpoint: {Endpoint}", @event.Endpoint);
                break;

            case "consecutivefailures":
                // Health degradation - could implement circuit breaker logic
                Logger.LogWarning("Health degradation detected, consider implementing circuit breaker");
                break;
        }
    }

    /// <summary>
    /// Handle health check events for monitoring
    /// </summary>
    [EventHandler]
    public async Task HandleWebApiHealthCheckEventAsync(WebApiHealthCheckEvent @event)
    {
        Logger.LogInformation("Health check completed: IsHealthy={IsHealthy}, ConsecutiveFailures={ConsecutiveFailures}", 
            @event.IsHealthy, @event.ConsecutiveFailures);

        if (!@event.IsHealthy && @event.ConsecutiveFailures > 5)
        {
            Logger.LogError("WebAPI is critically unhealthy with {ConsecutiveFailures} consecutive failures", 
                @event.ConsecutiveFailures);
            
            // Could implement additional recovery strategies here
            // Such as: reset connections, notify administrators, etc.
        }
    }

    /// <summary>
    /// Generic event handler for external configuration or control events
    /// </summary>
    public Task HandleEventAsync(EventBase @event)
    {
        // Generic handler that can process any EventBase-derived event
        switch (@event)
        {
            // Handle specific external events that might affect WebAPI behavior
            default:
                Logger.LogDebug("Received unhandled event of type: {EventType}", @event.GetType().Name);
                break;
        }

        return Task.CompletedTask;
    }

    #endregion

    #region Public API Methods

    public async Task<WebApiResponse<T>> GetAsync<T>(string endpoint, Dictionary<string, string>? headers = null)
    {
        return await SendRequestAsync<T>(HttpMethod.Get, endpoint, null, headers);
    }

    public async Task<WebApiResponse<T>> PostAsync<T>(string endpoint, object? payload = null, Dictionary<string, string>? headers = null)
    {
        return await SendRequestAsync<T>(HttpMethod.Post, endpoint, payload, headers);
    }

    public async Task<WebApiResponse<T>> PutAsync<T>(string endpoint, object? payload = null, Dictionary<string, string>? headers = null)
    {
        return await SendRequestAsync<T>(HttpMethod.Put, endpoint, payload, headers);
    }

    public async Task<WebApiResponse<T>> DeleteAsync<T>(string endpoint, Dictionary<string, string>? headers = null)
    {
        return await SendRequestAsync<T>(HttpMethod.Delete, endpoint, null, headers);
    }

    public async Task<WebApiResponse<string>> RequestAsync(HttpMethod method, string endpoint, object? payload = null, Dictionary<string, string>? headers = null)
    {
        return await SendRequestAsync<string>(method, endpoint, payload, headers);
    }

    public async Task<bool> UpdateConfigurationAsync(WebApiGAgentConfiguration configuration)
    {
        try
        {
            await PerformConfigAsync(configuration);
            
            await PublishAsync(new WebApiConfigurationUpdatedEvent
            {
                BaseUrl = configuration.BaseUrl,
                AuthenticationType = configuration.Authentication.Type.ToString(),
                UpdatedAt = DateTime.UtcNow
            });
            
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update WebAPI GAgent configuration");
            return false;
        }
    }

    public Task<WebApiGAgentConfiguration> GetConfigurationAsync()
    {
        return Task.FromResult(State.Configuration ?? new WebApiGAgentConfiguration());
    }

    public Task<WebApiHealthStatus> GetHealthStatusAsync()
    {
        return Task.FromResult(State.HealthStatus);
    }

    public Task<List<WebApiRequestRecord>> GetRequestHistoryAsync(int limit = 50)
    {
        var history = State.RequestHistory
            .OrderByDescending(r => r.RequestTime)
            .Take(limit)
            .ToList();
        return Task.FromResult(history);
    }

    public Task<WebApiMetrics> GetMetricsAsync()
    {
        return Task.FromResult(State.Metrics);
    }

    public async Task<bool> TestConnectionAsync()
    {
        if (State.Configuration == null || string.IsNullOrEmpty(State.Configuration.BaseUrl))
        {
            Logger.LogWarning("Cannot test connection: No base URL configured");
            return false;
        }

        try
        {
            // Simple HEAD request to base URL
            var response = await SendRequestInternalAsync(HttpMethod.Head, "/", null, null);
            
            await PublishAsync(new WebApiHealthCheckEvent
            {
                IsHealthy = response.IsSuccess,
                StatusMessage = response.IsSuccess ? "Connection test successful" : "Connection test failed",
                ConsecutiveFailures = response.IsSuccess ? 0 : State.HealthStatus.ConsecutiveFailures + 1,
                CheckTime = DateTime.UtcNow
            });
            
            return response.IsSuccess;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Connection test failed");
            
            await PublishAsync(new WebApiHealthCheckEvent
            {
                IsHealthy = false,
                StatusMessage = $"Connection test failed: {ex.Message}",
                ConsecutiveFailures = State.HealthStatus.ConsecutiveFailures + 1,
                CheckTime = DateTime.UtcNow
            });
            
            return false;
        }
    }

    #endregion

    #region Private Implementation

    private async Task<WebApiResponse<T>> SendRequestAsync<T>(HttpMethod method, string endpoint, object? payload, Dictionary<string, string>? headers)
    {
        var response = await SendRequestInternalAsync(method, endpoint, payload, headers);
        
        // Try to deserialize response for typed return
        if (typeof(T) == typeof(string))
        {
            return new WebApiResponse<T>
            {
                IsSuccess = response.IsSuccess,
                StatusCode = response.StatusCode,
                Data = (T)(object)(response.Data ?? string.Empty),
                ErrorMessage = response.ErrorMessage,
                Headers = response.Headers,
                Timestamp = response.Timestamp,
                ElapsedMilliseconds = response.ElapsedMilliseconds,
                RequestId = response.RequestId
            };
        }
        
        T? deserializedData = default;
        if (response.IsSuccess && !string.IsNullOrEmpty(response.Data))
        {
            try
            {
                deserializedData = JsonSerializer.Deserialize<T>(response.Data);
            }
            catch (JsonException ex)
            {
                Logger.LogWarning(ex, "Failed to deserialize response to type {Type}", typeof(T).Name);
            }
        }
        
        return new WebApiResponse<T>
        {
            IsSuccess = response.IsSuccess,
            StatusCode = response.StatusCode,
            Data = deserializedData,
            ErrorMessage = response.ErrorMessage,
            Headers = response.Headers,
            Timestamp = response.Timestamp,
            ElapsedMilliseconds = response.ElapsedMilliseconds,
            RequestId = response.RequestId
        };
    }

    private async Task<WebApiResponse<string>> SendRequestInternalAsync(HttpMethod method, string endpoint, object? payload, Dictionary<string, string>? headers)
    {
        if (State.Configuration == null)
        {
            throw new InvalidOperationException("WebAPI GAgent is not configured. Call UpdateConfigurationAsync first.");
        }

        var requestId = Guid.NewGuid().ToString();
        var stopwatch = Stopwatch.StartNew();
        var requestTime = DateTime.UtcNow;
        
        // Apply rate limiting
        if (State.Configuration.RateLimit.EnableRateLimit)
        {
            await ApplyRateLimitingAsync();
        }

        // Prepare request
        var request = await CreateHttpRequestAsync(method, endpoint, payload, headers);
        var requestRecord = new WebApiRequestRecord
        {
            RequestId = requestId,
            Method = method.Method,
            Endpoint = endpoint,
            RequestTime = requestTime,
            RequestHeaders = ExtractHeaders(request)
        };

        // Publish request event
        await PublishAsync(new WebApiRequestEvent
        {
            Method = method.Method,
            Endpoint = endpoint,
            PayloadJson = payload != null ? JsonSerializer.Serialize(payload) : null,
            Headers = requestRecord.RequestHeaders,
            RequestTime = requestTime
        });

        WebApiResponse<string> response;
        int retryCount = 0;

        // Execute request with retry logic
        while (true)
        {
            try
            {
                stopwatch.Restart();
                var httpResponse = await HttpClient.SendAsync(request);
                stopwatch.Stop();

                var responseContent = await httpResponse.Content.ReadAsStringAsync();
                var responseHeaders = ExtractHeaders(httpResponse);
                
                response = new WebApiResponse<string>
                {
                    RequestId = requestId,
                    IsSuccess = httpResponse.IsSuccessStatusCode,
                    StatusCode = (int)httpResponse.StatusCode,
                    Data = responseContent,
                    ErrorMessage = httpResponse.IsSuccessStatusCode ? null : $"HTTP {httpResponse.StatusCode}: {httpResponse.ReasonPhrase}",
                    Headers = responseHeaders,
                    Timestamp = requestTime,
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
                };

                // If successful or not retryable, break out of retry loop
                if (httpResponse.IsSuccessStatusCode || !ShouldRetry(httpResponse.StatusCode, retryCount))
                {
                    break;
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                response = new WebApiResponse<string>
                {
                    RequestId = requestId,
                    IsSuccess = false,
                    StatusCode = 0,
                    ErrorMessage = ex.Message,
                    Timestamp = requestTime,
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
                };

                // If not retryable exception or max retries reached, break
                if (!ShouldRetryException(ex) || !ShouldRetry(0, retryCount))
                {
                    await PublishAsync(new WebApiErrorEvent
                    {
                        RequestId = requestId,
                        ErrorType = ex.GetType().Name,
                        ErrorMessage = ex.Message,
                        Endpoint = endpoint,
                        RetryCount = retryCount,
                        ErrorTime = DateTime.UtcNow
                    });
                    break;
                }
            }

            // Prepare for retry
            retryCount++;
            await DelayForRetryAsync(retryCount);
            
            // Create new request for retry (original request cannot be reused)
            request.Dispose();
            request = await CreateHttpRequestAsync(method, endpoint, payload, headers);
        }

        // Complete request record
        requestRecord.ResponseTime = DateTime.UtcNow;
        requestRecord.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
        requestRecord.StatusCode = response.StatusCode;
        requestRecord.IsSuccess = response.IsSuccess;
        requestRecord.ErrorMessage = response.ErrorMessage;
        requestRecord.RetryCount = retryCount;
        requestRecord.ResponseHeaders = response.Headers;

        // Record the request
        RaiseEvent(new RequestExecutedLogEvent { RequestRecord = requestRecord });
        await ConfirmEvents();

        // Publish response event
        await PublishAsync(new WebApiResponseEvent
        {
            RequestId = requestId,
            StatusCode = response.StatusCode,
            IsSuccess = response.IsSuccess,
            ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
            ErrorMessage = response.ErrorMessage,
            ResponseHeaders = response.Headers,
            ResponseTime = DateTime.UtcNow
        });

        request.Dispose();
        return response;
    }

    private async Task<HttpRequestMessage> CreateHttpRequestAsync(HttpMethod method, string endpoint, object? payload, Dictionary<string, string>? headers)
    {
        var config = State.Configuration!;
        var requestUri = new Uri(new Uri(config.BaseUrl), endpoint.TrimStart('/'));
        
        var request = new HttpRequestMessage(method, requestUri);

        // Add default headers
        foreach (var header in config.DefaultHeaders)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Add custom headers
        if (headers != null)
        {
            foreach (var header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        // Add authentication headers
        await AddAuthenticationHeadersAsync(request, config.Authentication);

        // Add payload
        if (payload != null && (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch))
        {
            var json = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return request;
    }

    private async Task AddAuthenticationHeadersAsync(HttpRequestMessage request, WebApiAuthenticationConfig authConfig)
    {
        switch (authConfig.Type)
        {
            case WebApiAuthenticationType.BearerToken:
                if (!string.IsNullOrEmpty(authConfig.BearerToken))
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authConfig.BearerToken);
                }
                break;

            case WebApiAuthenticationType.ApiKey:
                if (!string.IsNullOrEmpty(authConfig.ApiKey) && !string.IsNullOrEmpty(authConfig.ApiKeyHeader))
                {
                    request.Headers.TryAddWithoutValidation(authConfig.ApiKeyHeader, authConfig.ApiKey);
                }
                break;

            case WebApiAuthenticationType.BasicAuth:
                if (!string.IsNullOrEmpty(authConfig.Username) && !string.IsNullOrEmpty(authConfig.Password))
                {
                    var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{authConfig.Username}:{authConfig.Password}"));
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
                }
                break;

            case WebApiAuthenticationType.CustomHeaders:
                foreach (var header in authConfig.CustomHeaders)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
                break;
        }

        await Task.CompletedTask; // Placeholder for potential async auth operations
    }

    private bool ShouldRetry(HttpStatusCode statusCode, int retryCount)
    {
        var config = State.Configuration?.Retry;
        if (config == null || !config.EnableRetry || retryCount >= config.MaxAttempts)
        {
            return false;
        }

        return config.RetryableStatusCodes.Contains((int)statusCode);
    }

    private bool ShouldRetry(int statusCode, int retryCount)
    {
        return ShouldRetry((HttpStatusCode)statusCode, retryCount);
    }

    private static bool ShouldRetryException(Exception ex)
    {
        return ex is HttpRequestException ||
               ex is TaskCanceledException ||
               ex is TimeoutException;
    }

    private async Task DelayForRetryAsync(int retryCount)
    {
        var config = State.Configuration?.Retry;
        if (config == null)
        {
            return;
        }

        var delay = Math.Min(
            config.BaseDelaySeconds * Math.Pow(config.BackoffMultiplier, retryCount - 1),
            config.MaxDelaySeconds);

        Logger.LogInformation("Retrying request in {Delay} seconds (attempt {RetryCount})", delay, retryCount);
        
        await Task.Delay(TimeSpan.FromSeconds(delay));
    }

    private async Task ApplyRateLimitingAsync()
    {
        // Simple rate limiting implementation
        await _rateLimitSemaphore.WaitAsync();
        try
        {
            // In a real implementation, you would check request timestamps
            // and delay if necessary to stay within rate limits
            await Task.CompletedTask;
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    private void CreateHttpClient()
    {
        var httpClientFactory = ServiceProvider.GetRequiredService<IHttpClientFactory>();
        _httpClient = httpClientFactory.CreateClient();
        
        if (State.Configuration != null)
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(State.Configuration.Timeouts.RequestTimeoutSeconds);
        }
    }

    private void RecreateHttpClient()
    {
        lock (_httpClientLock)
        {
            _httpClient?.Dispose();
            _httpClient = null;
        }
    }

    private static Dictionary<string, string> ExtractHeaders(HttpRequestMessage request)
    {
        var headers = new Dictionary<string, string>();
        
        foreach (var header in request.Headers)
        {
            headers[header.Key] = string.Join(", ", header.Value);
        }
        
        if (request.Content?.Headers != null)
        {
            foreach (var header in request.Content.Headers)
            {
                headers[header.Key] = string.Join(", ", header.Value);
            }
        }
        
        return headers;
    }

    private static Dictionary<string, string> ExtractHeaders(HttpResponseMessage response)
    {
        var headers = new Dictionary<string, string>();
        
        foreach (var header in response.Headers)
        {
            headers[header.Key] = string.Join(", ", header.Value);
        }
        
        if (response.Content.Headers != null)
        {
            foreach (var header in response.Content.Headers)
            {
                headers[header.Key] = string.Join(", ", header.Value);
            }
        }
        
        return headers;
    }

    private static void UpdateMetrics(WebApiGAgentState state, WebApiRequestRecord record)
    {
        state.Metrics.TotalRequests++;
        
        if (record.IsSuccess)
        {
            state.Metrics.SuccessfulRequests++;
        }
        else
        {
            state.Metrics.FailedRequests++;
        }
        
        state.Metrics.TotalRetries += record.RetryCount;
        
        // Update status code counts
        if (!state.Metrics.StatusCodeCounts.ContainsKey(record.StatusCode))
        {
            state.Metrics.StatusCodeCounts[record.StatusCode] = 0;
        }
        state.Metrics.StatusCodeCounts[record.StatusCode]++;
        
        // Calculate average response time
        var totalTime = (state.Metrics.AverageResponseTimeMs * (state.Metrics.TotalRequests - 1)) + record.ElapsedMilliseconds;
        state.Metrics.AverageResponseTimeMs = totalTime / state.Metrics.TotalRequests;
    }

    private static void UpdateHealthStatus(WebApiGAgentState state, bool isSuccess)
    {
        var previousStatus = state.HealthStatus;
        
        if (isSuccess)
        {
            state.HealthStatus.ConsecutiveFailures = 0;
            state.HealthStatus.IsHealthy = true;
            state.HealthStatus.StatusMessage = "API responding successfully";
        }
        else
        {
            state.HealthStatus.ConsecutiveFailures++;
            
            // Consider unhealthy after 3 consecutive failures
            if (state.HealthStatus.ConsecutiveFailures >= 3)
            {
                state.HealthStatus.IsHealthy = false;
                state.HealthStatus.StatusMessage = $"API unhealthy: {state.HealthStatus.ConsecutiveFailures} consecutive failures";
            }
        }
        
        state.HealthStatus.LastCheckAt = DateTime.UtcNow;
    }

    private void StartPeriodicHealthCheck()
    {
        // Start a timer for periodic health checks every 5 minutes
        _healthCheckTimer = this.RegisterGrainTimer(
            async (token) => await PeriodicHealthCheckAsync(token),
            new GrainTimerCreationOptions
            {
                DueTime = TimeSpan.FromMinutes(1),  // First check after 1 minute
                Period = TimeSpan.FromMinutes(5),   // Then every 5 minutes
                Interleave = true
            }
        );
        
        Logger.LogInformation("Periodic health check timer started for WebAPI GAgent");
    }

    private async Task PeriodicHealthCheckAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested || State.Configuration == null)
        {
            return;
        }

        try
        {
            Logger.LogDebug("Performing periodic health check");
            
            // Perform health check
            var isHealthy = await TestConnectionAsync();
            
            // Update metrics if enabled
            if (State.Configuration.EnableMetrics)
            {
                // Log current metrics periodically
                var metrics = State.Metrics;
                Logger.LogInformation(
                    "WebAPI Metrics - Total: {Total}, Success Rate: {SuccessRate}%, Avg Response: {AvgTime}ms",
                    metrics.TotalRequests,
                    metrics.TotalRequests > 0 ? (metrics.SuccessfulRequests * 100.0 / metrics.TotalRequests).ToString("F1") : "0",
                    metrics.AverageResponseTimeMs.ToString("F1")
                );
            }
            
            // Clean up old request history (keep only last 50 records)
            if (State.RequestHistory.Count > 100)
            {
                var recordsToRemove = State.RequestHistory.Count - 50;
                RaiseEvent(new RequestHistoryCleanupLogEvent 
                { 
                    RecordsRemoved = recordsToRemove,
                    CleanedAt = DateTime.UtcNow 
                });
                await ConfirmEvents();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during periodic health check");
        }
    }

    #endregion
}
