namespace Aevatar.Core.Abstractions;

[GenerateSerializer]
public class SetInitializeDtoTypeGEvent : GEventBase
{
    [Id(0)] public Type InitializeDtoType { get; set; }
}