namespace Aevatar.AtomicAgent.Dtos;

public class UpdateAtomicAgentDto
{
    public string? Name { get; set; }
    public Dictionary<string, string> Properties { get; set; }
}