// ReSharper disable once CheckNamespace
namespace Aevatar.Core.Abstractions;

[GenerateSerializer]
public abstract class EventBase
{
    public Guid? CorrelationId { get; set; }
    public StreamId? StreamId { get; set; }
}