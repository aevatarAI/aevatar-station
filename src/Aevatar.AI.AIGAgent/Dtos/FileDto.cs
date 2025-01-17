using Orleans;

namespace Aevatar.AI.Dtos;

[GenerateSerializer]
public class FileDto
{ 
    [Id(0)]
    public byte[] Content { get; set; }
    [Id(1)]
    public string Type { get; set; }
    [Id(2)]
    public string Name { get; set; }
}