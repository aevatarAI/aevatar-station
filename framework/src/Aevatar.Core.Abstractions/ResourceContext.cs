namespace Aevatar.Core.Abstractions;

/// <summary>
/// Represents a context containing available resources for a GAgent.
/// This enables agents to discover and utilize external resources without explicit coupling.
/// </summary>
[GenerateSerializer]
public class ResourceContext
{
    /// <summary>
    /// Unique identifier for this context instance
    /// </summary>
    [Id(0)]
    public Guid ContextId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// List of available resource GrainIds that the agent can potentially utilize
    /// </summary>
    [Id(1)]
    public List<GrainId> AvailableResources { get; set; } = [];

    /// <summary>
    /// Additional metadata about the context, such as workflow information, capabilities, etc.
    /// </summary>
    [Id(2)]
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Optional source identifier to track where this context originated from (e.g., "workflow:guid")
    /// </summary>
    [Id(3)]
    public string? SourceIdentifier { get; set; }

    /// <summary>
    /// Creates a new ResourceContext with the specified resources
    /// </summary>
    public static ResourceContext Create(IEnumerable<GrainId> resources, string? source = null)
    {
        return new ResourceContext
        {
            AvailableResources = [..resources],
            SourceIdentifier = source
        };
    }

    /// <summary>
    /// Adds metadata to the context
    /// </summary>
    public ResourceContext WithMetadata(string key, object value)
    {
        Metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Adds multiple metadata entries
    /// </summary>
    public ResourceContext WithMetadata(Dictionary<string, object> metadata)
    {
        foreach (var kvp in metadata)
        {
            Metadata[kvp.Key] = kvp.Value;
        }

        return this;
    }
}