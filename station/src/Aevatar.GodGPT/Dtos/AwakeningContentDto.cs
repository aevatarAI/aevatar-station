using Orleans;

namespace Aevatar.GodGPT.Dtos;

/// <summary>
/// Awakening content data transfer object
/// </summary>
public class AwakeningContentDto
{
    /// <summary>
    /// Awakening level
    /// </summary>
    [Id(0)]
    public int AwakeningLevel { get; set; }

    /// <summary>
    /// Awakening message content
    /// </summary>
    [Id(1)]
    public string AwakeningMessage { get; set; } = string.Empty;

    /// <summary>
    /// Current status of awakening generation
    /// </summary>
    [Id(2)]
    public int Status { get; set; } = (int)AwakeningStatus.NotStarted;
} 