using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using Aevatar.Core.Abstractions.Plugin;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;

namespace Aevatar.Core.Plugin;

/// <summary>
/// Advanced Orleans attribute enhancements and optimizations
/// </summary>
public static class OrleansAttributeEnhancements
{
    /// <summary>
    /// Enhanced Orleans attribute compatibility checker with detailed validation
    /// </summary>
    public static class AttributeCompatibilityValidator
    {
        /// <summary>
        /// Perform comprehensive attribute compatibility check with detailed reporting
        /// </summary>
        public static AttributeCompatibilityResult ValidateCompatibility(
            AgentMethodAttribute pluginAttr, 
            MethodInfo interfaceMethod, 
            ILogger logger)
        {
            var result = new AttributeCompatibilityResult
            {
                MethodName = interfaceMethod.Name,
                IsCompatible = true,
                Issues = new List<AttributeIssue>()
            };

            // Check ReadOnly attribute
            var interfaceReadOnly = interfaceMethod.GetCustomAttribute<ReadOnlyAttribute>() != null;
            if (pluginAttr.IsReadOnly != interfaceReadOnly)
            {
                result.Issues.Add(new AttributeIssue
                {
                    AttributeType = "ReadOnly",
                    PluginValue = pluginAttr.IsReadOnly,
                    InterfaceValue = interfaceReadOnly,
                    Severity = AttributeIssueSeverity.Warning,
                    Recommendation = interfaceReadOnly 
                        ? "Add IsReadOnly = true to plugin method for Orleans optimization"
                        : "Remove IsReadOnly = true from plugin method to match interface"
                });
                result.IsCompatible = false;
            }

            // Check AlwaysInterleave attribute
            var interfaceInterleave = interfaceMethod.GetCustomAttribute<AlwaysInterleaveAttribute>() != null;
            if (pluginAttr.AlwaysInterleave != interfaceInterleave)
            {
                result.Issues.Add(new AttributeIssue
                {
                    AttributeType = "AlwaysInterleave",
                    PluginValue = pluginAttr.AlwaysInterleave,
                    InterfaceValue = interfaceInterleave,
                    Severity = AttributeIssueSeverity.Warning,
                    Recommendation = interfaceInterleave
                        ? "Add AlwaysInterleave = true to plugin method for concurrent execution"
                        : "Remove AlwaysInterleave = true from plugin method to match interface"
                });
                result.IsCompatible = false;
            }

            // Check OneWay attribute
            var interfaceOneWay = interfaceMethod.GetCustomAttribute<OneWayAttribute>() != null;
            if (pluginAttr.OneWay != interfaceOneWay)
            {
                result.Issues.Add(new AttributeIssue
                {
                    AttributeType = "OneWay",
                    PluginValue = pluginAttr.OneWay,
                    InterfaceValue = interfaceOneWay,
                    Severity = interfaceOneWay ? AttributeIssueSeverity.Error : AttributeIssueSeverity.Warning,
                    Recommendation = interfaceOneWay
                        ? "Add OneWay = true to plugin method - interface expects fire-and-forget semantics"
                        : "Remove OneWay = true from plugin method to match interface"
                });
                
                if (interfaceOneWay && !pluginAttr.OneWay)
                {
                    result.IsCompatible = false; // This is a critical mismatch
                }
            }

            // Log compatibility results
            if (!result.IsCompatible)
            {
                logger.LogWarning("Orleans attribute compatibility issues found for method {MethodName}: {IssueCount} issues",
                    result.MethodName, result.Issues.Count);
                
                foreach (var issue in result.Issues)
                {
                    logger.Log(issue.Severity == AttributeIssueSeverity.Error ? LogLevel.Error : LogLevel.Warning,
                        "Attribute mismatch: {AttributeType} - Plugin: {PluginValue}, Interface: {InterfaceValue}. {Recommendation}",
                        issue.AttributeType, issue.PluginValue, issue.InterfaceValue, issue.Recommendation);
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Performance metrics for Orleans attribute optimization
    /// </summary>
    public static class AttributePerformanceAnalyzer
    {
        private static readonly ConcurrentDictionary<string, AttributePerformanceMetrics> _metrics = new();

        /// <summary>
        /// Analyze Orleans attribute performance impact
        /// </summary>
        public static AttributePerformanceMetrics AnalyzePerformanceImpact(MethodRoutingInfo routingInfo)
        {
            var metrics = _metrics.GetOrAdd(routingInfo.MethodName, _ => new AttributePerformanceMetrics
            {
                MethodName = routingInfo.MethodName,
                EstimatedOptimizationLevel = CalculateOptimizationLevel(routingInfo),
                ConcurrencyPotential = CalculateConcurrencyPotential(routingInfo),
                PerformanceRecommendations = GeneratePerformanceRecommendations(routingInfo)
            });

            return metrics;
        }

        private static OptimizationLevel CalculateOptimizationLevel(MethodRoutingInfo routingInfo)
        {
            var score = 0;
            if (routingInfo.IsReadOnly) score += 3; // High impact
            if (routingInfo.AlwaysInterleave) score += 2; // Medium impact  
            if (routingInfo.OneWay) score += 1; // Low impact

            return score switch
            {
                >= 5 => OptimizationLevel.High,
                >= 3 => OptimizationLevel.Medium,
                >= 1 => OptimizationLevel.Low,
                _ => OptimizationLevel.None
            };
        }

        private static ConcurrencyLevel CalculateConcurrencyPotential(MethodRoutingInfo routingInfo)
        {
            if (routingInfo.IsReadOnly && routingInfo.AlwaysInterleave) return ConcurrencyLevel.Maximum;
            if (routingInfo.IsReadOnly || routingInfo.AlwaysInterleave) return ConcurrencyLevel.High;
            if (routingInfo.OneWay) return ConcurrencyLevel.Medium;
            return ConcurrencyLevel.Sequential;
        }

        private static List<string> GeneratePerformanceRecommendations(MethodRoutingInfo routingInfo)
        {
            var recommendations = new List<string>();

            if (!routingInfo.IsReadOnly && IsReadOnlyCandidate(routingInfo))
            {
                recommendations.Add("Consider adding [ReadOnly] attribute if method doesn't modify grain state");
            }

            if (!routingInfo.AlwaysInterleave && IsInterleaveCandidate(routingInfo))
            {
                recommendations.Add("Consider adding [AlwaysInterleave] for I/O-bound or long-running methods");
            }

            if (!routingInfo.OneWay && IsOneWayCandidate(routingInfo))
            {
                recommendations.Add("Consider [OneWay] attribute for fire-and-forget operations");
            }

            return recommendations;
        }

        private static bool IsReadOnlyCandidate(MethodRoutingInfo routingInfo)
        {
            return routingInfo.MethodName.Contains("Get", StringComparison.OrdinalIgnoreCase) ||
                   routingInfo.MethodName.Contains("Read", StringComparison.OrdinalIgnoreCase) ||
                   routingInfo.MethodName.Contains("Query", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsInterleaveCandidate(MethodRoutingInfo routingInfo)
        {
            return routingInfo.MethodName.Contains("Async", StringComparison.OrdinalIgnoreCase) ||
                   routingInfo.MethodName.Contains("Download", StringComparison.OrdinalIgnoreCase) ||
                   routingInfo.MethodName.Contains("Process", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsOneWayCandidate(MethodRoutingInfo routingInfo)
        {
            return routingInfo.ReturnType == typeof(Task) && (
                   routingInfo.MethodName.Contains("Log", StringComparison.OrdinalIgnoreCase) ||
                   routingInfo.MethodName.Contains("Send", StringComparison.OrdinalIgnoreCase) ||
                   routingInfo.MethodName.Contains("Notify", StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Automatic Orleans attribute suggestion engine
    /// </summary>
    public static class AttributeSuggestionEngine
    {
        /// <summary>
        /// Suggest Orleans attributes based on method signature and name patterns
        /// </summary>
        public static AttributeSuggestion SuggestAttributes(MethodInfo method)
        {
            var suggestion = new AttributeSuggestion
            {
                MethodName = method.Name,
                SuggestedAttributes = new List<SuggestedAttribute>()
            };

            // Analyze method name patterns
            if (IsReadOnlyPattern(method.Name))
            {
                suggestion.SuggestedAttributes.Add(new SuggestedAttribute
                {
                    AttributeType = typeof(ReadOnlyAttribute),
                    Confidence = 0.8,
                    Reason = "Method name suggests read-only operation"
                });
            }

            // Analyze return type
            if (method.ReturnType == typeof(Task) && IsFireAndForgetPattern(method.Name))
            {
                suggestion.SuggestedAttributes.Add(new SuggestedAttribute
                {
                    AttributeType = typeof(OneWayAttribute),
                    Confidence = 0.7,
                    Reason = "Method appears to be fire-and-forget operation"
                });
            }

            // Analyze for concurrency potential
            if (IsAsyncMethod(method) && !IsStateModifyingPattern(method.Name))
            {
                suggestion.SuggestedAttributes.Add(new SuggestedAttribute
                {
                    AttributeType = typeof(AlwaysInterleaveAttribute),
                    Confidence = 0.6,
                    Reason = "Async method that may benefit from concurrent execution"
                });
            }

            return suggestion;
        }

        private static bool IsReadOnlyPattern(string methodName)
        {
            var readOnlyPatterns = new[] { "Get", "Read", "Query", "Find", "Search", "List", "Count", "Exists" };
            return readOnlyPatterns.Any(pattern => methodName.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsFireAndForgetPattern(string methodName)
        {
            var fireAndForgetPatterns = new[] { "Log", "Send", "Notify", "Publish", "Record", "Track" };
            return fireAndForgetPatterns.Any(pattern => methodName.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsAsyncMethod(MethodInfo method)
        {
            return method.ReturnType == typeof(Task) || 
                   (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>));
        }

        private static bool IsStateModifyingPattern(string methodName)
        {
            var modifyingPatterns = new[] { "Set", "Update", "Create", "Delete", "Modify", "Change", "Add", "Remove" };
            return modifyingPatterns.Any(pattern => methodName.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }
    }
}

/// <summary>
/// Result of attribute compatibility validation
/// </summary>
public class AttributeCompatibilityResult
{
    public string MethodName { get; set; } = string.Empty;
    public bool IsCompatible { get; set; }
    public List<AttributeIssue> Issues { get; set; } = new();
}

/// <summary>
/// Specific attribute compatibility issue
/// </summary>
public class AttributeIssue
{
    public string AttributeType { get; set; } = string.Empty;
    public bool PluginValue { get; set; }
    public bool InterfaceValue { get; set; }
    public AttributeIssueSeverity Severity { get; set; }
    public string Recommendation { get; set; } = string.Empty;
}

/// <summary>
/// Severity levels for attribute issues
/// </summary>
public enum AttributeIssueSeverity
{
    Warning,
    Error
}

/// <summary>
/// Performance metrics for Orleans attributes
/// </summary>
public class AttributePerformanceMetrics
{
    public string MethodName { get; set; } = string.Empty;
    public OptimizationLevel EstimatedOptimizationLevel { get; set; }
    public ConcurrencyLevel ConcurrencyPotential { get; set; }
    public List<string> PerformanceRecommendations { get; set; } = new();
}

/// <summary>
/// Orleans optimization levels
/// </summary>
public enum OptimizationLevel
{
    None,
    Low,
    Medium,
    High
}

/// <summary>
/// Concurrency capability levels
/// </summary>
public enum ConcurrencyLevel
{
    Sequential,
    Medium,
    High,
    Maximum
}

/// <summary>
/// Attribute suggestion for a method
/// </summary>
public class AttributeSuggestion
{
    public string MethodName { get; set; } = string.Empty;
    public List<SuggestedAttribute> SuggestedAttributes { get; set; } = new();
}

/// <summary>
/// A suggested Orleans attribute
/// </summary>
public class SuggestedAttribute
{
    public Type AttributeType { get; set; } = null!;
    public double Confidence { get; set; }
    public string Reason { get; set; } = string.Empty;
} 