using System;

namespace Aevatar.GAgents.Basic.Common;

/// <summary>
/// Attribute to attach documentation links to properties or classes
/// Used for frontend integration to provide help links
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = true)]
public class DocumentationLinkAttribute : Attribute
{
    /// <summary>
    /// The URL to the documentation
    /// </summary>
    public string DocumentationUrl { get; }

    /// <summary>
    /// Optional description for the documentation link
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Creates a new DocumentationLinkAttribute
    /// </summary>
    /// <param name="documentationUrl">The URL to the documentation</param>
    public DocumentationLinkAttribute(string documentationUrl)
    {
        DocumentationUrl = documentationUrl ?? throw new ArgumentNullException(nameof(documentationUrl));
    }
}
