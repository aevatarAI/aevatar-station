using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Aevatar.Core.Observability;

public static class EventPublishLatencyMetrics
{
    private static readonly Meter Meter = new(OpenTelemetryConstants.AevatarStreamsMeterName);
    private static readonly Histogram<double> PublishLatencyHistogram = Meter.CreateHistogram<double>(
        OpenTelemetryConstants.EventPublishLatencyHistogram, "s", "Event publish-to-consume latency");

    /// <summary>
    /// Records latency metrics for EventWrapperBase with automatic method name and parameter type detection
    /// </summary>
    /// <param name="latency">Latency in seconds</param>
    /// <param name="item">The event item (parameter type is derived automatically)</param>
    /// <param name="logger">Optional logger for additional logging</param>
    /// <param name="methodName">Automatically captured method name</param>
    /// <param name="filePath">Automatically captured file path for class name</param>
    public static void Record(double latency, EventWrapperBase item, ILogger? logger = null, 
        [CallerMemberName] string methodName = "", [CallerFilePath] string? filePath = null)
    {
        var parameterType = item.GetType().Name;
        var agentType = GetAgentTypeFromGrainId(item, logger) ?? "unknown";
        var eventId = item.GetType().GetProperty("EventId")?.GetValue(item)?.ToString() ?? "unknown";
        
        var className = GetClassNameFromFilePath(filePath);
        var fullMethodName = className != null ? $"{className}.{methodName}" : methodName;
        
        PublishLatencyHistogram.Record(latency,
            new KeyValuePair<string, object?>("agent_type", agentType),
            new KeyValuePair<string, object?>("method_name", fullMethodName),
            new KeyValuePair<string, object?>("parameter_type", parameterType),
            new KeyValuePair<string, object?>("event_category", OpenTelemetryConstants.StreamEventType));
            
        logger?.LogInformation("[PublishLatency] latency={Latency}s parameter={ParameterType} agent_type={AgentType} event_id={EventId} method={MethodName}",
            latency, parameterType, agentType, eventId, fullMethodName);
    }

    /// <summary>
    /// Records latency metrics for StateWrapperBase with automatic method name and parameter type detection
    /// </summary>
    /// <param name="latency">Latency in seconds</param>
    /// <param name="item">The state item (parameter type is derived automatically)</param>
    /// <param name="logger">Optional logger for additional logging</param>
    /// <param name="methodName">Automatically captured method name</param>
    /// <param name="filePath">Automatically captured file path for class name</param>
    public static void Record(double latency, StateWrapperBase item, ILogger? logger = null, 
        [CallerMemberName] string methodName = "", [CallerFilePath] string? filePath = null)
    {
        var parameterType = item.GetType().Name;
        var agentType = GetAgentTypeFromGrainId(item, logger) ?? "unknown";
        
        var className = GetClassNameFromFilePath(filePath);
        var fullMethodName = className != null ? $"{className}.{methodName}" : methodName;
        
        PublishLatencyHistogram.Record(latency,
            new KeyValuePair<string, object?>("agent_type", agentType),
            new KeyValuePair<string, object?>("method_name", fullMethodName),
            new KeyValuePair<string, object?>("parameter_type", parameterType),
            new KeyValuePair<string, object?>("event_category", OpenTelemetryConstants.StateProjectionEventType));
            
        logger?.LogInformation("[PublishLatency] latency={Latency}s parameter={ParameterType} agent_type={AgentType} method={MethodName}",
            latency, parameterType, agentType, fullMethodName);
    }

    /// <summary>
    /// Extracts the agent type (class name) from the grain ID string representation
    /// </summary>
    private static string? GetAgentTypeFromGrainId(object item, ILogger? logger)
    {
        try
        {
            var grainIdProperty = item.GetType().GetProperty("GrainId");
            var grainIdValue = grainIdProperty?.GetValue(item);
            if (grainIdValue == null)
            {
                logger?.LogWarning("[GetAgentTypeFromGrainId] GrainId property is null for item type {ItemType}", item.GetType().Name);
                return null;
            }

            // Get the string representation of the GrainId
            var grainIdString = grainIdValue.ToString();
            if (string.IsNullOrEmpty(grainIdString))
            {
                logger?.LogWarning("[GetAgentTypeFromGrainId] GrainId string representation is empty for item type {ItemType}", item.GetType().Name);
                return null;
            }

            // GrainId typically contains type information at the beginning
            // Format is often like: "GrainType/InstanceId" or similar
            // Extract the type part and clean it up
            
            // Try to extract type from the GrainId string representation
            // This is a heuristic approach - adjust based on actual GrainId format
            var parts = grainIdString.Split(['/', '+', '@'], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0)
            {
                var typePart = parts[0];
                
                // Remove namespace if present
                var lastDot = typePart.LastIndexOf('.');
                if (lastDot >= 0)
                    typePart = typePart.Substring(lastDot + 1);
                
                // Remove generic type parameters
                var genericIndex = typePart.IndexOfAny(['`', '<']);
                if (genericIndex >= 0)
                    typePart = typePart.Substring(0, genericIndex);
                    
                return typePart;
            }
            
            logger?.LogWarning("[GetAgentTypeFromGrainId] Failed to parse agent type from GrainId string '{GrainIdString}' for item type {ItemType}", 
                grainIdString, item.GetType().Name);
            return null;
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "[GetAgentTypeFromGrainId] Exception while extracting agent type for item type {ItemType}", item.GetType().Name);
            return null;
        }
    }

    private static string? GetClassNameFromFilePath(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return null;

        var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
        return fileName;
    }
} 