namespace Aevatar.Core.Abstractions;

[GenerateSerializer]
public class InitializeDtoTypeGEvent : GEventBase
{
    [Id(0)] public Type InitializeDtoType { get; set; }
}