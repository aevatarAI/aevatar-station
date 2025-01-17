using System.Collections.Generic;
using Orleans;

namespace Aevatar.AI.Dtos;

[GenerateSerializer]
public class InitializeDto
{
    [Id(0)]
    public List<FileDto> Files { get; set; }
    [Id(1)]
    public string Instructions { get; set; }
    [Id(2)]
    public string LLM { get; set; }
}