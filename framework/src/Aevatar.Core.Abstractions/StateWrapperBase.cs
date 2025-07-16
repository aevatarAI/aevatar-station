namespace Aevatar.Core.Abstractions;

[GenerateSerializer]
public abstract class StateWrapperBase
{
    [Id(0)]
    public DateTime PublishedTimestampUtc { get; set; }
}