namespace Aevatar.Core.Interception.Models;

/// <summary>
/// Defines the available interception modes for the application.
/// </summary>
public enum InterceptionMode
{
    /// <summary>
    /// No interception is performed. Maximum performance, no observability.
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Build-time interception using Fody IL weaving.
    /// Best for production with stable interception configuration.
    /// </summary>
    Fody = 1
}
