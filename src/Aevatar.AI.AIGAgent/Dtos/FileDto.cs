using Orleans;

namespace Aevatar.AI.Dtos;

[GenerateSerializer]
public struct FileDto
{
    [Id(0)] public byte[] Content;
    [Id(1)] public string Type;
}