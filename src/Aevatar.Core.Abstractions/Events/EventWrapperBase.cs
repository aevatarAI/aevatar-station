// ReSharper disable once CheckNamespace
namespace Aevatar.Core.Abstractions;

using System.Collections.Generic;

[GenerateSerializer]
public abstract class EventWrapperBase
{
    // Constants for context metadata keys
    public const string TraceIdKey = "TraceId";
    public const string SpanIdKey = "SpanId";
    public const string TraceFlagsKey = "TraceFlags";
    public const string BaggagePrefixKey = "Baggage.";
    
    [Id(0)]
    public Dictionary<string, string> ContextMetadata { get; set; } = new Dictionary<string, string>();
}