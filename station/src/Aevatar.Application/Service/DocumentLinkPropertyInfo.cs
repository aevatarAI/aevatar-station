using System;

namespace Aevatar.Service;

public class DocumentLinkPropertyInfo
{
    public string DeclaringTypeFullName { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string? DocumentationUrl { get; set; }
} 