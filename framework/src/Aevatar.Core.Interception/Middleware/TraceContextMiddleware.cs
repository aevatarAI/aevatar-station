using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Aevatar.Core.Interception.Context;
using System.Linq;

namespace Aevatar.Core.Interception.Middleware;

/// <summary>
/// HTTP middleware that reads trace ID from HTTP context and sets it in TraceContext
/// for method interception during HTTP requests.
/// </summary>
public class TraceContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TraceContextMiddleware> _logger;

    public TraceContextMiddleware(RequestDelegate next, ILogger<TraceContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _logger.LogInformation("TraceContextMiddleware initialized");
    }

    /// <summary>
    /// Processes the HTTP request to set trace context
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        _logger.LogInformation("TraceContextMiddleware invoked for request {Method} {Path} at {Timestamp}", 
            context.Request.Method, context.Request.Path, DateTime.UtcNow);

        try
        {
            // Log all available headers for debugging
            _logger.LogDebug("Request headers: {Headers}", 
                string.Join(", ", context.Request.Headers.Select(h => $"{h.Key}={h.Value}")));

            // Try to get trace ID from various sources in order of preference
            var traceId = GetTraceIdFromContext(context);
            
            _logger.LogInformation("Extracted trace ID: {TraceId} for request {Method} {Path}", 
                traceId ?? "NULL", context.Request.Method, context.Request.Path);
            
            if (!string.IsNullOrEmpty(traceId))
            {
                // Set the trace ID in TraceContext for method interception
                var previousTraceId = TraceContext.ActiveTraceId;
                TraceContext.ActiveTraceId = traceId;
                
                _logger.LogInformation("Set trace ID {TraceId} in TraceContext (previous: {PreviousTraceId}) for request {Method} {Path}", 
                    traceId, previousTraceId ?? "NULL", context.Request.Method, context.Request.Path);
            }
            else
            {
                // Clear any existing trace context if no trace ID found
                var previousTraceId = TraceContext.ActiveTraceId;
                TraceContext.ActiveTraceId = null;
                
                _logger.LogWarning("No trace ID found, cleared TraceContext (previous: {PreviousTraceId}) for request {Method} {Path}", 
                    previousTraceId ?? "NULL", context.Request.Method, context.Request.Path);
            }

            // Continue with the request pipeline
            _logger.LogDebug("Continuing request pipeline for {Method} {Path}", 
                context.Request.Method, context.Request.Path);
            await _next(context);
            
            _logger.LogDebug("Request pipeline completed for {Method} {Path}", 
                context.Request.Method, context.Request.Path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in TraceContextMiddleware for request {Method} {Path}", 
                context.Request.Method, context.Request.Path);
            throw;
        }
        finally
        {
            // Clean up trace context after the request
            var currentTraceId = TraceContext.ActiveTraceId;
            TraceContext.ActiveTraceId = null;
            
            _logger.LogDebug("Cleaned up TraceContext (was: {CurrentTraceId}) for request {Method} {Path}", 
                currentTraceId ?? "NULL", context.Request.Method, context.Request.Path);
        }
    }

    /// <summary>
    /// Extracts trace ID from HTTP context using various strategies
    /// </summary>
    private string? GetTraceIdFromContext(HttpContext context)
    {
        _logger.LogDebug("Starting trace ID extraction for request {Method} {Path}", 
            context.Request.Method, context.Request.Path);

        // 1. Check for custom header (highest priority)
        if (context.Request.Headers.TryGetValue("X-Trace-Id", out var traceIdHeader) && 
            !string.IsNullOrEmpty(traceIdHeader))
        {
            var traceId = traceIdHeader.ToString();
            _logger.LogDebug("Found trace ID from X-Trace-Id header: {TraceId}", traceId);
            return traceId;
        }
        else
        {
            _logger.LogDebug("X-Trace-Id header not found or empty");
        }

        // 2. Check for OpenTelemetry traceparent header
        if (context.Request.Headers.TryGetValue("traceparent", out var traceparentHeader) && 
            !string.IsNullOrEmpty(traceparentHeader))
        {
            var traceparent = traceparentHeader.ToString();
            _logger.LogDebug("Found traceparent header: {Traceparent}", traceparent);
            
            // Extract trace ID from traceparent format: 00-<trace-id>-<span-id>-<trace-flags>
            var parts = traceparent.Split('-');
            if (parts.Length >= 2 && parts[1].Length == 32)
            {
                var traceId = parts[1];
                _logger.LogDebug("Extracted trace ID from traceparent: {TraceId}", traceId);
                return traceId;
            }
            else
            {
                _logger.LogDebug("Invalid traceparent format: {Parts} parts, part[1] length: {Length}", 
                    parts.Length, parts.Length >= 2 ? parts[1].Length : 0);
            }
        }
        else
        {
            _logger.LogDebug("traceparent header not found or empty");
        }

        // 3. Check for W3C trace context header
        if (context.Request.Headers.TryGetValue("tracecontext", out var tracecontextHeader) && 
            !string.IsNullOrEmpty(tracecontextHeader))
        {
            var tracecontext = tracecontextHeader.ToString();
            _logger.LogDebug("Found tracecontext header: {Tracecontext}", tracecontext);
            
            // Extract trace ID from tracecontext format
            var traceIdMatch = System.Text.RegularExpressions.Regex.Match(tracecontext, @"trace-id=([a-f0-9]{32})");
            if (traceIdMatch.Success)
            {
                var traceId = traceIdMatch.Groups[1].Value;
                _logger.LogDebug("Extracted trace ID from tracecontext: {TraceId}", traceId);
                return traceId;
            }
            else
            {
                _logger.LogDebug("No valid trace-id found in tracecontext using regex pattern");
            }
        }
        else
        {
            _logger.LogDebug("tracecontext header not found or empty");
        }

        // 4. Check for query parameter
        if (context.Request.Query.TryGetValue("traceId", out var traceIdQuery) && 
            !string.IsNullOrEmpty(traceIdQuery))
        {
            var traceId = traceIdQuery.ToString();
            _logger.LogDebug("Found trace ID from query parameter: {TraceId}", traceId);
            return traceId;
        }
        else
        {
            _logger.LogDebug("traceId query parameter not found or empty");
        }

        // 5. Check for correlation ID header (fallback)
        if (context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationIdHeader) && 
            !string.IsNullOrEmpty(correlationIdHeader))
        {
            var traceId = correlationIdHeader.ToString();
            _logger.LogDebug("Found trace ID from X-Correlation-Id header (fallback): {TraceId}", traceId);
            return traceId;
        }
        else
        {
            _logger.LogDebug("X-Correlation-Id header not found or empty");
        }

        _logger.LogWarning("No trace ID found from any source for request {Method} {Path}", 
            context.Request.Method, context.Request.Path);
        return null;
    }
}
