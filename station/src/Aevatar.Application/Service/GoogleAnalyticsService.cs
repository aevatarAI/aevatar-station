using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Aevatar.Application.Contracts.Analytics;
using Aevatar.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Service;

/// <summary>
/// Google Analytics event tracking service interface
/// </summary>
public interface IGoogleAnalyticsService
{
    /// <summary>
    /// Track event to Google Analytics
    /// </summary>
    /// <param name="eventRequest">Event request data</param>
    /// <returns>Tracking result</returns>
    Task<GoogleAnalyticsEventResponseDto> TrackEventAsync(GoogleAnalyticsEventRequestDto eventRequest);
} 

/// <summary>
/// Google Analytics event tracking service implementation
/// </summary>
public class GoogleAnalyticsService : IGoogleAnalyticsService, ITransientDependency
{
    private readonly HttpClient _httpClient;
    private readonly GoogleAnalyticsOptions _options;
    private readonly ILogger<GoogleAnalyticsService> _logger;

    public GoogleAnalyticsService(
        HttpClient httpClient,
        IOptions<GoogleAnalyticsOptions> options,
        ILogger<GoogleAnalyticsService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    /// <summary>
    /// Track event to Google Analytics
    /// </summary>
    /// <param name="eventRequest">Event request data</param>
    /// <returns>Tracking result</returns>
    public async Task<GoogleAnalyticsEventResponseDto> TrackEventAsync(GoogleAnalyticsEventRequestDto eventRequest)
    {
        try
        {
            if (!_options.EnableAnalytics)
            {
                _logger.LogDebug("Analytics reporting is disabled in configuration");
                return new GoogleAnalyticsEventResponseDto
                {
                    Success = false,
                    ErrorMessage = "Analytics reporting is disabled"
                };
            }
            
            if (string.IsNullOrWhiteSpace(_options.MeasurementId) || string.IsNullOrWhiteSpace(_options.ApiSecret))
            {
                _logger.LogWarning("[GoogleAnalyticsService][TrackEventAsync] GA configuration not properly set");
                return new GoogleAnalyticsEventResponseDto
                {
                    Success = false,
                    ErrorMessage = "Google Analytics not configured"
                };
            }

            var payload = CreateMeasurementProtocolPayload(eventRequest);
            var jsonPayload = JsonConvert.SerializeObject(payload, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            var url = BuildRequestUrl();
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            _logger.LogDebug("[GoogleAnalyticsService][TrackEventAsync] Sending event: {EventName}, ClientId: {ClientId}, URL: {Url}",
                eventRequest.EventName, eventRequest.ClientId, url);

            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("[GoogleAnalyticsService][TrackEventAsync] Event sent successfully: {EventName}",
                    eventRequest.EventName);

                var result = new GoogleAnalyticsEventResponseDto
                {
                    Success = true
                };

                return result;
            }
            else
            {
                _logger.LogError("[GoogleAnalyticsService][TrackEventAsync] Failed to send event: {EventName}, Status: {StatusCode}, Response: {Response}",
                    eventRequest.EventName, response.StatusCode, responseContent);

                return new GoogleAnalyticsEventResponseDto
                {
                    Success = false,
                    ErrorMessage = $"HTTP {response.StatusCode}: {responseContent}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GoogleAnalyticsService][TrackEventAsync] Exception occurred while sending event: {EventName}",
                eventRequest.EventName);

            return new GoogleAnalyticsEventResponseDto
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Create GA Measurement Protocol payload
    /// </summary>
    private GAMeasurementProtocolPayload CreateMeasurementProtocolPayload(GoogleAnalyticsEventRequestDto eventRequest)
    {
        var payload = new GAMeasurementProtocolPayload
        {
            client_id = !string.IsNullOrWhiteSpace(eventRequest.ClientId) 
                ? eventRequest.ClientId 
                : string.Empty,
            user_id = eventRequest.UserId,
            timestamp_micros = eventRequest.TimestampMicros ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000
        };

        var gaEvent = new GAEvent
        {
            name = eventRequest.EventName,
            parameters = new Dictionary<string, object>(eventRequest.Parameters)
        };

        if (!string.IsNullOrWhiteSpace(eventRequest.SessionId))
        {
            gaEvent.parameters["session_id"] = eventRequest.SessionId;
        }

        payload.events.Add(gaEvent);
        return payload;
    }

    /// <summary>
    /// Build request URL
    /// </summary>
    private string BuildRequestUrl()
    {
        var baseUrl = _options.ApiEndpoint;
        return $"{baseUrl}?measurement_id={_options.MeasurementId}&api_secret={_options.ApiSecret}";
    }
} 