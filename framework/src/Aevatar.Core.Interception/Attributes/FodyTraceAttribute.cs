using System;

namespace Aevatar.Core.Interception.Attributes;

/// <summary>
/// Attribute to mark methods or classes for Fody IL weaving tracing.
/// This code will be injected at build time and cannot be toggled at runtime.
/// When applied to a class, all methods in that class will be traced.
/// Method-level attributes take precedence over class-level attributes.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class FodyTraceAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the operation name for tracing.
    /// If not specified, the method name will be used.
    /// </summary>
    public string? OperationName { get; set; }
    
    /// <summary>
    /// Gets or sets whether method parameters should be captured as tags.
    /// </summary>
    public bool CaptureParameters { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether return values should be captured as tags.
    /// </summary>
    public bool CaptureReturnValue { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the maximum size for captured parameter and return values.
    /// </summary>
    public int MaxCaptureSize { get; set; } = 1000;
    
    /// <summary>
    /// Gets or sets additional tags to include in the trace.
    /// </summary>
    public string[]? Tags { get; set; }
}
