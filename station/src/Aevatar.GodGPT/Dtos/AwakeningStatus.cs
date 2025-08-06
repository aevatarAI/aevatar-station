namespace Aevatar.GodGPT.Dtos;

/// <summary>
/// Awakening generation status
/// </summary>
public enum AwakeningStatus
{
    /// <summary>
    /// Not started
    /// </summary>
    NotStarted = 0,
    
    /// <summary>
    /// Generating in progress
    /// </summary>
    Generating = 1,
    
    /// <summary>
    /// Generation completed (success or failure)
    /// </summary>
    Completed = 2
}