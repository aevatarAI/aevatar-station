using Aevatar.Core.Interception.Models;

namespace Aevatar.Core.Interception.Configurations;

/// <summary>
/// Configuration for method interception and tracing services.
/// </summary>
public class InterceptionConfiguration
{
    /// <summary>
    /// The interception mode to use.
    /// </summary>
    public InterceptionMode Mode { get; set; } = InterceptionMode.None;
    
    /// <summary>
    /// Whether to enable performance metrics collection.
    /// </summary>
    public bool EnablePerformanceMetrics { get; set; } = true;
    
    /// <summary>
    /// Whether to enable method-level logging.
    /// </summary>
    public bool EnableMethodLogging { get; set; } = true;
    
    /// <summary>
    /// General tracing configuration.
    /// </summary>
    public TraceConfig TraceConfig { get; set; } = new();
    
    /// <summary>
    /// Interception behavior configuration.
    /// TODO: This configuration is not currently used in the tracing logic.
    /// </summary>
    public MethodCaptureConfig MethodCapture { get; set; } = new();
    
    /// <summary>
    /// Creates a configuration for Fody-based interception.
    /// </summary>
    public static InterceptionConfiguration CreateFody()
    {
        return new InterceptionConfiguration
        {
            Mode = InterceptionMode.Fody,
            EnablePerformanceMetrics = true,
            EnableMethodLogging = true,
            MethodCapture = new MethodCaptureConfig
            {
                EnableParameterCapture = true,
                EnableExceptionCapture = true
            }
        };
    }
    
    /// <summary>
    /// Creates a configuration for no interception.
    /// </summary>
    public static InterceptionConfiguration CreateNoInterception()
    {
        return new InterceptionConfiguration
        {
            Mode = InterceptionMode.None,
            EnablePerformanceMetrics = false,
            EnableMethodLogging = false
        };
    }
}
