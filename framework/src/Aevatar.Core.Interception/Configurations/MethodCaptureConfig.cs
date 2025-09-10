namespace Aevatar.Core.Interception.Configurations;

/// <summary>
/// Configuration for method interception behavior.
/// TODO: This configuration is not currently used in the tracing logic.
/// Future implementation will support parameter and exception capture.
/// </summary>
public class MethodCaptureConfig
{
    /// <summary>
    /// Whether to capture method parameters.
    /// TODO: Not implemented in current tracing logic.
    /// </summary>
    public bool EnableParameterCapture { get; set; } = false;
    
    /// <summary>
    /// Whether to capture exceptions.
    /// TODO: Not implemented in current tracing logic.
    /// </summary>
    public bool EnableExceptionCapture { get; set; } = true;
    
    /// <summary>
    /// Maximum size for captured parameter values.
    /// TODO: Not implemented in current tracing logic.
    /// </summary>
    public int MaxCaptureSize { get; set; } = 1000;
}
